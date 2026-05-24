namespace NetStatInfoWin.Models;

internal sealed class UsageWindowSnapshot(
    UsageRange selectedRange,
    DateTimeOffset windowStart,
    DateTimeOffset windowEnd,
    DateTimeOffset refreshedAt,
    long totalSentBytes,
    long totalReceivedBytes,
    IReadOnlyList<AppUsageSummary> appRows)
{
    public UsageRange SelectedRange { get; } = selectedRange;

    public DateTimeOffset WindowStart { get; } = windowStart;

    public DateTimeOffset WindowEnd { get; } = windowEnd;

    public DateTimeOffset RefreshedAt { get; } = refreshedAt;

    public long TotalSentBytes { get; } = totalSentBytes;

    public long TotalReceivedBytes { get; } = totalReceivedBytes;

    public long TotalBytes { get; } = totalSentBytes + totalReceivedBytes;

    public IReadOnlyList<AppUsageSummary> AppRows { get; } = appRows;
}
