using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface ISessionRepository : IRepository<Session>
{
    Task<IReadOnlyList<Session>> GetByFormationAsync(int idFormation);
    Task<IReadOnlyList<Session>> GetUpcomingAsync(int limite = 5);
    Task<IReadOnlyList<Session>> GetByAnneeAsync(int annee);
}
