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
 * PromptEditorScreenResult — typed result for the reusable prompt editor screen
 */

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Captures the outcome of one prompt editor screen session.
/// </summary>
public sealed record class PromptEditorScreenResult
{
    #region Properties

    /// <summary>
    /// Gets the prompt text that was submitted.
    /// </summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the editor was canceled.
    /// </summary>
    public bool IsCanceled { get; init; }

    #endregion
}