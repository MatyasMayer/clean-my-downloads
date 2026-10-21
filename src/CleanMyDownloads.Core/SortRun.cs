namespace CleanMyDownloads.Core;

public sealed record SortRunResult(string RunId, int Moved, int Duplicates, int Skipped, int Failed, int Waiting = 0);

/// <summary>
/// SPEC sections 2, 5 and 6: sort top-level files into subject, topic and type folders, re-sort files the sorter
/// sorted earlier, and log every action.
/// </summary>
public static class SortRun
{
    public static SortRunResult Execute(IFolderReader folder, IFileMover mover, IClock clock, MoveLog log,
        string path, TypeTable? types = null, string? runId = null,
        IInUseCheck? inUse = null, SettleWait? wait = null, SubjectClassifier? subjects = null,
        PlanOptions? plan = null)
    {
        inUse ??= new DiskInUseCheck();
        plan = PlanOptions.Resolve(plan, types, subjects, wait);
        runId ??= Guid.NewGuid().ToString("N")[..8];
        var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        string Logged(string full) => Path.Combine(folderName, Path.GetRelativePath(path, full));

        int moved = 0, duplicates = 0, skipped = 0, failed = 0, waiting = 0;
        log.RunStart(clock.Now, runId);

        var sortPlan = SortPlan.Build(folder, path, File.Exists(log.Path) ? File.ReadAllLines(log.Path) : [], plan, clock.Now);
        var busy = new Dictionary<PlannedMove, bool>();
        bool IsBusy(PlannedMove m) => busy.TryGetValue(m, out var b) ? b : busy[m] = inUse.IsInUse(m.From.Source(path));
        bool CanMoveNow(PlannedMove m) => File.Exists(m.From.Source(path)) && m.WaitingUntil == null && !IsBusy(m);

        // Decided up front: siblings must be judged before this run's own moves change the folder.
        var heldBack = sortPlan.Moves.Where(m => sortPlan.IsHeldBack(m, CanMoveNow)).ToHashSet();

        foreach (var move in sortPlan.Moves)
        {
            var fromDir = move.From.Dir;
            var source = move.From.Source(path);
            var destDir = Path.Combine(path, move.Destination);
            try
            {
                if (move.StaysPut) continue;
                if (!File.Exists(source))
                {
                    log.Skipped(clock.Now, Logged(source), "file no longer exists");
                    skipped++;
                    continue;
                }
                if (move.WaitingUntil != null)
                {
                    waiting++;
                    continue;
                }
                if (IsBusy(move))
                {
                    log.Skipped(clock.Now, Logged(source), "in use, will retry next run");
                    skipped++;
                    continue;
                }
                if (heldBack.Contains(move)) continue;
                var target = ResolveTarget(source, destDir, out var identicalTo);
                if (identicalTo != null)
                {
                    log.Duplicate(clock.Now, Logged(source), Logged(identicalTo));
                    duplicates++;
                    continue;
                }
                mover.Move(source, target);
                log.Moved(clock.Now, Logged(source), Logged(target));
                moved++;
                if (move.From.IsSorted && sortPlan.Sorted.CreatedFolders.Contains(fromDir, StringComparer.OrdinalIgnoreCase))
                    EmptyFolders.RemoveUpTo(Path.Combine(path, fromDir), path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                log.Failed(clock.Now, Logged(source), e.Message);
                failed++;
            }
        }

        log.RunEnd(clock.Now, runId);
        return new SortRunResult(runId, moved, duplicates, skipped, failed, waiting);
    }

    /// <summary>First free name (Windows-style suffix), or the existing byte-identical file.</summary>
    internal static string ResolveTarget(string source, string destDir, out string? identicalTo, string? fileName = null)
    {
        identicalTo = null;
        fileName ??= Path.GetFileName(source);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var candidate = Path.Combine(destDir, name + ext);
        for (var n = 2; File.Exists(candidate); n++)
        {
            if (SameContent(source, candidate))
            {
                identicalTo = candidate;
                return candidate;
            }
            candidate = Path.Combine(destDir, $"{name} ({n}){ext}");
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
