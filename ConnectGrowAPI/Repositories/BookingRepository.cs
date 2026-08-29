
using ConnectGrowAPI.Data;
using ConnectGrowAPI.Interfaces;
using ConnectGrowAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectGrowAPI.Repositories;

public class BookingRepository : Repository<Booking>, IBookingRepository
{
    public BookingRepository(ApplicationDbContext db) : base(db) { }
 
    public Task<Booking?> GetByReferenceAsync(string bookingReference, CancellationToken ct = default) =>
        Set.Include(b => b.Webinar)
           .Include(b => b.User)
           .Include(b => b.Transaction)
           .FirstOrDefaultAsync(b => b.BookingReference == bookingReference, ct);
 
    public Task<Booking?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        Set.Include(b => b.Webinar)
           .Include(b => b.User)
           .Include(b => b.Transaction)
           .Include(b => b.Evaluation)
           .FirstOrDefaultAsync(b => b.Id == id, ct);
 
    public async Task<IReadOnlyList<Booking>> GetForUserAsync(Guid userId, CancellationToken ct = default) =>
        await Set.AsNoTracking()
                 .Include(b => b.Webinar)
                 .Include(b => b.Evaluation)
                 .Where(b => b.UserId == userId)
                 .OrderByDescending(b => b.CreatedAt)
                 .ToListAsync(ct);
 
    public Task<bool> HasActiveBookingAsync(Guid userId, int webinarId, CancellationToken ct = default) =>
        Set.AnyAsync(b => b.UserId == userId
                       && b.WebinarId == webinarId
                       && b.Status != BookingStatus.Cancelled, ct);
 
    public Task<int> CountHeldSeatsAsync(int webinarId, DateTime holdCutoffUtc, CancellationToken ct = default) =>
        Set.CountAsync(b => b.WebinarId == webinarId
                         && (b.Status == BookingStatus.Paid
                          || b.Status == BookingStatus.Attended
                          || b.Status == BookingStatus.NoShow
                          || (b.Status == BookingStatus.Pending && b.CreatedAt > holdCutoffUtc)), ct);
}