using System.Globalization;

namespace CpdBooking.Web;

/// <summary>Formats dates the GOV.UK way, in UK time: "14 November 2026 at 9am".</summary>
public static class UkTime
{
    // IANA id on Linux (App Service); Windows id as a fallback for local development on Windows.
    private static readonly TimeZoneInfo London =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/London", out var iana) ? iana
        : TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

    public static string Format(DateTimeOffset value)
    {
        var local = TimeZoneInfo.ConvertTime(value, London);
        var culture = CultureInfo.InvariantCulture;

        string time;
        if (local.Hour == 12 && local.Minute == 0)
        {
            time = "midday";
        }
        else
        {
            var hour = local.Hour % 12 == 0 ? 12 : local.Hour % 12;
            var minutes = local.Minute == 0 ? string.Empty : ":" + local.Minute.ToString("00", culture);
            time = $"{hour.ToString(culture)}{minutes}{(local.Hour < 12 ? "am" : "pm")}";
        }

        return $"{local.ToString("d MMMM yyyy", culture)} at {time}";
    }
}
