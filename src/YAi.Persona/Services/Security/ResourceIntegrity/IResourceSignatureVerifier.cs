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
 * Interface for verifying the integrity of official bundled resources.
 */

namespace YAi.Persona.Services.Security.ResourceIntegrity;

/// <summary>
/// Verifies the integrity of official YAi bundled resources against a signed manifest.
/// </summary>
public interface IResourceSignatureVerifier
{
    /// <summary>
    /// Verifies all files listed in <c>manifest.yai.json</c> against the manifest signature
    /// using the bundled public key.
    /// </summary>
    /// <param name="resourceRoot">
    /// Absolute path to the signed resource root directory
    /// (the directory containing <c>manifest.yai.json</c>, <c>manifest.yai.sig</c>,
    /// and <c>public-key.yai.pem</c>).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ResourceIntegrityResult"/> describing the outcome and any diagnostics.
    /// </returns>
    Task<ResourceIntegrityResult> VerifyAsync(string resourceRoot, CancellationToken ct = default);
}
