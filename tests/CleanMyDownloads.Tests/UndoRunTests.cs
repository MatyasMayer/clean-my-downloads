using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public sealed class UndoRunTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "cmd-undo-" + Guid.NewGuid().ToString("N"));
    readonly string downloads;
    readonly MoveLog log;

    sealed class FixedClock : IClock
    {
        public DateTime Now => new(2026, 10, 21, 14, 0, 0);
    }

    public UndoRunTests()
    {
        downloads = Path.Combine(root, "Downloads");
        Directory.CreateDirectory(downloads);
        log = new MoveLog(Path.Combine(root, "log", "move.log"));
    }

    public void Dispose() => Directory.Delete(root, true);

    string P(string rel) => Path.Combine(downloads, rel);

    void Put(string rel, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(P(rel))!);
        File.WriteAllText(P(rel), content);
    }

    void Sort(string runId) =>
        SortRun.Execute(new DiskFolderReader(), new DiskFileMover(), new FixedClock(), log, downloads, runId: runId);

    UndoRunResult Undo() => UndoRun.Execute(new DiskFileMover(), new FixedClock(), log, downloads);

    string[] Layout() => Directory.EnumerateFileSystemEntries(downloads, "*", SearchOption.AllDirectories)
        .Select(p => Path.GetRelativePath(downloads, p) + (File.Exists(p) ? ":" + File.ReadAllText(p) : "")).Order().ToArray();

    [Fact]
    public void Sorting_then_undoing_restores_the_original_layout_and_removes_emptied_folders()
    {
        Put("a.pdf", "A");
        Put("b.png", "B");
        var before = Layout();
        Sort("r1");

        var result = Undo();

        Assert.Equal(2, result.Restored);
        Assert.Equal(before, Layout());
    }

    [Fact]
    public void Only_the_most_recent_run_is_undone()
    {
        Put("a.pdf", "A");
        Sort("r1");
        Put("b.png", "B");
        Sort("r2");

        Undo();

        Assert.True(File.Exists(P(@"Documents\a.pdf")));
        Assert.True(File.Exists(P("b.png")));
        Assert.False(Directory.Exists(P("Pictures")));
    }

    [Fact]
    public void Empty_runs_after_the_last_real_run_are_passed_over()
    {
        Put("a.pdf", "A");
        Sort("r1");
        Sort("r2");

        Undo();

        Assert.True(File.Exists(P("a.pdf")));
    }

    [Fact]
    public void Files_moved_or_deleted_since_are_skipped_and_reported()
    {
        Put("a.pdf", "A");
        Put("b.png", "B");
        Sort("r1");
        File.Delete(P(@"Documents\a.pdf"));

        var result = Undo();

        Assert.Equal(1, result.Restored);
        Assert.Equal(1, result.Skipped);
        Assert.True(File.Exists(P("b.png")));
        Assert.Contains(File.ReadAllLines(log.Path), l => l.Contains("SKIPPED") && l.Contains(@"Downloads\Documents\a.pdf"));
    }

    [Fact]
    public void Taken_original_location_gets_a_suffix()
    {
        Put("a.pdf", "A");
        Sort("r1");
        Put("a.pdf", "new");

        Undo();

        Assert.Equal("new", File.ReadAllText(P("a.pdf")));
        Assert.Equal("A", File.ReadAllText(P("a (2).pdf")));
    }

    [Fact]
    public void Taken_original_location_with_identical_content_leaves_the_file_put_and_logs_a_duplicate()
    {
        Put("a.pdf", "A");
        Sort("r1");
        Put("a.pdf", "A");

        var result = Undo();

        Assert.Equal(1, result.Duplicates);
        Assert.True(File.Exists(P(@"Documents\a.pdf")));
        Assert.Contains(File.ReadAllLines(log.Path), l => l.Contains("DUPLICATE") && l.Contains(@"Downloads\Documents\a.pdf"));
    }

    [Fact]
    public void Undo_is_logged_as_its_own_run_and_is_not_itself_undone_by_a_second_undo()
    {
        Put("a.pdf", "A");
        Sort("r1");
        Undo();

        var lines = File.ReadAllLines(log.Path);
        Assert.Contains(lines, l => l.Contains(@"Downloads\Documents\a.pdf → Downloads\a.pdf"));
        Assert.Contains(lines, l => l.Contains("RUN START undo-r1"));
        Assert.Contains(lines, l => l.Contains("RUN END undo-r1"));

        var again = Undo();
        Assert.Equal(0, again.Restored);
        Assert.True(File.Exists(P("a.pdf")));
    }

    [Fact]
    public void Folders_that_still_hold_other_files_are_kept()
    {
        Put("a.pdf", "A");
        Sort("r1");
        Put(@"Documents\mine.txt", "m");

        Undo();

        Assert.True(File.Exists(P(@"Documents\mine.txt")));
    }
}
