using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IQuestionRepository : IRepository<Question>
{
    Task<IReadOnlyList<Question>> GetByQuestionnaireAsync(int idQuestionnaire);
}
