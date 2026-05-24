namespace NetStatInfoWin.Helpers;

internal interface IResourceService
{
    string GetString(string key);

    string Format(string key, params object[] arguments);
}
