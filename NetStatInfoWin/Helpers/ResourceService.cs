using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace NetStatInfoWin.Helpers;

internal sealed class ResourceService : IResourceService
{
    private readonly ResourceLoader _resourceLoader = new();

    public string GetString(string key)
    {
        string value = _resourceLoader.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? key : value;
    }

    public string Format(string key, params object[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, GetString(key), arguments);
    }
}
