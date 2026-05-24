using System.Globalization;

namespace NetStatInfoWin.Helpers;

internal static class ValueFormatter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        double value = bytes;
        int unitIndex = 0;

        while (value >= 1024 && unitIndex < Units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return string.Format(CultureInfo.CurrentCulture, "{0:N2} {1}", value, Units[unitIndex]);
    }

    public static string FormatTime(DateTimeOffset timestamp)
    {
        return timestamp.ToLocalTime().ToString("t", CultureInfo.CurrentCulture);
    }

    public static string CreateInitials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "?";
        }

        string[] parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "?";
        }

        if (parts.Length == 1)
        {
            return parts[0][0].ToString(CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture);
        }

        return string.Concat(
            parts[0][0].ToString(CultureInfo.CurrentCulture),
            parts[^1][0].ToString(CultureInfo.CurrentCulture)).ToUpper(CultureInfo.CurrentCulture);
    }
}
