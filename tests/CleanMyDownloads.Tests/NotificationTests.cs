using CleanMyDownloads.Core;
using CleanMyDownloads.Sorter;
using Xunit;

namespace CleanMyDownloads.Tests;

public sealed class NotificationTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "cmd-notif-" + Guid.NewGuid().ToString("N"));

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }

    sealed class RecordingNotifier : INotifier
    {
        public List<(string Title, string Message)> Shown = [];
        public void Notify(string title, string message) => Shown.Add((title, message));
    }

    sealed class NoViewer : IDocumentViewer { public void Show(string text) { } }

    void SortFiles(int count, RecordingNotifier notifier)
    {
        var downloads = Path.Combine(root, "dl");
        Directory.CreateDirectory(downloads);
        for (var i = 0; i < count; i++)
        {
            var p = Path.Combine(downloads, $"pic{i}.png");
            File.WriteAllText(p, "x" + i);
            File.SetCreationTime(p, new DateTime(2026, 1, 1));
            File.SetLastWriteTime(p, new DateTime(2026, 1, 1));
        }
        App.Run(["--sort", downloads], new DiskFolderReader(), new SystemClock(), new NoViewer(),
            logPath: Path.Combine(root, "log.txt"), settingsStore: new SettingsStore(Path.Combine(root, "s", "settings.json")),
            notifier: notifier);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_run_that_moved_fewer_than_three_files_is_silent(int count)
    {
        var notifier = new RecordingNotifier();
        SortFiles(count, notifier);
        Assert.Empty(notifier.Shown);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void A_run_that_moved_three_or_more_files_shows_one_notification_with_the_count(int count)
    {
        var notifier = new RecordingNotifier();
        SortFiles(count, notifier);
        var shown = Assert.Single(notifier.Shown);
        Assert.Contains(count.ToString(), shown.Message);
    }

    [Fact]
    public void Invalid_settings_show_a_notification_saying_what_is_wrong()
    {
        var file = Path.Combine(root, "settings.json");
        Directory.CreateDirectory(root);
        File.WriteAllText(file, "{ nope");
        var notifier = new RecordingNotifier();

        var code = App.Run(["--sort"], new DiskFolderReader(), new SystemClock(), new NoViewer(),
            settingsStore: new SettingsStore(file), notifier: notifier);

        Assert.Equal(3, code);
        var shown = Assert.Single(notifier.Shown);
        Assert.Contains("JSON", shown.Message);
    }
}
