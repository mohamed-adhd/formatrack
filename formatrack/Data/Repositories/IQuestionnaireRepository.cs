using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IQuestionnaireRepository : IRepository<Questionnaire>
{
    Task<IReadOnlyList<Questionnaire>> GetBySessionAsync(int idSession);
}
