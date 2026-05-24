using NetStatInfoWin.Helpers;

namespace NetStatInfoWin.Models;

internal sealed partial class AdapterUsageSummary(
    string name,
    string subtitle,
    long sentBytes,
    long receivedBytes,
    string sentLabel,
    string receivedLabel,
    string formattedSentBytes,
    string formattedReceivedBytes,
    string formattedTotalBytes) : ObservableObject
{
    private string _subtitle = subtitle;
    private long _sentBytes = sentBytes;
    private long _receivedBytes = receivedBytes;
    private string _formattedSentBytes = formattedSentBytes;
    private string _formattedReceivedBytes = formattedReceivedBytes;
    private string _formattedTotalBytes = formattedTotalBytes;

    public string Name { get; } = name;

    public string Subtitle
    {
        get => _subtitle;
        private set => SetProperty(ref _subtitle, value);
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

    public string SentLabel { get; } = sentLabel;

    public string ReceivedLabel { get; } = receivedLabel;

    public string FormattedSentBytes
    {
        get => _formattedSentBytes;
        private set => SetProperty(ref _formattedSentBytes, value);
    }

    public string FormattedReceivedBytes
    {
        get => _formattedReceivedBytes;
        private set => SetProperty(ref _formattedReceivedBytes, value);
    }

    public string FormattedTotalBytes
    {
        get => _formattedTotalBytes;
        private set => SetProperty(ref _formattedTotalBytes, value);
    }

    public void UpdateFrom(AdapterUsageSummary other)
    {
        Subtitle = other.Subtitle;
        SentBytes = other.SentBytes;
        ReceivedBytes = other.ReceivedBytes;
        FormattedSentBytes = other.FormattedSentBytes;
        FormattedReceivedBytes = other.FormattedReceivedBytes;
        FormattedTotalBytes = other.FormattedTotalBytes;
    }
}
