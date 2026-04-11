using System.Text.Json;
using proyecto.Core.Schema;
using proyecto.Data.Repositories;
using proyecto.Models;

namespace proyecto.Core.Services;

public class SourceItemService : ISourceItemService
{
    private readonly ISourceItemRepository _repo;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public SourceItemService(ISourceItemRepository repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<SourceItem>> GetAllAsync() => _repo.GetAllAsync();
    public Task<SourceItem?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
    public Task<IEnumerable<SourceItem>> GetBySourceIdAsync(int sourceId) => _repo.GetBySourceIdAsync(sourceId);

    public Task<SourceItem> SaveAsync(IngestDocument document, int sourceId, string? endpoint, bool isLocalUpload, string? savedBy)
    {
        var item = new SourceItem
        {
            SourceId      = sourceId,
            Json          = JsonSerializer.Serialize(document, _jsonOptions),
            Endpoint      = endpoint,
            IsLocalUpload = isLocalUpload,
            SavedBy       = savedBy,
            CreatedAt     = DateTime.UtcNow,
        };

        return _repo.AddAsync(item);
    }

    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}
