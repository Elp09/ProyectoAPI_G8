using proyecto.Models;

namespace proyecto.Core.Services;

public interface ISecretService
{
    Task<IEnumerable<Secret>> GetAllAsync();
    Task<Secret?> GetByIdAsync(int id);
    Task<IEnumerable<Secret>> GetBySourceIdAsync(int sourceId);
    Task<Secret> AddAsync(Secret secret);
    Task DeleteAsync(int id);
}
