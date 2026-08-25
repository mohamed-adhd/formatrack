using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IUtilisateurService
{
    Task<IReadOnlyList<Utilisateur>> GetUtilisateursAsync();
    Task<Utilisateur?> GetUtilisateurAsync(int idUtilisateur);
    Task<Utilisateur?> GetUtilisateurParEmailAsync(string email);
    Task<int> EnregistrerUtilisateurAsync(Utilisateur utilisateur, string? motDePasse = null);
    Task<bool> ActiverUtilisateurAsync(int idUtilisateur, bool actif);
    Task<bool> SupprimerUtilisateurAsync(int idUtilisateur);
}
