using System.Text;

namespace CleanMyDownloads.Core;

/// <summary>SPEC section 6: Preview sort. Pure read; never touches the folder.</summary>
public static class Preview
{
    public static string Build(IFolderReader folder, string path, IClock clock, TypeTable? types = null,
        SubjectClassifier? subjects = null)
    {
        types ??= TypeTable.Default;
        subjects ??= SubjectClassifier.Default;
        var sb = new StringBuilder();
        sb.Append($"Preview sort of {path} ({clock.Now:yyyy-MM-dd HH:mm})\r\n");
        var files = folder.ListTopLevelFiles(path).Where(f => !IgnoreRules.IsIgnored(f)).ToList();
        if (files.Count == 0)
            sb.Append("(no files to sort)\r\n");
        foreach (var f in files)
            sb.Append($"{f.Name} -> {subjects.DestinationFor(f.Name) ?? types.FolderFor(f.Name)}\r\n");
        return sb.ToString();
    }
}
