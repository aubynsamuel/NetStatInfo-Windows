using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal sealed class UsageWindowProvider(TimeProvider timeProvider) : IUsageWindowProvider
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly DateTimeOffset _sessionStartedAt = timeProvider.GetUtcNow().ToLocalTime();

    public UsageWindow CreateWindow(UsageRange range)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow().ToLocalTime();
        DateTimeOffset startTime = range switch
        {
            UsageRange.Session => _sessionStartedAt,
            UsageRange.LastHour => now.AddHours(-1),
            UsageRange.LastSixHours => now.AddHours(-6),
            UsageRange.Today => new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset),
            _ => _sessionStartedAt,
        };

        return new UsageWindow(range, startTime, now);
    }
}
