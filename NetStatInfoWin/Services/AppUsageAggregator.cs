using NetStatInfoWin.Helpers;
using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal sealed class AppUsageAggregator(IResourceService resourceService) : IAppUsageAggregator
{
    private const string SystemUsageKey = "system:unattributed";
    private readonly IResourceService _resourceService = resourceService;

    public UsageWindowSnapshot BuildSnapshot(UsageWindow window, IReadOnlyList<ConnectionProfileUsageCapture> profileCaptures)
    {
        Dictionary<string, AggregateUsageBucket> bucketsByKey = new(StringComparer.OrdinalIgnoreCase);
        long attributedSentBytes = 0;
        long attributedReceivedBytes = 0;
        long totalSentBytes = 0;
        long totalReceivedBytes = 0;

        foreach (ConnectionProfileUsageCapture capture in profileCaptures)
        {
            totalSentBytes += capture.TotalSentBytes;
            totalReceivedBytes += capture.TotalReceivedBytes;

            foreach (AttributedAppUsageRecord usage in capture.AttributedUsages)
            {
                attributedSentBytes += usage.SentBytes;
                attributedReceivedBytes += usage.ReceivedBytes;

                if (!bucketsByKey.TryGetValue(usage.UsageKey, out AggregateUsageBucket? bucket))
                {
                    bucket = new AggregateUsageBucket(
                        usage.UsageKey,
                        ResolveDisplayName(usage),
                        usage.BucketKind);
                    bucketsByKey.Add(usage.UsageKey, bucket);
                }

                bucket.AddUsage(usage.SentBytes, usage.ReceivedBytes);
            }
        }

        long unattributedSentBytes = Math.Max(0, totalSentBytes - attributedSentBytes);
        long unattributedReceivedBytes = Math.Max(0, totalReceivedBytes - attributedReceivedBytes);
        if (unattributedSentBytes > 0 || unattributedReceivedBytes > 0)
        {
            bucketsByKey[SystemUsageKey] = new AggregateUsageBucket(
                SystemUsageKey,
                _resourceService.GetString("SystemUnattributedLabel"),
                AppUsageBucketKind.System,
                unattributedSentBytes,
                unattributedReceivedBytes);
        }

        long totalBytes = totalSentBytes + totalReceivedBytes;
        List<AppUsageSummary> summaries = bucketsByKey.Values
            .Select(bucket => CreateSummary(bucket, totalBytes))
            .OrderByDescending(summary => summary.TotalBytes)
            .ThenBy(summary => summary.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new UsageWindowSnapshot(
            window.Range,
            window.StartTime,
            window.EndTime,
            window.EndTime,
            totalSentBytes,
            totalReceivedBytes,
            summaries);
    }

    private AppUsageSummary CreateSummary(AggregateUsageBucket bucket, long overallTotalBytes)
    {
        string formattedSentBytes = ValueFormatter.FormatBytes(bucket.SentBytes);
        string formattedReceivedBytes = ValueFormatter.FormatBytes(bucket.ReceivedBytes);
        string formattedTotalBytes = ValueFormatter.FormatBytes(bucket.TotalBytes);
        string formattedShare = _resourceService.Format(
            "UsageShareFormat",
            ValueFormatter.FormatPercentage(overallTotalBytes == 0 ? 0 : (double)bucket.TotalBytes / overallTotalBytes));
        string formattedBreakdown = _resourceService.Format("UsageBreakdownFormat", formattedSentBytes, formattedReceivedBytes);

        return new AppUsageSummary(
            bucket.UsageKey,
            bucket.DisplayName,
            bucket.BucketKind,
            bucket.SentBytes,
            bucket.ReceivedBytes,
            bucket.TotalBytes,
            formattedBreakdown,
            formattedTotalBytes,
            formattedShare);
    }

    private string ResolveDisplayName(AttributedAppUsageRecord usage)
    {
        return string.IsNullOrWhiteSpace(usage.DisplayName)
            ? _resourceService.GetString("UnknownAppUsageLabel")
            : usage.DisplayName;
    }

    private sealed class AggregateUsageBucket
    {
        public AggregateUsageBucket(string usageKey, string displayName, AppUsageBucketKind bucketKind)
            : this(usageKey, displayName, bucketKind, 0, 0)
        {
        }

        public AggregateUsageBucket(
            string usageKey,
            string displayName,
            AppUsageBucketKind bucketKind,
            long sentBytes,
            long receivedBytes)
        {
            UsageKey = usageKey;
            DisplayName = displayName;
            BucketKind = bucketKind;
            SentBytes = sentBytes;
            ReceivedBytes = receivedBytes;
        }

        public string UsageKey { get; }

        public string DisplayName { get; }

        public AppUsageBucketKind BucketKind { get; }

        public long SentBytes { get; private set; }

        public long ReceivedBytes { get; private set; }

        public long TotalBytes => SentBytes + ReceivedBytes;

        public void AddUsage(long sentBytes, long receivedBytes)
        {
            SentBytes += sentBytes;
            ReceivedBytes += receivedBytes;
        }
    }
}
