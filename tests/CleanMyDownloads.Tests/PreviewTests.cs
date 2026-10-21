using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public class PreviewTests
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
    public void Lists_each_non_ignored_file_with_its_type_folder()
    {
        var folder = new FakeFolder(
            new("kackar.JPG", false, false),
            new("notes.weird", false, false),
            new("movie.crdownload", false, false),
            new("desktop.ini", false, false),
            new("secret.pdf", true, false));

        var text = Preview.Build(folder, @"C:\scratch", new FixedClock());

        Assert.Equal(
            "Preview sort of C:\\scratch (2026-10-21 14:00)\r\n" +
            "kackar.JPG -> Pictures\r\n" +
            "notes.weird -> Other\r\n",
            text);
    }

    [Fact]
    public void Subject_wins_over_type_and_respects_the_not_a_subject_list()
    {
        var folder = new FakeFolder(
            new("BI-AG1.21_ukol2.opp", false, false),
            new("bi-zma-01-uvod.pdf", false, false),
            new("mi-fit.apk", false, false));

        var text = Preview.Build(folder, @"C:\scratch", new FixedClock(),
            subjects: new SubjectClassifier(["MI-FIT"]));

        Assert.Equal(
            "Preview sort of C:\\scratch (2026-10-21 14:00)\r\n" +
            "BI-AG1.21_ukol2.opp -> School\\BI-AG1\r\n" +
            "bi-zma-01-uvod.pdf -> School\\BI-ZMA\r\n" +
            "mi-fit.apk -> Other\r\n",
            text);
    }

    [Fact]
    public void Says_so_when_nothing_to_sort()
    {
        var text = Preview.Build(new FakeFolder(), @"C:\scratch", new FixedClock());
        Assert.Equal("Preview sort of C:\\scratch (2026-10-21 14:00)\r\n(no files to sort)\r\n", text);
    }
}
