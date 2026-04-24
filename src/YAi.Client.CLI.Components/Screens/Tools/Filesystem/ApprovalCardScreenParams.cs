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
 * ApprovalCardScreenParams — carries primitive parameters for ApprovalCardScreen
 */

namespace YAi.Client.CLI.Components.Screens.Tools.Filesystem;

/// <summary>
/// Carries primitive parameters for <see cref="ApprovalCardScreen"/> that cannot
/// be registered directly in DI (e.g., <c>int</c>).
/// </summary>
public sealed class ApprovalCardScreenParams
{
    /// <summary>Number of steps remaining after the current one.</summary>
    public int RemainingCount { get; }

    /// <summary>Initializes the params with the remaining step count.</summary>
    /// <param name="remainingCount">Steps remaining after current.</param>
    public ApprovalCardScreenParams (int remainingCount)
    {
        RemainingCount = remainingCount;
    }
}
