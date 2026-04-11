using proyecto.Models;

namespace proyecto.Data.Repositories;

public interface ISourceRepository : IRepository<Source>
{
    Task<Source?> GetByNameAsync(string name);
}
