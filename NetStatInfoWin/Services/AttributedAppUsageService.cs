using System.Globalization;
using Windows.Foundation.Metadata;
using Windows.Networking.Connectivity;
using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal sealed class AttributedAppUsageService : IAttributedAppUsageService
{
    private const string UnknownUsageKey = "name:unknown";
    private readonly IAppUsageDisplayNameResolver _displayNameResolver;

    public AttributedAppUsageService()
        : this(new AppUsageDisplayNameResolver())
    {
    }

    internal AttributedAppUsageService(IAppUsageDisplayNameResolver displayNameResolver)
    {
        _displayNameResolver = displayNameResolver;
    }

    public async Task<IReadOnlyList<ConnectionProfileUsageCapture>> GetUsageByProfileAsync(UsageWindow window, CancellationToken cancellationToken)
    {
        if (!ApiInformation.IsMethodPresent(typeof(ConnectionProfile).FullName!, "GetAttributedNetworkUsageAsync"))
        {
            throw new UsageLoadException(UsageLoadFailureKind.Unsupported);
        }

        IReadOnlyList<ConnectionProfile> profiles = GetCandidateProfiles();
        if (profiles.Count == 0)
        {
            return [];
        }

        List<ConnectionProfileUsageCapture> captures = [];
        bool anyQuerySucceeded = false;
        bool sawUnsupportedFailure = false;
        Exception? lastFailure = null;

        foreach (ConnectionProfile profile in profiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                ConnectionProfileUsageCapture? capture = await ReadProfileUsageAsync(profile, window).ConfigureAwait(false);
                anyQuerySucceeded = true;

                if (capture is not null)
                {
                    captures.Add(capture);
                }
            }
            catch (Exception ex) when (IsUnsupportedFailure(ex))
            {
                sawUnsupportedFailure = true;
                lastFailure = ex;
            }
            catch (Exception ex)
            {
                lastFailure = ex;
            }
        }

        if (anyQuerySucceeded)
        {
            return captures;
        }

        if (sawUnsupportedFailure)
        {
            throw new UsageLoadException(UsageLoadFailureKind.Unsupported, lastFailure);
        }

        throw new UsageLoadException(UsageLoadFailureKind.QueryFailed, lastFailure);
    }

    private static IReadOnlyList<ConnectionProfile> GetCandidateProfiles()
    {
        List<ConnectionProfile> profiles = [];

        ConnectionProfile? internetProfile = NetworkInformation.GetInternetConnectionProfile();
        if (internetProfile is not null)
        {
            profiles.Add(internetProfile);
        }

        profiles.AddRange(NetworkInformation.GetConnectionProfiles());

        List<ConnectionProfile> uniqueProfiles = [];
        HashSet<string> seenKeys = new(StringComparer.OrdinalIgnoreCase);

        foreach (ConnectionProfile profile in profiles)
        {
            if (!IsInternetCapable(profile))
            {
                continue;
            }

            string key = GetProfileKey(profile);
            if (seenKeys.Add(key))
            {
                uniqueProfiles.Add(profile);
            }
        }

        return uniqueProfiles;
    }

    private static bool IsInternetCapable(ConnectionProfile profile)
    {
        NetworkConnectivityLevel connectivityLevel = profile.GetNetworkConnectivityLevel();
        return connectivityLevel == NetworkConnectivityLevel.InternetAccess ||
               connectivityLevel == NetworkConnectivityLevel.ConstrainedInternetAccess;
    }

    private static string GetProfileKey(ConnectionProfile profile)
    {
        if (profile.NetworkAdapter is not null)
        {
            return profile.NetworkAdapter.NetworkAdapterId.ToString();
        }

        if (!string.IsNullOrWhiteSpace(profile.ProfileName))
        {
            return string.Create(CultureInfo.InvariantCulture, $"profile:{profile.ProfileName}");
        }

        return string.Create(CultureInfo.InvariantCulture, $"connectivity:{profile.GetNetworkConnectivityLevel()}");
    }

    private async Task<ConnectionProfileUsageCapture?> ReadProfileUsageAsync(ConnectionProfile profile, UsageWindow window)
    {
        NetworkUsageStates states = CreateUsageStates();

        IReadOnlyList<AttributedNetworkUsage> attributedUsages =
            await profile.GetAttributedNetworkUsageAsync(window.StartTime, window.EndTime, states);

        IReadOnlyList<NetworkUsage> aggregateUsages =
            await profile.GetNetworkUsageAsync(window.StartTime, window.EndTime, DataUsageGranularity.Total, states);

        List<AttributedAppUsageRecord> records = [];
        foreach (AttributedNetworkUsage usage in attributedUsages)
        {
            long sentBytes = SaturateToLong(usage.BytesSent);
            long receivedBytes = SaturateToLong(usage.BytesReceived);
            if (sentBytes == 0 && receivedBytes == 0)
            {
                continue;
            }

            string? attributionId = string.IsNullOrWhiteSpace(usage.AttributionId)
                ? null
                : usage.AttributionId.Trim();
            string displayName = _displayNameResolver.ResolveDisplayName(attributionId, usage.AttributionName);

            records.Add(new AttributedAppUsageRecord(
                CreateUsageKey(attributionId, displayName),
                displayName,
                attributionId,
                AppUsageBucketKind.Application,
                sentBytes,
                receivedBytes));
        }

        long totalSentBytes = 0;
        long totalReceivedBytes = 0;

        foreach (NetworkUsage usage in aggregateUsages)
        {
            totalSentBytes += SaturateToLong(usage.BytesSent);
            totalReceivedBytes += SaturateToLong(usage.BytesReceived);
        }

        if (totalSentBytes == 0 && totalReceivedBytes == 0 && records.Count == 0)
        {
            return null;
        }

        return new ConnectionProfileUsageCapture(
            GetProfileKey(profile),
            totalSentBytes,
            totalReceivedBytes,
            records);
    }

    private static NetworkUsageStates CreateUsageStates()
    {
        return new NetworkUsageStates
        {
            Roaming = TriStates.DoNotCare,
            Shared = TriStates.DoNotCare,
        };
    }

    private static bool IsUnsupportedFailure(Exception exception)
    {
        return exception is UnauthorizedAccessException ||
               exception is NotImplementedException ||
               exception is TypeLoadException;
    }

    private static long SaturateToLong(ulong value)
    {
        return value > long.MaxValue ? long.MaxValue : (long)value;
    }

    private static string CreateUsageKey(string? attributionId, string displayName)
    {
        if (!string.IsNullOrWhiteSpace(attributionId))
        {
            return string.Create(CultureInfo.InvariantCulture, $"id:{attributionId}");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return UnknownUsageKey;
        }

        return string.Create(CultureInfo.InvariantCulture, $"name:{displayName.Trim().ToUpperInvariant()}");
    }
}
