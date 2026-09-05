namespace ConnectGrow.Services;

public static class SouthAfricanTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        
        foreach (var id in new[] { "Africa/Johannesburg", "South Africa Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "SAST", "SAST");
    }

    public static DateTime ToSast(this DateTime utc)
    {
        // Values from the API carry Kind=Utc because the JSON ends in Z
        var asUtc = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(asUtc, Zone);
    }

    public static string ToSastDisplay(this DateTime utc) =>
        utc.ToSast().ToString("d MMMM yyyy 'at' h:mm tt");

    public static string ToSastDate(this DateTime utc) =>
        utc.ToSast().ToString("d MMMM yyyy");

    public static string ToSastTime(this DateTime utc) =>
        utc.ToSast().ToString("h:mm tt");

    public static string ToSastRange(this DateTime startUtc, DateTime endUtc) =>
        $"{startUtc.ToSastTime()} - {endUtc.ToSastTime()} SAST";

    //Converts a wall-clock time typed by an admin into the UTC instant to send to the API.
  
    public static DateTime FromSastInput(DateTime wallClock) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified), Zone);
}
