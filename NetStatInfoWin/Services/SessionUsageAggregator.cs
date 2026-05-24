using System.Globalization;
using System.Net.NetworkInformation;
using NetStatInfoWin.Helpers;
using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal sealed class SessionUsageAggregator(IResourceService resourceService) : ISessionUsageAggregator
{
    private readonly Dictionary<string, AdapterCounterSnapshot> _baselineByAdapterId = new(StringComparer.OrdinalIgnoreCase);
    private readonly IResourceService _resourceService = resourceService;
    private readonly DateTimeOffset _sessionStartedAt = DateTimeOffset.Now;

    public SessionNetworkSnapshot BuildSessionSnapshot(NetworkCapture capture)
    {
        List<AdapterUsageSummary> adapterSummaries = new();
        long totalSent = 0;
        long totalReceived = 0;

        foreach (AdapterCounterSnapshot adapter in capture.Adapters.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            AdapterCounterSnapshot baseline = GetOrUpdateBaseline(adapter);
            long sentDelta = CalculateDelta(adapter.SentBytes, baseline.SentBytes);
            long receivedDelta = CalculateDelta(adapter.ReceivedBytes, baseline.ReceivedBytes);

            totalSent += sentDelta;
            totalReceived += receivedDelta;

            string adapterTypeLabel = GetAdapterTypeLabel(adapter.InterfaceType);
            string statusLabel = GetStatusLabel(adapter.OperationalStatus);
            string subtitle = string.Format(CultureInfo.CurrentCulture, "{0} · {1}", adapterTypeLabel, statusLabel);

            adapterSummaries.Add(new AdapterUsageSummary(
                adapter.Name,
                subtitle,
                sentDelta,
                receivedDelta,
                _resourceService.GetString("SentShortLabel"),
                _resourceService.GetString("ReceivedShortLabel"),
                ValueFormatter.FormatBytes(sentDelta),
                ValueFormatter.FormatBytes(receivedDelta),
                ValueFormatter.FormatBytes(sentDelta + receivedDelta)));
        }

        return new SessionNetworkSnapshot(
            capture.Timestamp,
            _sessionStartedAt,
            totalSent,
            totalReceived,
            adapterSummaries,
            capture.ActiveProcesses);
    }

    private static long CalculateDelta(long current, long baseline)
    {
        return current < baseline ? 0 : current - baseline;
    }

    private AdapterCounterSnapshot GetOrUpdateBaseline(AdapterCounterSnapshot adapter)
    {
        if (!_baselineByAdapterId.TryGetValue(adapter.Id, out AdapterCounterSnapshot? baseline))
        {
            _baselineByAdapterId[adapter.Id] = adapter;
            return adapter;
        }

        if (adapter.SentBytes < baseline.SentBytes || adapter.ReceivedBytes < baseline.ReceivedBytes)
        {
            _baselineByAdapterId[adapter.Id] = adapter;
            return adapter;
        }

        return baseline;
    }

    private string GetAdapterTypeLabel(NetworkInterfaceType interfaceType)
    {
        return interfaceType switch
        {
            NetworkInterfaceType.Wireless80211 => _resourceService.GetString("AdapterTypeWifi"),
            NetworkInterfaceType.Ethernet => _resourceService.GetString("AdapterTypeEthernet"),
            NetworkInterfaceType.GigabitEthernet => _resourceService.GetString("AdapterTypeEthernet"),
            NetworkInterfaceType.FastEthernetFx => _resourceService.GetString("AdapterTypeEthernet"),
            NetworkInterfaceType.FastEthernetT => _resourceService.GetString("AdapterTypeEthernet"),
            NetworkInterfaceType.Ppp => _resourceService.GetString("AdapterTypeVpn"),
            _ => _resourceService.GetString("AdapterTypeOther"),
        };
    }

    private string GetStatusLabel(OperationalStatus status)
    {
        return status == OperationalStatus.Up
            ? _resourceService.GetString("AdapterStatusUp")
            : _resourceService.GetString("AdapterStatusInactive");
    }
}
