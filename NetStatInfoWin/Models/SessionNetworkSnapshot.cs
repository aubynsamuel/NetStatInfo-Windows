namespace NetStatInfoWin.Models;

internal sealed class SessionNetworkSnapshot(
    DateTimeOffset timestamp,
    DateTimeOffset sessionStartedAt,
    long totalSentBytes,
    long totalReceivedBytes,
    IReadOnlyList<AdapterUsageSummary> activeAdapters,
    IReadOnlyList<ProcessConnectionSummary> activeProcesses)
{

    public DateTimeOffset Timestamp { get; } = timestamp;

    public DateTimeOffset SessionStartedAt { get; } = sessionStartedAt;

    public long TotalSentBytes { get; } = totalSentBytes;

    public long TotalReceivedBytes { get; } = totalReceivedBytes;

    public IReadOnlyList<AdapterUsageSummary> ActiveAdapters { get; } = activeAdapters;

    public IReadOnlyList<ProcessConnectionSummary> ActiveProcesses { get; } = activeProcesses;
}
