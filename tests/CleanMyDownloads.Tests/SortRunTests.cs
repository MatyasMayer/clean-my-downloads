using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public sealed class SortRunTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "cmd-test-" + Guid.NewGuid().ToString("N"));
    readonly string downloads;
    readonly string logPath;

    sealed class FixedClock : IClock
    {
        public DateTime Now => new(2026, 10, 21, 14, 0, 0);
    }

    sealed class FailingMover(string failName, Exception error) : IFileMover
    {
        readonly DiskFileMover real = new();
        public void Move(string source, string destination)
        {
            if (Path.GetFileName(source) == failName) throw error;
            real.Move(source, destination);
        }
    }

    public SortRunTests()
    {
        downloads = Path.Combine(root, "Downloads");
        Directory.CreateDirectory(downloads);
        logPath = Path.Combine(root, "log", "move.log");
    }

    public void Dispose() => Directory.Delete(root, true);

    string Put(string rel, string content)
    {
        var p = Path.Combine(downloads, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, content);
        File.SetCreationTime(p, new DateTime(2026, 1, 1));
        File.SetLastWriteTime(p, new DateTime(2026, 1, 1));
        return p;
    }

    SortRunResult Sort(IFileMover? mover = null, string runId = "r1") =>
        SortRun.Execute(new DiskFolderReader(), mover ?? new DiskFileMover(), new FixedClock(),
            new MoveLog(logPath), downloads, runId: runId);

    string[] LogLines() => File.ReadAllLines(logPath);

    [Fact]
    public void Moves_file_into_type_folder_and_logs_one_line_between_run_markers()
    {
        Put("a.pdf", "x");

        var result = Sort();

        Assert.Equal(1, result.Moved);
        Assert.False(File.Exists(Path.Combine(downloads, "a.pdf")));
        Assert.Equal("x", File.ReadAllText(Path.Combine(downloads, "Documents", "a.pdf")));
        Assert.Equal(
        [
            "2026-10-21 14:00  RUN START r1",
            @"2026-10-21 14:00  Downloads\a.pdf → Downloads\Documents\a.pdf",
            "2026-10-21 14:00  RUN END r1",
        ], LogLines());
    }

    [Fact]
    public void Creates_type_folder_only_when_a_file_goes_into_it()
    {
        Put("a.pdf", "x");
        Sort();
        Assert.False(Directory.Exists(Path.Combine(downloads, "Pictures")));
    }

    [Fact]
    public void Name_taken_with_different_content_gets_windows_style_suffix()
    {
        Put(@"Documents\zadani.pdf", "old");
        Put(@"Documents\zadani (2).pdf", "older");
        Put("zadani.pdf", "new");

        Sort();

        Assert.Equal("old", File.ReadAllText(Path.Combine(downloads, "Documents", "zadani.pdf")));
        Assert.Equal("new", File.ReadAllText(Path.Combine(downloads, "Documents", "zadani (3).pdf")));
        Assert.Contains(@"2026-10-21 14:00  Downloads\zadani.pdf → Downloads\Documents\zadani (3).pdf", LogLines());
    }

    [Fact]
    public void Name_taken_with_identical_content_stays_put_and_is_logged_as_duplicate()
    {
        Put(@"Documents\a.pdf", "same");
        var src = Put("a.pdf", "same");

        var result = Sort();

        Assert.Equal(1, result.Duplicates);
        Assert.Equal(0, result.Moved);
        Assert.True(File.Exists(src));
        Assert.Contains(@"2026-10-21 14:00  DUPLICATE  Downloads\a.pdf  (same content as Downloads\Documents\a.pdf)", LogLines());
    }

    [Fact]
    public void Identical_to_a_suffixed_copy_is_also_a_duplicate()
    {
        Put(@"Documents\a.pdf", "one");
        Put(@"Documents\a (2).pdf", "two");
        Put("a.pdf", "two");

        var result = Sort();

        Assert.Equal(1, result.Duplicates);
        Assert.True(File.Exists(Path.Combine(downloads, "a.pdf")));
    }

    [Fact]
    public void Failed_move_is_logged_with_reason_and_run_carries_on()
    {
        var locked = Put("locked.pdf", "x");
        Put("b.png", "y");

        var result = Sort(new FailingMover("locked.pdf", new IOException("The process cannot access the file")));

        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Moved);
        Assert.True(File.Exists(locked));
        Assert.True(File.Exists(Path.Combine(downloads, "Pictures", "b.png")));
        Assert.Contains(@"2026-10-21 14:00  FAILED  Downloads\locked.pdf  (The process cannot access the file)", LogLines());
    }

    [Fact]
    public void Permission_denied_is_a_failure_too()
    {
        Put("a.pdf", "x");
        var result = Sort(new FailingMover("a.pdf", new UnauthorizedAccessException("Access denied")));
        Assert.Equal(1, result.Failed);
    }

    [Fact]
    public void Failed_move_leaves_no_empty_type_folder()
    {
        Put("a.pdf", "x");
        Sort(new FailingMover("a.pdf", new IOException("disk")));
        Assert.Empty(Directory.GetDirectories(downloads));
    }

    [Fact]
    public void Ignored_files_are_left_alone()
    {
        var p = Put("movie.crdownload", "x");
        Sort();
        Assert.True(File.Exists(p));
    }

    [Fact]
    public void Each_run_appends_its_own_marked_block_and_never_edits_earlier_lines()
    {
        Put("a.pdf", "x");
        Sort(runId: "r1");
        var first = LogLines();
        Put("b.png", "y");
        Sort(runId: "r2");

        var all = LogLines();
        Assert.Equal(first, all.Take(first.Length));
        Assert.Contains("2026-10-21 14:00  RUN START r2", all);
        Assert.Equal(first.Length + 3, all.Length);
    }

    [Fact]
    public void Empty_run_still_writes_markers()
    {
        Sort();
        Assert.Equal(2, LogLines().Length);
    }
}
