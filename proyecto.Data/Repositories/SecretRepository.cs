using Microsoft.EntityFrameworkCore;
using proyecto.Data.Repositories;
using proyecto.Models;

namespace proyecto.Data.Repositories;

public class SecretRepository : ISecretRepository
{
    private readonly DataDbContext _db;

    public SecretRepository(DataDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Secret>> GetAllAsync()
        => await _db.Secrets.AsNoTracking().ToListAsync();

    public async Task<Secret?> GetByIdAsync(int id)
        => await _db.Secrets.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IEnumerable<Secret>> GetBySourceIdAsync(int sourceId)
        => await _db.Secrets.AsNoTracking().Where(s => s.SourceId == sourceId).ToListAsync();

    public async Task<Secret> AddAsync(Secret secret)
    {
        _db.Secrets.Add(secret);
        await _db.SaveChangesAsync();
        return secret;
    }

    public async Task UpdateAsync(Secret secret)
    {
        _db.Secrets.Update(secret);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var secret = await _db.Secrets.FindAsync(id);
        if (secret is not null)
        {
            _db.Secrets.Remove(secret);
            await _db.SaveChangesAsync();
        }
    }
}
