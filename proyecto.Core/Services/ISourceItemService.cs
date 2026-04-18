using proyecto.Models;

namespace proyecto.Core.Services;

public interface ISourceItemService
{
    Task<IEnumerable<SourceItem>> GetAllAsync();
    Task<SourceItem?> GetByIdAsync(int id);
    Task<IEnumerable<SourceItem>> GetBySourceIdAsync(int sourceId);

    /// <summary>
    /// Guarda JSON crudo en la base de datos tal como está.
    /// </summary>
    Task<SourceItem> SaveAsync(string rawJson, int sourceId, string? endpoint, bool isLocalUpload, string? savedBy);

    Task DeleteAsync(int id);
}
