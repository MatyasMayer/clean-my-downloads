namespace CleanMyDownloads.Core;

/// <summary>SPEC section 8: the user-editable settings, as read at the start of a run.</summary>
public sealed record Settings(
    TypeTable Types,
    IReadOnlyList<string> GenericWords,
    IReadOnlyList<string> LanguageTags,
    IReadOnlyList<string> NotASubject,
    TimeSpan SettleTime,
    TimeSpan OppSettleTime,
    TimeSpan LooseFileWait)
{
    public static IReadOnlyDictionary<string, string[]> DefaultTypeFolders => TypeTable.DefaultFolders;

    public static IReadOnlyList<string> DefaultGenericWords { get; } =
    [
        "task", "ukol", "úkol", "cviceni", "cvičení", "cv", "lecture", "prednaska", "přednáška",
        "slides", "final", "copy", "kopie", "zadani", "zadání", "reseni", "řešení", "solution",
        "homework", "hw", "notes", "poznamky", "poznámky", "img", "image", "screenshot",
        "snimek", "snímek", "received", "download", "file", "new", "untitled", "document",
        "dokument", "scan", "part", "win", "x64", "setup", "installer",
    ];

    public static IReadOnlyList<string> DefaultLanguageTags { get; } = ["cs", "cz", "en", "sk", "eng"];
}

/// <summary>Outcome of reading settings: usable settings, or a message saying what is wrong.</summary>
public abstract record SettingsResult
{
    SettingsResult() { }

    public sealed record Loaded(Settings Settings) : SettingsResult;

    /// <summary>The run must be skipped; never fall back to defaults.</summary>
    public sealed record Invalid(string Message) : SettingsResult;
}
