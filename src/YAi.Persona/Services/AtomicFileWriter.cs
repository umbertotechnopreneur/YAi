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
 * YAi!
 * Atomic file write helper
 */

namespace YAi.Persona.Services;

internal static class AtomicFileWriter
{
    public static void WriteAtomic(string destPath, byte[] data)
    {
        var dir = Path.GetDirectoryName(destPath) ?? throw new InvalidOperationException("Destination directory not found");
        Directory.CreateDirectory(dir);
        var tempPath = Path.Combine(dir, Path.GetRandomFileName());
        File.WriteAllBytes(tempPath, data);
        // Try atomic replace where available
        try
        {
            if (File.Exists(destPath))
            {
                // Attempt File.Replace on Windows/NTFS
                File.Replace(tempPath, destPath, null);
            }
            else
            {
                File.Move(tempPath, destPath);
            }
        }
        catch
        {
            // cleanup temp file on failure
            try { File.Delete(tempPath); } catch { }
            throw;
        }
    }
}

