namespace CleanMyDownloads.Core;

public sealed class DiskFolderReader : IFolderReader
{
    public IReadOnlyList<FileEntry> ListTopLevelFiles(string path) =>
        new DirectoryInfo(path).EnumerateFiles()
            .Select(f => new FileEntry(
                f.Name,
                f.Attributes.HasFlag(FileAttributes.Hidden),
                f.Attributes.HasFlag(FileAttributes.System)))
            .ToList();
}
