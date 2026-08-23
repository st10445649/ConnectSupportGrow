using ConnectGrowAPI.Models;

namespace ConnectGrowAPI.Interfaces;

public interface IBookingRepository : IRepository<Booking>
{
    Task<Booking?> GetByReferenceAsync(string bookingReference, CancellationToken ct = default);
    Task<Booking?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetForUserAsync(Guid userId, CancellationToken ct = default);
 
    // active bookings mean bookings made by users and haven'tb been cancelled.
    // used to block duplicate bookings
    Task<bool> HasActiveBookingAsync(Guid userId, int webinarId, CancellationToken ct = default);
 
    //total count of all bookings including paid for and pending bookings (within window)
    Task<int> CountHeldSeatsAsync(int webinarId, DateTime holdCutoffUtc, CancellationToken ct = default);
}