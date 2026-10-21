using CleanMyDownloads.Core;
using CleanMyDownloads.Sorter;
using Xunit;

namespace CleanMyDownloads.Tests;

public sealed class AppSettingsTests : IDisposable
{
    readonly string file = Path.Combine(Path.GetTempPath(), "cmd-app-" + Guid.NewGuid().ToString("N"), "settings.json");

    public void Dispose()
    {
        var dir = Path.GetDirectoryName(file)!;
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    sealed class Folder(params FileEntry[] files) : IFolderReader
    {
        public bool Read;
        public IReadOnlyList<FileEntry> ListTopLevelFiles(string path) { Read = true; return files; }
    }

    sealed class Viewer : IDocumentViewer
    {
        public string? Shown;
        public void Show(string text) => Shown = text;
    }

    [Fact]
    public void Preview_uses_the_type_table_from_settings()
    {
        new SettingsStore(file).Load();
        File.WriteAllText(file, File.ReadAllText(file).Replace("\"opp\"", "\"opp\", \"blend\""));
        var viewer = new Viewer();

        var code = App.Run(["--preview", @"C:\x"], new Folder(new FileEntry("s.blend", false, false)),
            new SystemClock(), viewer, settingsStore: new SettingsStore(file));

        Assert.Equal(0, code);
        Assert.Contains("s.blend -> OpenPonk", viewer.Shown);
    }

    [Fact]
    public void Invalid_settings_are_also_written_to_the_move_log()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "{ nope");
        var logPath = Path.Combine(Path.GetDirectoryName(file)!, "move.log");

        App.Run(["--sort", @"C:\x"], new Folder(), new SystemClock(), new Viewer(),
            logPath: logPath, settingsStore: new SettingsStore(file));

        Assert.Contains("JSON", File.ReadAllText(logPath));
    }

    [Fact]
    public void Init_settings_writes_defaults_and_nothing_else()
    {
        var logPath = Path.Combine(Path.GetDirectoryName(file)!, "move.log");

        var code = App.Run(["--init-settings"], new Folder(), new SystemClock(), new Viewer(),
            logPath: logPath, settingsStore: new SettingsStore(file));

        Assert.Equal(0, code);
        Assert.True(File.Exists(file));
        Assert.False(File.Exists(logPath));
    }

    [Fact]
    public void Invalid_settings_skip_the_run_and_report_what_is_wrong()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "{ nope");
        var folder = new Folder();
        var viewer = new Viewer();
        string? reported = null;

        var code = App.Run(["--preview"], folder, new SystemClock(), viewer,
            settingsStore: new SettingsStore(file), reportInvalidSettings: m => reported = m,
            logPath: Path.Combine(Path.GetDirectoryName(file)!, "move.log"));

        Assert.NotEqual(0, code);
        Assert.False(folder.Read);
        Assert.Null(viewer.Shown);
        Assert.Contains("JSON", reported);
    }
}
