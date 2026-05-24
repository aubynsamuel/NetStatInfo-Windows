namespace NetStatInfoWin.Models;

internal sealed class ConnectionProfileUsageCapture(
    string profileKey,
    long totalSentBytes,
    long totalReceivedBytes,
    IReadOnlyList<AttributedAppUsageRecord> attributedUsages)
{
    public string ProfileKey { get; } = profileKey;

    public long TotalSentBytes { get; } = totalSentBytes;

    public long TotalReceivedBytes { get; } = totalReceivedBytes;

    public long TotalBytes { get; } = totalSentBytes + totalReceivedBytes;

    public IReadOnlyList<AttributedAppUsageRecord> AttributedUsages { get; } = attributedUsages;
}
