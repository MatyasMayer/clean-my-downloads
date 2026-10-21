namespace CleanMyDownloads.Core;

public sealed record SortRunResult(string RunId, int Moved, int Duplicates, int Skipped, int Failed, int Waiting = 0);

/// <summary>SPEC sections 1 and 6: sort top-level files into type folders, logging every action.</summary>
public static class SortRun
{
    public static SortRunResult Execute(IFolderReader folder, IFileMover mover, IClock clock, MoveLog log,
        string path, TypeTable? types = null, string? runId = null,
        IInUseCheck? inUse = null, SettleWait? wait = null, SubjectClassifier? subjects = null,
        PlanOptions? plan = null)
    {
        inUse ??= new DiskInUseCheck();
        plan ??= PlanOptions.Default with
        {
            Types = types ?? TypeTable.Default,
            Subjects = subjects ?? SubjectClassifier.Default,
            Wait = wait ?? SettleWait.Default,
        };
        runId ??= Guid.NewGuid().ToString("N")[..8];
        var shown = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        string Show(string full) => Path.Combine(shown, Path.GetRelativePath(path, full));

        int moved = 0, duplicates = 0, skipped = 0, failed = 0, waiting = 0;
        log.RunStart(clock.Now, runId);

        var sorted = SortedFiles.From(File.Exists(log.Path) ? File.ReadAllLines(log.Path) : [], shown);
        var candidates = folder.ListTopLevelFiles(path).Where(f => !IgnoreRules.IsIgnored(f)).Select(f => (Dir: "", File: f)).ToList();
        foreach (var group in sorted.Paths.GroupBy(p => Path.GetDirectoryName(p)!, StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(Path.Combine(path, group.Key))) continue;
            var names = group.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            candidates.AddRange(folder.ListTopLevelFiles(Path.Combine(path, group.Key))
                .Where(f => names.Contains(f.Name) && !IgnoreRules.IsIgnored(f)).Select(f => (Dir: group.Key, File: f)));
        }
        var planned = SortPlanner.Plan(candidates.Select(c => c.File).ToList(),
            ExistingFoldersIn(folder, path), plan, clock.Now);
        for (var i = 0; i < planned.Count; i++)
        {
            var (file, _, destination, plannedWait) = planned[i];
            var fromDir = candidates[i].Dir;
            // A sorted file has already settled once; editing it in place doesn't make it wait again.
            var waitingUntil = fromDir == "" ? plannedWait : null;
            var source = Path.Combine(path, fromDir, file.Name);
            var typeDir = Path.Combine(path, destination);
            try
            {
                if (string.Equals(fromDir, destination, StringComparison.OrdinalIgnoreCase)) continue;
                if (!File.Exists(source))
                {
                    log.Skipped(clock.Now, Show(source), "file no longer exists");
                    skipped++;
                    continue;
                }
                if (waitingUntil != null)
                {
                    waiting++;
                    continue;
                }
                if (inUse.IsInUse(source))
                {
                    log.Skipped(clock.Now, Show(source), "in use, will retry next run");
                    skipped++;
                    continue;
                }
                var target = ResolveTarget(source, typeDir, out var identicalTo);
                if (identicalTo != null)
                {
                    log.Duplicate(clock.Now, Show(source), Show(identicalTo));
                    duplicates++;
                    continue;
                }
                mover.Move(source, target);
                log.Moved(clock.Now, Show(source), Show(target));
                moved++;
                if (fromDir != "") RemoveIfSorterEmptied(path, fromDir, sorted);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                log.Failed(clock.Now, Show(source), e.Message);
                failed++;
            }
        }

        log.RunEnd(clock.Now, runId);
        return new SortRunResult(runId, moved, duplicates, skipped, failed, waiting);
    }

    /// <summary>The only deletion the sorter makes: a folder it created, left empty by a re-sort move.</summary>
    static void RemoveIfSorterEmptied(string root, string relDir, SortedFiles sorted)
    {
        var dir = Path.Combine(root, relDir);
        if (sorted.CreatedFolders.Contains(relDir, StringComparer.OrdinalIgnoreCase)
            && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
            Directory.Delete(dir);
    }

    internal static ExistingFolders ExistingFoldersIn(IFolderReader folder, string path) => new(
        folder.ListSubfolderNames(Path.Combine(path, SortPlanner.TopicsFolder)),
        folder.ListSubfolderNames(Path.Combine(path, SubjectClassifier.SchoolFolder)));

    /// <summary>First free name (Windows-style suffix), or the existing byte-identical file.</summary>
    internal static string ResolveTarget(string source, string typeDir, out string? identicalTo, string? fileName = null)
    {
        identicalTo = null;
        fileName ??= Path.GetFileName(source);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var candidate = Path.Combine(typeDir, name + ext);
        for (var n = 2; File.Exists(candidate); n++)
        {
            if (SameContent(source, candidate))
            {
                identicalTo = candidate;
                return candidate;
            }
            candidate = Path.Combine(typeDir, $"{name} ({n}){ext}");
        }
        return candidate;
    }

    static bool SameContent(string a, string b)
    {
        if (new FileInfo(a).Length != new FileInfo(b).Length) return false;
        using var fa = File.OpenRead(a);
        using var fb = File.OpenRead(b);
        var ba = new byte[81920];
        var bb = new byte[81920];
        while (true)
        {
            var ra = fa.ReadAtLeast(ba, ba.Length, throwOnEndOfStream: false);
            var rb = fb.ReadAtLeast(bb, bb.Length, throwOnEndOfStream: false);
            if (ra != rb || !ba.AsSpan(0, ra).SequenceEqual(bb.AsSpan(0, rb))) return false;
            if (ra == 0) return true;
        }
    }
}
