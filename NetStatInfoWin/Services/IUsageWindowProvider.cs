using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal interface IUsageWindowProvider
{
    UsageWindow CreateWindow(UsageRange range);
}
