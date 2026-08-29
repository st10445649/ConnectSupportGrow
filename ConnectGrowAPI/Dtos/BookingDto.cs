using System.ComponentModel.DataAnnotations;

namespace ConnectGrowAPI.Dtos;

public class CreateBookingRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "A valid webinar must be selected.")]
    public int WebinarId { get; set; }
}

public class BookingDto
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
}

public class BookingCreatedDto
{
    public BookingDto Booking { get; set; } = null!;
    public string? PaymentRedirectUrl { get; set; }
    public bool RequiresPayment { get; set; }
}

public class CancelBookingRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}