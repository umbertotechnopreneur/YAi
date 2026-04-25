<#
YAi!

Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
Website: https://umbertogiacobbi.biz
Email: hello@umbertogiacobbi.biz

This file is part of YAi!.

YAi! is free software: you can redistribute it and/or modify it under the terms
of the GNU Affero General Public License version 3 as published by the Free
Software Foundation.

YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
PURPOSE. See the GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License along
with YAi!. If not, see <https://www.gnu.org/licenses/>.

YAi!
Compatibility splash screen for the YAi logo asset
#>

. (Join-Path $PSScriptRoot 'splash-helpers.ps1')
Initialize-SplashConsole

function printf {
    param(
        [Parameter (ValueFromRemainingArguments = $true)]
        [string[]]$ArgumentList
    )

    Write-CenteredConsoleBlock @ArgumentList
}

printf @'
\e[38;2;0;180;140m+-----------------------+
\e[38;2;36;198;149m|         YAi!          |
\e[38;2;76;217;100m|  local-first startup  |
\e[38;2;36;198;149m|   trust-first flow    |
\e[38;2;0;180;140m+-----------------------+
\e[38;2;200;240;255mbooting the workspace...
'@

[Console]::WriteLine()
