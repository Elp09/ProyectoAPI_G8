using proyecto.Data.Repositories;
using proyecto.Models;

namespace proyecto.Core.Services;

public class SourceService : ISourceService
{
    private readonly ISourceRepository _repo;

    public SourceService(ISourceRepository repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<Source>> GetAllAsync() => _repo.GetAllAsync();
    public Task<Source?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
    public Task<Source> AddAsync(Source source) => _repo.AddAsync(source);
    public Task UpdateAsync(Source source) => _repo.UpdateAsync(source);
    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}
