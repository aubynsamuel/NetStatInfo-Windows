using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal interface ISessionUsageAggregator
{
    SessionNetworkSnapshot BuildSessionSnapshot(NetworkCapture capture);
}
