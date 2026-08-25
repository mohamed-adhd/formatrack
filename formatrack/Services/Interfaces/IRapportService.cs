using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IRapportService
{
    Task<Rapport> GenererRapportAsync(string titre, string typeRapport, IReadOnlyList<string> colonnes, IReadOnlyList<IReadOnlyList<object?>> lignes, int? idUtilisateur = null);
    Task<IReadOnlyList<Rapport>> GetRapportsAsync();
    Task<bool> SupprimerRapportAsync(int idRapport);
}