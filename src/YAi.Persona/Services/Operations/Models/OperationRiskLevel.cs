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
 * YAi.Persona — Operation Models
 * Risk classification for planned operations (generic, not filesystem-specific)
 */

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// Risk classification applied to each operation step.
/// The application enforces risk — the model only proposes it.
/// Shared across all skills (filesystem, git, dotnet, npm, etc.).
/// </summary>
public enum OperationRiskLevel
{
    /// <summary>Read-only. No state changes. No approval needed.</summary>
    ReadOnly,

    /// <summary>Creates a new item without overwriting. Approval required.</summary>
    LocalWrite,

    /// <summary>May replace existing content. Approval and mitigation (backup) required.</summary>
    OverwriteRisk,

    /// <summary>Moves to recoverable trash. Approval and mitigation required.</summary>
    DestructiveRecoverable,

    /// <summary>Permanently deletes. Blocked in v1.</summary>
    DestructivePermanent,

    /// <summary>Targets a path outside the workspace root. Blocked in v1.</summary>
    OutsideWorkspace
}
