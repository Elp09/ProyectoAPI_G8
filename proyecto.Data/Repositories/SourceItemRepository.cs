using Microsoft.EntityFrameworkCore;
using proyecto.Data.Repositories;
using proyecto.Models;

namespace proyecto.Data.Repositories;

public class SourceItemRepository : ISourceItemRepository
{
    private readonly DataDbContext _db;

    public SourceItemRepository(DataDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SourceItem>> GetAllAsync()
        => await _db.SourceItems.AsNoTracking().OrderByDescending(i => i.CreatedAt).ToListAsync();

    public async Task<SourceItem?> GetByIdAsync(int id)
        => await _db.SourceItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);

    public async Task<IEnumerable<SourceItem>> GetBySourceIdAsync(int sourceId)
        => await _db.SourceItems.AsNoTracking().Where(i => i.SourceId == sourceId).OrderByDescending(i => i.CreatedAt).ToListAsync();

    public async Task<SourceItem> AddAsync(SourceItem item)
    {
        _db.SourceItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(SourceItem item)
    {
        _db.SourceItems.Update(item);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _db.SourceItems.FindAsync(id);
        if (item is not null)
        {
            _db.SourceItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }
}
