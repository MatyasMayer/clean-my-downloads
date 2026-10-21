namespace CleanMyDownloads.Core;

public sealed record UndoRunResult(string? UndoneRunId, int Restored, int Duplicates, int Skipped, int Failed,
    IReadOnlyList<string> SkippedFiles);

/// <summary>SPEC section 6: move everything from the most recent run back, applying the sort collision rules.</summary>
public static class UndoRun
{
    public const string RunIdPrefix = "undo-";

    public static UndoRunResult Execute(IFileMover mover, IClock clock, MoveLog log, string path)
    {
        var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(path)) ?? "";
        string Full(string logged) => Path.Combine(parent, logged);
        bool Inside(string logged) => logged.StartsWith(folderName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        var lines = File.Exists(log.Path) ? File.ReadAllLines(log.Path) : [];
        var run = MoveLogParser.Parse(lines).LastOrDefault(r =>
            !r.RunId.StartsWith(RunIdPrefix) && r.Entries.Any(e => e.Kind == MoveLogKind.Moved && Inside(e.Source)));
        if (run == null) return new UndoRunResult(null, 0, 0, 0, 0, []);

        int restored = 0, duplicates = 0, failed = 0;
        var skippedFiles = new List<string>();
        log.RunStart(clock.Now, RunIdPrefix + run.RunId);

        foreach (var entry in run.Entries.Where(e => e.Kind == MoveLogKind.Moved && Inside(e.Source)).Reverse())
        {
            var current = Full(entry.Destination!);
            var original = Full(entry.Source);
            try
            {
                if (!File.Exists(current))
                {
                    log.Skipped(clock.Now, entry.Destination!, "file was moved or deleted since the run");
                    skippedFiles.Add(entry.Destination!);
                    continue;
                }
                var target = SortRun.ResolveTarget(current, Path.GetDirectoryName(original)!, out var identicalTo,
                    Path.GetFileName(original));
                if (identicalTo != null)
                {
                    log.Duplicate(clock.Now, entry.Destination!, entry.Source);
                    duplicates++;
                    continue;
                }
                mover.Move(current, target);
                log.Moved(clock.Now, entry.Destination!, Path.Combine(folderName, Path.GetRelativePath(path, target)));
                restored++;
                EmptyFolders.RemoveUpTo(Path.GetDirectoryName(current)!, path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                log.Failed(clock.Now, entry.Destination!, e.Message);
                failed++;
            }
        }

        log.RunEnd(clock.Now, RunIdPrefix + run.RunId);
        return new UndoRunResult(run.RunId, restored, duplicates, skippedFiles.Count, failed, skippedFiles);
    }
}
