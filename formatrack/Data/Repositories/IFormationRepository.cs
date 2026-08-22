using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IFormationRepository : IRepository<Formation>
{
    Task<IReadOnlyList<Formation>> SearchAsync(string? recherche);
}
