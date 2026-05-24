using System.Net.NetworkInformation;
using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal sealed class NetworkSnapshotService(
    OwnedConnectionTableReader ownedConnectionTableReader,
    ProcessConnectionSummarizer processConnectionSummarizer) : INetworkSnapshotService
{
    private readonly OwnedConnectionTableReader _ownedConnectionTableReader = ownedConnectionTableReader;
    private readonly ProcessConnectionSummarizer _processConnectionSummarizer = processConnectionSummarizer;

    public Task<NetworkCapture> CaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<AdapterCounterSnapshot> adapters = new();

                foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                        string.IsNullOrWhiteSpace(networkInterface.Name))
                    {
                        continue;
                    }

                    IPInterfaceStatistics statistics;
                    try
                    {
                        statistics = networkInterface.GetIPStatistics();
                    }
                    catch (NetworkInformationException)
                    {
                        continue;
                    }

                    if (networkInterface.OperationalStatus != OperationalStatus.Up &&
                        statistics.BytesSent == 0 &&
                        statistics.BytesReceived == 0)
                    {
                        continue;
                    }

                    adapters.Add(new AdapterCounterSnapshot(
                        networkInterface.Id,
                        networkInterface.Name,
                        networkInterface.NetworkInterfaceType,
                        networkInterface.OperationalStatus,
                        statistics.BytesSent,
                        statistics.BytesReceived));
                }

                IReadOnlyList<OwnedConnectionRecord> ownedConnections = _ownedConnectionTableReader.ReadAllConnections();
                IReadOnlyList<ProcessConnectionSummary> processSummaries = _processConnectionSummarizer.Summarize(ownedConnections);

                return new NetworkCapture(DateTimeOffset.Now, adapters, processSummaries);
            },
            cancellationToken);
    }
}
