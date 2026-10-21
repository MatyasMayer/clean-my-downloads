using System.Text;

namespace CleanMyDownloads.Core;

/// <summary>SPEC section 6: Preview sort. Pure read; never touches the folder.</summary>
public static class Preview
{
    /// <param name="logPath">The move log, to find sorted files that a sort would re-sort; none when null or missing.</param>
    public static string Build(IFolderReader folder, string path, IClock clock, TypeTable? types = null, SettleWait? wait = null,
        SubjectClassifier? subjects = null, PlanOptions? plan = null, string? logPath = null)
    {
        plan = PlanOptions.Resolve(plan, types, subjects, wait);
        var sb = new StringBuilder();
        sb.Append($"Preview sort of {path} ({clock.Now:yyyy-MM-dd HH:mm})\r\n");
        var logLines = logPath != null && File.Exists(logPath) ? File.ReadAllLines(logPath) : [];
        var sortPlan = SortPlan.Build(folder, path, logLines, plan, clock.Now);
        // Whether a file is in use is only known when sorting; preview holds back on what it can see.
        var lines = sortPlan.Moves.Where(m => !m.StaysPut && !sortPlan.IsHeldBack(m, o => o.WaitingUntil == null))
            .Select(m => $"{Path.Combine(m.From.Dir, m.From.File.Name)} -> "
                + (m.WaitingUntil is { } until ? $"waiting until {until:yyyy-MM-dd HH:mm}" : m.Destination))
            .ToList();
        if (lines.Count == 0)
            sb.Append("(no files to sort)\r\n");
        foreach (var line in lines)
            sb.Append(line).Append("\r\n");
        return sb.ToString();
    }
}
