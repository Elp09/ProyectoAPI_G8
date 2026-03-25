using proyecto.Models;

namespace proyecto.Core.Services;

public interface ISourceService
{
    Task<IEnumerable<Source>> GetAllAsync();
    Task<Source?> GetByIdAsync(int id);
    Task<Source> AddAsync(Source source);
    Task UpdateAsync(Source source);
    Task DeleteAsync(int id);
}
