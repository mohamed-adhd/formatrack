using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface INoteRepository : IRepository<Note>
{
    Task<IReadOnlyList<Note>> GetByModuleSessionAsync(int idModule, int idSession);
    Task<IReadOnlyList<Note>> GetByStagiaireAsync(int idStagiaire);
    Task<IReadOnlyList<Note>> GetBySessionAsync(int idSession);
    Task<Note?> GetUniqueAsync(int idStagiaire, int idModule, int idSession);
    Task BulkSaveAsync(IEnumerable<Note> notes);
}
