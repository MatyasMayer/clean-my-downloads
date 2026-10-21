namespace CleanMyDownloads.Core;

/// <summary>A top-level file as the sorter sees it. Times are null when unknown (treated as long settled).</summary>
public sealed record FileEntry(string Name, bool IsHidden, bool IsSystem,
    DateTime? Created = null, DateTime? Modified = null)
{
    /// <summary>The later of created and modified (SPEC section 4), or null when both are unknown.</summary>
    public DateTime? LastTouched => Created is null ? Modified : Modified is null ? Created : (Created > Modified ? Created : Modified);
}
