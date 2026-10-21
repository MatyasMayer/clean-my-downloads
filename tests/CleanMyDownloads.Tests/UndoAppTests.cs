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

    public void Dispose() => Directory.Delete(root, true);

    [Fact]
    public void Undo_mode_reverses_the_last_sort_of_the_given_folder()
    {
        var downloads = Path.Combine(root, "Downloads");
        Directory.CreateDirectory(downloads);
        File.WriteAllText(Path.Combine(downloads, "a.pdf"), "A");
        var logPath = Path.Combine(root, "log", "move.log");

        Assert.Equal(0, App.Run(["--sort", downloads], new DiskFolderReader(), new SystemClock(), new NoViewer(), logPath: logPath));
        Assert.False(File.Exists(Path.Combine(downloads, "a.pdf")));

        Assert.Equal(0, App.Run(["--undo", downloads], new DiskFolderReader(), new SystemClock(), new NoViewer(), logPath: logPath));
        Assert.Equal("A", File.ReadAllText(Path.Combine(downloads, "a.pdf")));
    }
}
