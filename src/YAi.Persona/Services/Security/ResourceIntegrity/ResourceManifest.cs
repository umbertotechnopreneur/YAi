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
 * Root model for manifest.yai.json.
 */

namespace YAi.Persona.Services.Security.ResourceIntegrity;

/// <summary>
/// Represents the root object of <c>manifest.yai.json</c>.
/// </summary>
public sealed class ResourceManifest
{
    /// <summary>Gets or sets the schema version of this manifest format.</summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>Gets or sets the project name.</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when this manifest was signed.</summary>
    public string SignedAtUtc { get; set; } = string.Empty;

    /// <summary>Gets or sets the identity of the signer.</summary>
    public string Signer { get; set; } = string.Empty;

    /// <summary>Gets or sets the signature algorithm used (e.g., <c>Ed25519</c>).</summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of signed file entries.</summary>
    public IReadOnlyList<ResourceManifestFile> Files { get; set; } = [];
}
