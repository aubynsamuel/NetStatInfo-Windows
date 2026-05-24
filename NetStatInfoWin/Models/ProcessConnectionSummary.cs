using NetStatInfoWin.Helpers;

namespace NetStatInfoWin.Models;

internal sealed partial class ProcessConnectionSummary(
    int processId,
    string displayName,
    string initials,
    int activeConnectionCount,
    string activeConnectionCountLabel,
    string processIdentifierLabel,
    string protocolMix,
    string remoteEndpointSummary) : ObservableObject
{
    private string _displayName = displayName;
    private string _initials = initials;
    private int _activeConnectionCount = activeConnectionCount;
    private string _activeConnectionCountLabel = activeConnectionCountLabel;
    private string _processIdentifierLabel = processIdentifierLabel;
    private string _protocolMix = protocolMix;
    private string _remoteEndpointSummary = remoteEndpointSummary;

    public int ProcessId { get; } = processId;

    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    public string Initials
    {
        get => _initials;
        private set => SetProperty(ref _initials, value);
    }

    public int ActiveConnectionCount
    {
        get => _activeConnectionCount;
        private set => SetProperty(ref _activeConnectionCount, value);
    }

    public string ActiveConnectionCountLabel
    {
        get => _activeConnectionCountLabel;
        private set => SetProperty(ref _activeConnectionCountLabel, value);
    }

    public string ProcessIdentifierLabel
    {
        get => _processIdentifierLabel;
        private set => SetProperty(ref _processIdentifierLabel, value);
    }

    public string ProtocolMix
    {
        get => _protocolMix;
        private set => SetProperty(ref _protocolMix, value);
    }

    public string RemoteEndpointSummary
    {
        get => _remoteEndpointSummary;
        private set => SetProperty(ref _remoteEndpointSummary, value);
    }

    public void UpdateFrom(ProcessConnectionSummary other)
    {
        DisplayName = other.DisplayName;
        Initials = other.Initials;
        ActiveConnectionCount = other.ActiveConnectionCount;
        ActiveConnectionCountLabel = other.ActiveConnectionCountLabel;
        ProcessIdentifierLabel = other.ProcessIdentifierLabel;
        ProtocolMix = other.ProtocolMix;
        RemoteEndpointSummary = other.RemoteEndpointSummary;
    }
}
