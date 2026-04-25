/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi! is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Affero General Public License version 3 as published by the Free
 * Software Foundation.
 *
 * YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with YAi!. If not, see <https://www.gnu.org/licenses/>.
 *
 * YAi.Persona — Cerbero
 * RegexCommandSafetyAnalyzer — regex-based command safety analyzer for PowerShell and Bash
 */

#region Using directives

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using YAi.Persona.Services.Operations.Safety.Cerbero.Models;

#endregion

namespace YAi.Persona.Services.Operations.Safety.Cerbero;

/// <summary>
/// Analyzes shell commands against a table of regex-based danger patterns.
/// Covers PowerShell and Bash dialects.
/// </summary>
public sealed class RegexCommandSafetyAnalyzer : ICommandSafetyAnalyzer
{
    #region Fields

    private static readonly IReadOnlyList<(CommandShellKind Shell, Regex Pattern, string Reason)> Rules =
    [
        // PowerShell — piped web request to Invoke-Expression
        (
            CommandShellKind.PowerShell,
            new Regex (@"(?i)(iwr|irm|curl|wget)\b.*\|\s*(iex|Invoke-Expression)\b", RegexOptions.Compiled),
            "Piped web request to Invoke-Expression"
        ),
        // PowerShell — Invoke-WebRequest piped to Invoke-Expression (long form)
        (
            CommandShellKind.PowerShell,
            new Regex (@"(?i)Invoke-WebRequest\b.*Invoke-Expression\b", RegexOptions.Compiled),
            "Invoke-WebRequest piped to Invoke-Expression"
        ),
        // PowerShell — recursive force delete of drive root
        (
            CommandShellKind.PowerShell,
            new Regex (@"(?i)Remove-Item\s+-Recurse\s+-Force\s+[cC]:\\", RegexOptions.Compiled),
            "Recursive force delete of drive root"
        ),
        // PowerShell — RunAs elevation via Start-Process
        (
            CommandShellKind.PowerShell,
            new Regex (@"(?i)Start-Process\b.+-Verb\s+RunAs\b", RegexOptions.Compiled),
            "Start-Process with RunAs elevation"
        ),

        // Bash — piped web download to shell interpreter
        (
            CommandShellKind.Bash,
            new Regex (@"(?i)(curl|wget)\s+\S+\s*\|\s*(bash|sh|zsh)\b", RegexOptions.Compiled),
            "Piped web download to shell interpreter"
        ),
        // Bash — recursive force remove of filesystem root
        (
            CommandShellKind.Bash,
            new Regex (@"(?i)rm\s+(-\w*r\w*f|-\w*f\w*r)\s+/(\s|$)", RegexOptions.Compiled),
            "Recursive force remove of filesystem root"
        ),
        // Bash — recursive force remove of home directory
        (
            CommandShellKind.Bash,
            new Regex (@"(?i)rm\s+(-\w*r\w*f|-\w*f\w*r)\s+~", RegexOptions.Compiled),
            "Recursive force remove of home directory"
        ),
        // Bash — zero-fill to block device
        (
            CommandShellKind.Bash,
            new Regex (@"(?i)dd\s+if=/dev/zero\s+of=/dev/sd", RegexOptions.Compiled),
            "Zero-fill write to block device"
        ),
        // Bash — filesystem format command
        (
            CommandShellKind.Bash,
            new Regex (@"(?i)mkfs\.\w+", RegexOptions.Compiled),
            "Filesystem format command"
        ),
    ];

    #endregion

    /// <summary>
    /// Analyzes the command against all rules applicable to its shell dialect.
    /// </summary>
    /// <param name="context">The command and its shell dialect.</param>
    /// <returns>A <see cref="CommandSafetyResult"/> with the verdict and any matched findings.</returns>
    public CommandSafetyResult Analyze (CommandSafetyContext context)
    {
        ArgumentNullException.ThrowIfNull (context);

        List<CommandSafetyFinding> findings = Rules
            .Where (r => r.Shell == context.ShellKind && r.Pattern.IsMatch (context.Command))
            .Select (r => new CommandSafetyFinding
            {
                Pattern = r.Pattern.ToString (),
                Reason = r.Reason,
                RiskLevel = CommandRiskLevel.Blocked
            })
            .ToList ();

        return new CommandSafetyResult
        {
            IsBlocked = findings.Count > 0,
            Findings = findings
        };
    }
}
