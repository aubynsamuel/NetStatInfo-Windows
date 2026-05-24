using System.Collections.ObjectModel;
using NetStatInfoWin.Helpers;
using NetStatInfoWin.Models;
using NetStatInfoWin.Services;

namespace NetStatInfoWin.ViewModels;

internal sealed class MainViewModel : ObservableObject
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private readonly IAttributedAppUsageService _attributedAppUsageService;
    private readonly IUsageWindowProvider _usageWindowProvider;
    private readonly IAppUsageAggregator _appUsageAggregator;
    private readonly IDelayProvider _delayProvider;
    private readonly IResourceService _resourceService;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private UsageRange _selectedRange = UsageRange.Session;
    private bool _isLoading = true;
    private bool _hasLoadedAtLeastOnce;
    private string _errorMessage = string.Empty;
    private string _formattedTotalUsage = "0 B";
    private string _formattedSentUsage = "0 B";
    private string _formattedReceivedUsage = "0 B";
    private string _selectedRangeDisplayLabel = string.Empty;
    private string _windowLabel = string.Empty;
    private string _lastUpdatedLabel = string.Empty;
    private CancellationTokenSource? _pollingCancellationTokenSource;

    public MainViewModel(
        IAttributedAppUsageService attributedAppUsageService,
        IUsageWindowProvider usageWindowProvider,
        IAppUsageAggregator appUsageAggregator,
        IDelayProvider delayProvider,
        IResourceService resourceService)
    {
        _attributedAppUsageService = attributedAppUsageService;
        _usageWindowProvider = usageWindowProvider;
        _appUsageAggregator = appUsageAggregator;
        _delayProvider = delayProvider;
        _resourceService = resourceService;

        AppUsages = [];
        RefreshAutomationName = _resourceService.GetString("RefreshButtonAutomationName");
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        StartPollingCommand = new RelayCommand(StartPolling);
        StopPollingCommand = new RelayCommand(StopPolling);

        UpdateSelectedRangePresentation();
    }

    public ObservableCollection<AppUsageSummary> AppUsages { get; }

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

    public string SelectedRangeDisplayLabel
    {
        get => _selectedRangeDisplayLabel;
        private set => SetProperty(ref _selectedRangeDisplayLabel, value);
    }

    public string WindowLabel
    {
        get => _windowLabel;
        private set => SetProperty(ref _windowLabel, value);
    }

    public string LastUpdatedLabel
    {
        get => _lastUpdatedLabel;
        private set => SetProperty(ref _lastUpdatedLabel, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool IsLoadingFirstLoad => _isLoading && !_hasLoadedAtLeastOnce;

    public bool IsContentVisible => !_isLoading || _hasLoadedAtLeastOnce;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasUsageData => AppUsages.Count > 0;

    public bool HasNoUsageData => !HasUsageData;

    public bool IsSessionSelected => _selectedRange == UsageRange.Session;

    public bool IsLastHourSelected => _selectedRange == UsageRange.LastHour;

    public bool IsLastSixHoursSelected => _selectedRange == UsageRange.LastSixHours;

    public bool IsTodaySelected => _selectedRange == UsageRange.Today;

    public Task RefreshAsync()
    {
        return RefreshAsync(reorderAppRows: true);
    }

    public async Task SelectRangeAsync(UsageRange range)
    {
        if (_selectedRange == range)
        {
            return;
        }

        _selectedRange = range;
        UpdateSelectedRangePresentation();
        await RefreshAsync(reorderAppRows: true);
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

    private async Task RefreshAsync(bool reorderAppRows)
    {
        await _refreshLock.WaitAsync();

        try
        {
            _isLoading = true;
            NotifyVisualStateChanged();

            UsageWindow window = _usageWindowProvider.CreateWindow(_selectedRange);
            IReadOnlyList<ConnectionProfileUsageCapture> profileCaptures =
                await _attributedAppUsageService.GetUsageByProfileAsync(window, CancellationToken.None);

            UsageWindowSnapshot snapshot = _appUsageAggregator.BuildSnapshot(window, profileCaptures);

            FormattedTotalUsage = ValueFormatter.FormatBytes(snapshot.TotalBytes);
            FormattedSentUsage = ValueFormatter.FormatBytes(snapshot.TotalSentBytes);
            FormattedReceivedUsage = ValueFormatter.FormatBytes(snapshot.TotalReceivedBytes);
            SelectedRangeDisplayLabel = GetRangeDisplayLabel(snapshot.SelectedRange);
            WindowLabel = BuildWindowLabel(snapshot);
            LastUpdatedLabel = _resourceService.Format("LastUpdatedFormat", ValueFormatter.FormatTime(snapshot.RefreshedAt));

            SyncCollection(
                AppUsages,
                snapshot.AppRows,
                static item => item.UsageKey,
                static (current, incoming) => current.UpdateFrom(incoming),
                reorderExistingItems: reorderAppRows);

            ErrorMessage = string.Empty;
            _hasLoadedAtLeastOnce = true;
        }
        catch (UsageLoadException ex)
        {
            ErrorMessage = ex.Kind switch
            {
                UsageLoadFailureKind.Unsupported => _resourceService.GetString("UsageUnsupportedError"),
                _ => _resourceService.GetString("GenericRefreshError"),
            };
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

    private async Task PollUntilCancelledAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(reorderAppRows: false);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _delayProvider.DelayAsync(PollInterval, cancellationToken);
                await RefreshAsync(reorderAppRows: false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void UpdateSelectedRangePresentation()
    {
        SelectedRangeDisplayLabel = GetRangeDisplayLabel(_selectedRange);
        OnPropertyChanged(nameof(IsSessionSelected));
        OnPropertyChanged(nameof(IsLastHourSelected));
        OnPropertyChanged(nameof(IsLastSixHoursSelected));
        OnPropertyChanged(nameof(IsTodaySelected));
    }

    private string GetRangeDisplayLabel(UsageRange range)
    {
        return range switch
        {
            UsageRange.Session => _resourceService.GetString("RangeSessionLabel"),
            UsageRange.LastHour => _resourceService.GetString("RangeLastHourLabel"),
            UsageRange.LastSixHours => _resourceService.GetString("RangeLastSixHoursLabel"),
            UsageRange.Today => _resourceService.GetString("RangeTodayLabel"),
            _ => _resourceService.GetString("RangeSessionLabel"),
        };
    }

    private string BuildWindowLabel(UsageWindowSnapshot snapshot)
    {
        return snapshot.SelectedRange switch
        {
            UsageRange.Session => _resourceService.Format(
                "SessionStartedFormat",
                ValueFormatter.FormatTime(snapshot.WindowStart)),
            UsageRange.Today => _resourceService.Format(
                "WindowSinceFormat",
                ValueFormatter.FormatTime(snapshot.WindowStart)),
            _ => _resourceService.Format(
                "WindowRangeFormat",
                ValueFormatter.FormatTime(snapshot.WindowStart),
                ValueFormatter.FormatTime(snapshot.WindowEnd)),
        };
    }

    private void NotifyVisualStateChanged()
    {
        OnPropertyChanged(nameof(IsLoadingFirstLoad));
        OnPropertyChanged(nameof(IsContentVisible));
        OnPropertyChanged(nameof(HasUsageData));
        OnPropertyChanged(nameof(HasNoUsageData));
    }

    private static void SyncCollection<T, TKey>(
        ObservableCollection<T> collection,
        IReadOnlyList<T> values,
        Func<T, TKey> keySelector,
        Action<T, T> updateExisting,
        bool reorderExistingItems)
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
}
