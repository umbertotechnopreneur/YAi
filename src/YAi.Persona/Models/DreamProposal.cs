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
 * YAi! Persona
 * Dream proposal model produced by DreamingService
 */

namespace YAi.Persona.Models;

/// <summary>
/// Represents a proposed memory promotion generated during the dreaming reflection phase.
/// Proposals are written to <c>data/dreams/DREAMS.md</c> and reviewed by the user before
/// being promoted to permanent memory via <c>PromotionService</c>.
/// </summary>
/// <param name="Type">The memory type (<c>memory</c>, <c>lesson</c>, or <c>correction</c>).</param>
/// <param name="Content">The proposed memory content (max 200 chars).</param>
/// <param name="Rationale">One-sentence explanation of why this is worth remembering.</param>
/// <param name="Confidence">Extraction confidence score (0.0 – 1.0).</param>
public sealed record DreamProposal (string Type, string Content, string Rationale, double Confidence);
