using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using NetStatInfoWin.Helpers;
using NetStatInfoWin.Models;
using NetStatInfoWin.Services;

namespace NetStatInfoWin.ViewModels;

internal sealed partial class MainViewModel : ObservableObject
{
    private readonly INetworkSnapshotService _networkSnapshotService;
    private readonly ISessionUsageAggregator _sessionUsageAggregator;
    private readonly IResourceService _resourceService;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private bool _isLoading = true;
    private bool _hasLoadedAtLeastOnce;
    private string _errorMessage = string.Empty;
    private string _formattedTotalUsage = "0 B";
    private string _formattedSentUsage = "0 B";
    private string _formattedReceivedUsage = "0 B";
    private string _lastUpdatedLabel = string.Empty;
    private string _sessionStartedLabel = string.Empty;
    private CancellationTokenSource? _pollingCancellationTokenSource;

    public MainViewModel(
        INetworkSnapshotService networkSnapshotService,
        ISessionUsageAggregator sessionUsageAggregator,
        IResourceService resourceService)
    {
        _networkSnapshotService = networkSnapshotService;
        _sessionUsageAggregator = sessionUsageAggregator;
        _resourceService = resourceService;

        ActiveAdapters = new ObservableCollection<AdapterUsageSummary>();
        ActiveProcesses = new ObservableCollection<ProcessConnectionSummary>();

        RefreshAutomationName = _resourceService.GetString("RefreshButtonAutomationName");
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        StartPollingCommand = new RelayCommand(StartPolling);
        StopPollingCommand = new RelayCommand(StopPolling);
    }

    public ObservableCollection<AdapterUsageSummary> ActiveAdapters { get; }

    public ObservableCollection<ProcessConnectionSummary> ActiveProcesses { get; }

    public AsyncRelayCommand RefreshCommand { get; }

    public RelayCommand StartPollingCommand { get; }

    public RelayCommand StopPollingCommand { get; }

    public string RefreshAutomationName { get; }

    public string FormattedTotalUsage
    {
        get => _formattedTotalUsage;
        private set => SetProperty(ref _formattedTotalUsage, value);
    }

    public string FormattedSentUsage
    {
        get => _formattedSentUsage;
        private set => SetProperty(ref _formattedSentUsage, value);
    }

    public string FormattedReceivedUsage
    {
        get => _formattedReceivedUsage;
        private set => SetProperty(ref _formattedReceivedUsage, value);
    }

    public string LastUpdatedLabel
    {
        get => _lastUpdatedLabel;
        private set => SetProperty(ref _lastUpdatedLabel, value);
    }

    public string SessionStartedLabel
    {
        get => _sessionStartedLabel;
        private set => SetProperty(ref _sessionStartedLabel, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ErrorVisibility));
            }
        }
    }

    public bool IsLoadingFirstLoad => _isLoading && !_hasLoadedAtLeastOnce;

    public bool IsContentVisible => !_isLoading || _hasLoadedAtLeastOnce;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasAdapterData => ActiveAdapters.Count > 0;

    public bool HasNoAdapterData => !HasAdapterData;

    public bool HasProcessData => ActiveProcesses.Count > 0;

    public bool HasNoProcessData => !HasProcessData;

    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;

    public Visibility LoadingVisibility => IsLoadingFirstLoad ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ContentVisibility => IsContentVisible ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AdapterSectionVisibility => HasAdapterData ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AdapterEmptyVisibility => HasNoAdapterData ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ProcessEmptyVisibility => HasNoProcessData ? Visibility.Visible : Visibility.Collapsed;

    public Task RefreshAsync()
    {
        return RefreshAsync(preserveProcessOrder: false);
    }

    private async Task RefreshAsync(bool preserveProcessOrder)
    {
        if (!await _refreshLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            _isLoading = true;
            NotifyVisualStateChanged();

            NetworkCapture capture = await _networkSnapshotService.CaptureSnapshotAsync(CancellationToken.None);
            SessionNetworkSnapshot sessionSnapshot = _sessionUsageAggregator.BuildSessionSnapshot(capture);

            FormattedTotalUsage = ValueFormatter.FormatBytes(sessionSnapshot.TotalSentBytes + sessionSnapshot.TotalReceivedBytes);
            FormattedSentUsage = ValueFormatter.FormatBytes(sessionSnapshot.TotalSentBytes);
            FormattedReceivedUsage = ValueFormatter.FormatBytes(sessionSnapshot.TotalReceivedBytes);
            LastUpdatedLabel = _resourceService.Format("LastUpdatedFormat", ValueFormatter.FormatTime(sessionSnapshot.Timestamp));
            SessionStartedLabel = _resourceService.Format("SessionStartedFormat", ValueFormatter.FormatTime(sessionSnapshot.SessionStartedAt));

            SyncCollection(
                ActiveAdapters,
                sessionSnapshot.ActiveAdapters,
                static item => item.Name,
                static (current, incoming) => current.UpdateFrom(incoming));

            SyncCollection(
                ActiveProcesses,
                sessionSnapshot.ActiveProcesses,
                static item => item.ProcessId,
                static (current, incoming) => current.UpdateFrom(incoming),
                reorderExistingItems: !preserveProcessOrder);

            ErrorMessage = string.Empty;
            _hasLoadedAtLeastOnce = true;
        }
        catch
        {
            ErrorMessage = _resourceService.GetString("GenericRefreshError");
        }
        finally
        {
            _isLoading = false;
            NotifyVisualStateChanged();
            _refreshLock.Release();
        }
    }

    public void StartPolling()
    {
        if (_pollingCancellationTokenSource is not null)
        {
            return;
        }

        _pollingCancellationTokenSource = new CancellationTokenSource();
        _ = PollUntilCancelledAsync(_pollingCancellationTokenSource.Token);
    }

    public void StopPolling()
    {
        _pollingCancellationTokenSource?.Cancel();
        _pollingCancellationTokenSource?.Dispose();
        _pollingCancellationTokenSource = null;
    }

    private static void SyncCollection<T, TKey>(
        ObservableCollection<T> collection,
        IReadOnlyList<T> values,
        Func<T, TKey> keySelector,
        Action<T, T> updateExisting,
        bool reorderExistingItems = true)
        where TKey : notnull
    {
        Dictionary<TKey, T> existingByKey = collection.ToDictionary(keySelector);

        for (int index = 0; index < values.Count; index++)
        {
            T incoming = values[index];
            TKey key = keySelector(incoming);

            if (existingByKey.TryGetValue(key, out T? existing))
            {
                updateExisting(existing, incoming);

                if (reorderExistingItems)
                {
                    int currentIndex = collection.IndexOf(existing);
                    if (currentIndex != index)
                    {
                        collection.Move(currentIndex, index);
                    }
                }

                existingByKey.Remove(key);
                continue;
            }

            collection.Insert(index, incoming);
        }

        foreach (T leftover in existingByKey.Values)
        {
            collection.Remove(leftover);
        }
    }

    private async Task PollUntilCancelledAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(2));

        await RefreshAsync(preserveProcessOrder: true);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await RefreshAsync(preserveProcessOrder: true);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void NotifyVisualStateChanged()
    {
        OnPropertyChanged(nameof(IsLoadingFirstLoad));
        OnPropertyChanged(nameof(IsContentVisible));
        OnPropertyChanged(nameof(HasAdapterData));
        OnPropertyChanged(nameof(HasNoAdapterData));
        OnPropertyChanged(nameof(HasProcessData));
        OnPropertyChanged(nameof(HasNoProcessData));
        OnPropertyChanged(nameof(LoadingVisibility));
        OnPropertyChanged(nameof(ContentVisibility));
        OnPropertyChanged(nameof(AdapterSectionVisibility));
        OnPropertyChanged(nameof(AdapterEmptyVisibility));
        OnPropertyChanged(nameof(ProcessEmptyVisibility));
    }
}
