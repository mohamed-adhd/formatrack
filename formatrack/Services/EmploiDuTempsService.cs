using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class EmploiDuTempsService : IEmploiDuTempsService
{
    private readonly IEmploiDuTempsRepository _repo;
    private readonly IFormationRepository _formations;

    public EmploiDuTempsService(IEmploiDuTempsRepository? repo = null, IFormationRepository? formations = null)
    {
        _repo = repo ?? new EmploiDuTempsRepository();
        _formations = formations ?? new FormationRepository();
    }

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByFormationAsync(int idFormation)
        => await _repo.GetByFormationAsync(idFormation);

    public async Task<IReadOnlyList<EmploiDuTemps>> GetPubliesAsync()
        => await _repo.GetPubliesAsync();

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByDepartementAsync(string departement)
        => await _repo.GetByDepartementAsync(departement);

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByPromotionAsync(string promotion)
        => await _repo.GetByPromotionAsync(promotion);

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByUploaderAsync(int uploadedBy)
        => await _repo.GetByUploaderAsync(uploadedBy);

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByRoleAsync(string role, string departement, string promotion, int userId)
    {
        return role switch
        {
            "Administrateur" => await _repo.GetAllAsync(),
            "ResponsableFormation" => await _repo.GetPubliesAsync(),
            "ChefDepartement" => await _repo.GetByDepartementAsync(departement),
            "Formateur" => await _repo.GetByDepartementAsync(departement),
            "Stagiaire" => await _repo.GetByPromotionAsync(promotion),
            "Decideur" => await _repo.GetPubliesAsync(),
            _ => new List<EmploiDuTemps>()
        };
    }

    public async Task<EmploiDuTemps?> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

    public async Task<int> AjouterAsync(EmploiDuTemps emploi)
        => await _repo.AddAsync(emploi);

    public async Task<bool> ModifierAsync(EmploiDuTemps emploi)
        => await _repo.UpdateAsync(emploi);

    public async Task<bool> SupprimerAsync(int id)
        => await _repo.DeleteAsync(id);

    public async Task<bool> PublierAsync(int id)
    {
        var e = await _repo.GetByIdAsync(id);
        if (e == null) return false;
        e.Statut = "Publie";
        return await _repo.UpdateAsync(e);
    }

    public async Task<bool> DepublierAsync(int id)
    {
        var e = await _repo.GetByIdAsync(id);
        if (e == null) return false;
        e.Statut = "Brouillon";
        return await _repo.UpdateAsync(e);
    }
}
