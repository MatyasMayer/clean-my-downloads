namespace CleanMyDownloads.Core;

/// <summary>
/// SPEC section 2: what the move log says the sorter put inside the sorted folder. Paths are relative to the
/// sorted folder. Replaying the moves in order means a file the sorter moved again (or undo moved back) is
/// tracked at its latest path only.
/// </summary>
public sealed record SortedFiles(IReadOnlyCollection<string> Paths, IReadOnlyCollection<string> CreatedFolders)
{
    public static SortedFiles None { get; } = new([], []);

    public static SortedFiles From(IEnumerable<string> logLines, string sortedFolderName)
    {
        var prefix = sortedFolderName + Path.DirectorySeparatorChar;
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? Relative(string logged) =>
            logged.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? logged[prefix.Length..] : null;

        foreach (var run in MoveLogParser.Parse(logLines))
            foreach (var e in run.Entries.Where(e => e.Kind == MoveLogKind.Moved))
            {
                if (Relative(e.Source) is { } from) paths.Remove(from);
                if (Relative(e.Destination!) is not { } to) continue;
                var dir = Path.GetDirectoryName(to);
                if (string.IsNullOrEmpty(dir)) continue;
                paths.Add(to);
                folders.Add(dir);
            }
        return new SortedFiles(paths, folders);
    }
}
