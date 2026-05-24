using System.Globalization;
using NetStatInfoWin.Helpers;
using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal sealed class ProcessConnectionSummarizer(IProcessMetadataService processMetadataService, IResourceService resourceService)
{
    private readonly IProcessMetadataService _processMetadataService = processMetadataService;
    private readonly IResourceService _resourceService = resourceService;

    public IReadOnlyList<ProcessConnectionSummary> Summarize(IReadOnlyList<OwnedConnectionRecord> connections)
    {
        List<ProcessConnectionSummary> summaries = new();

        foreach (IGrouping<int, OwnedConnectionRecord> group in connections.GroupBy(connection => connection.ProcessId))
        {
            ProcessMetadata metadata = TryGetMetadata(group.Key);

            int tcpCount = group.Count(item => item.Protocol == ConnectionProtocol.Tcp);
            int udpCount = group.Count(item => item.Protocol == ConnectionProtocol.Udp);

            string protocolMix = CreateProtocolMix(tcpCount, udpCount);
            string endpointSummary = CreateEndpointSummary(group.ToList());
            string connectionCountLabel = _resourceService.Format("ProcessConnectionCountFormat", group.Count());
            string processIdentifierLabel = _resourceService.Format("ProcessIdentifierFormat", group.Key);

            summaries.Add(new ProcessConnectionSummary(
                group.Key,
                metadata.DisplayName,
                metadata.Initials,
                group.Count(),
                connectionCountLabel,
                processIdentifierLabel,
                protocolMix,
                endpointSummary));
        }

        return summaries
            .OrderByDescending(summary => summary.ActiveConnectionCount)
            .ThenBy(summary => summary.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private string CreateEndpointSummary(IReadOnlyCollection<OwnedConnectionRecord> group)
    {
        List<string> remoteEndpoints = group
            .Where(item => !string.IsNullOrWhiteSpace(item.RemoteEndpoint))
            .Select(item => item.RemoteEndpoint!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> localEndpoints = group
            .Where(item => string.IsNullOrWhiteSpace(item.RemoteEndpoint))
            .Select(item => item.LocalEndpoint)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (remoteEndpoints.Count == 0 && localEndpoints.Count == 0)
        {
            return _resourceService.GetString("ProcessNoEndpointSummary");
        }

        if (remoteEndpoints.Count == 0)
        {
            return localEndpoints.Count == 1
                ? _resourceService.Format("ProcessListeningSingleFormat", localEndpoints[0])
                : _resourceService.Format("ProcessListeningMultipleFormat", localEndpoints.Count);
        }

        if (localEndpoints.Count == 0)
        {
            return remoteEndpoints.Count == 1
                ? _resourceService.Format("ProcessSingleRemoteFormat", remoteEndpoints[0])
                : _resourceService.Format("ProcessMultipleRemoteFormat", remoteEndpoints.Count, remoteEndpoints[0]);
        }

        return _resourceService.Format("ProcessRemoteAndListenerFormat", remoteEndpoints.Count, localEndpoints.Count);
    }

    private string CreateProtocolMix(int tcpCount, int udpCount)
    {
        List<string> parts = new();
        if (tcpCount > 0)
        {
            parts.Add(string.Format(CultureInfo.CurrentCulture, "{0} {1}", _resourceService.GetString("ProtocolTcp"), tcpCount));
        }

        if (udpCount > 0)
        {
            parts.Add(string.Format(CultureInfo.CurrentCulture, "{0} {1}", _resourceService.GetString("ProtocolUdp"), udpCount));
        }

        return string.Join(" · ", parts);
    }

    private ProcessMetadata TryGetMetadata(int processId)
    {
        try
        {
            return _processMetadataService.GetProcessMetadata(processId);
        }
        catch
        {
            string fallbackName = _resourceService.Format("ProcessFallbackNameFormat", processId);
            return new ProcessMetadata(fallbackName, ValueFormatter.CreateInitials(fallbackName));
        }
    }
}
