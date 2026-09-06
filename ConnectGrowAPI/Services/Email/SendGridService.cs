using System.Text;
using ConnectGrowAPI.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ConnectGrowAPI.Services.Email;
//https://app.sendgrid.com/guide/integrate/langs/csharp
public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly EmailOptions _options;

    private readonly ICalendarService _calendar;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        ISendGridClient client,
        ICalendarService calendar,
        IOptions<EmailOptions> options,
        ILogger<SendGridEmailService> logger)
    {
        _client = client;
        _calendar = calendar;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendBookingConfirmationAsync(
        BookingEmailModel booking, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.BookingConfirmation(booking, _options.ClientBaseUrl);

        var message = BuildMessage(booking.ToEmail, booking.FullName, subject, html, text);

        AttachCalendar(message, _calendar.BuildInvitation(booking, OrganiserEmail), "REQUEST",
            $"{booking.BookingReference}.ics");

        return await SendAsync(message, "booking confirmation", booking.ToEmail, ct);
    }

    public async Task<bool> SendAdminBookingNotificationAsync(
        BookingEmailModel booking, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AdminEmail))
        {
            _logger.LogDebug("No Email:AdminEmail configured; skipping the admin copy.");
            return false;
        }

        var (subject, html, text) = EmailTemplates.AdminBookingNotification(booking, _options.ClientBaseUrl);

        var message = BuildMessage(_options.AdminEmail, "Practice Admin", subject, html, text);

        return await SendAsync(message, "admin booking notification", _options.AdminEmail, ct);
    }

    public async Task<bool> SendPasswordResetAsync(
        string toEmail, string firstName, string resetUrl, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.PasswordReset(firstName, resetUrl, _options.ClientBaseUrl);

        var message = BuildMessage(toEmail, firstName, subject, html, text);

        message.SetClickTracking(false, false);

        return await SendAsync(message, "password reset", toEmail, ct);
    }

    public async Task<bool> SendWebinarCancelledAsync(
        BookingEmailModel booking, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.WebinarCancelled(booking, _options.ClientBaseUrl);

        var message = BuildMessage(booking.ToEmail, booking.FullName, subject, html, text);

        return await SendAsync(message, "webinar cancellation", booking.ToEmail, ct);
    }

    public async Task<bool> SendWelcomeAsync(
        string toEmail, string firstName, CancellationToken ct = default)
    {
        var (subject, html, text) = EmailTemplates.Welcome(firstName, _options.ClientBaseUrl);

        var message = BuildMessage(toEmail, firstName, subject, html, text);

        return await SendAsync(message, "welcome", toEmail, ct);
    }


    private string OrganiserEmail =>
        string.IsNullOrWhiteSpace(_options.OrganiserEmail) ? _options.FromEmail : _options.OrganiserEmail;

    private SendGridMessage BuildMessage(
        string toEmail, string toName, string subject, string html, string text)
    {
        var recipient = _options.RedirectAllToTestRecipient && !string.IsNullOrWhiteSpace(_options.TestRecipient)
            ? _options.TestRecipient!
            : toEmail;

        if (!string.Equals(recipient, toEmail, StringComparison.OrdinalIgnoreCase))
        {
            subject = $"[to: {toEmail}] {subject}";

            _logger.LogInformation(
                "Email redirected from {Original} to {TestRecipient}.", toEmail, recipient);
        }

        return MailHelper.CreateSingleEmail(
            new EmailAddress(_options.FromEmail, _options.FromName),
            new EmailAddress(recipient, toName),
            subject,
            plainTextContent: text,
            htmlContent: html);
    }

        private static void AttachCalendar(
        SendGridMessage message, string icsContent, string method, string fileName)
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(icsContent));
 
        message.AddAttachment(
            filename: fileName,
            base64Content: base64,
            type: $"text/calendar; charset=utf-8; method={method}",
            disposition: "attachment");
    }

    private async Task<bool> SendAsync(
        SendGridMessage message, string kind, string recipient, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogError("Email:FromEmail is not configured; {Kind} not sent.", kind);
            return false;
        }

        try
        {
            var response = await _client.SendEmailAsync(message, ct);

            if (response.IsSuccessStatusCode)
            {
                
                _logger.LogInformation(
                    "Queued {Kind} to {Recipient} ({Status}).",
                    kind, recipient, (int)response.StatusCode);

                return true;
            }

            var body = await response.Body.ReadAsStringAsync(ct);

            _logger.LogError(
                "SendGrid rejected {Kind} to {Recipient}: {Status} {Body}",
                kind, recipient, (int)response.StatusCode, body);

            return false;
        }
        catch (Exception ex)
        {
           
            _logger.LogError(ex, "Failed to send {Kind} to {Recipient}.", kind, recipient);
            return false;
        }
    }
}