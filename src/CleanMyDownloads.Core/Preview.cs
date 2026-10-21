using System.Text;

namespace CleanMyDownloads.Core;

/// <summary>SPEC section 6: Preview sort. Pure read; never touches the folder.</summary>
public static class Preview
{
    public static string Build(IFolderReader folder, string path, IClock clock, TypeTable? types = null, SettleWait? wait = null,
        SubjectClassifier? subjects = null, PlanOptions? plan = null)
    {
        plan ??= PlanOptions.Default with
        {
            Types = types ?? TypeTable.Default,
            Subjects = subjects ?? SubjectClassifier.Default,
            Wait = wait ?? SettleWait.Default,
        };
        var sb = new StringBuilder();
        sb.Append($"Preview sort of {path} ({clock.Now:yyyy-MM-dd HH:mm})\r\n");
        var files = folder.ListTopLevelFiles(path).Where(f => !IgnoreRules.IsIgnored(f)).ToList();
        if (files.Count == 0)
            sb.Append("(no files to sort)\r\n");
        foreach (var p in SortPlanner.Plan(files, SortRun.ExistingFoldersIn(folder, path), plan, clock.Now))
        {
            var dest = p.WaitingUntil is { } u ? $"waiting until {u:yyyy-MM-dd HH:mm}" : p.Destination;
            sb.Append($"{p.File.Name} -> {dest}\r\n");
        }
        return sb.ToString();
    }
}
