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
 * Resource integrity verification status codes.
 */

namespace YAi.Persona.Services.Security.ResourceIntegrity;

/// <summary>
/// Describes the outcome of a resource integrity verification pass.
/// </summary>
public enum ResourceIntegrityStatus
{
    /// <summary>All files verified successfully against the signed manifest.</summary>
    Verified,

    /// <summary>One or more files failed verification (hash mismatch, missing file, bad signature, etc.).</summary>
    Failed,

    /// <summary>Verification was intentionally skipped (e.g., <c>YaiSkipResourceSigning=true</c>).</summary>
    Skipped,

    /// <summary>No manifest or signature files were found; the resource root is unsigned.</summary>
    NotSigned,

    /// <summary>The public key or signature could not be loaded or is untrusted.</summary>
    Untrusted
}
