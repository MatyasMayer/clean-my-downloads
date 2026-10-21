using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public class SettledTests
{
    static readonly DateTime T = new(2026, 10, 21, 10, 0, 0);
    static FileEntry File(string name, DateTime created, DateTime modified) =>
        new(name, false, false, created, modified);

    [Fact]
    public void Waits_three_hours_from_the_later_of_created_and_modified()
    {
        var f = File("a.pdf", T, T.AddHours(1));
        Assert.Equal(T.AddHours(4), SettledRules.WaitingUntil(f, SettleWait.Default, T.AddHours(2)));
    }

    [Fact]
    public void Settled_exactly_at_the_wait()
    {
        var f = File("a.pdf", T, T);
        Assert.Null(SettledRules.WaitingUntil(f, SettleWait.Default, T.AddHours(3)));
    }

    [Fact]
    public void Opp_waits_24_hours()
    {
        var f = File("model.OPP", T, T);
        Assert.Equal(T.AddHours(24), SettledRules.WaitingUntil(f, SettleWait.Default, T.AddHours(23)));
        Assert.Null(SettledRules.WaitingUntil(f, SettleWait.Default, T.AddHours(24)));
    }

    [Fact]
    public void Wait_times_are_parameters()
    {
        var f = File("a.pdf", T, T);
        var wait = new SettleWait(TimeSpan.FromMinutes(5), TimeSpan.FromHours(1));
        Assert.Null(SettledRules.WaitingUntil(f, wait, T.AddMinutes(5)));
    }
}
