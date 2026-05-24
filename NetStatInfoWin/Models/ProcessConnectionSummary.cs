namespace NetStatInfoWin.Models;

public sealed class ProcessConnectionSummary(
    int processId,
    string displayName,
    string initials,
    int activeConnectionCount,
    string activeConnectionCountLabel,
    string processIdentifierLabel,
    string protocolMix,
    string remoteEndpointSummary)
{
    public int ProcessId { get; } = processId;

    public string DisplayName { get; } = displayName;

    public string Initials { get; } = initials;

    public int ActiveConnectionCount { get; } = activeConnectionCount;

    public string ActiveConnectionCountLabel { get; } = activeConnectionCountLabel;

    public string ProcessIdentifierLabel { get; } = processIdentifierLabel;

    public string ProtocolMix { get; } = protocolMix;

    public string RemoteEndpointSummary { get; } = remoteEndpointSummary;
}
