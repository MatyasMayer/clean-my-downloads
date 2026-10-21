namespace CleanMyDownloads.Core;

/// <summary>The only deletion the sorter makes: folders it created, left empty by moving files out.</summary>
static class EmptyFolders
{
    /// <summary>
    /// Removes <paramref name="dir"/> if empty, then each empty parent below <paramref name="root"/>
    /// (<c>Topics\</c> and <c>School\</c> exist only because a folder went into them). Never the root itself.
    /// </summary>
    public static void RemoveUpTo(string dir, string root)
    {
        if (string.Equals(Path.TrimEndingDirectorySeparator(dir), Path.TrimEndingDirectorySeparator(root),
                StringComparison.OrdinalIgnoreCase)) return;
        if (!Directory.Exists(dir) || Directory.EnumerateFileSystemEntries(dir).Any()) return;
        Directory.Delete(dir);
        RemoveUpTo(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(dir))!, root);
    }
}
