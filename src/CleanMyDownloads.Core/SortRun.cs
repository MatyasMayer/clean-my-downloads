namespace CleanMyDownloads.Core;

public sealed record SortRunResult(string RunId, int Moved, int Duplicates, int Skipped, int Failed, int Waiting = 0);

/// <summary>SPEC sections 1 and 6: sort top-level files into type folders, logging every action.</summary>
public static class SortRun
{
    public static SortRunResult Execute(IFolderReader folder, IFileMover mover, IClock clock, MoveLog log,
        string path, TypeTable? types = null, string? runId = null,
        IInUseCheck? inUse = null, SettleWait? wait = null, SubjectClassifier? subjects = null)
    {
        inUse ??= new DiskInUseCheck();
        wait ??= SettleWait.Default;
        types ??= TypeTable.Default;
        subjects ??= SubjectClassifier.Default;
        runId ??= Guid.NewGuid().ToString("N")[..8];
        var shown = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        string Show(string full) => Path.Combine(shown, Path.GetRelativePath(path, full));

        int moved = 0, duplicates = 0, skipped = 0, failed = 0, waiting = 0;
        log.RunStart(clock.Now, runId);

        foreach (var file in folder.ListTopLevelFiles(path).Where(f => !IgnoreRules.IsIgnored(f)))
        {
            var source = Path.Combine(path, file.Name);
            var typeDir = Path.Combine(path, subjects.DestinationFor(file.Name) ?? types.FolderFor(file.Name));
            try
            {
                if (!File.Exists(source))
                {
                    log.Skipped(clock.Now, Show(source), "file no longer exists");
                    skipped++;
                    continue;
                }
                if (SettledRules.WaitingUntil(file, wait, clock.Now) != null)
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
