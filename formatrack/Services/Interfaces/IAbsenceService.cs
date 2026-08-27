using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IAbsenceService
{
    Task<IReadOnlyList<AbsenceRetard>> ListerParUtilisateurAsync(int idUtilisateur);
    Task<int> AjouterAsync(AbsenceRetard item);
    Task<bool> ModifierMotifAsync(int id, string motif, bool justifiee);
}
