using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class ModuleService : IModuleService
{
    private readonly IModuleRepository _repo;
    public ModuleService(IModuleRepository? repo = null) => _repo = repo ?? new ModuleRepository();

    public async Task<IReadOnlyList<Module>> GetByFormationAsync(int idFormation) => await _repo.GetByFormationAsync(idFormation);
    public async Task<IReadOnlyList<Module>> GetCommunsAsync() => await _repo.GetCommunsAsync();
    public async Task<Module?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);
}

public class NoteService : INoteService
{
    private readonly INoteRepository _repo;
    public NoteService(INoteRepository? repo = null) => _repo = repo ?? new NoteRepository();

    public async Task<IReadOnlyList<Note>> GetByModuleSessionAsync(int idModule, int idSession) => await _repo.GetByModuleSessionAsync(idModule, idSession);
    public async Task<IReadOnlyList<Note>> GetByStagiaireAsync(int idStagiaire) => await _repo.GetByStagiaireAsync(idStagiaire);
    public async Task<IReadOnlyList<Note>> GetBySessionAsync(int idSession) => await _repo.GetBySessionAsync(idSession);
    public async Task<Note?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

    public async Task BulkSaveAsync(IEnumerable<Note> notes, int saisiPar)
    {
        foreach (var note in notes)
            note.SaisiPar = saisiPar;
        await _repo.BulkSaveAsync(notes);
    }

    public async Task<bool> SupprimerAsync(int id) => await _repo.DeleteAsync(id);
}
