using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IEvaluationRepository : IRepository<Evaluation>
{
    Task<IReadOnlyList<Evaluation>> GetByUtilisateurAsync(int idUtilisateur);
    Task<bool> TerminerAsync(int idEvaluation, double scoreTotal, double pourcentage);
}
