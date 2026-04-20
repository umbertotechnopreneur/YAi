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

