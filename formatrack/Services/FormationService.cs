using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class FormationService : IFormationService
{
    private readonly IFormationRepository _formations;
    private readonly ISessionRepository _sessions;

    public FormationService(IFormationRepository? formations = null, ISessionRepository? sessions = null)
    {
        _formations = formations ?? new FormationRepository();
        _sessions = sessions ?? new SessionRepository();
    }

    public async Task<IReadOnlyList<Formation>> GetFormationsAsync(string? recherche = null)
        => await _formations.SearchAsync(recherche);

    public async Task<Formation?> GetFormationAsync(int idFormation)
        => await _formations.GetByIdAsync(idFormation);

    public async Task<int> EnregistrerFormationAsync(Formation formation)
        => formation.IdFormation > 0
            ? await _formations.UpdateAsync(formation) ? formation.IdFormation : 0
            : await _formations.AddAsync(formation);

    public async Task<bool> SupprimerFormationAsync(int idFormation)
    {
        foreach (var session in await _sessions.GetByFormationAsync(idFormation))
            await _sessions.DeleteAsync(session.IdSession);
        return await _formations.DeleteAsync(idFormation);
    }
}