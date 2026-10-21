namespace CleanMyDownloads.Core;

/// <summary>SPEC section 3.3: extension to type folder.</summary>
public sealed class TypeTable
{
    public const string OtherFolder = "Other";

    readonly Dictionary<string, string> folderByExtension = new(StringComparer.OrdinalIgnoreCase);

    public TypeTable(IReadOnlyDictionary<string, string[]> extensionsByFolder)
    {
        foreach (var (folder, extensions) in extensionsByFolder)
            foreach (var ext in extensions)
                folderByExtension["." + ext] = folder;
    }

    public static TypeTable Default { get; } = new(new Dictionary<string, string[]>
    {
        ["Pictures"] = ["png", "jpg", "jpeg", "gif", "webp", "heic", "bmp", "svg"],
        ["Videos"] = ["mp4", "mov", "mkv", "avi", "webm"],
        ["Music"] = ["mp3", "wav", "flac", "m4a", "ogg"],
        ["Documents"] = ["pdf", "doc", "docx", "odt", "rtf", "txt", "md", "ppt", "pptx", "odp", "xls", "xlsx", "ods", "csv"],
        ["Archives"] = ["zip", "rar", "7z", "tar", "gz"],
        ["Installers"] = ["exe", "msi"],
        ["OpenPonk"] = ["opp"],
    });

    public string FolderFor(string fileName) =>
        folderByExtension.TryGetValue(Path.GetExtension(fileName), out var folder) ? folder : OtherFolder;
}
