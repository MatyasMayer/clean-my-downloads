namespace CleanMyDownloads.Core;

/// <summary>Seam over the file system: read-only listing of a folder's top-level files.</summary>
public interface IFolderReader
{
    IReadOnlyList<FileEntry> ListTopLevelFiles(string path);

    /// <summary>Names of the folders directly inside the given path; empty when it does not exist.</summary>
    IReadOnlyList<string> ListSubfolderNames(string path) => [];
}
