using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessions;
    private readonly IParticipationRepository _participations;

    public SessionService(ISessionRepository? sessions = null, IParticipationRepository? participations = null)
    {
        _sessions = sessions ?? new SessionRepository();
        _participations = participations ?? new ParticipationRepository();
    }

    public async Task<IReadOnlyList<Session>> GetSessionsAsync()
        => await _sessions.GetAllAsync();

    public async Task<IReadOnlyList<Session>> GetSessionsFormationAsync(int idFormation)
        => await _sessions.GetByFormationAsync(idFormation);

    public async Task<IReadOnlyList<Session>> GetProchainesSessionsAsync(int limite = 5)
        => await _sessions.GetUpcomingAsync(limite);

    public async Task<Session?> GetSessionAsync(int idSession)
        => await _sessions.GetByIdAsync(idSession);

    public async Task<int> EnregistrerSessionAsync(Session session)
        => session.IdSession > 0
            ? await _sessions.UpdateAsync(session) ? session.IdSession : 0
            : await _sessions.AddAsync(session);

    public async Task<bool> SupprimerSessionAsync(int idSession)
    {
        foreach (var participation in await GetParticipationsSessionAsync(idSession))
            await _participations.DeleteAsync(participation.IdParticipation);
        return await _sessions.DeleteAsync(idSession);
    }

    public async Task<IReadOnlyList<Participation>> GetParticipationsSessionAsync(int idSession)
        => await _participations.GetBySessionAsync(idSession);

    public async Task<int> InscrireParticipantAsync(Participation participation)
        => await _participations.AddAsync(participation);

    public async Task<bool> RetirerParticipantAsync(int idParticipation)
        => await _participations.DeleteAsync(idParticipation);
}