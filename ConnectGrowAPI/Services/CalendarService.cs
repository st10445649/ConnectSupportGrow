using System.Globalization;
using System.Text;
using ConnectGrowAPI.Models;

namespace ConnectGrowAPI.Services;

// Builds ICS calendar invitations 
public interface ICalendarService
{
    //Returns the ICS file content for a booking, ready to attach.
    string BuildInvitation(BookingEmailModel booking, string organiserEmail);

    //METHOD:CANCEL, so calendar clients remove the event rather than duplicating it.
    string BuildCancellation(BookingEmailModel booking, string organiserEmail);
}

public class CalendarService : ICalendarService
{
    public string BuildInvitation(BookingEmailModel booking, string organiserEmail) =>
        Build(booking, organiserEmail, method: "REQUEST", status: "CONFIRMED", sequence: 0);

    public string BuildCancellation(BookingEmailModel booking, string organiserEmail) =>
       
        Build(booking, organiserEmail, method: "CANCEL", status: "CANCELLED", sequence: 1);

    private static string Build(
        BookingEmailModel booking,
        string organiserEmail,
        string method,
        string status,
        int sequence)
    {
        var uid = $"{booking.BookingReference}@connectsupportgrow.co.za";

        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Connect Support Grow//Webinar Booking//EN",
            "CALSCALE:GREGORIAN",

            $"METHOD:{method}",

            "BEGIN:VEVENT",
            $"UID:{uid}",
            $"SEQUENCE:{sequence}",
            $"DTSTAMP:{FormatUtc(DateTime.UtcNow)}",

            $"DTSTART:{FormatUtc(booking.StartDateTimeUtc)}",
            $"DTEND:{FormatUtc(booking.EndDateTimeUtc)}",

            $"SUMMARY:{Escape(booking.WebinarTitle)}",
            $"DESCRIPTION:{Escape(BuildDescription(booking))}",

            $"LOCATION:{Escape(booking.JoinUrl ?? "Online — Microsoft Teams")}",

            $"ORGANIZER;CN=Connect Support Grow:mailto:{organiserEmail}",
            $"ATTENDEE;CN={Escape(booking.FullName)};RSVP=TRUE:mailto:{booking.ToEmail}",
            $"STATUS:{status}",
            "TRANSP:OPAQUE"
        };

        if (method == "REQUEST")
        {
            lines.AddRange(new[]
            {
                "BEGIN:VALARM",
                "TRIGGER:-PT30M",
                "ACTION:DISPLAY",
                $"DESCRIPTION:{Escape($"Starting soon: {booking.WebinarTitle}")}",
                "END:VALARM"
            });
        }

        lines.Add("END:VEVENT");
        lines.Add("END:VCALENDAR");

        var builder = new StringBuilder();

        foreach (var line in lines)
        {
           
            builder.Append(Fold(line)).Append("\r\n");
        }

        return builder.ToString();
    }

    private static string BuildDescription(BookingEmailModel booking)
    {
        var parts = new List<string>
        {
            booking.WebinarDescription,
            string.Empty,
            $"Booking reference: {booking.BookingReference}"
        };

        if (booking.CpdPoints > 0)
            parts.Add($"CPD points: {booking.CpdPoints} (credited once attendance is confirmed)");

        if (!string.IsNullOrWhiteSpace(booking.JoinUrl))
        {
            parts.Add(string.Empty);
            parts.Add($"Join link: {booking.JoinUrl}");
        }

        return string.Join("\n", parts);
    }

    private static string FormatUtc(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return utc.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
    }


    private static string Fold(string line)
    {
        const int maxOctets = 73;   // leaves room for the CRLF

        if (Encoding.UTF8.GetByteCount(line) <= maxOctets) return line;

        var result = new StringBuilder();
        var current = new StringBuilder();
        var currentBytes = 0;
        var isFirstLine = true;

        foreach (var character in line)
        {
            var size = Encoding.UTF8.GetByteCount(character.ToString());

            var budget = isFirstLine ? maxOctets : maxOctets - 1;

            if (currentBytes + size > budget)
            {
                if (result.Length > 0) result.Append("\r\n ");
                result.Append(current);

                current.Clear();
                currentBytes = 0;
                isFirstLine = false;
            }

            current.Append(character);
            currentBytes += size;
        }

        if (current.Length > 0)
        {
            if (result.Length > 0) result.Append("\r\n ");
            result.Append(current);
        }

        return result.ToString();
    }
}