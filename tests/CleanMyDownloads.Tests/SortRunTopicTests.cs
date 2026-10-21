using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public sealed class SortRunTopicTests : IDisposable
{
    static readonly DateTime Now = new(2026, 10, 21, 14, 0, 0);
    readonly string root = Path.Combine(Path.GetTempPath(), "cmd-topic-" + Guid.NewGuid().ToString("N"));

    sealed class FixedClock : IClock
    {
        public DateTime Now => SortRunTopicTests.Now;
    }

    public void Dispose() => Directory.Delete(root, true);

    void Put(string rel, DateTime when)
    {
        var p = Path.Combine(root, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, rel);
        File.SetCreationTime(p, when);
        File.SetLastWriteTime(p, when);
    }

    bool Has(string rel) => File.Exists(Path.Combine(root, rel));

    SortRunResult Sort() => SortRun.Execute(new DiskFolderReader(), new DiskFileMover(), new FixedClock(),
        new MoveLog(Path.Combine(root, "log", "move.log")), root, runId: "r1");

    [Fact]
    public void Siblings_move_into_a_new_topic_folder_and_a_fresh_loose_file_keeps_waiting()
    {
        Put("faktura-2026-09.pdf", Now.AddDays(-3));
        Put("faktura-2026-10.pdf", Now.AddDays(-3));
        Put("lonely-thing.pdf", Now.AddHours(-5));

        var result = Sort();

        Assert.True(Has(@"Topics\faktura\faktura-2026-09.pdf"));
        Assert.True(Has(@"Topics\faktura\faktura-2026-10.pdf"));
        Assert.True(Has("lonely-thing.pdf"));
        Assert.Equal((2, 1), (result.Moved, result.Waiting));
    }

    [Fact]
    public void Existing_topic_folder_takes_a_single_new_file()
    {
        Directory.CreateDirectory(Path.Combine(root, "Topics", "costy-taxi"));
        Put("costy-taxi_task-06.opp", Now.AddDays(-3));

        Sort();

        Assert.True(Has(@"Topics\costy-taxi\costy-taxi_task-06.opp"));
    }

    [Fact]
    public void Existing_school_folder_makes_a_bare_code_a_known_subject()
    {
        Directory.CreateDirectory(Path.Combine(root, "School", "BI-AG1"));
        Put("ag1-notes.pdf", Now.AddDays(-3));

        Sort();

        Assert.True(Has(@"School\BI-AG1\ag1-notes.pdf"));
    }

    [Fact]
    public void Preview_shows_topics_and_loose_wait()
    {
        Put("faktura-1.pdf", Now.AddDays(-3));
        Put("faktura-2.pdf", Now.AddDays(-3));
        Put("lonely-thing.pdf", Now.AddHours(-5));

        var text = Preview.Build(new DiskFolderReader(), root, new FixedClock());

        Assert.Contains(@"faktura-1.pdf -> Topics\faktura", text);
        Assert.Contains("lonely-thing.pdf -> waiting until 2026-10-22 09:00", text);
    }
}
