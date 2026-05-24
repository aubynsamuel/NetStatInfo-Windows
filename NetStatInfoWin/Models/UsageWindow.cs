namespace NetStatInfoWin.Models;

internal sealed class UsageWindow(UsageRange range, DateTimeOffset startTime, DateTimeOffset endTime)
{
    public UsageRange Range { get; } = range;

    public DateTimeOffset StartTime { get; } = startTime;

    public DateTimeOffset EndTime { get; } = endTime;
}
