using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface ISessionService
{
    Task<IReadOnlyList<Session>> GetSessionsAsync();
    Task<IReadOnlyList<Session>> GetSessionsFormationAsync(int idFormation);
    Task<IReadOnlyList<Session>> GetProchainesSessionsAsync(int limite = 5);
    Task<Session?> GetSessionAsync(int idSession);
    Task<int> EnregistrerSessionAsync(Session session);
    Task<bool> SupprimerSessionAsync(int idSession);
    Task<IReadOnlyList<Participation>> GetParticipationsSessionAsync(int idSession);
    Task<int> InscrireParticipantAsync(Participation participation);
    Task<bool> RetirerParticipantAsync(int idParticipation);
}
