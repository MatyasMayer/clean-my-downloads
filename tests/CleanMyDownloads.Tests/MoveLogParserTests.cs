using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public class MoveLogParserTests
{
    [Fact]
    public void Parses_runs_with_their_entries()
    {
        string[] lines =
        [
            "2026-10-21 14:00  RUN START r1",
            @"2026-10-21 14:00  Downloads\a.pdf → Downloads\Documents\a.pdf",
            @"2026-10-21 14:00  DUPLICATE  Downloads\b.pdf  (same content as Downloads\Documents\b.pdf)",
            @"2026-10-21 14:00  FAILED  Downloads\c.pdf  (in use)",
            "2026-10-21 14:00  RUN END r1",
            "2026-10-21 15:00  RUN START r2",
            @"2026-10-21 15:00  Downloads\d.png → Downloads\Pictures\d.png",
        ];

        var runs = MoveLogParser.Parse(lines);

        Assert.Equal(["r1", "r2"], runs.Select(r => r.RunId));
        Assert.True(runs[0].Ended);
        Assert.False(runs[1].Ended);
        Assert.Collection(runs[0].Entries,
            e => { Assert.Equal(MoveLogKind.Moved, e.Kind); Assert.Equal(@"Downloads\a.pdf", e.Source); Assert.Equal(@"Downloads\Documents\a.pdf", e.Destination); },
            e => { Assert.Equal(MoveLogKind.Duplicate, e.Kind); Assert.Equal(@"Downloads\b.pdf", e.Source); },
            e => { Assert.Equal(MoveLogKind.Failed, e.Kind); Assert.Equal("in use", e.Detail); });
    }
}
