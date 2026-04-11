using proyecto.Data.Repositories;
using proyecto.Models;

namespace proyecto.Core.Services;

public class SecretService : ISecretService
{
    private readonly ISecretRepository _repo;

    public SecretService(ISecretRepository repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<Secret>> GetAllAsync() => _repo.GetAllAsync();
    public Task<Secret?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
    public Task<IEnumerable<Secret>> GetBySourceIdAsync(int sourceId) => _repo.GetBySourceIdAsync(sourceId);
    public Task<Secret> AddAsync(Secret secret) => _repo.AddAsync(secret);
    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}
