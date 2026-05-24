namespace NetStatInfoWin.Services;

internal interface IDelayProvider
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
