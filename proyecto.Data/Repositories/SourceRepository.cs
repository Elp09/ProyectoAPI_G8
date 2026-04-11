using Microsoft.EntityFrameworkCore;
using proyecto.Data.Repositories;
using proyecto.Models;

namespace proyecto.Data.Repositories;

public class SourceRepository : ISourceRepository
{
    private readonly DataDbContext _db;

    public SourceRepository(DataDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Source>> GetAllAsync()
        => await _db.Sources.AsNoTracking().ToListAsync();

    public async Task<Source?> GetByIdAsync(int id)
        => await _db.Sources.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Source?> GetByNameAsync(string name)
        => await _db.Sources.AsNoTracking().FirstOrDefaultAsync(s => s.Name == name);

    public async Task<Source> AddAsync(Source source)
    {
        _db.Sources.Add(source);
        await _db.SaveChangesAsync();
        return source;
    }

    public async Task UpdateAsync(Source source)
    {
        _db.Sources.Update(source);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var source = await _db.Sources.FindAsync(id);
        if (source is not null)
        {
            _db.Sources.Remove(source);
            await _db.SaveChangesAsync();
        }
    }
}
