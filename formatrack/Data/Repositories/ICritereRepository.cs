using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface ICritereRepository : IRepository<Critere>
{
    Task<IReadOnlyList<Critere>> GetByQuestionnaireAsync(int idQuestionnaire);
}
