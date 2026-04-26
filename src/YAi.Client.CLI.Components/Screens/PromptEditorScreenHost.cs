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
 * YAi.Client.CLI.Components
 * PromptEditorScreenHost — RazorScreen host for the reusable prompt editor
 */

#region Using directives

using Microsoft.Extensions.DependencyInjection;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Hosts <see cref="PromptEditorScreen"/> and returns the submitted prompt result.
/// </summary>
public sealed class PromptEditorScreenHost : RazorScreen<PromptEditorScreen, PromptEditorScreenResult>
{
    #region Fields

    private readonly PromptEditorScreenParameters _screenParameters;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditorScreenHost"/> class.
    /// </summary>
    /// <param name="screenParameters">The parameters injected into the prompt screen.</param>
    public PromptEditorScreenHost (PromptEditorScreenParameters screenParameters)
    {
        _screenParameters = screenParameters ?? throw new ArgumentNullException (nameof (screenParameters));
    }

    #endregion

    #region Protected methods

    /// <inheritdoc />
    protected override void ConfigureServices (IServiceCollection services)
    {
        services.AddSingleton (_screenParameters);
    }

    #endregion
}