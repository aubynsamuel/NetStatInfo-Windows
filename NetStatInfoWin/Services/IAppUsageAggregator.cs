using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal interface IAppUsageAggregator
{
    UsageWindowSnapshot BuildSnapshot(UsageWindow window, IReadOnlyList<ConnectionProfileUsageCapture> profileCaptures);
}
