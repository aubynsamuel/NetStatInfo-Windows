namespace NetStatInfoWin.Services;

internal interface IAppUsageDisplayNameResolver
{
    string ResolveDisplayName(string? attributionId, string? attributionName);
}
