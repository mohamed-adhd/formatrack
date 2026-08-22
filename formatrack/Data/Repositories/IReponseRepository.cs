using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IReponseRepository : IRepository<Reponse>
{
    Task<IReadOnlyList<Reponse>> GetByEvaluationAsync(int idEvaluation);
}
