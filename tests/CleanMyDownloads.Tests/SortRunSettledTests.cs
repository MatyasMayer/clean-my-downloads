using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public sealed class SortRunSettledTests : IDisposable
{
    static readonly DateTime Now = new(2026, 10, 21, 14, 0, 0);
    readonly string root = Path.Combine(Path.GetTempPath(), "cmd-settled-" + Guid.NewGuid().ToString("N"));
    readonly string downloads;
    readonly string logPath;

    sealed class FixedClock : IClock
    {
        public DateTime Now => SortRunSettledTests.Now;
    }

    sealed class FakeInUse(params string[] names) : IInUseCheck
    {
        public bool IsInUse(string path) => names.Contains(Path.GetFileName(path));
    }

    public SortRunSettledTests()
    {
        downloads = Path.Combine(root, "Downloads");
        Directory.CreateDirectory(downloads);
        logPath = Path.Combine(root, "log", "move.log");
    }

    public void Dispose() => Directory.Delete(root, true);

    void Put(string name, TimeSpan age)
    {
        var p = Path.Combine(downloads, name);
        File.WriteAllText(p, "x");
        File.SetCreationTime(p, Now - age);
        File.SetLastWriteTime(p, Now - age);
    }

    SortRunResult Sort(IInUseCheck? inUse = null) =>
        SortRun.Execute(new DiskFolderReader(), new DiskFileMover(), new FixedClock(), new MoveLog(logPath),
            downloads, runId: "r1", inUse: inUse ?? new FakeInUse());

    [Fact]
    public void File_younger_than_three_hours_is_left_exactly_as_it_is()
    {
        Put("a.pdf", TimeSpan.FromHours(2));

        var result = Sort();

        Assert.Equal(0, result.Moved);
        Assert.Equal(1, result.Waiting);
        Assert.Equal(["a.pdf"], Directory.GetFileSystemEntries(downloads).Select(Path.GetFileName));
    }

    [Fact]
    public void File_older_than_three_hours_moves()
    {
        Put("a.pdf", TimeSpan.FromHours(3));
        Assert.Equal(1, Sort().Moved);
        Assert.True(File.Exists(Path.Combine(downloads, "Documents", "a.pdf")));
    }

    [Fact]
    public void Opp_file_needs_24_hours()
    {
        Put("m.opp", TimeSpan.FromHours(23));
        Put("n.opp", TimeSpan.FromHours(24));

        Sort();

        Assert.True(File.Exists(Path.Combine(downloads, "m.opp")));
        Assert.False(File.Exists(Path.Combine(downloads, "n.opp")));
    }

    [Fact]
    public void File_in_use_is_skipped_logged_and_stays_put()
    {
        Put("a.pdf", TimeSpan.FromDays(1));

        var result = Sort(new FakeInUse("a.pdf"));

        Assert.Equal(1, result.Skipped);
        Assert.True(File.Exists(Path.Combine(downloads, "a.pdf")));
        Assert.Contains(File.ReadAllLines(logPath), l => l.Contains(@"SKIPPED  Downloads\a.pdf  (in use"));
    }

    [Fact]
    public void File_in_use_is_retried_next_run()
    {
        Put("a.pdf", TimeSpan.FromDays(1));
        Sort(new FakeInUse("a.pdf"));

        Assert.Equal(1, Sort().Moved);
    }

    [Fact]
    public void Real_locked_file_counts_as_in_use()
    {
        Put("a.pdf", TimeSpan.FromDays(1));
        using var _ = new FileStream(Path.Combine(downloads, "a.pdf"), FileMode.Open, FileAccess.Read, FileShare.None);

        var result = SortRun.Execute(new DiskFolderReader(), new DiskFileMover(), new FixedClock(),
            new MoveLog(logPath), downloads, runId: "r1");

        Assert.Equal(1, result.Skipped);
    }
}
