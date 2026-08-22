using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IFormationService
{
    Task<IReadOnlyList<Formation>> GetFormationsAsync(string? recherche = null);
}
