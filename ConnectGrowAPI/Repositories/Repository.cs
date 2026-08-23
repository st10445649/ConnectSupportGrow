
using ConnectGrowAPI.Data;
using ConnectGrowAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Db;
    protected readonly DbSet<T> Set;
 
    public Repository(ApplicationDbContext db)
    {
        Db = db;
        Set = db.Set<T>();
    }
 
    public virtual Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Set.FindAsync(new object[] { id }, ct).AsTask();
 
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await Set.AsNoTracking().ToListAsync(ct);
 
    public virtual async Task AddAsync(T entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);
 
    public virtual void Update(T entity) => Set.Update(entity);
 
    public virtual void Remove(T entity) => Set.Remove(entity);
 
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Db.SaveChangesAsync(ct);
 
    public IQueryable<T> Query() => Set.AsQueryable();
}