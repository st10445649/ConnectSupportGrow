using ConnectGrowAPI.Data;
using ConnectGrowAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectGrowAPI.Interfaces;

public interface IWebinarRepository : IRepository<Webinar>
{
     Task<IReadOnlyList<Webinar>> GetPublishedUpcomingAsync(string? category, CancellationToken ct = default);
    Task<Webinar?> GetDetailAsync(int id, CancellationToken ct = default);
 
    Task<int> IncrementBookingCountAsync(int webinarId, CancellationToken ct = default);
 
    Task<int> DecrementBookingCountAsync(int webinarId, CancellationToken ct = default);
}
 
public class WebinarRepository : Repository<Webinar>, IWebinarRepository
{
    public WebinarRepository(ApplicationDbContext db) : base(db) { }
 
    public async Task<IReadOnlyList<Webinar>> GetPublishedUpcomingAsync(
        string? category, CancellationToken ct = default)
    {
        var query = Set.AsNoTracking()
                       .Where(w => w.Status == WebinarStatus.Published
                                && w.StartDateTime > DateTime.UtcNow);
 
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(w => w.Category == category);
 
        return await query.OrderBy(w => w.StartDateTime).ToListAsync(ct);
    }
 
    public Task<Webinar?> GetDetailAsync(int id, CancellationToken ct = default) =>
        Set.AsNoTracking()
           .Include(w => w.Recordings)
           .FirstOrDefaultAsync(w => w.Id == id, ct);
 
    public Task<int> IncrementBookingCountAsync(int webinarId, CancellationToken ct = default) =>
        Db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE webinars SET current_bookings = current_bookings + 1 WHERE id = {webinarId}", ct);
 
    public Task<int> DecrementBookingCountAsync(int webinarId, CancellationToken ct = default) =>
        Db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE webinars SET current_bookings = GREATEST(current_bookings - 1, 0) WHERE id = {webinarId}", ct);
}