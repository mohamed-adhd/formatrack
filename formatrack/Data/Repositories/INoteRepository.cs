using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface INoteRepository : IRepository<Note>
{
    Task<IReadOnlyList<Note>> GetByModuleSessionAsync(int idModule, int idSession);
    Task<IReadOnlyList<Note>> GetByStagiaireAsync(int idStagiaire);
    Task<IReadOnlyList<Note>> GetBySessionAsync(int idSession);
    Task<IReadOnlyList<Note>> GetAllNotesWithDetailsAsync(int? idFormation = null, string? promotion = null, IEnumerable<int>? sessionIds = null, string? etat = null);
    Task<Note?> GetUniqueAsync(int idStagiaire, int idModule, int idSession);
    Task BulkSaveAsync(IEnumerable<Note> notes);
}
