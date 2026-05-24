using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal interface INetworkSnapshotService
{
    Task<NetworkCapture> CaptureSnapshotAsync(CancellationToken cancellationToken);
}
