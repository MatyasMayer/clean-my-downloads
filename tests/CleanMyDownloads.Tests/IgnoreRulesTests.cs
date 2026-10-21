using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public class IgnoreRulesTests
{
    [Theory]
    [InlineData("a.crdownload")]
    [InlineData("a.PART")]
    [InlineData("a.partial")]
    [InlineData("a.download")]
    [InlineData("a.tmp")]
    [InlineData("desktop.ini")]
    [InlineData("Desktop.INI")]
    public void Ignores_by_name(string name) =>
        Assert.True(IgnoreRules.IsIgnored(new FileEntry(name, false, false)));

    [Fact]
    public void Ignores_hidden() =>
        Assert.True(IgnoreRules.IsIgnored(new FileEntry("a.pdf", IsHidden: true, IsSystem: false)));

    [Fact]
    public void Ignores_system() =>
        Assert.True(IgnoreRules.IsIgnored(new FileEntry("a.pdf", IsHidden: false, IsSystem: true)));

    [Fact]
    public void Keeps_ordinary_file() =>
        Assert.False(IgnoreRules.IsIgnored(new FileEntry("a.pdf", false, false)));
}
