using NetStatInfoWin.Models;
using NetStatInfoWin.Services;
using NetStatInfoWin.Tests.TestSupport;

namespace NetStatInfoWin.Tests.Services;

[TestClass]
public sealed class AppUsageAggregatorTests
{
    private static readonly UsageWindow Window = new(
        UsageRange.LastHour,
        new DateTimeOffset(2026, 5, 24, 11, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero));

    [TestMethod]
    public void BuildSnapshot_MergesMatchingAppsAcrossProfiles()
    {
        var aggregator = new AppUsageAggregator(new TestResourceService());

        IReadOnlyList<ConnectionProfileUsageCapture> captures =
        [
            new ConnectionProfileUsageCapture(
                "wifi",
                500,
                900,
                [
                    new AttributedAppUsageRecord("id:browser", "Browser", "browser", AppUsageBucketKind.Application, 300, 500),
                ]),
            new ConnectionProfileUsageCapture(
                "ethernet",
                200,
                400,
                [
                    new AttributedAppUsageRecord("id:browser", "Browser", "browser", AppUsageBucketKind.Application, 100, 250),
                    new AttributedAppUsageRecord("id:chat", "Chat", "chat", AppUsageBucketKind.Application, 100, 150),
                ]),
        ];

        UsageWindowSnapshot snapshot = aggregator.BuildSnapshot(Window, captures);

        AppUsageSummary browser = snapshot.AppRows.Single(item => item.UsageKey == "id:browser");
        Assert.AreEqual(400L, browser.SentBytes);
        Assert.AreEqual(750L, browser.ReceivedBytes);
        Assert.AreEqual(1150L, browser.TotalBytes);
    }

    [TestMethod]
    public void BuildSnapshot_ResidualTraffic_AddsSystemBucket()
    {
        var aggregator = new AppUsageAggregator(new TestResourceService());

        IReadOnlyList<ConnectionProfileUsageCapture> captures =
        [
            new ConnectionProfileUsageCapture(
                "wifi",
                1000,
                500,
                [
                    new AttributedAppUsageRecord("id:browser", "Browser", "browser", AppUsageBucketKind.Application, 600, 300),
                ]),
        ];

        UsageWindowSnapshot snapshot = aggregator.BuildSnapshot(Window, captures);

        AppUsageSummary systemBucket = snapshot.AppRows.Single(item => item.IsSystemBucket);
        Assert.AreEqual("System / unattributed", systemBucket.DisplayName);
        Assert.AreEqual(400L, systemBucket.SentBytes);
        Assert.AreEqual(200L, systemBucket.ReceivedBytes);
    }

    [TestMethod]
    public void BuildSnapshot_SortsByTotalBytesThenName()
    {
        var aggregator = new AppUsageAggregator(new TestResourceService());

        IReadOnlyList<ConnectionProfileUsageCapture> captures =
        [
            new ConnectionProfileUsageCapture(
                "wifi",
                600,
                600,
                [
                    new AttributedAppUsageRecord("id:zeta", "Zeta", "zeta", AppUsageBucketKind.Application, 300, 300),
                    new AttributedAppUsageRecord("id:alpha", "Alpha", "alpha", AppUsageBucketKind.Application, 300, 300),
                    new AttributedAppUsageRecord("id:browser", "Browser", "browser", AppUsageBucketKind.Application, 400, 400),
                ]),
        ];

        UsageWindowSnapshot snapshot = aggregator.BuildSnapshot(Window, captures);

        CollectionAssert.AreEqual(
            new[] { "Browser", "Alpha", "Zeta" },
            snapshot.AppRows.Select(item => item.DisplayName).ToArray());
    }
}
