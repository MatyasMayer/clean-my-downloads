namespace CleanMyDownloads.Core;

/// <summary>Names of the sorter's folders that already exist (<c>Topics\*</c> and <c>School\*</c>).</summary>
public sealed record ExistingFolders(IReadOnlyCollection<string> Topics, IReadOnlyCollection<string> Subjects)
{
    public static ExistingFolders None { get; } = new([], []);
}

/// <summary>Everything the plan depends on besides the files, the folders and the clock.</summary>
public sealed record PlanOptions(TypeTable Types, SubjectClassifier Subjects, TopicRules Topics,
    SettleWait Wait, TimeSpan LooseFileWait)
{
    public static PlanOptions Default { get; } =
        new(TypeTable.Default, SubjectClassifier.Default, TopicRules.Default, SettleWait.Default, TimeSpan.FromDays(1));

    /// <summary>An explicit <paramref name="plan"/> wins; otherwise the defaults with any given part replaced.</summary>
    internal static PlanOptions Resolve(PlanOptions? plan, TypeTable? types, SubjectClassifier? subjects, SettleWait? wait) =>
        plan ?? Default with
        {
            Types = types ?? TypeTable.Default,
            Subjects = subjects ?? SubjectClassifier.Default,
            Wait = wait ?? SettleWait.Default,
        };

    public static PlanOptions From(Settings s) =>
        new(s.Types, new SubjectClassifier(s.NotASubject), new TopicRules(s.GenericWords, s.LanguageTags),
            new SettleWait(s.SettleTime, s.OppSettleTime), s.LooseFileWait);
}

public enum PlacementKind { Subject, Topic, Type }

/// <summary>
/// Where a file belongs. <see cref="Destination"/> is relative to the sorted folder; <see cref="WaitingUntil"/>
/// is null once the file may move.
/// </summary>
public sealed record Placement(FileEntry File, PlacementKind Kind, string Destination, DateTime? WaitingUntil);

/// <summary>
/// SPEC sections 3 and 4: decides subject, topic or type folder for a set of files, and when each may move.
/// A pure function of (candidate files, existing folders, options, now); it never touches the disk, so
/// preview, sort and re-sorting of already sorted files can all share it.
/// </summary>
public static class SortPlanner
{
    public const string TopicsFolder = "Topics";

    /// <summary>One placement per input file, in input order.</summary>
    public static IReadOnlyList<Placement> Plan(IReadOnlyList<FileEntry> files, ExistingFolders existing,
        PlanOptions options, DateTime now)
    {
        var subjectOf = new string?[files.Count];
        var subjectsSeen = new HashSet<string>(existing.Subjects.Select(s => s.ToUpperInvariant()), StringComparer.Ordinal);
        for (var i = 0; i < files.Count; i++)
            if (options.Subjects.FolderFor(files[i].Name) is { } code)
            {
                subjectOf[i] = code;
                subjectsSeen.Add(code);
            }

        // Bare codes: the body of a known subject, when exactly one known subject has that body.
        var byBody = subjectsSeen.GroupBy(c => c[(c.IndexOf('-') + 1)..].ToLowerInvariant())
            .Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.Single());
        var stems = new IReadOnlyList<string>?[files.Count];
        for (var i = 0; i < files.Count; i++)
        {
            if (subjectOf[i] != null) continue;
            var words = TopicRules.Words(Path.GetFileNameWithoutExtension(files[i].Name));
            subjectOf[i] = words.Select(w => byBody.GetValueOrDefault(w)).FirstOrDefault(c => c != null);
            if (subjectOf[i] == null) stems[i] = options.Topics.StemOf(files[i].Name);
        }

        var topicOf = AssignTopics(stems, existing.Topics);

        var result = new Placement[files.Count];
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (subjectOf[i] is { } subject)
                result[i] = new(file, PlacementKind.Subject, Path.Combine(SubjectClassifier.SchoolFolder, subject),
                    SettledRules.WaitingUntil(file, options.Wait, now));
            else if (topicOf[i] is { } topic)
                result[i] = new(file, PlacementKind.Topic, Path.Combine(TopicsFolder, topic),
                    SettledRules.WaitingUntil(file, options.Wait, now));
            else
                result[i] = new(file, PlacementKind.Type, options.Types.FolderFor(file.Name),
                    stems[i] != null ? LooseWaitingUntil(file, options.LooseFileWait, now)
                                     : SettledRules.WaitingUntil(file, options.Wait, now));
        }
        return result;
    }

    static DateTime? LooseWaitingUntil(FileEntry file, TimeSpan wait, DateTime now)
    {
        if (file.LastTouched is not { } last) return null;
        var until = last + wait;
        return now >= until ? null : until;
    }

    /// <summary>Topic folder name per file index, or null for files that stay loose.</summary>
    static string?[] AssignTopics(IReadOnlyList<IReadOnlyList<string>?> stems, IReadOnlyCollection<string> existingTopics)
    {
        var topicOf = new string?[stems.Count];
        var existing = existingTopics.Select(n => (Name: n, Words: (IReadOnlyList<string>)TopicRules.Words(n)))
            .Where(e => e.Words.Count > 0)
            .OrderBy(e => string.Concat(e.Words).Length).ThenBy(e => e.Name, StringComparer.Ordinal).ToList();

        // An existing folder takes a single matching file; it is never renamed.
        var pending = new List<int>();
        for (var i = 0; i < stems.Count; i++)
        {
            if (stems[i] is not { } stem) continue;
            var hit = existing.FirstOrDefault(e => TopicRules.SameTopic(e.Words, stem));
            if (hit.Name != null) topicOf[i] = hit.Name;
            else pending.Add(i);
        }

        // New topics: merge stems that are the same topic, shortest first; need at least 2 files.
        var byStem = new Dictionary<string, List<int>>();
        foreach (var i in pending)
            (byStem.TryGetValue(string.Join('-', stems[i]!), out var l) ? l : byStem[string.Join('-', stems[i]!)] = []).Add(i);

        static int LengthWithoutHyphens(string s) => s.Replace("-", "").Length;
        var groups = new List<List<string>>();
        foreach (var stem in byStem.Keys.OrderBy(LengthWithoutHyphens))
        {
            var words = stem.Split('-');
            // A stem can bridge groups that don't match each other: merge every group it matches.
            var matching = groups.Where(g => g.Any(k => TopicRules.SameTopic(k.Split('-'), words))).ToList();
            if (matching.Count == 0) { groups.Add([stem]); continue; }
            foreach (var other in matching.Skip(1))
            {
                matching[0].AddRange(other);
                groups.Remove(other);
            }
            matching[0].Add(stem);
        }
        foreach (var group in groups)
        {
            var members = group.SelectMany(k => byStem[k]).ToList();
            if (members.Count < 2) continue;
            var shortest = group.Min(LengthWithoutHyphens);
            var name = group.Where(k => LengthWithoutHyphens(k) == shortest)
                .OrderByDescending(k => byStem[k].Count).ThenByDescending(k => k.Split('-').Length)
                .ThenBy(k => k, StringComparer.Ordinal).First();
            foreach (var i in members) topicOf[i] = name;
        }
        return topicOf;
    }
}
