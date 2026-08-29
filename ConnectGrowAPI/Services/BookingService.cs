
using ConnectGrowAPI.Data;
using ConnectGrowAPI.Dtos;
using ConnectGrowAPI.Interfaces;
using ConnectGrowAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectGrowAPI.Services;

public interface IBookingService
{
    Task<Result<BookingCreatedDto>> CreateAsync(Guid userId, int webinarId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BookingDto>>> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task<Result<BookingDto>> GetByIdAsync(int bookingId, Guid userId, bool isAdmin, CancellationToken ct = default);
    Task<Result> CancelAsync(int bookingId, Guid userId, bool isAdmin, CancellationToken ct = default);


    Task<Result> ConfirmPaymentAsync(
        string bookingReference,
        string gatewayReference,
        decimal amountPaid,
        string paymentMethod,
        string? rawPayload,
        CancellationToken ct = default);

    Task<Result> RecordFailedPaymentAsync(
        string bookingReference,
        string gatewayReference,
        string errorMessage,
        string? rawPayload,
        CancellationToken ct = default);
}

public class BookingOptions
{
    public const string SectionName = "Booking";

    public int PendingHoldMinutes { get; set; } = 20;
}

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IWebinarRepository _webinars;
    private readonly IPaymentService _payments;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BookingService> _logger;
    private readonly TimeSpan _holdWindow;

    public BookingService(
        IBookingRepository bookings,
        IWebinarRepository webinars,
        IPaymentService payments,
        ApplicationDbContext db,
        IConfiguration config,
        ILogger<BookingService> logger)
    {
        _bookings = bookings;
        _webinars = webinars;
        _payments = payments;
        _db = db;
        _logger = logger;

        var minutes = config.GetValue<int?>($"{BookingOptions.SectionName}:PendingHoldMinutes") ?? 20;
        _holdWindow = TimeSpan.FromMinutes(minutes);
    }


    public async Task<Result<BookingCreatedDto>> CreateAsync(
        Guid userId, int webinarId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var webinar = await _webinars.GetByIdAsync(webinarId, ct);
        if (webinar is null)
            return Result<BookingCreatedDto>.NotFound("That webinar could not be found.");

        if (webinar.Status == WebinarStatus.Cancelled)
            return Result<BookingCreatedDto>.Invalid("This webinar has been cancelled.");

        if (!webinar.IsBookable(now))
            return Result<BookingCreatedDto>.Invalid(
                "This webinar is no longer open for registration.");

        if (await _bookings.HasActiveBookingAsync(userId, webinarId, ct))
            return Result<BookingCreatedDto>.Conflict(
                "You have already booked this webinar. See your dashboard for details.");

        // Capacity gate counts live rows rather than trusting the 
        // counter, and includes Pending bookings still inside their hold window
        var heldSeats = await _bookings.CountHeldSeatsAsync(webinarId, now - _holdWindow, ct);
        if (heldSeats >= webinar.Capacity)
            return Result<BookingCreatedDto>.Conflict("This webinar is fully booked.");

        var booking = new Booking
        {
            UserId = userId,
            WebinarId = webinarId,
            BookingReference = GenerateReference(),
            BookingDate = now,
            Amount = webinar.Price,   
            Status = BookingStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _bookings.AddAsync(booking, ct);

        try
        {
            await _bookings.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
    
            _logger.LogInformation(
                "Duplicate booking blocked by unique index for user {UserId}, webinar {WebinarId}.",
                userId, webinarId);
            return Result<BookingCreatedDto>.Conflict("You have already booked this webinar.");
        }


        if (booking.Amount <= 0m)
        {
            var confirmed = await ConfirmPaymentAsync(
                booking.BookingReference,
                $"FREE-{booking.BookingReference}",
                0m,
                "None",
                null,
                ct);

            if (confirmed.IsFailure)
                return Result<BookingCreatedDto>.Failure(confirmed.ErrorType, confirmed.Error!);

            var free = await _bookings.GetWithDetailsAsync(booking.Id, ct);
            return Result<BookingCreatedDto>.Success(new BookingCreatedDto
            {
                Booking = MapToDto(free!, now),
                RequiresPayment = false,
                PaymentRedirectUrl = null
            });
        }


        var withDetails = await _bookings.GetWithDetailsAsync(booking.Id, ct);

        var checkout = await _payments.CreateCheckoutAsync(withDetails!, ct);
        if (checkout.IsFailure)
        {
           
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = now;
            _bookings.Update(booking);
            await _bookings.SaveChangesAsync(ct);

            return Result<BookingCreatedDto>.Failure(checkout.ErrorType, checkout.Error!);
        }

        return Result<BookingCreatedDto>.Success(new BookingCreatedDto
        {
            Booking = MapToDto(withDetails!, now),
            RequiresPayment = true,
            PaymentRedirectUrl = checkout.Value
        });
    }


    public async Task<Result<IReadOnlyList<BookingDto>>> GetForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var bookings = await _bookings.GetForUserAsync(userId, ct);
        IReadOnlyList<BookingDto> dtos = bookings.Select(b => MapToDto(b, now)).ToList();
        return Result<IReadOnlyList<BookingDto>>.Success(dtos);
    }

    public async Task<Result<BookingDto>> GetByIdAsync(
        int bookingId, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetWithDetailsAsync(bookingId, ct);
        if (booking is null)
            return Result<BookingDto>.NotFound("Booking not found.");

        // Ownership check. Returning NotFound rather than Forbidden avoids
        // confirming to a probing user that the booking id exists.
        if (booking.UserId != userId && !isAdmin)
            return Result<BookingDto>.NotFound("Booking not found.");

        return Result<BookingDto>.Success(MapToDto(booking, DateTime.UtcNow));
    }


    public async Task<Result> CancelAsync(
        int bookingId, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var booking = await _bookings.GetWithDetailsAsync(bookingId, ct);
        if (booking is null)
            return Result.NotFound("Booking not found.");

        if (booking.UserId != userId && !isAdmin)
            return Result.NotFound("Booking not found.");

        if (!booking.CanBeCancelled())
            return Result.Invalid($"A booking with status {booking.Status} cannot be cancelled.");

        var wasPaid = booking.IsPaid;

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        _bookings.Update(booking);
        await _bookings.SaveChangesAsync(ct);

   
        if (wasPaid)
            await _webinars.DecrementBookingCountAsync(booking.WebinarId, ct);

        if (wasPaid)
        {
            _logger.LogWarning(
                "Paid booking {Reference} cancelled. A refund may be required.",
                booking.BookingReference);
        }

        return Result.Success();
    }

    

    public async Task<Result> ConfirmPaymentAsync(
        string bookingReference,
        string gatewayReference,
        decimal amountPaid,
        string paymentMethod,
        string? rawPayload,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var booking = await _db.Bookings
            .Include(b => b.Webinar)
            .Include(b => b.Transaction)
            .FirstOrDefaultAsync(b => b.BookingReference == bookingReference, ct);

        if (booking is null)
        {
            _logger.LogWarning(
                "Payment callback referenced unknown booking {Reference}.", bookingReference);
            return Result.NotFound("Booking not found.");
        }

        // Idempotency: the gateway retries its callback until it gets a 200, so
        // a repeat for an already-confirmed booking is a success, not an error.
        if (booking.Status == BookingStatus.Paid || booking.Status == BookingStatus.Attended)
        {
            _logger.LogInformation(
                "Duplicate payment callback for already-paid booking {Reference}. Ignored.",
                bookingReference);
            await tx.CommitAsync(ct);
            return Result.Success();
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            _logger.LogWarning(
                "Payment received for cancelled booking {Reference}. Flagged for manual review.",
                bookingReference);
            return Result.Conflict("Booking was cancelled.");
        }

        // Amount check. The caller should already have verified this against the
        // gateway payload; repeating it here means the invariant holds no matter
        // which code path reaches this method.
        if (amountPaid != booking.Amount)
        {
            _logger.LogError(
                "Amount mismatch on booking {Reference}: expected {Expected}, received {Received}.",
                bookingReference, booking.Amount, amountPaid);
            return Result.Invalid("Payment amount does not match the booking total.");
        }

        booking.Status = BookingStatus.Paid;
        booking.PaymentReference = gatewayReference;
        booking.PaidAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        if (booking.Transaction is null)
        {
            _db.Transactions.Add(new Transaction
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                Amount = amountPaid,
                PaymentMethod = paymentMethod,
                TransactionReference = gatewayReference,
                Status = TransactionStatus.Success,
                ResponseData = rawPayload,
                TransactionDate = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
        }
        else
        {
            booking.Transaction.Status = TransactionStatus.Success;
            booking.Transaction.TransactionReference = gatewayReference;
            booking.Transaction.CompletedAt = DateTime.UtcNow;
            booking.Transaction.ResponseData = rawPayload;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // The unique index on transaction_reference caught a concurrent
            // duplicate callback. The other request is doing the work.
            _logger.LogInformation(
                "Concurrent duplicate callback for {Reference} rejected by unique index.",
                bookingReference);
            await tx.RollbackAsync(ct);
            return Result.Success();
        }

        // Atomic increment. Deliberately unconditional: the money has been taken,
        // so an oversell is a business decision for the admin, not grounds to
        // reject a paid booking.
        await _webinars.IncrementBookingCountAsync(booking.WebinarId, ct);

        if (booking.Webinar is not null && booking.Webinar.CurrentBookings + 1 > booking.Webinar.Capacity)
        {
            _logger.LogWarning(
                "Webinar {WebinarId} is oversold after confirming {Reference}. Admin review needed.",
                booking.WebinarId, bookingReference);
        }

        await tx.CommitAsync(ct);

        _logger.LogInformation(
            "Booking {Reference} confirmed as paid ({Amount}).", bookingReference, amountPaid);

        // The BookingConfirmedEvent is raised here once the event dispatcher is
        // in place — email, ICS, receipt and CPD handlers subscribe to it.
        return Result.Success();
    }

    public async Task<Result> RecordFailedPaymentAsync(
        string bookingReference,
        string gatewayReference,
        string errorMessage,
        string? rawPayload,
        CancellationToken ct = default)
    {
        var booking = await _bookings.GetByReferenceAsync(bookingReference, ct);
        if (booking is null)
            return Result.NotFound("Booking not found.");

        if (booking.Status == BookingStatus.Paid)
        {
            _logger.LogWarning(
                "Failure callback for already-paid booking {Reference}. Ignored.", bookingReference);
            return Result.Success();
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;

        _db.Transactions.Add(new Transaction
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            Amount = booking.Amount,
            PaymentMethod = "PayFast",
            TransactionReference = string.IsNullOrWhiteSpace(gatewayReference)
                ? $"FAILED-{booking.BookingReference}-{DateTime.UtcNow.Ticks}"
                : gatewayReference,
            Status = TransactionStatus.Failed,
            ErrorMessage = errorMessage,
            ResponseData = rawPayload,
            TransactionDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payment failed for booking {Reference}: {Error}", bookingReference, errorMessage);

        return Result.Success();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reference format: CSG-YYMMDD-XXXXXX. Readable enough to quote over the
    /// phone, random enough not to be guessable or enumerable.
    /// </summary>
    private static string GenerateReference()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // no I, L, O, 0, 1
        var chars = new char[6];
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(6);

        for (var i = 0; i < 6; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];

        return $"CSG-{DateTime.UtcNow:yyMMdd}-{new string(chars)}";
    }

    /// <summary>PostgreSQL SQLSTATE 23505 = unique_violation.</summary>
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: "23505" };

    private static BookingDto MapToDto(Booking b, DateTime utcNow) => new()
    {
        Id = b.Id,
        BookingReference = b.BookingReference,
        Status = b.Status.ToString(),
        Amount = b.Amount,
        BookingDate = b.BookingDate,
        PaidAt = b.PaidAt,
        AttendanceConfirmed = b.AttendanceConfirmed,

        WebinarId = b.WebinarId,
        WebinarTitle = b.Webinar?.Title ?? string.Empty,
        WebinarCategory = b.Webinar?.Category ?? string.Empty,
        StartDateTime = b.Webinar?.StartDateTime ?? default,
        EndDateTime = b.Webinar?.EndDateTime ?? default,
        CpdPoints = b.Webinar?.CpdPoints ?? 0,

        // Withheld unless paid — an unpaid reservation must not yield a join link.
        JoinUrl = b.IsPaid ? b.Webinar?.TeamsJoinUrl : null,

        HasEvaluation = b.Evaluation is not null,
        CanSubmitEvaluation = b.IsPaid
                              && b.Evaluation is null
                              && b.Webinar is not null
                              && b.Webinar.EndDateTime < utcNow
    };
}