using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface ISessionService
{
    Task<IReadOnlyList<Session>> GetProchainesSessionsAsync(int limite = 5);
}
