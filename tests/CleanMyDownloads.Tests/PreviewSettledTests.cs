using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public class PreviewSettledTests
{
    sealed class FakeFolder(params FileEntry[] files) : IFolderReader
    {
        public IReadOnlyList<FileEntry> ListTopLevelFiles(string path) => files;
    }

    sealed class FixedClock : IClock
    {
        public DateTime Now => new(2026, 10, 21, 14, 0, 0);
    }

    [Fact]
    public void Unsettled_file_shows_waiting_until_date()
    {
        var t = new DateTime(2026, 10, 21, 13, 0, 0);
        var folder = new FakeFolder(
            new("new.pdf", false, false, t, t),
            new("old.png", false, false, t.AddDays(-2), t.AddDays(-2)));

        var text = Preview.Build(folder, @"C:\scratch", new FixedClock());

        Assert.Equal(
            "Preview sort of C:\\scratch (2026-10-21 14:00)\r\n" +
            "new.pdf -> waiting until 2026-10-21 16:00\r\n" +
            "old.png -> Pictures\r\n",
            text);
    }
}
