namespace CleanMyDownloads.Core;

/// <summary>Seam over the file system: read-only listing of a folder's top-level files.</summary>
public interface IFolderReader
{
    IReadOnlyList<FileEntry> ListTopLevelFiles(string path);
}
