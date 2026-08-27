using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IUtilisateurService
{
    Task<IReadOnlyList<Utilisateur>> GetUtilisateursAsync();
    Task<IReadOnlyList<Utilisateur>> GetUtilisateursParDepartementAsync(string departement);
    Task<IReadOnlyList<Utilisateur>> GetUtilisateursParPromotionAsync(string promotion);
    Task<IReadOnlyList<Utilisateur>> GetFormateursParDepartementAsync(string departement);
    Task<Utilisateur?> GetUtilisateurAsync(int idUtilisateur);
    Task<Utilisateur?> GetUtilisateurParEmailAsync(string email);
    Task<int> EnregistrerUtilisateurAsync(Utilisateur utilisateur, string? motDePasse = null);
    Task<bool> ActiverUtilisateurAsync(int idUtilisateur, bool actif);
    Task<bool> SupprimerUtilisateurAsync(int idUtilisateur);
}
