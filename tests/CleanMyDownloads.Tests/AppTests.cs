using CleanMyDownloads.Core;
using CleanMyDownloads.Sorter;
using Xunit;

namespace CleanMyDownloads.Tests;

public class AppTests
{
    sealed class FakeFolder(params FileEntry[] files) : IFolderReader
    {
        public string? PathRead;
        public IReadOnlyList<FileEntry> ListTopLevelFiles(string path) { PathRead = path; return files; }
    }

    sealed class RecordingViewer : IDocumentViewer
    {
        public string? Shown;
        public void Show(string text) => Shown = text;
    }

    static SettingsStore TempStore() =>
        new(Path.Combine(Path.GetTempPath(), "cmd-apptests-" + Guid.NewGuid().ToString("N"), "settings.json"));

    [Fact]
    public void Preview_with_path_argument_shows_the_preview_for_that_folder()
    {
        var folder = new FakeFolder(new FileEntry("a.png", false, false));
        var viewer = new RecordingViewer();

        var code = App.Run(["--preview", @"C:\scratch"], folder, new SystemClock(), viewer, settingsStore: TempStore());

        Assert.Equal(0, code);
        Assert.Equal(@"C:\scratch", folder.PathRead);
        Assert.Contains("a.png -> Pictures", viewer.Shown);
    }

    [Fact]
    public void Preview_without_path_defaults_to_Downloads()
    {
        var folder = new FakeFolder();

        App.Run(["--preview"], folder, new SystemClock(), new RecordingViewer(), settingsStore: TempStore());

        Assert.EndsWith("Downloads", folder.PathRead);
    }
}
