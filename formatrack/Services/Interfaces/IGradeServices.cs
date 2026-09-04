using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IModuleService
{
    Task<IReadOnlyList<Module>> GetByFormationAsync(int idFormation);
    Task<IReadOnlyList<Module>> GetCommunsAsync();
    Task<Module?> GetByIdAsync(int id);
}

public interface INoteService
{
    Task<IReadOnlyList<Note>> GetByModuleSessionAsync(int idModule, int idSession);
    Task<IReadOnlyList<Note>> GetByStagiaireAsync(int idStagiaire);
    Task<IReadOnlyList<Note>> GetBySessionAsync(int idSession);
    Task<IReadOnlyList<Note>> GetAllNotesWithDetailsAsync(int? idFormation = null, string? promotion = null, IEnumerable<int>? sessionIds = null, string? etat = null);
    Task<Note?> GetByIdAsync(int id);
    Task BulkSaveAsync(IEnumerable<Note> notes, int saisiPar);
    Task<bool> SupprimerAsync(int id);
}
