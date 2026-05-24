using System.Globalization;
using NetStatInfoWin.Helpers;

namespace NetStatInfoWin.Tests.TestSupport;

internal sealed class TestResourceService : IResourceService
{
    public string Format(string key, params object[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, GetString(key), arguments);
    }

    public string GetString(string key)
    {
        return key switch
        {
            "RefreshButtonAutomationName" => "Refresh app usage",
            "LastUpdatedFormat" => "Updated {0}",
            "SessionStartedFormat" => "Started {0}",
            "WindowSinceFormat" => "Since {0}",
            "WindowRangeFormat" => "{0} to {1}",
            "RangeSessionLabel" => "Session",
            "RangeLastHourLabel" => "Last hour",
            "RangeLastSixHoursLabel" => "Last 6 hours",
            "RangeTodayLabel" => "Today",
            "UsageUnsupportedError" => "Windows did not provide attributed app usage for this packaged app. Make sure the packaged app is running with the required capability.",
            "GenericRefreshError" => "Try refreshing again. If the problem continues, restart the app.",
            "SystemUnattributedLabel" => "System / unattributed",
            "UnknownAppUsageLabel" => "Unknown app",
            "UsageBreakdownFormat" => "Sent {0} - Received {1}",
            "UsageShareFormat" => "{0} of total",
            _ => key,
        };
    }
}
