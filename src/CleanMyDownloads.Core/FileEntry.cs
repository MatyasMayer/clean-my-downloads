namespace CleanMyDownloads.Core;

/// <summary>A top-level file as the sorter sees it.</summary>
public sealed record FileEntry(string Name, bool IsHidden, bool IsSystem);
