using System.Collections.Concurrent;
using System.IO;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Management.Deployment;

namespace NetStatInfoWin.Services;

internal sealed class AppUsageDisplayNameResolver : IAppUsageDisplayNameResolver
{
    private readonly ConcurrentDictionary<string, string> _resolvedNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<string, string?> _appInfoLookup;
    private readonly Func<string, string?> _packageLookup;

    public AppUsageDisplayNameResolver()
        : this(TryResolveFromAppInfo, TryResolveFromPackageFamily)
    {
    }

    internal AppUsageDisplayNameResolver(
        Func<string, string?> appInfoLookup,
        Func<string, string?> packageLookup)
    {
        _appInfoLookup = appInfoLookup;
        _packageLookup = packageLookup;
    }

    public string ResolveDisplayName(string? attributionId, string? attributionName)
    {
        string preferredName = attributionName?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(preferredName))
        {
            return preferredName;
        }

        string normalizedAttributionId = attributionId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedAttributionId))
        {
            return string.Empty;
        }

        return _resolvedNames.GetOrAdd(normalizedAttributionId, ResolveDisplayNameFromId);
    }

    private string ResolveDisplayNameFromId(string attributionId)
    {
        string? appInfoName = _appInfoLookup(attributionId);
        if (!string.IsNullOrWhiteSpace(appInfoName))
        {
            return appInfoName;
        }

        foreach (string candidate in GetPackageFamilyCandidates(attributionId))
        {
            string? packageName = _packageLookup(candidate);
            if (!string.IsNullOrWhiteSpace(packageName))
            {
                return packageName;
            }
        }

        return CreateReadableFallback(attributionId);
    }

    private static IEnumerable<string> GetPackageFamilyCandidates(string attributionId)
    {
        yield return attributionId;

        int appUserModelSeparatorIndex = attributionId.IndexOf('!');
        if (appUserModelSeparatorIndex > 0)
        {
            yield return attributionId[..appUserModelSeparatorIndex];
        }
    }

    private static string? TryResolveFromAppInfo(string attributionId)
    {
        if (!ApiInformation.IsMethodPresent(typeof(AppInfo).FullName!, nameof(AppInfo.GetFromAppUserModelId)))
        {
            return null;
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            return null;
        }

        try
        {
            AppInfo appInfo = AppInfo.GetFromAppUserModelId(attributionId);
            return NormalizeLookupResult(appInfo.DisplayInfo?.DisplayName);
        }
        catch
        {
            return null;
        }
    }

    private static string? TryResolveFromPackageFamily(string packageFamilyName)
    {
        if (string.IsNullOrWhiteSpace(packageFamilyName))
        {
            return null;
        }

        try
        {
            PackageManager packageManager = new();
            Package? package = packageManager
                .FindPackagesForUser(string.Empty, packageFamilyName)
                .FirstOrDefault();

            if (package is null)
            {
                return null;
            }

            string? displayName = NormalizeLookupResult(package.DisplayName);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            return NormalizeLookupResult(package.Id?.Name);
        }
        catch
        {
            return null;
        }
    }

    private static string CreateReadableFallback(string attributionId)
    {
        string candidate = attributionId.Trim();

        int appUserModelSeparatorIndex = candidate.IndexOf('!');
        if (appUserModelSeparatorIndex > 0)
        {
            candidate = candidate[..appUserModelSeparatorIndex];
        }

        int publisherHashSeparatorIndex = candidate.IndexOf('_');
        if (publisherHashSeparatorIndex > 0)
        {
            candidate = candidate[..publisherHashSeparatorIndex];
        }

        if (candidate.Contains('\\') || candidate.Contains('/'))
        {
            candidate = Path.GetFileName(candidate);
        }

        if (candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            candidate = Path.GetFileNameWithoutExtension(candidate);
        }

        candidate = candidate.Replace('.', ' ').Replace('_', ' ').Trim();

        return string.IsNullOrWhiteSpace(candidate)
            ? attributionId
            : candidate;
    }

    private static string? NormalizeLookupResult(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
