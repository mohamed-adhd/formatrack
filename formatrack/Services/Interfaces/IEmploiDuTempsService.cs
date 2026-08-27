using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IEmploiDuTempsService
{
    Task<IReadOnlyList<EmploiDuTemps>> GetByFormationAsync(int idFormation);
    Task<IReadOnlyList<EmploiDuTemps>> GetPubliesAsync();
    Task<IReadOnlyList<EmploiDuTemps>> GetByDepartementAsync(string departement);
    Task<IReadOnlyList<EmploiDuTemps>> GetByPromotionAsync(string promotion);
    Task<IReadOnlyList<EmploiDuTemps>> GetByUploaderAsync(int uploadedBy);
    Task<IReadOnlyList<EmploiDuTemps>> GetByRoleAsync(string role, string departement, string promotion, int userId);
    Task<EmploiDuTemps?> GetByIdAsync(int id);
    Task<int> AjouterAsync(EmploiDuTemps emploi);
    Task<bool> ModifierAsync(EmploiDuTemps emploi);
    Task<bool> SupprimerAsync(int id);
    Task<bool> PublierAsync(int id);
    Task<bool> DepublierAsync(int id);
}
