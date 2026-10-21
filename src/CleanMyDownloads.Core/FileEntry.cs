namespace CleanMyDownloads.Core;

/// <summary>A top-level file as the sorter sees it. Times are null when unknown (treated as long settled).</summary>
public sealed record FileEntry(string Name, bool IsHidden, bool IsSystem,
    DateTime? Created = null, DateTime? Modified = null);
