using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class UtilisateurService : IUtilisateurService
{
    private readonly IUtilisateurRepository _repository;

    public UtilisateurService(IUtilisateurRepository? repository = null)
        => _repository = repository ?? new UtilisateurRepository();

    public async Task<IReadOnlyList<Utilisateur>> GetUtilisateursAsync()
        => await _repository.GetAllAsync();

    public async Task<Utilisateur?> GetUtilisateurAsync(int idUtilisateur)
        => await _repository.GetByIdAsync(idUtilisateur);

    public async Task<Utilisateur?> GetUtilisateurParEmailAsync(string email)
        => await _repository.GetByEmailAsync(email);

    public async Task<int> EnregistrerUtilisateurAsync(Utilisateur utilisateur, string? motDePasse = null)
    {
        if (!string.IsNullOrWhiteSpace(motDePasse))
        {
            utilisateur.MotDePasseHash = PasswordHasher.Hash(motDePasse);
        }
        else if (utilisateur.IdUtilisateur > 0 && string.IsNullOrWhiteSpace(utilisateur.MotDePasseHash))
        {
            var existant = await _repository.GetByIdAsync(utilisateur.IdUtilisateur);
            utilisateur.MotDePasseHash = existant?.MotDePasseHash ?? string.Empty;
        }

        return utilisateur.IdUtilisateur > 0
            ? await _repository.UpdateAsync(utilisateur) ? utilisateur.IdUtilisateur : 0
            : await _repository.AddAsync(utilisateur);
    }

    public async Task<bool> ActiverUtilisateurAsync(int idUtilisateur, bool actif)
        => await _repository.SetActifAsync(idUtilisateur, actif);

    public async Task<bool> SupprimerUtilisateurAsync(int idUtilisateur)
        => await _repository.DeleteAsync(idUtilisateur);
}