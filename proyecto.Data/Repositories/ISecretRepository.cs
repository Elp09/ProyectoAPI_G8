using proyecto.Models;

namespace proyecto.Data.Repositories;

public interface ISecretRepository : IRepository<Secret>
{
    Task<IEnumerable<Secret>> GetBySourceIdAsync(int sourceId);
}
