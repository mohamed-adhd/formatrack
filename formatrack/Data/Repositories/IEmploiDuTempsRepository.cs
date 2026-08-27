using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IEmploiDuTempsRepository : IRepository<EmploiDuTemps>
{
    Task<IReadOnlyList<EmploiDuTemps>> GetByFormationAsync(int idFormation);
    Task<IReadOnlyList<EmploiDuTemps>> GetByTypeAsync(string typeEmploi);
    Task<IReadOnlyList<EmploiDuTemps>> GetPubliesAsync();
    Task<IReadOnlyList<EmploiDuTemps>> GetByDepartementAsync(string departement);
    Task<IReadOnlyList<EmploiDuTemps>> GetByPromotionAsync(string promotion);
    Task<IReadOnlyList<EmploiDuTemps>> GetByUploaderAsync(int uploadedBy);
}
