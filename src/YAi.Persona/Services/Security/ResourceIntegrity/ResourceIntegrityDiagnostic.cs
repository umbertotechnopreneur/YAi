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
 * A structured diagnostic produced during resource integrity verification.
 */

namespace YAi.Persona.Services.Security.ResourceIntegrity;

/// <summary>
/// A structured diagnostic entry produced during resource integrity verification.
/// </summary>
public sealed class ResourceIntegrityDiagnostic
{
    // -----------------------------------------------------------------------------------------
    // Well-known diagnostic codes
    // -----------------------------------------------------------------------------------------

    /// <summary>manifest.yai.json was not found.</summary>
    public const string ManifestMissing = "manifest_missing";

    /// <summary>manifest.yai.sig was not found.</summary>
    public const string SignatureMissing = "signature_missing";

    /// <summary>public-key.yai.pem was not found.</summary>
    public const string PublicKeyMissing = "public_key_missing";

    /// <summary>The manifest signature did not verify against the public key.</summary>
    public const string ManifestSignatureInvalid = "manifest_signature_invalid";

    /// <summary>manifest.yai.json could not be parsed as valid JSON.</summary>
    public const string ManifestJsonInvalid = "manifest_json_invalid";

    /// <summary>The manifest path is invalid or could not be resolved.</summary>
    public const string ManifestPathInvalid = "manifest_path_invalid";

    /// <summary>A file listed in the manifest was not found on disk.</summary>
    public const string FileMissing = "file_missing";

    /// <summary>The computed SHA-256 hash did not match the expected hash in the manifest.</summary>
    public const string FileHashMismatch = "file_hash_mismatch";

    /// <summary>The actual file size in bytes did not match the expected size in the manifest.</summary>
    public const string FileSizeMismatch = "file_size_mismatch";

    /// <summary>A manifest entry path contained <c>..</c> path traversal sequences.</summary>
    public const string PathTraversalRejected = "path_traversal_rejected";

    /// <summary>A manifest entry path was an absolute path, which is not permitted.</summary>
    public const string AbsolutePathRejected = "absolute_path_rejected";

    /// <summary>A manifest entry resolved to a path outside the resource root.</summary>
    public const string OutsideResourceRootRejected = "outside_resource_root_rejected";

    /// <summary>The signature algorithm declared in the manifest is not supported.</summary>
    public const string UnsupportedAlgorithm = "unsupported_algorithm";

    /// <summary>An unexpected exception occurred during verification.</summary>
    public const string VerificationException = "verification_exception";

    // -----------------------------------------------------------------------------------------
    // Properties
    // -----------------------------------------------------------------------------------------

    /// <summary>Gets or sets the machine-readable diagnostic code (one of the constants above).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity: <c>error</c> or <c>warning</c>.</summary>
    public string Severity { get; set; } = "error";

    /// <summary>Gets or sets the human-readable diagnostic message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the relative path of the affected file, if applicable.</summary>
    public string? RelativePath { get; set; }

    /// <summary>Gets or sets additional detail (e.g., expected vs actual hash).</summary>
    public string? Detail { get; set; }

    // -----------------------------------------------------------------------------------------
    // Factory helpers
    // -----------------------------------------------------------------------------------------

    internal static ResourceIntegrityDiagnostic Error(string code, string message, string? relativePath = null, string? detail = null) =>
        new() { Code = code, Severity = "error", Message = message, RelativePath = relativePath, Detail = detail };

    internal static ResourceIntegrityDiagnostic Warning(string code, string message, string? relativePath = null, string? detail = null) =>
        new() { Code = code, Severity = "warning", Message = message, RelativePath = relativePath, Detail = detail };
}
