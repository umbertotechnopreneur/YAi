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
 * RazorScreen host for the combined conversation transcript and prompt screen
 */

#region Using directives

using Microsoft.Extensions.DependencyInjection;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Hosts <see cref="ConversationPromptScreen"/> and returns the submitted prompt result.
/// </summary>
public sealed class ConversationPromptScreenHost : RazorScreen<ConversationPromptScreen, ConversationPromptScreenResult>
{
    #region Fields

    private readonly ConversationPromptScreenParameters _screenParameters;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationPromptScreenHost"/> class.
    /// </summary>
    /// <param name="screenParameters">The parameters injected into the conversation screen.</param>
    public ConversationPromptScreenHost (ConversationPromptScreenParameters screenParameters)
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