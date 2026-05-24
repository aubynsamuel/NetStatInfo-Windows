using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal interface IAttributedAppUsageService
{
    Task<IReadOnlyList<ConnectionProfileUsageCapture>> GetUsageByProfileAsync(UsageWindow window, CancellationToken cancellationToken);
}
