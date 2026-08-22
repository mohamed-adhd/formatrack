using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IFormationService
{
    Task<IReadOnlyList<Formation>> GetFormationsAsync(string? recherche = null);
    Task<Formation?> GetFormationAsync(int idFormation);
    Task<int> EnregistrerFormationAsync(Formation formation);
    Task<bool> SupprimerFormationAsync(int idFormation);
}
