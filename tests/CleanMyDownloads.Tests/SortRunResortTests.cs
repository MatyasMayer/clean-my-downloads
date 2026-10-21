using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public sealed class SortRunResortTests : IDisposable
{
    static readonly DateTime Start = new(2026, 10, 21, 14, 0, 0);
    readonly string root = Path.Combine(Path.GetTempPath(), "cmd-resort-" + Guid.NewGuid().ToString("N"));
    readonly string downloads;
    readonly MoveLog log;
    readonly Clock clock = new() { Now = Start };

    sealed class Clock : IClock
    {
        public DateTime Now { get; set; }
    }

    public SortRunResortTests()
    {
        downloads = Path.Combine(root, "Downloads");
        Directory.CreateDirectory(downloads);
        log = new MoveLog(Path.Combine(root, "log", "move.log"));
    }

    public void Dispose() => Directory.Delete(root, true);

    string P(string rel) => Path.Combine(downloads, rel);

    void Put(string rel, string? content = null, DateTime? when = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(P(rel))!);
        File.WriteAllText(P(rel), content ?? rel);
        File.SetCreationTime(P(rel), when ?? Start.AddDays(-5));
        File.SetLastWriteTime(P(rel), when ?? Start.AddDays(-5));
    }

    bool Has(string rel) => File.Exists(P(rel));

    SortRunResult Sort(string runId)
    {
        clock.Now = clock.Now.AddHours(1);
        return SortRun.Execute(new DiskFolderReader(), new DiskFileMover(), clock, log, downloads, runId: runId);
    }

    UndoRunResult Undo() => UndoRun.Execute(new DiskFileMover(), clock, log, downloads);

    string[] Layout() => Directory.EnumerateFileSystemEntries(downloads, "*", SearchOption.AllDirectories)
        .Select(p => Path.GetRelativePath(downloads, p)).Order().ToArray();

    [Fact]
    public void Pull_back_moves_a_lone_sorted_file_into_the_new_topic_with_its_new_sibling()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        Assert.True(Has(@"OpenPonk\costy-taxi_task-04.opp"));

        Put("costy-taxi_task-05.opp");
        var result = Sort("r2");

        Assert.True(Has(@"Topics\costy-taxi\costy-taxi_task-04.opp"));
        Assert.True(Has(@"Topics\costy-taxi\costy-taxi_task-05.opp"));
        Assert.Equal(2, result.Moved);
    }

    [Fact]
    public void Sorter_created_folder_left_empty_is_removed_but_no_file_is_lost()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        Put("costy-taxi_task-05.opp");
        Sort("r2");

        Assert.False(Directory.Exists(P("OpenPonk")));
        Assert.Equal(2, Directory.GetFiles(P(@"Topics\costy-taxi")).Length);
    }

    [Fact]
    public void Topic_becomes_subject_when_its_bare_code_turns_into_a_known_subject()
    {
        Put("ag1-notes.pdf");
        Put("ag1-scan.pdf");
        Sort("r1");
        Assert.True(Has(@"Topics\ag1\ag1-notes.pdf"));

        Put("bi-ag1-cv03.pdf");
        Sort("r2");

        Assert.True(Has(@"School\BI-AG1\ag1-notes.pdf"));
        Assert.True(Has(@"School\BI-AG1\ag1-scan.pdf"));
        Assert.False(Directory.Exists(P(@"Topics\ag1")));
    }

    [Fact]
    public void A_renamed_file_is_never_moved_and_is_not_a_sibling()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        File.Move(P(@"OpenPonk\costy-taxi_task-04.opp"), P(@"OpenPonk\my-renamed.opp"));
        Put("costy-taxi_task-05.opp");

        Sort("r2");

        Assert.True(Has(@"OpenPonk\my-renamed.opp"));
        Assert.True(Has(@"OpenPonk\costy-taxi_task-05.opp"));
    }

    [Fact]
    public void A_file_the_user_moved_elsewhere_is_never_touched()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        Directory.CreateDirectory(P("Mine"));
        File.Move(P(@"OpenPonk\costy-taxi_task-04.opp"), P(@"Mine\costy-taxi_task-04.opp"));
        Put("costy-taxi_task-05.opp");

        Sort("r2");

        Assert.True(Has(@"Mine\costy-taxi_task-04.opp"));
        Assert.True(Has(@"OpenPonk\costy-taxi_task-05.opp"));
    }

    [Fact]
    public void Renaming_a_folder_releases_everything_inside_it()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        Directory.Move(P("OpenPonk"), P("My OpenPonk"));
        Put("costy-taxi_task-05.opp");

        Sort("r2");

        Assert.True(Has(@"My OpenPonk\costy-taxi_task-04.opp"));
        Assert.True(Has(@"OpenPonk\costy-taxi_task-05.opp"));
        Assert.False(Has(@"Topics\costy-taxi\costy-taxi_task-04.opp"));
    }

    [Fact]
    public void Foreign_folders_and_their_contents_are_never_touched()
    {
        Put(@"extracted\costy-taxi_task-04.opp");
        Put(@"extracted\bi-ag1-cv03.pdf");
        Put("costy-taxi_task-05.opp");

        Sort("r1");

        Assert.True(Has(@"extracted\costy-taxi_task-04.opp"));
        Assert.True(Has(@"extracted\bi-ag1-cv03.pdf"));
        Assert.True(Has(@"OpenPonk\costy-taxi_task-05.opp"));
    }

    [Fact]
    public void Editing_a_sorted_file_in_place_keeps_it_sorted()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        File.WriteAllText(P(@"OpenPonk\costy-taxi_task-04.opp"), "edited");
        File.SetLastWriteTime(P(@"OpenPonk\costy-taxi_task-04.opp"), clock.Now);
        Put("costy-taxi_task-05.opp");

        Sort("r2");

        Assert.Equal("edited", File.ReadAllText(P(@"Topics\costy-taxi\costy-taxi_task-04.opp")));
    }

    [Fact]
    public void A_stable_layout_is_left_alone_on_the_next_run()
    {
        Put("a.pdf");
        Put("b.png");
        Sort("r1");
        var before = Layout();

        var result = Sort("r2");

        Assert.Equal(0, result.Moved);
        Assert.Equal(before, Layout());
    }

    [Fact]
    public void Topic_to_subject_resort_removes_the_emptied_topics_folder_too()
    {
        Put("ag1-notes.pdf");
        Put("ag1-scan.pdf");
        Sort("r1");
        Put("bi-ag1-cv03.pdf");
        Sort("r2");

        Assert.False(Directory.Exists(P("Topics")));
    }

    [Fact]
    public void A_sorted_file_is_not_pulled_into_a_topic_whose_new_sibling_is_still_waiting()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        Put("costy-taxi_task-05.opp", when: clock.Now.AddHours(1));

        var result = Sort("r2");

        Assert.True(Has(@"OpenPonk\costy-taxi_task-04.opp"));
        Assert.True(Has("costy-taxi_task-05.opp"));
        Assert.False(Directory.Exists(P("Topics")));
        Assert.Equal(0, result.Moved);
    }

    sealed class BusyEverything : IInUseCheck
    {
        public bool IsInUse(string path) => true;
    }

    [Fact]
    public void A_sorted_file_is_not_pulled_into_a_topic_whose_new_sibling_is_in_use()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        Put("costy-taxi_task-05.opp");

        clock.Now = clock.Now.AddHours(1);
        SortRun.Execute(new DiskFolderReader(), new DiskFileMover(), clock, log, downloads, runId: "r2", inUse: new BusyEverything());

        Assert.True(Has(@"OpenPonk\costy-taxi_task-04.opp"));
        Assert.False(Directory.Exists(P("Topics")));
    }

    [Fact]
    public void Preview_lists_the_resort_moves_of_sorted_files()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        Put("costy-taxi_task-05.opp");

        var text = Preview.Build(new DiskFolderReader(), downloads, clock, logPath: log.Path);

        Assert.Contains(@"OpenPonk\costy-taxi_task-04.opp -> Topics\costy-taxi", text);
        Assert.Contains(@"costy-taxi_task-05.opp -> Topics\costy-taxi", text);
    }

    [Fact]
    public void Undo_reverts_the_resort_moves_of_the_last_run()
    {
        Put("costy-taxi_task-04.opp");
        Sort("r1");
        Put("costy-taxi_task-05.opp");
        var before = Layout();
        Sort("r2");

        var result = Undo();

        Assert.Equal("r2", result.UndoneRunId);
        Assert.True(Has(@"OpenPonk\costy-taxi_task-04.opp"));
        Assert.True(Has("costy-taxi_task-05.opp"));
        Assert.Equal(before, Layout());
    }
}
