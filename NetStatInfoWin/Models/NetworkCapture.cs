using System.Net.NetworkInformation;

namespace NetStatInfoWin.Models;

internal sealed class NetworkCapture(
    DateTimeOffset timestamp,
    IReadOnlyList<AdapterCounterSnapshot> adapters,
    IReadOnlyList<ProcessConnectionSummary> activeProcesses)
{

    public DateTimeOffset Timestamp { get; } = timestamp;

    public IReadOnlyList<AdapterCounterSnapshot> Adapters { get; } = adapters;

    public IReadOnlyList<ProcessConnectionSummary> ActiveProcesses { get; } = activeProcesses;
}

internal sealed class AdapterCounterSnapshot(
    string id,
    string name,
    NetworkInterfaceType interfaceType,
    OperationalStatus operationalStatus,
    long sentBytes,
    long receivedBytes)
{

    public string Id { get; } = id;

    public string Name { get; } = name;

    public NetworkInterfaceType InterfaceType { get; } = interfaceType;

    public OperationalStatus OperationalStatus { get; } = operationalStatus;

    public long SentBytes { get; } = sentBytes;

    public long ReceivedBytes { get; } = receivedBytes;
}

internal sealed class OwnedConnectionRecord(int processId, ConnectionProtocol protocol, string localEndpoint, string? remoteEndpoint)
{
    public int ProcessId { get; } = processId;

    public ConnectionProtocol Protocol { get; } = protocol;

    public string LocalEndpoint { get; } = localEndpoint;

    public string? RemoteEndpoint { get; } = remoteEndpoint;
}

internal sealed class ProcessMetadata(string displayName, string initials)
{
    public string DisplayName { get; } = displayName;

    public string Initials { get; } = initials;
}

internal enum ConnectionProtocol
{
    Tcp,
    Udp,
}
