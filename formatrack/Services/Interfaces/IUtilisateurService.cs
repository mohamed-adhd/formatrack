namespace formatrack.Services.Interfaces;
using formatrack.Models;

public interface IUtilisateurService
{
    Task<IReadOnlyList<Utilisateur>> GetUtilisateursAsync();
    Task<Utilisateur?> GetUtilisateurAsync(int idUtilisateur);
    Task<Utilisateur?> GetUtilisateurParEmailAsync(string email);
    Task<int> EnregistrerUtilisateurAsync(Utilisateur utilisateur, string? motDePasse = null);
    Task<bool> ActiverUtilisateurAsync(int idUtilisateur, bool actif);
    Task<bool> SupprimerUtilisateurAsync(int idUtilisateur);
}
