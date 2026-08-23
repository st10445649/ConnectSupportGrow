using ConnectGrowAPI.Models;
using ConnectGrowAPI.Services;

namespace ConnectGrowAPI.Interfaces;

public interface IPaymentService
{
     Task<Result<string>> CreateCheckoutAsync(Booking booking, CancellationToken ct = default);
}