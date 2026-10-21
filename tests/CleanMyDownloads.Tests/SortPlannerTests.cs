using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public class SortPlannerTests
{
    static readonly DateTime Now = new(2026, 10, 21, 14, 0, 0);
    static readonly DateTime Old = Now.AddDays(-5);

    static Dictionary<string, string> Plan(string[] names, string[]? topics = null, string[]? subjects = null,
        PlanOptions? options = null) =>
        SortPlanner.Plan(names.Select(n => new FileEntry(n, false, false, Old, Old)).ToList(),
                new ExistingFolders(topics ?? [], subjects ?? []), options ?? PlanOptions.Default, Now)
            .ToDictionary(p => p.File.Name, p => p.Destination);

    static string Topic(string name) => Path.Combine("Topics", name);
    static string School(string code) => Path.Combine("School", code);

    [Fact]
    public void Real_downloads_costy_taxi_forms_a_topic_and_the_rest_is_loose()
    {
        var plan = Plan(["costy-taxi_task-05.opp", "costy-taxi_task-05_cs.pdf", "openponk-class-editor-win-v5.1.1.zip",
            "costy-taxi_task-04.opp", "togif-2.gif", "received_906334184082038.gif", "MorePartyKalvoda.gif", "kackar.JPG"]);

        Assert.Equal(Topic("costy-taxi"), plan["costy-taxi_task-05.opp"]);
        Assert.Equal(Topic("costy-taxi"), plan["costy-taxi_task-05_cs.pdf"]);
        Assert.Equal(Topic("costy-taxi"), plan["costy-taxi_task-04.opp"]);
        Assert.Equal("Archives", plan["openponk-class-editor-win-v5.1.1.zip"]);
        Assert.Equal("Pictures", plan["togif-2.gif"]);
        Assert.Equal("Pictures", plan["received_906334184082038.gif"]);
        Assert.Equal("Pictures", plan["MorePartyKalvoda.gif"]);
        Assert.Equal("Pictures", plan["kackar.JPG"]);
        Assert.Equal(3, plan.Values.Count(v => v == Topic("costy-taxi")));
    }

    [Fact]
    public void Every_spelling_of_the_same_name_joins_costy_taxi()
    {
        var plan = Plan(["costy-taxi_task-04.opp", "costy_taxi_reseni.pdf", "CostyTaxi-notes.txt",
            "costy-taxi_task-04 (1).opp", "costytaxi.pdf"]);

        Assert.All(plan.Values, v => Assert.Equal(Topic("costy-taxi"), v));
    }

    [Fact]
    public void A_topic_is_named_after_its_shortest_stem_in_its_most_common_spelling()
    {
        var plan = Plan(["costytaxi.pdf", "costy-taxi-a.pdf", "costy-taxi-b.pdf", "costy-taxi-extra-1.pdf", "costy-taxi.pdf"]);

        Assert.All(plan.Values, v => Assert.Equal(Topic("costy-taxi"), v));
    }

    [Fact]
    public void Invoices_form_a_topic_but_screenshots_photos_and_copies_stay_loose()
    {
        var plan = Plan(["Screenshot 2026-10-01 120000.png", "Screenshot 2026-10-02 093011.png", "IMG_20261001_1530.jpg",
            "IMG_20261003_0812.jpg", "faktura-2026-09.pdf", "faktura-2026-10.pdf", "zadani.pdf", "zadani (1).pdf"]);

        Assert.Equal(Topic("faktura"), plan["faktura-2026-09.pdf"]);
        Assert.Equal(Topic("faktura"), plan["faktura-2026-10.pdf"]);
        Assert.Equal("Pictures", plan["Screenshot 2026-10-01 120000.png"]);
        Assert.Equal("Pictures", plan["IMG_20261001_1530.jpg"]);
        Assert.Equal("Documents", plan["zadani.pdf"]);
        Assert.Equal("Documents", plan["zadani (1).pdf"]);
    }

    [Fact]
    public void Linear_algebra_notes_and_exam_form_a_topic_while_regression_stays_loose()
    {
        var plan = Plan(["linear-algebra-notes.pdf", "linear-regression-hw.pdf", "linear-algebra-exam-2025.pdf"]);

        Assert.Equal(Topic("linear-algebra"), plan["linear-algebra-notes.pdf"]);
        Assert.Equal(Topic("linear-algebra"), plan["linear-algebra-exam-2025.pdf"]);
        Assert.Equal("Documents", plan["linear-regression-hw.pdf"]);
    }

    [Fact]
    public void A_stem_only_matches_whole_words_so_cat_is_not_catalog()
    {
        var plan = Plan(["cat.pdf", "catalog.pdf"]);

        Assert.Equal("Documents", plan["cat.pdf"]);
        Assert.Equal("Documents", plan["catalog.pdf"]);
    }

    [Fact]
    public void A_stem_under_three_characters_is_no_stem()
    {
        var plan = Plan(["ab-1.pdf", "ab-2.pdf"]);

        Assert.Equal("Documents", plan["ab-1.pdf"]);
        Assert.Equal("Documents", plan["ab-2.pdf"]);
    }

    [Fact]
    public void Existing_topic_folder_takes_a_single_matching_file()
    {
        var plan = Plan(["costytaxi-extra.pdf", "other.pdf"], topics: ["costy-taxi"]);

        Assert.Equal(Topic("costy-taxi"), plan["costytaxi-extra.pdf"]);
        Assert.Equal("Documents", plan["other.pdf"]);
    }

    [Fact]
    public void Existing_topic_folder_is_never_renamed_by_a_shorter_new_spelling()
    {
        var plan = Plan(["costytaxi.pdf", "costytaxi-b.pdf"], topics: ["costy-taxi"]);

        Assert.All(plan.Values, v => Assert.Equal(Topic("costy-taxi"), v));
    }

    [Fact]
    public void Bare_code_joins_a_known_subject_from_an_existing_folder()
    {
        var plan = Plan(["ag1-notes.pdf", "ag2-notes.pdf"], subjects: ["BI-AG1"]);

        Assert.Equal(School("BI-AG1"), plan["ag1-notes.pdf"]);
        Assert.Equal("Documents", plan["ag2-notes.pdf"]);
    }

    [Fact]
    public void Bare_code_joins_a_full_code_seen_in_the_same_run()
    {
        var plan = Plan(["bi-ag1-cviceni-03.pdf", "BI-AG1.21_ukol2.opp", "ag1-notes.pdf", "ag1-cheatsheet.pdf"]);

        Assert.All(plan.Values, v => Assert.Equal(School("BI-AG1"), v));
    }

    [Fact]
    public void Bare_code_shared_by_two_known_subjects_is_ambiguous_and_ignored()
    {
        var plan = Plan(["ag1-notes.pdf"], subjects: ["BI-AG1", "MI-AG1"]);

        Assert.Equal("Documents", plan["ag1-notes.pdf"]);
    }

    [Fact]
    public void Generic_words_and_language_tags_come_from_options()
    {
        var options = PlanOptions.Default with { Topics = new TopicRules(["draft"], ["de"]) };

        var plan = Plan(["report_de_draft-2.pdf", "report-draft.pdf", "task-1.pdf", "task-2.pdf"], options: options);

        Assert.Equal(Topic("report"), plan["report_de_draft-2.pdf"]);
        Assert.Equal(Topic("report"), plan["report-draft.pdf"]);
        // "task" is not generic here, and "1"/"2" end the stem, so these two form a topic.
        Assert.Equal(Topic("task"), plan["task-1.pdf"]);
    }

    [Fact]
    public void A_stem_that_matches_two_separate_groups_merges_them()
    {
        // "abc" and "ab-cd" are different topics, but "abc-d" is the same as both.
        var plan = Plan(["abc.pdf", "ab-cd.pdf", "abc-d.pdf"]);

        Assert.All(plan.Values, v => Assert.Equal(Topic("abc"), v));
    }

    static Placement One(FileEntry f) =>
        SortPlanner.Plan([f], new ExistingFolders([], []), PlanOptions.Default, Now).Single();

    [Fact]
    public void Loose_file_with_a_stem_waits_one_day_from_the_later_of_created_and_modified()
    {
        var created = Now.AddHours(-30);
        var modified = Now.AddHours(-10);

        var placement = One(new FileEntry("lonely-thing.pdf", false, false, created, modified));

        Assert.Equal("Documents", placement.Destination);
        Assert.Equal(modified.AddDays(1), placement.WaitingUntil);
    }

    [Fact]
    public void Loose_file_with_a_stem_moves_after_one_day()
    {
        var t = Now.AddDays(-1);
        Assert.Null(One(new FileEntry("lonely-thing.pdf", false, false, t, t)).WaitingUntil);
    }

    [Fact]
    public void Loose_file_with_no_stem_moves_once_settled()
    {
        var t = Now.AddHours(-4);
        Assert.Null(One(new FileEntry("IMG_1530.jpg", false, false, t, t)).WaitingUntil);
        var fresh = Now.AddHours(-1);
        Assert.Equal(fresh.AddHours(3), One(new FileEntry("IMG_1530.jpg", false, false, fresh, fresh)).WaitingUntil);
    }

    [Fact]
    public void Loose_wait_for_a_stemmed_file_comes_from_options()
    {
        var t = Now.AddHours(-5);
        var options = PlanOptions.Default with { LooseFileWait = TimeSpan.FromHours(6) };

        var p = SortPlanner.Plan([new FileEntry("lonely-thing.pdf", false, false, t, t)],
            new ExistingFolders([], []), options, Now).Single();

        Assert.Equal(t.AddHours(6), p.WaitingUntil);
    }

    [Fact]
    public void Topic_member_waits_only_until_settled_not_a_day()
    {
        var t = Now.AddHours(-4);
        var plan = SortPlanner.Plan(
            [new FileEntry("faktura-1.pdf", false, false, t, t), new FileEntry("faktura-2.pdf", false, false, t, t)],
            new ExistingFolders([], []), PlanOptions.Default, Now);

        Assert.All(plan, p => { Assert.Equal(Topic("faktura"), p.Destination); Assert.Null(p.WaitingUntil); });
    }

    [Fact]
    public void Subject_file_follows_the_settled_rule_and_keeps_its_kind()
    {
        var plan = SortPlanner.Plan([new FileEntry("BI-AG1.21_ukol2.opp", false, false, Now.AddHours(-5), Now.AddHours(-5))],
            new ExistingFolders([], []), PlanOptions.Default, Now).Single();

        Assert.Equal(PlacementKind.Subject, plan.Kind);
        Assert.Equal(Now.AddHours(19), plan.WaitingUntil);
    }
}
