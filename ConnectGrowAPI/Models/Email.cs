namespace ConnectGrowAPI.Models;
public class BookingEmailModel
{
    public string ToEmail { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string BookingReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }

    public int WebinarId { get; set; }
    public string WebinarTitle { get; set; } = string.Empty;
    public string WebinarDescription { get; set; } = string.Empty;

    //UTC. Converted to SAST for display and used as-is in the ICS file
    public DateTime StartDateTimeUtc { get; set; }

    public DateTime EndDateTimeUtc { get; set; }

    public int CpdPoints { get; set; }

    //Only ever populated for a paid booking.
    public string? JoinUrl { get; set; }
}