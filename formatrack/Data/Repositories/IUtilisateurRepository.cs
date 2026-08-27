using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IUtilisateurRepository : IRepository<Utilisateur>
{
    Task<Utilisateur?> GetByEmailAsync(string email);
    Task<bool> SetActifAsync(int idUtilisateur, bool actif);
    Task<IReadOnlyList<Utilisateur>> GetByDepartementAsync(string departement);
    Task<IReadOnlyList<Utilisateur>> GetByPromotionAsync(string promotion);
    Task<IReadOnlyList<Utilisateur>> GetFormateursByDepartementAsync(string departement);
}
