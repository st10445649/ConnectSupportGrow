namespace ConnectGrow.Models;
public class Bookings
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool AttendanceConfirmed { get; set; }
 
    public int WebinarId { get; set; }
    public string WebinarTitle { get; set; } = string.Empty;
    public string WebinarCategory { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int CpdPoints { get; set; }
 
    public string? JoinUrl { get; set; }
 
    public bool HasEvaluation { get; set; }
    public bool CanSubmitEvaluation { get; set; }
 
    public bool IsPaid => Status is "Paid" or "Attended";
    public bool IsUpcoming => StartDateTime > DateTime.UtcNow;
}
 
public class BookingCreatedModel
{
    public Bookings Booking { get; set; } = null!;
    public string? PaymentRedirectUrl { get; set; }
    public bool RequiresPayment { get; set; }
}