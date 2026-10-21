namespace CleanMyDownloads.Core;

/// <summary>A file the run looks at: <see cref="Dir"/> is "" for a top-level file, else the sorted folder it sits in.</summary>
internal sealed record Candidate(string Dir, FileEntry File)
{
    public bool IsSorted => Dir != "";
    public string Source(string root) => Path.Combine(root, Dir, File.Name);
}

/// <summary>A candidate and where the planner wants it.</summary>
internal sealed record PlannedMove(Candidate From, Placement To)
{
    public string Destination => To.Destination;

    /// <summary>A sorted file has already settled once; editing it in place doesn't make it wait again.</summary>
    public DateTime? WaitingUntil => From.IsSorted ? null : To.WaitingUntil;

    public bool StaysPut => string.Equals(From.Dir, Destination, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// What preview and sort both decide before touching anything: which files are candidates (top-level files plus
/// sorted files, SPEC section 2), where each belongs, and which re-sort moves must hold back.
/// </summary>
internal sealed class SortPlan(IReadOnlyList<PlannedMove> moves, ExistingFolders existing, SortedFiles sorted)
{
    public IReadOnlyList<PlannedMove> Moves { get; } = moves;
    public SortedFiles Sorted { get; } = sorted;

    public static SortPlan Build(IFolderReader folder, string path, IEnumerable<string> logLines, PlanOptions options, DateTime now)
    {
        var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        var sorted = SortedFiles.From(logLines, folderName);
        var candidates = folder.ListTopLevelFiles(path).Where(f => !IgnoreRules.IsIgnored(f))
            .Select(f => new Candidate("", f)).ToList();
        foreach (var group in sorted.Paths.GroupBy(p => Path.GetDirectoryName(p)!, StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(Path.Combine(path, group.Key))) continue;
            var names = group.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            candidates.AddRange(folder.ListTopLevelFiles(Path.Combine(path, group.Key))
                .Where(f => names.Contains(f.Name) && !IgnoreRules.IsIgnored(f)).Select(f => new Candidate(group.Key, f)));
        }
        var existing = new ExistingFolders(
            folder.ListSubfolderNames(Path.Combine(path, SortPlanner.TopicsFolder)),
            folder.ListSubfolderNames(Path.Combine(path, SubjectClassifier.SchoolFolder)));
        var placements = SortPlanner.Plan(candidates.Select(c => c.File).ToList(), existing, options, now);
        return new SortPlan(candidates.Select((c, i) => new PlannedMove(c, placements[i])).ToList(), existing, sorted);
    }

    /// <summary>
    /// SPEC 3.2: a new topic needs two files, so a sorted file is pulled into a not-yet-existing topic folder only
    /// when another file that can move now goes there too.
    /// </summary>
    public bool IsHeldBack(PlannedMove move, Func<PlannedMove, bool> canMoveNow)
    {
        if (!move.From.IsSorted || move.StaysPut || move.To.Kind != PlacementKind.Topic) return false;
        var topic = Path.GetFileName(move.Destination);
        if (existing.Topics.Contains(topic, StringComparer.OrdinalIgnoreCase)) return false;
        return !Moves.Any(o => o != move && !o.StaysPut
            && string.Equals(o.Destination, move.Destination, StringComparison.OrdinalIgnoreCase) && canMoveNow(o));
    }
}
