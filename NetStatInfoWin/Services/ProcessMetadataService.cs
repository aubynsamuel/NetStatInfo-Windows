using System.ComponentModel;
using System.Diagnostics;
using NetStatInfoWin.Helpers;
using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal sealed class ProcessMetadataService : IProcessMetadataService
{
    public ProcessMetadata GetProcessMetadata(int processId)
    {
        using Process process = Process.GetProcessById(processId);
        string displayName = string.IsNullOrWhiteSpace(process.ProcessName)
            ? processId.ToString()
            : process.ProcessName;

        try
        {
            string? fileName = process.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                displayName = Path.GetFileNameWithoutExtension(fileName);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
        catch (Win32Exception)
        {
        }

        string initials = ValueFormatter.CreateInitials(displayName);
        return new ProcessMetadata(displayName, initials);
    }
}
