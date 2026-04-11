using proyecto.Models;

namespace proyecto.Data.Repositories;

public interface ISourceItemRepository : IRepository<SourceItem>
{
    Task<IEnumerable<SourceItem>> GetBySourceIdAsync(int sourceId);
}
