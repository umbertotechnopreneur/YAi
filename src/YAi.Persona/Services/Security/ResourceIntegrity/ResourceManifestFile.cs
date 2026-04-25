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
 * A single file entry from the signed resource manifest.
 */

namespace YAi.Persona.Services.Security.ResourceIntegrity;

/// <summary>
/// Represents a single file entry in <c>manifest.yai.json</c>.
/// </summary>
public sealed class ResourceManifestFile
{
    /// <summary>Gets or sets the path relative to the signed resource root (forward-slash, no leading slash).</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the resource kind (e.g., <c>skill</c>, <c>template</c>, <c>prompt</c>, <c>regex</c>).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the expected SHA-256 hash of the file content (lowercase hex).</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>Gets or sets the expected size of the file in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Gets or sets the optional version string from the file's frontmatter.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets optional tags associated with this file.</summary>
    public IReadOnlyList<string>? Tags { get; set; }
}
