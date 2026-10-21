namespace CleanMyDownloads.Core;

/// <summary>SPEC section 2: files a run never looks at.</summary>
public static class IgnoreRules
{
    static readonly HashSet<string> InProgressExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".crdownload", ".part", ".partial", ".download", ".tmp" };

    public static bool IsIgnored(FileEntry file) =>
        file.IsHidden
        || file.IsSystem
        || string.Equals(file.Name, "desktop.ini", StringComparison.OrdinalIgnoreCase)
        || InProgressExtensions.Contains(Path.GetExtension(file.Name));
}
