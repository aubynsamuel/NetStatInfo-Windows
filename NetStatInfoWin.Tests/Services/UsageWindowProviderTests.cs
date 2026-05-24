using NetStatInfoWin.Models;
using NetStatInfoWin.Services;

namespace NetStatInfoWin.Tests.Services;

[TestClass]
public sealed class UsageWindowProviderTests
{
    [TestMethod]
    public void CreateWindow_Session_UsesAppLaunchTimeAsStart()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 24, 8, 0, 0, TimeSpan.Zero));
        var provider = new UsageWindowProvider(timeProvider);

        timeProvider.UtcNow = new DateTimeOffset(2026, 5, 24, 9, 30, 0, TimeSpan.Zero);

        UsageWindow window = provider.CreateWindow(UsageRange.Session);

        Assert.AreEqual(new DateTimeOffset(2026, 5, 24, 8, 0, 0, TimeSpan.Zero).ToLocalTime(), window.StartTime);
        Assert.AreEqual(timeProvider.UtcNow.ToLocalTime(), window.EndTime);
    }

    [TestMethod]
    public void CreateWindow_LastHour_ReturnsRollingOneHourWindow()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 24, 12, 15, 0, TimeSpan.Zero));
        var provider = new UsageWindowProvider(timeProvider);

        UsageWindow window = provider.CreateWindow(UsageRange.LastHour);

        DateTimeOffset expectedEnd = timeProvider.UtcNow.ToLocalTime();
        Assert.AreEqual(expectedEnd.AddHours(-1), window.StartTime);
        Assert.AreEqual(expectedEnd, window.EndTime);
    }

    [TestMethod]
    public void CreateWindow_LastSixHours_ReturnsRollingSixHourWindow()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 24, 18, 45, 0, TimeSpan.Zero));
        var provider = new UsageWindowProvider(timeProvider);

        UsageWindow window = provider.CreateWindow(UsageRange.LastSixHours);

        DateTimeOffset expectedEnd = timeProvider.UtcNow.ToLocalTime();
        Assert.AreEqual(expectedEnd.AddHours(-6), window.StartTime);
        Assert.AreEqual(expectedEnd, window.EndTime);
    }

    [TestMethod]
    public void CreateWindow_Today_StartsAtLocalMidnight()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 24, 18, 45, 0, TimeSpan.Zero));
        var provider = new UsageWindowProvider(timeProvider);

        UsageWindow window = provider.CreateWindow(UsageRange.Today);

        DateTimeOffset expectedEnd = timeProvider.UtcNow.ToLocalTime();
        DateTimeOffset expectedStart = new(expectedEnd.Year, expectedEnd.Month, expectedEnd.Day, 0, 0, 0, expectedEnd.Offset);

        Assert.AreEqual(expectedStart, window.StartTime);
        Assert.AreEqual(expectedEnd, window.EndTime);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return UtcNow;
        }
    }
}
