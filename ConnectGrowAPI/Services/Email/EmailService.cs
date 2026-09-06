using ConnectGrowAPI.Models;

namespace ConnectGrowAPI.Services.Email;

public class EmailOptions
{

   // https://app.sendgrid.com/guide/integrate/langs/csharp
   //https://medium.com/@kpareek2592/sending-emails-in-c-with-sendgrid-a-comprehensive-guide-3a63ccb73e4a
   //https://code-maze.com/csharp-send-emails-with-sendgrid-api/
   //https://singhsharp.medium.com/send-emails-with-custom-templates-using-sendgrid-7e0ed578ff06

   //https://medium.com/@jha.aaryan/building-a-robust-email-service-in-net-a-complete-guide-to-professional-email-delivery-83b40227b169
   //https://dev.to/edudeveloper/simple-email-sending-api-with-net-2bk3 
    public const string SectionName = "Email";

    public string ApiKey { get; set; } = string.Empty;

    public string FromEmail { get; set; } = "solutionsshieldtech@gmail.com";

    public string FromName { get; set; } = "Connect Support Grow";

    //Receives a copy of every booking confirmation, per the requirements.
    public string? AdminEmail { get; set; }

    public string ClientBaseUrl { get; set; } = string.Empty;
    public bool RedirectAllToTestRecipient { get; set; }

    public string? TestRecipient { get; set; }

    //Organiser address on calendar invitations. Falls back to FromEmail.
    public string? OrganiserEmail { get; set; }
}

public interface IEmailService
{
    Task<bool> SendBookingConfirmationAsync(BookingEmailModel model, CancellationToken ct = default);

    Task<bool> SendAdminBookingNotificationAsync(BookingEmailModel model, CancellationToken ct = default);

    Task<bool> SendPasswordResetAsync(string toEmail, string firstName, string resetUrl, CancellationToken ct = default);

    Task<bool> SendWebinarCancelledAsync(BookingEmailModel model, CancellationToken ct = default);

    Task<bool> SendWelcomeAsync(string toEmail, string firstName, CancellationToken ct = default);
}

public class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger) => _logger = logger;

    // just for testing. if something is broken in the backend the whole process won't crash
    private Task<bool> Skip(string kind, string recipient)
    {
        _logger.LogWarning(
            "Email not configured — {Kind} to {Recipient} was NOT sent. Set Email:ApiKey to enable delivery.",
            kind, recipient);

        return Task.FromResult(false);
    }

    public Task<bool> SendBookingConfirmationAsync(BookingEmailModel m, CancellationToken ct = default) =>
        Skip("booking confirmation", m.ToEmail);

    public Task<bool> SendAdminBookingNotificationAsync(BookingEmailModel m, CancellationToken ct = default) =>
        Skip("admin booking notification", m.ToEmail);

    public Task<bool> SendPasswordResetAsync(string toEmail, string firstName, string resetUrl, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Email not configured — password reset for {Email} was NOT sent. Link: {ResetUrl}",
            toEmail, resetUrl);

        return Task.FromResult(false);
    }

    public Task<bool> SendWebinarCancelledAsync(BookingEmailModel m, CancellationToken ct = default) =>
        Skip("webinar cancellation", m.ToEmail);

    public Task<bool> SendWelcomeAsync(string toEmail, string firstName, CancellationToken ct = default) =>
        Skip("welcome", toEmail);
}