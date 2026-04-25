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
 * Result returned by IResourceSignatureVerifier.
 */

namespace YAi.Persona.Services.Security.ResourceIntegrity;

/// <summary>
/// The result of a resource integrity verification pass.
/// </summary>
public sealed class ResourceIntegrityResult
{
    /// <summary>Gets whether verification completed without any errors.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the overall verification status.</summary>
    public ResourceIntegrityStatus Status { get; init; }

    /// <summary>Gets the trust classification for official built-in resources.</summary>
    public ResourceTrustClassification TrustClassification { get; init; }

    /// <summary>Gets all diagnostics produced during verification.</summary>
    public IReadOnlyList<ResourceIntegrityDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>Gets the relative paths of files that verified successfully.</summary>
    public IReadOnlyList<string> VerifiedFiles { get; init; } = [];

    /// <summary>Gets the relative paths of files that failed verification.</summary>
    public IReadOnlyList<string> FailedFiles { get; init; } = [];

    /// <summary>Gets the resolved path of the manifest file.</summary>
    public string? ManifestPath { get; init; }

    /// <summary>Gets the resolved path of the signature file.</summary>
    public string? SignaturePath { get; init; }

    /// <summary>Gets the resolved path of the public key file.</summary>
    public string? PublicKeyPath { get; init; }

    // -----------------------------------------------------------------------------------------
    // Factory helpers
    // -----------------------------------------------------------------------------------------

    internal static ResourceIntegrityResult Fail(
        ResourceIntegrityStatus status,
        ResourceIntegrityDiagnostic diagnostic,
        string? manifestPath = null,
        string? signaturePath = null,
        string? publicKeyPath = null) =>
        new()
        {
            Success = false,
            Status = status,
            TrustClassification = ResourceTrustClassification.OfficialBuiltInVerificationFailed,
            Diagnostics = [diagnostic],
            ManifestPath = manifestPath,
            SignaturePath = signaturePath,
            PublicKeyPath = publicKeyPath
        };
}
