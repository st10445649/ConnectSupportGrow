using ConnectGrowAPI.Models;
using ConnectGrowAPI.Services;

namespace ConnectGrowAPI.Interfaces;

public interface IPaymentService
{
     Task<Result<string>> CreateCheckoutAsync(Booking booking, CancellationToken ct = default);
}


public class NullPaymentService : IPaymentService
{
    private readonly ILogger<NullPaymentService> _logger;
 
    public NullPaymentService(ILogger<NullPaymentService> logger) => _logger = logger;
 
    public Task<Result<string>> CreateCheckoutAsync(Booking booking, CancellationToken ct = default)
    {
        _logger.LogError(
            "No payment gateway is configured. Booking {Reference} cannot be paid.",
            booking.BookingReference);
 
        return Task.FromResult(Result<string>.Failure(
            ErrorType.Unexpected,
            "Payment is not available at the moment. Please try again later."));
    }
}