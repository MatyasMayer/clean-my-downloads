namespace CleanMyDownloads.Core;

/// <summary>Seam over the actual file move, so failed moves (in use, permission denied, disk error) are testable.</summary>
public interface IFileMover
{
    /// <summary>Moves a file, creating the destination folder if needed. Never overwrites. Throws on failure.</summary>
    void Move(string source, string destination);
}

public sealed class DiskFileMover : IFileMover
{
    public void Move(string source, string destination)
    {
        var dir = Path.GetDirectoryName(destination)!;
        var created = !Directory.Exists(dir);
        Directory.CreateDirectory(dir);
        try
        {
            File.Move(source, destination, overwrite: false);
        }
        catch
        {
            // Remove only a folder we just created for this move; it is empty by construction.
            if (created && !Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
            throw;
        }
    }
}
