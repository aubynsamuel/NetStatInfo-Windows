using NetStatInfoWin.Helpers;
using NetStatInfoWin.Models;
using NetStatInfoWin.Services;

namespace NetStatInfoWin.Tests.Services;

[TestClass]
public sealed class SessionUsageAggregatorTests
{
    private static readonly DateTimeOffset SnapshotTime = new(2026, 5, 24, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void BuildSessionSnapshot_FirstSnapshot_EstablishesBaselineWithZeroTotals()
    {
        var aggregator = new SessionUsageAggregator(new FakeResourceService());
        var capture = new NetworkCapture(
            SnapshotTime,
            [
                new AdapterCounterSnapshot("wifi", "Wi-Fi", System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211, System.Net.NetworkInformation.OperationalStatus.Up, 1024, 2048),
            ],
            []);

        SessionNetworkSnapshot snapshot = aggregator.BuildSessionSnapshot(capture);

        Assert.AreEqual(0L, snapshot.TotalSentBytes);
        Assert.AreEqual(0L, snapshot.TotalReceivedBytes);
        Assert.AreEqual("0 B", snapshot.ActiveAdapters[0].FormattedTotalBytes);
    }

    [TestMethod]
    public void BuildSessionSnapshot_LaterSnapshot_ComputesCorrectDeltas()
    {
        var aggregator = new SessionUsageAggregator(new FakeResourceService());

        aggregator.BuildSessionSnapshot(new NetworkCapture(
            SnapshotTime,
            [
                new AdapterCounterSnapshot("wifi", "Wi-Fi", System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211, System.Net.NetworkInformation.OperationalStatus.Up, 1000, 2000),
            ],
            []));

        SessionNetworkSnapshot snapshot = aggregator.BuildSessionSnapshot(new NetworkCapture(
            SnapshotTime.AddMinutes(1),
            [
                new AdapterCounterSnapshot("wifi", "Wi-Fi", System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211, System.Net.NetworkInformation.OperationalStatus.Up, 2500, 5000),
            ],
            []));

        Assert.AreEqual(1500L, snapshot.TotalSentBytes);
        Assert.AreEqual(3000L, snapshot.TotalReceivedBytes);
        Assert.AreEqual("4.39 KB", snapshot.ActiveAdapters[0].FormattedTotalBytes);
    }

    [TestMethod]
    public void BuildSessionSnapshot_ResetCounters_DoesNotProduceNegativeTotals()
    {
        var aggregator = new SessionUsageAggregator(new FakeResourceService());

        aggregator.BuildSessionSnapshot(new NetworkCapture(
            SnapshotTime,
            [
                new AdapterCounterSnapshot("wifi", "Wi-Fi", System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211, System.Net.NetworkInformation.OperationalStatus.Up, 5000, 5000),
            ],
            []));

        SessionNetworkSnapshot snapshot = aggregator.BuildSessionSnapshot(new NetworkCapture(
            SnapshotTime.AddMinutes(1),
            [
                new AdapterCounterSnapshot("wifi", "Wi-Fi", System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211, System.Net.NetworkInformation.OperationalStatus.Up, 100, 200),
            ],
            []));

        Assert.AreEqual(0L, snapshot.TotalSentBytes);
        Assert.AreEqual(0L, snapshot.TotalReceivedBytes);
    }

    [TestMethod]
    public void BuildSessionSnapshot_NewAdapterAfterBaseline_StartsAtZero()
    {
        var aggregator = new SessionUsageAggregator(new FakeResourceService());

        aggregator.BuildSessionSnapshot(new NetworkCapture(
            SnapshotTime,
            [
                new AdapterCounterSnapshot("wifi", "Wi-Fi", System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211, System.Net.NetworkInformation.OperationalStatus.Up, 2000, 4000),
            ],
            []));

        SessionNetworkSnapshot snapshot = aggregator.BuildSessionSnapshot(new NetworkCapture(
            SnapshotTime.AddMinutes(1),
            [
                new AdapterCounterSnapshot("wifi", "Wi-Fi", System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211, System.Net.NetworkInformation.OperationalStatus.Up, 2600, 4200),
                new AdapterCounterSnapshot("ethernet", "Ethernet", System.Net.NetworkInformation.NetworkInterfaceType.Ethernet, System.Net.NetworkInformation.OperationalStatus.Up, 9999, 9999),
            ],
            []));

        Assert.AreEqual(600L, snapshot.TotalSentBytes);
        Assert.AreEqual(200L, snapshot.TotalReceivedBytes);
        Assert.AreEqual("0 B", snapshot.ActiveAdapters.Single(item => item.Name == "Ethernet").FormattedTotalBytes);
    }

    private sealed class FakeResourceService : IResourceService
    {
        public string Format(string key, params object[] arguments)
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, GetString(key), arguments);
        }

        public string GetString(string key)
        {
            return key switch
            {
                "AdapterTypeWifi" => "Wi-Fi",
                "AdapterTypeEthernet" => "Ethernet",
                "AdapterTypeVpn" => "VPN",
                "AdapterTypeOther" => "Network",
                "AdapterStatusUp" => "Active",
                "AdapterStatusInactive" => "Inactive",
                "SentShortLabel" => "Sent",
                "ReceivedShortLabel" => "Received",
                _ => key,
            };
        }
    }
}
