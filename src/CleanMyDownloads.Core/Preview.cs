using System.Text;

namespace CleanMyDownloads.Core;

/// <summary>SPEC section 6: Preview sort. Pure read; never touches the folder.</summary>
public static class Preview
{
    public static string Build(IFolderReader folder, string path, IClock clock, TypeTable? types = null, SettleWait? wait = null)
    {
        wait ??= SettleWait.Default;
        types ??= TypeTable.Default;
        var sb = new StringBuilder();
        sb.Append($"Preview sort of {path} ({clock.Now:yyyy-MM-dd HH:mm})\r\n");
        var files = folder.ListTopLevelFiles(path).Where(f => !IgnoreRules.IsIgnored(f)).ToList();
        if (files.Count == 0)
            sb.Append("(no files to sort)\r\n");
        foreach (var f in files)
        {
            var until = SettledRules.WaitingUntil(f, wait, clock.Now);
            var dest = until is { } u ? $"waiting until {u:yyyy-MM-dd HH:mm}" : types.FolderFor(f.Name);
            sb.Append($"{f.Name} -> {dest}\r\n");
        }
        return sb.ToString();
    }
}
