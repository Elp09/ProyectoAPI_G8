using proyecto.Core.Schema;
using proyecto.Models;

namespace proyecto.Core.Services;

public interface ISourceItemService
{
    Task<IEnumerable<SourceItem>> GetAllAsync();
    Task<SourceItem?> GetByIdAsync(int id);
    Task<IEnumerable<SourceItem>> GetBySourceIdAsync(int sourceId);

    /// <summary>
    /// Guarda un IngestDocument ya normalizado en la base de datos.
    /// </summary>
    Task<SourceItem> SaveAsync(IngestDocument document, int sourceId, string? endpoint, bool isLocalUpload, string? savedBy);

    Task DeleteAsync(int id);
}
