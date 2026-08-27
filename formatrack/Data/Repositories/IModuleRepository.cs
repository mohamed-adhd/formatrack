using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IModuleRepository : IRepository<Module>
{
    Task<IReadOnlyList<Module>> GetByFormationAsync(int idFormation);
    Task<IReadOnlyList<Module>> GetCommunsAsync();
}
