using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public class TypeTableTests
{
    [Theory]
    [InlineData("kackar.JPG", "Pictures")]
    [InlineData("a.png", "Pictures")]
    [InlineData("clip.MkV", "Videos")]
    [InlineData("song.flac", "Music")]
    [InlineData("zadani.pdf", "Documents")]
    [InlineData("data.csv", "Documents")]
    [InlineData("x.7z", "Archives")]
    [InlineData("setup.exe", "Installers")]
    [InlineData("model.opp", "OpenPonk")]
    [InlineData("weird.xyz", "Other")]
    [InlineData("README", "Other")]
    [InlineData("trailingdot.", "Other")]
    public void Maps_file_name_to_type_folder(string name, string expected) =>
        Assert.Equal(expected, TypeTable.Default.FolderFor(name));
}
