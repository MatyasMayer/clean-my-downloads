using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public class SubjectClassifierTests
{
    static string? Folder(string name, params string[] notASubject) =>
        new SubjectClassifier(notASubject).FolderFor(name);

    [Theory]
    [InlineData("bi-zma-01-uvod.pdf", "BI-ZMA")]
    [InlineData("MI-MZI_skripta.pdf", "MI-MZI")]
    [InlineData("BI-AG1.21_ukol2.opp", "BI-AG1")]
    [InlineData("poznamky BI-PA2 final.docx", "BI-PA2")]
    public void Detects_code_case_insensitively_anywhere_in_the_name(string name, string expected) =>
        Assert.Equal(expected, Folder(name));

    [Theory]
    [InlineData("costy-taxi_task-05_cs.pdf")]
    [InlineData("bible.pdf")]
    [InlineData("nikon_d3500.jpg")]
    [InlineData("Mi-Band-8.apk")]
    [InlineData("bi-weekly-report.pdf")]
    [InlineData("BIPA1_notes.pdf")]
    [InlineData("")]
    public void Ignores_names_that_only_look_similar(string name) =>
        Assert.Null(Folder(name));

    [Theory]
    [InlineData("BIE-AG1.pdf", "BI-AG1")]
    [InlineData("BIK-AG1.pdf", "BI-AG1")]
    [InlineData("bi-ag1.pdf", "BI-AG1")]
    [InlineData("NIE-PDB.pdf", "NI-PDB")]
    [InlineData("MIE-MZI.pdf", "MI-MZI")]
    [InlineData("ANIE-XYZ.pdf", "ANI-XYZ")]
    [InlineData("QNIE-XYZ.pdf", "QNI-XYZ")]
    [InlineData("NI-PDB.pdf", "NI-PDB")]
    [InlineData("MI-PDB.pdf", "MI-PDB")]
    public void Course_variants_share_one_folder_but_mi_stays_apart_from_ni(string name, string expected) =>
        Assert.Equal(expected, Folder(name));

    [Fact]
    public void Code_on_the_not_a_subject_list_is_ignored()
    {
        Assert.Null(Folder("mi-fit.apk", "MI-FIT"));
        Assert.Equal("MI-FIT", Folder("mi-fit.apk"));
    }

    [Fact]
    public void Not_a_subject_list_is_case_insensitive_and_another_code_in_the_name_still_counts()
    {
        Assert.Null(Folder("mi-fit.apk", "mi-fit"));
        Assert.Equal("BI-ZMA", Folder("mi-fit-bi-zma.txt", "MI-FIT"));
    }
}
