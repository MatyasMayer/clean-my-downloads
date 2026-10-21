using CleanMyDownloads.Core;
using CleanMyDownloads.Sorter;
using Xunit;

namespace CleanMyDownloads.Tests;

public sealed class UndoAppTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "cmd-undoapp-" + Guid.NewGuid().ToString("N"));

    sealed class NoViewer : IDocumentViewer
    {
        public void Show(string text) { }
    }

    sealed class RecordingViewer : IDocumentViewer
    {
        public string? Shown;
        public void Show(string text) => Shown = text;
    }

    public void Dispose() => Directory.Delete(root, true);

    string SortOneFile(string downloads, string logPath, SettingsStore store)
    {
        Directory.CreateDirectory(downloads);
        File.WriteAllText(Path.Combine(downloads, "a.pdf"), "A");
        File.SetCreationTime(Path.Combine(downloads, "a.pdf"), DateTime.Now.AddDays(-2));
        File.SetLastWriteTime(Path.Combine(downloads, "a.pdf"), DateTime.Now.AddDays(-2));
        App.Run(["--sort", downloads], new DiskFolderReader(), new SystemClock(), new NoViewer(), logPath: logPath, settingsStore: store);
        return downloads;
    }

    [Fact]
    public void Undo_does_not_need_valid_settings()
    {
        var downloads = Path.Combine(root, "Downloads");
        var logPath = Path.Combine(root, "log", "move.log");
        var store = new SettingsStore(Path.Combine(root, "settings.json"));
        SortOneFile(downloads, logPath, store);
        File.WriteAllText(store.FilePath, "{ nope");

        var code = App.Run(["--undo", downloads], new DiskFolderReader(), new SystemClock(), new NoViewer(), logPath: logPath, settingsStore: store);

        Assert.Equal(0, code);
        Assert.True(File.Exists(Path.Combine(downloads, "a.pdf")));
    }

    [Fact]
    public void Undo_shows_the_files_it_had_to_skip()
    {
        var downloads = Path.Combine(root, "Downloads");
        var logPath = Path.Combine(root, "log", "move.log");
        var store = new SettingsStore(Path.Combine(root, "settings.json"));
        SortOneFile(downloads, logPath, store);
        File.Delete(Path.Combine(downloads, "Documents", "a.pdf"));
        var viewer = new RecordingViewer();

        App.Run(["--undo", downloads], new DiskFolderReader(), new SystemClock(), viewer, logPath: logPath, settingsStore: store);

        Assert.Contains("a.pdf", viewer.Shown);
    }

    [Fact]
    public void Undo_mode_reverses_the_last_sort_of_the_given_folder()
    {
        var downloads = Path.Combine(root, "Downloads");
        Directory.CreateDirectory(downloads);
        File.WriteAllText(Path.Combine(downloads, "a.pdf"), "A");
        File.SetCreationTime(Path.Combine(downloads, "a.pdf"), DateTime.Now.AddDays(-2));
        File.SetLastWriteTime(Path.Combine(downloads, "a.pdf"), DateTime.Now.AddDays(-2));
        var logPath = Path.Combine(root, "log", "move.log");

        Assert.Equal(0, App.Run(["--sort", downloads], new DiskFolderReader(), new SystemClock(), new NoViewer(), logPath: logPath, settingsStore: new SettingsStore(Path.Combine(root, "settings.json"))));
        Assert.False(File.Exists(Path.Combine(downloads, "a.pdf")));

        Assert.Equal(0, App.Run(["--undo", downloads], new DiskFolderReader(), new SystemClock(), new NoViewer(), logPath: logPath, settingsStore: new SettingsStore(Path.Combine(root, "settings.json"))));
        Assert.Equal("A", File.ReadAllText(Path.Combine(downloads, "a.pdf")));
    }
}
