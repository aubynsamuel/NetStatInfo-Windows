namespace NetStatInfoWin.Models;

public sealed class AdapterUsageSummary(
    string name,
    string subtitle,
    long sentBytes,
    long receivedBytes,
    string sentLabel,
    string receivedLabel,
    string formattedSentBytes,
    string formattedReceivedBytes,
    string formattedTotalBytes)
{
    public string Name { get; } = name;

    public string Subtitle { get; } = subtitle;

    public long SentBytes { get; } = sentBytes;

    public long ReceivedBytes { get; } = receivedBytes;

    public string SentLabel { get; } = sentLabel;

    public string ReceivedLabel { get; } = receivedLabel;

    public string FormattedSentBytes { get; } = formattedSentBytes;

    public string FormattedReceivedBytes { get; } = formattedReceivedBytes;

    public string FormattedTotalBytes { get; } = formattedTotalBytes;
}
