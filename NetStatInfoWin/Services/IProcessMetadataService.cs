using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal interface IProcessMetadataService
{
    ProcessMetadata GetProcessMetadata(int processId);
}
