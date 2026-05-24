using NetStatInfoWin.Models;
using NetStatInfoWin.Services;
using NetStatInfoWin.Tests.TestSupport;
using NetStatInfoWin.ViewModels;

namespace NetStatInfoWin.Tests.ViewModels;

[TestClass]
public sealed class MainViewModelTests
{
    [TestMethod]
    public async Task SelectRangeAsync_ChangesRangeAndRefreshesImmediately()
    {
        var usageService = new FakeAttributedAppUsageService(static window => CreateUsage(window, "Browser", 400, 600));
        var windowProvider = new FakeUsageWindowProvider();
        var viewModel = CreateViewModel(usageService, windowProvider);

        await viewModel.SelectRangeAsync(UsageRange.LastHour);

        Assert.IsTrue(viewModel.IsLastHourSelected);
        Assert.AreEqual(UsageRange.LastHour, usageService.RequestedRanges.Single());
        Assert.AreEqual("Last hour", viewModel.SelectedRangeDisplayLabel);
        Assert.AreEqual(1, viewModel.AppUsages.Count);
    }

    [TestMethod]
    public async Task StartPolling_WhenDelayCompletes_RefreshesSelectedRange()
    {
        var usageService = new FakeAttributedAppUsageService(static window => CreateUsage(window, "Browser", 400, 600));
        var windowProvider = new FakeUsageWindowProvider();
        var delayProvider = new ManualDelayProvider();
        var viewModel = CreateViewModel(usageService, windowProvider, delayProvider);

        await viewModel.SelectRangeAsync(UsageRange.Today);
        usageService.Clear();

        viewModel.StartPolling();
        await WaitUntilAsync(() => usageService.CallCount >= 1);

        Assert.AreEqual(UsageRange.Today, usageService.RequestedRanges[0]);
        Assert.AreEqual(TimeSpan.FromSeconds(30), delayProvider.LastDelay);

        delayProvider.ReleaseNext();
        await WaitUntilAsync(() => usageService.CallCount >= 2);

        Assert.AreEqual(UsageRange.Today, usageService.RequestedRanges[1]);
        viewModel.StopPolling();
    }

    [TestMethod]
    public async Task RefreshAsync_UsesCurrentSelectedRange()
    {
        var usageService = new FakeAttributedAppUsageService(static window => CreateUsage(window, "Browser", 400, 600));
        var windowProvider = new FakeUsageWindowProvider();
        var viewModel = CreateViewModel(usageService, windowProvider);

        await viewModel.SelectRangeAsync(UsageRange.LastSixHours);
        usageService.Clear();

        await viewModel.RefreshAsync();

        Assert.AreEqual(UsageRange.LastSixHours, usageService.RequestedRanges.Single());
    }

    [TestMethod]
    public async Task RefreshAsync_WhenUnsupportedFailure_SetsSpecificErrorState()
    {
        var usageService = new FakeAttributedAppUsageService(static _ => throw new UsageLoadException(UsageLoadFailureKind.Unsupported));
        var windowProvider = new FakeUsageWindowProvider();
        var viewModel = CreateViewModel(usageService, windowProvider);

        await viewModel.RefreshAsync();

        Assert.IsTrue(viewModel.HasError);
        Assert.AreEqual(
            "Windows did not provide attributed app usage for this packaged app. Make sure the packaged app is running with the required capability.",
            viewModel.ErrorMessage);
    }

    [TestMethod]
    public async Task RefreshAsync_WhenSnapshotIsEmpty_ShowsEmptyState()
    {
        var usageService = new FakeAttributedAppUsageService(static _ => []);
        var windowProvider = new FakeUsageWindowProvider();
        var viewModel = CreateViewModel(usageService, windowProvider);

        await viewModel.RefreshAsync();

        Assert.IsTrue(viewModel.HasNoUsageData);
        Assert.AreEqual("0 B", viewModel.FormattedTotalUsage);
    }

    private static MainViewModel CreateViewModel(
        FakeAttributedAppUsageService usageService,
        FakeUsageWindowProvider windowProvider,
        IDelayProvider? delayProvider = null)
    {
        return new MainViewModel(
            usageService,
            windowProvider,
            new AppUsageAggregator(new TestResourceService()),
            delayProvider ?? new ImmediateDelayProvider(),
            new TestResourceService());
    }

    private static IReadOnlyList<ConnectionProfileUsageCapture> CreateUsage(
        UsageWindow window,
        string displayName,
        long sentBytes,
        long receivedBytes)
    {
        return
        [
            new ConnectionProfileUsageCapture(
                $"{window.Range}-profile",
                sentBytes,
                receivedBytes,
                [
                    new AttributedAppUsageRecord(
                        $"id:{displayName.ToUpperInvariant()}",
                        displayName,
                        displayName.ToLowerInvariant(),
                        AppUsageBucketKind.Application,
                        sentBytes,
                        receivedBytes),
                ]),
        ];
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(2);
        while (!predicate())
        {
            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail("Timed out waiting for asynchronous work to complete.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class FakeAttributedAppUsageService(
        Func<UsageWindow, IReadOnlyList<ConnectionProfileUsageCapture>> responseFactory) : IAttributedAppUsageService
    {
        private readonly Func<UsageWindow, IReadOnlyList<ConnectionProfileUsageCapture>> _responseFactory = responseFactory;

        public List<UsageRange> RequestedRanges { get; } = [];

        public int CallCount => RequestedRanges.Count;

        public void Clear()
        {
            RequestedRanges.Clear();
        }

        public Task<IReadOnlyList<ConnectionProfileUsageCapture>> GetUsageByProfileAsync(
            UsageWindow window,
            CancellationToken cancellationToken)
        {
            RequestedRanges.Add(window.Range);
            return Task.FromResult(_responseFactory(window));
        }
    }

    private sealed class FakeUsageWindowProvider : IUsageWindowProvider
    {
        public UsageWindow CreateWindow(UsageRange range)
        {
            DateTimeOffset endTime = range switch
            {
                UsageRange.Session => new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero),
                UsageRange.LastHour => new DateTimeOffset(2026, 5, 24, 13, 0, 0, TimeSpan.Zero),
                UsageRange.LastSixHours => new DateTimeOffset(2026, 5, 24, 18, 0, 0, TimeSpan.Zero),
                UsageRange.Today => new DateTimeOffset(2026, 5, 24, 20, 0, 0, TimeSpan.Zero),
                _ => new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero),
            };

            DateTimeOffset startTime = range switch
            {
                UsageRange.Session => endTime.AddHours(-2),
                UsageRange.LastHour => endTime.AddHours(-1),
                UsageRange.LastSixHours => endTime.AddHours(-6),
                UsageRange.Today => new DateTimeOffset(endTime.Year, endTime.Month, endTime.Day, 0, 0, 0, endTime.Offset),
                _ => endTime.AddHours(-2),
            };

            return new UsageWindow(range, startTime, endTime);
        }
    }

    private sealed class ImmediateDelayProvider : IDelayProvider
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ManualDelayProvider : IDelayProvider
    {
        private readonly Queue<TaskCompletionSource<bool>> _waiters = [];

        public TimeSpan? LastDelay { get; private set; }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            LastDelay = delay;

            var waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => waiter.TrySetCanceled(cancellationToken));
            _waiters.Enqueue(waiter);

            return waiter.Task;
        }

        public void ReleaseNext()
        {
            if (_waiters.Count > 0)
            {
                _waiters.Dequeue().TrySetResult(true);
            }
        }
    }
}
