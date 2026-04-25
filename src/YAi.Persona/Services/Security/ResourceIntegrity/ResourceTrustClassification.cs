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
 * YAi.Persona
 * Resource trust classification.
 */

namespace YAi.Persona.Services.Security.ResourceIntegrity;

/// <summary>
/// Classifies the trust level of a resource based on its origin and verification outcome.
/// </summary>
public enum ResourceTrustClassification
{
    /// <summary>The resource is an official YAi built-in resource that passed signature verification.</summary>
    OfficialSignedBuiltIn,

    /// <summary>The resource is an official YAi built-in resource but signature or hash verification failed.</summary>
    OfficialBuiltInVerificationFailed,

    /// <summary>The resource was created by the user in their own workspace (not required to be signed in V1).</summary>
    UserWorkspaceFile,

    /// <summary>The resource was imported from a third-party source and is not signed.</summary>
    ImportedUnsignedSkill,

    /// <summary>The resource was imported from a third-party source and carries a third-party signature.</summary>
    ImportedSignedThirdPartySkill
}
