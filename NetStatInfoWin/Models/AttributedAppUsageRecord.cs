namespace NetStatInfoWin.Models;

internal sealed class AttributedAppUsageRecord(
    string usageKey,
    string displayName,
    string? attributionId,
    AppUsageBucketKind bucketKind,
    long sentBytes,
    long receivedBytes)
{
    public string UsageKey { get; } = usageKey;

    public string DisplayName { get; } = displayName;

    public string? AttributionId { get; } = attributionId;

    public AppUsageBucketKind BucketKind { get; } = bucketKind;

    public long SentBytes { get; } = sentBytes;

    public long ReceivedBytes { get; } = receivedBytes;

    public long TotalBytes { get; } = sentBytes + receivedBytes;
}
