using NetStatInfoWin.Helpers;

namespace NetStatInfoWin.Models;

internal sealed class AppUsageSummary : ObservableObject
{
    private string _displayName;
    private long _sentBytes;
    private long _receivedBytes;
    private long _totalBytes;
    private string _formattedBreakdownLabel;
    private string _formattedTotalBytes;
    private string _formattedShare;

    public AppUsageSummary(
        string usageKey,
        string displayName,
        AppUsageBucketKind bucketKind,
        long sentBytes,
        long receivedBytes,
        long totalBytes,
        string formattedBreakdownLabel,
        string formattedTotalBytes,
        string formattedShare)
    {
        UsageKey = usageKey;
        BucketKind = bucketKind;
        _displayName = displayName;
        _sentBytes = sentBytes;
        _receivedBytes = receivedBytes;
        _totalBytes = totalBytes;
        _formattedBreakdownLabel = formattedBreakdownLabel;
        _formattedTotalBytes = formattedTotalBytes;
        _formattedShare = formattedShare;
    }

    public string UsageKey { get; }

    public AppUsageBucketKind BucketKind { get; }

    public bool IsSystemBucket => BucketKind == AppUsageBucketKind.System;

    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    public long SentBytes
    {
        get => _sentBytes;
        private set => SetProperty(ref _sentBytes, value);
    }

    public long ReceivedBytes
    {
        get => _receivedBytes;
        private set => SetProperty(ref _receivedBytes, value);
    }

    public long TotalBytes
    {
        get => _totalBytes;
        private set => SetProperty(ref _totalBytes, value);
    }

    public string FormattedBreakdownLabel
    {
        get => _formattedBreakdownLabel;
        private set => SetProperty(ref _formattedBreakdownLabel, value);
    }

    public string FormattedTotalBytes
    {
        get => _formattedTotalBytes;
        private set => SetProperty(ref _formattedTotalBytes, value);
    }

    public string FormattedShare
    {
        get => _formattedShare;
        private set => SetProperty(ref _formattedShare, value);
    }

    public void UpdateFrom(AppUsageSummary other)
    {
        DisplayName = other.DisplayName;
        SentBytes = other.SentBytes;
        ReceivedBytes = other.ReceivedBytes;
        TotalBytes = other.TotalBytes;
        FormattedBreakdownLabel = other.FormattedBreakdownLabel;
        FormattedTotalBytes = other.FormattedTotalBytes;
        FormattedShare = other.FormattedShare;
    }
}
