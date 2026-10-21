using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public class DiskFolderReaderTests : IDisposable
{
    readonly string dir = Directory.CreateTempSubdirectory("cmd-test").FullName;

    public void Dispose()
    {
        foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            File.SetAttributes(f, FileAttributes.Normal);
        Directory.Delete(dir, true);
    }

    [Fact]
    public void Lists_only_top_level_files_and_flags_hidden_and_system()
    {
        File.WriteAllText(Path.Combine(dir, "a.pdf"), "x");
        var hidden = Path.Combine(dir, "h.pdf");
        File.WriteAllText(hidden, "x");
        File.SetAttributes(hidden, FileAttributes.Hidden);
        var sys = Path.Combine(dir, "s.pdf");
        File.WriteAllText(sys, "x");
        File.SetAttributes(sys, FileAttributes.System);
        Directory.CreateDirectory(Path.Combine(dir, "sub"));
        File.WriteAllText(Path.Combine(dir, "sub", "inner.pdf"), "x");

        var entries = new DiskFolderReader().ListTopLevelFiles(dir).OrderBy(e => e.Name).ToList();

        Assert.Equal(new[] { "a.pdf", "h.pdf", "s.pdf" }, entries.Select(e => e.Name));
        Assert.Equal(new[] { false, true, false }, entries.Select(e => e.IsHidden));
        Assert.Equal(new[] { false, false, true }, entries.Select(e => e.IsSystem));
    }

    [Fact]
    public void Preview_leaves_folder_untouched()
    {
        File.WriteAllText(Path.Combine(dir, "a.pdf"), "x");
        Directory.CreateDirectory(Path.Combine(dir, "sub"));
        var before = Snapshot();

        Preview.Build(new DiskFolderReader(), dir, new SystemClock());

        Assert.Equal(before, Snapshot());
    }

    string[] Snapshot() => Directory.EnumerateFileSystemEntries(dir, "*", SearchOption.AllDirectories)
        .Select(p => p + "|" + File.GetLastWriteTimeUtc(p).Ticks).Order().ToArray();
}
