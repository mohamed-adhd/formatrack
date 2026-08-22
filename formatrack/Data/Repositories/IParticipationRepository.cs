using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IParticipationRepository : IRepository<Participation>
{
    Task<IReadOnlyList<Participation>> GetBySessionAsync(int idSession);
    Task<IReadOnlyList<Participation>> GetByUtilisateurAsync(int idUtilisateur);
}
