using System.Globalization;
using System.Net;
using System.Text;
using ConnectGrowAPI.Models;

namespace ConnectGrowAPI.Services.Email;

//some basic email templates that can be adjusted later in html for serv-side rendering in client email inboxes
//https://www.youtube.com/watch?v=LS2Vu_55YJs 
public static class EmailTemplates
{
    private const string Purple = "#9258ad";
    private const string Ink = "#1f2937";
    private const string Muted = "#6b7280";
    private const string Border = "#e5e7eb";

    private static readonly TimeZoneInfo Sast = ResolveSast();

    private static TimeZoneInfo ResolveSast()
    {
        foreach (var id in new[] { "Africa/Johannesburg", "South Africa Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "SAST", "SAST");
    }

    private static DateTime ToSast(DateTime utc)
    {
        var asUtc = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(asUtc, Sast);
    }

    private static string LongDate(DateTime utc) =>
        ToSast(utc).ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);

    private static string TimeRange(DateTime startUtc, DateTime endUtc) =>
        $"{ToSast(startUtc):HH:mm} – {ToSast(endUtc):HH:mm} SAST";


    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    public static (string Subject, string Html, string Text) BookingConfirmation(
        BookingEmailModel booking, string clientBaseUrl)
    {
        var subject = $"Booking confirmed: {booking.WebinarTitle}";

        var joinBlock = string.IsNullOrWhiteSpace(booking.JoinUrl)
            ? $@"<p style=""margin:0;color:{Muted};font-size:14px;"">
                   Your joining link will follow closer to the session.
                 </p>"
            : $@"<a href=""{E(booking.JoinUrl)}""
                    style=""display:inline-block;background:{Purple};color:#ffffff;text-decoration:none;
                           padding:12px 28px;border-radius:6px;font-weight:bold;font-size:16px;"">
                   Join the webinar
                 </a>";

        var cpdRow = booking.CpdPoints > 0
            ? Row("CPD points", $"{booking.CpdPoints} (credited once attendance is confirmed)")
            : string.Empty;

        var body = $@"
          <p style=""margin:0 0 16px;font-size:16px;color:{Ink};"">Hi {E(booking.FirstName)},</p>

          <p style=""margin:0 0 24px;font-size:16px;color:{Ink};line-height:1.6;"">
            Your place is confirmed. The details are below, and a calendar invitation
            is attached to this email.
          </p>

          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%""
                 style=""background:#faf7fc;border:1px solid {Border};border-radius:8px;margin-bottom:24px;"">
            <tr><td style=""padding:20px;"">
              <p style=""margin:0 0 16px;font-size:18px;font-weight:bold;color:{Ink};"">
                {E(booking.WebinarTitle)}
              </p>
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                {Row("Date", LongDate(booking.StartDateTimeUtc))}
                {Row("Time", TimeRange(booking.StartDateTimeUtc, booking.EndDateTimeUtc))}
                {Row("Reference", booking.BookingReference)}
                {Row("Amount paid", $"R{booking.Amount.ToString("N2", CultureInfo.InvariantCulture)}")}
                {cpdRow}
              </table>
            </td></tr>
          </table>

          <p style=""margin:0 0 24px;text-align:center;"">{joinBlock}</p>

          <p style=""margin:0 0 8px;font-size:14px;color:{Muted};line-height:1.6;"">
            Can't attend live? Sessions are recorded, and you'll be emailed a personal
            viewing link once the recording is ready.
          </p>

          <p style=""margin:0;font-size:14px;color:{Muted};line-height:1.6;"">
            Your booking is always available in
            <a href=""{clientBaseUrl}/Dashboard"" style=""color:{Purple};"">your dashboard</a>.
          </p>";

        var text = $@"Hi {booking.FirstName},

Your place is confirmed.

{booking.WebinarTitle}
Date: {LongDate(booking.StartDateTimeUtc)}
Time: {TimeRange(booking.StartDateTimeUtc, booking.EndDateTimeUtc)}
Reference: {booking.BookingReference}
Amount paid: R{booking.Amount.ToString("N2", CultureInfo.InvariantCulture)}
{(booking.CpdPoints > 0 ? $"CPD points: {booking.CpdPoints} (credited once attendance is confirmed)\n" : "")}
{(string.IsNullOrWhiteSpace(booking.JoinUrl) ? "Your joining link will follow closer to the session." : $"Join: {booking.JoinUrl}")}

A calendar invitation is attached.

Your dashboard: {clientBaseUrl}/Dashboard

— Connect Support Grow";

        return (subject, Wrap("Booking confirmed", body, clientBaseUrl), text);
    }


    public static (string Subject, string Html, string Text) AdminBookingNotification(
        BookingEmailModel booking, string clientBaseUrl)
    {
        var subject = $"New booking: {booking.WebinarTitle} — {booking.FullName}";

        var body = $@"
          <p style=""margin:0 0 24px;font-size:16px;color:{Ink};"">A new booking has been paid for.</p>

          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%""
                 style=""background:#faf7fc;border:1px solid {Border};border-radius:8px;"">
            <tr><td style=""padding:20px;"">
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                {Row("Webinar", E(booking.WebinarTitle))}
                {Row("Attendee", E(booking.FullName))}
                {Row("Email", E(booking.ToEmail))}
                {Row("Date", LongDate(booking.StartDateTimeUtc))}
                {Row("Reference", booking.BookingReference)}
                {Row("Amount", $"R{booking.Amount.ToString("N2", CultureInfo.InvariantCulture)}")}
                {Row("Paid at", booking.PaidAt.HasValue ? ToSast(booking.PaidAt.Value).ToString("d MMM yyyy HH:mm") + " SAST" : "—")}
              </table>
            </td></tr>
          </table>";

        var text = $@"New booking paid.

Webinar: {booking.WebinarTitle}
Attendee: {booking.FullName} ({booking.ToEmail})
Date: {LongDate(booking.StartDateTimeUtc)}
Reference: {booking.BookingReference}
Amount: R{booking.Amount.ToString("N2", CultureInfo.InvariantCulture)}";

        return (subject, Wrap("New booking", body, clientBaseUrl), text);
    }


    public static (string Subject, string Html, string Text) PasswordReset(
        string firstName, string resetUrl, string clientBaseUrl)
    {
        const string subject = "Reset your Connect Support Grow password";

        var body = $@"
          <p style=""margin:0 0 16px;font-size:16px;color:{Ink};"">Hi {E(firstName)},</p>

          <p style=""margin:0 0 24px;font-size:16px;color:{Ink};line-height:1.6;"">
            We received a request to reset your password. Use the button below to choose
            a new one.
          </p>

          <p style=""margin:0 0 24px;text-align:center;"">
            <a href=""{E(resetUrl)}""
               style=""display:inline-block;background:{Purple};color:#ffffff;text-decoration:none;
                      padding:12px 28px;border-radius:6px;font-weight:bold;font-size:16px;"">
              Reset my password
            </a>
          </p>

          <p style=""margin:0 0 16px;font-size:14px;color:{Muted};line-height:1.6;"">
            This link expires shortly and can only be used once.
          </p>

          <p style=""margin:0 0 24px;font-size:14px;color:{Muted};line-height:1.6;"">
            If you didn't ask for this, you can ignore this email — your password
            will not change.
          </p>

          <p style=""margin:0;font-size:12px;color:{Muted};line-height:1.6;word-break:break-all;"">
            If the button doesn't work, copy this into your browser:<br>
            {E(resetUrl)}
          </p>";

        var text = $@"Hi {firstName},

We received a request to reset your password. Open this link to choose a new one:

{resetUrl}

This link expires shortly and can only be used once.

If you didn't ask for this, you can ignore this email — your password will not change.

— Connect Support Grow";

        return (subject, Wrap("Reset your password", body, clientBaseUrl), text);
    }


    public static (string Subject, string Html, string Text) WebinarCancelled(
        BookingEmailModel booking, string clientBaseUrl)
    {
        var subject = $"Cancelled: {booking.WebinarTitle}";

        var body = $@"
          <p style=""margin:0 0 16px;font-size:16px;color:{Ink};"">Hi {E(booking.FirstName)},</p>

          <p style=""margin:0 0 24px;font-size:16px;color:{Ink};line-height:1.6;"">
            We're sorry — <strong>{E(booking.WebinarTitle)}</strong> on
            {LongDate(booking.StartDateTimeUtc)} has been cancelled.
          </p>

          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%""
                 style=""background:#fef2f2;border:1px solid #fecaca;border-radius:8px;margin-bottom:24px;"">
            <tr><td style=""padding:20px;"">
              <p style=""margin:0;font-size:15px;color:#991b1b;line-height:1.6;"">
                Your payment of R{booking.Amount.ToString("N2", CultureInfo.InvariantCulture)}
                (reference {booking.BookingReference}) will be refunded. Please allow a few
                working days for it to appear.
              </p>
            </td></tr>
          </table>

          <p style=""margin:0;font-size:14px;color:{Muted};line-height:1.6;"">
            You can see other upcoming sessions in
            <a href=""{clientBaseUrl}/Webinars"" style=""color:{Purple};"">our webinar catalogue</a>.
          </p>";

        var text = $@"Hi {booking.FirstName},

We're sorry — {booking.WebinarTitle} on {LongDate(booking.StartDateTimeUtc)} has been cancelled.

Your payment of R{booking.Amount.ToString("N2", CultureInfo.InvariantCulture)} (reference {booking.BookingReference}) will be refunded. Please allow a few working days.

Other sessions: {clientBaseUrl}/Webinars

— Connect Support Grow";

        return (subject, Wrap("Webinar cancelled", body, clientBaseUrl), text);
    }


    public static (string Subject, string Html, string Text) Welcome(
        string firstName, string clientBaseUrl)
    {
        const string subject = "Welcome to Connect Support Grow";

        var body = $@"
          <p style=""margin:0 0 16px;font-size:16px;color:{Ink};"">Hi {E(firstName)},</p>

          <p style=""margin:0 0 24px;font-size:16px;color:{Ink};line-height:1.6;"">
            Thanks for joining us. You can now book webinars, track your CPD points and
            access recordings from your dashboard.
          </p>

          <p style=""margin:0 0 24px;text-align:center;"">
            <a href=""{clientBaseUrl}/Webinars""
               style=""display:inline-block;background:{Purple};color:#ffffff;text-decoration:none;
                      padding:12px 28px;border-radius:6px;font-weight:bold;font-size:16px;"">
              Browse webinars
            </a>
          </p>";

        var text = $@"Hi {firstName},

Thanks for joining us. You can now book webinars, track your CPD points and access recordings from your dashboard.

Browse webinars: {clientBaseUrl}/Webinars

— Connect Support Grow";

        return (subject, Wrap("Welcome", body, clientBaseUrl), text);
    }

    private static string Row(string label, string value) => $@"
      <tr>
        <td style=""padding:6px 0;font-size:14px;color:{Muted};width:130px;vertical-align:top;"">{label}</td>
        <td style=""padding:6px 0;font-size:14px;color:{Ink};font-weight:600;"">{value}</td>
      </tr>";

    private static string Wrap(string preheader, string body, string clientBaseUrl)
    {
        var builder = new StringBuilder();

        builder.Append($@"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>{E(preheader)}</title>
</head>
<body style=""margin:0;padding:0;background:#f4f4f5;-webkit-font-smoothing:antialiased;"">

  <!-- Preheader: the grey preview line in an inbox list. Hidden in the body
       itself, otherwise it would appear twice. -->
  <div style=""display:none;max-height:0;overflow:hidden;opacity:0;"">{E(preheader)}</div>

  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background:#f4f4f5;"">
    <tr>
      <td align=""center"" style=""padding:32px 16px;"">

        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%""
               style=""max-width:600px;background:#ffffff;border-radius:12px;overflow:hidden;
                      font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;"">

          <tr>
            <td style=""background:{Purple};padding:24px 32px;"">
              <p style=""margin:0;color:#ffffff;font-size:20px;font-weight:bold;"">
                Connect Support Grow
              </p>
              <p style=""margin:4px 0 0;color:#f0e4f5;font-size:13px;"">
                Neurodiversity affirming therapy training
              </p>
            </td>
          </tr>

          <tr><td style=""padding:32px;"">{body}</td></tr>

          <tr>
            <td style=""background:#fafafa;border-top:1px solid {Border};padding:24px 32px;"">
              <p style=""margin:0 0 8px;font-size:12px;color:{Muted};line-height:1.6;"">
                Connect Support Grow · Durban, South Africa
              </p>
              <p style=""margin:0;font-size:12px;color:{Muted};line-height:1.6;"">
                <a href=""{clientBaseUrl}"" style=""color:{Muted};"">Website</a> ·
                <a href=""{clientBaseUrl}/Home/Faq"" style=""color:{Muted};"">FAQ</a> ·
                <a href=""{clientBaseUrl}/Account/Settings"" style=""color:{Muted};"">Account settings</a>
              </p>
            </td>
          </tr>
        </table>

      </td>
    </tr>
  </table>
</body>
</html>");

        return builder.ToString();
    }
}