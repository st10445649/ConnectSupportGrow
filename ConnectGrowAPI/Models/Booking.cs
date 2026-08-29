
namespace ConnectGrowAPI.Models;

public class Booking
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int WebinarId { get; set; }
    public Webinar Webinar { get; set; } = null!;

    public string BookingReference { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    public decimal Amount { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    //Gateway's own transaction id (PayFast <c>pf_payment_id</c>). Unique when present
    public string? PaymentReference { get; set; }

    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    //Set by the admin after the event, enabling CPD certificate generation.
    public bool AttendanceConfirmed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Transaction? Transaction { get; set; }
    public Evaluation? Evaluation { get; set; }

    public bool IsPaid => Status == BookingStatus.Paid || Status == BookingStatus.Attended;

 
    //pending booking holds for a limited period until payment is made otherwise abdandoned carts would block other booking
    public bool IsHoldExpired(DateTime utcNow, TimeSpan holdWindow) =>
        Status == BookingStatus.Pending && CreatedAt.Add(holdWindow) < utcNow;

    public bool CanBeCancelled() =>
        Status == BookingStatus.Pending || Status == BookingStatus.Paid;
}

/// Payment audit trail. One row per payment attempt, including failures, so a
// disputed charge can be reconstructed from the database alone.
public class Transaction
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    //Provider's transaction id. Unique (idempotent)
    
    public string TransactionReference { get; set; } = string.Empty;

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public string? ErrorMessage { get; set; }

    //Raw gateway payload stored as JSON for dispute resolution.
    public string? ResponseData { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

// Post-webinar feedback. One per booking, enforced by a unique index on
// BookingId rather than an application-level check.
public class Evaluation
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int Rating { get; set; }

    public string? Feedback { get; set; }
    public bool WouldRecommend { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}