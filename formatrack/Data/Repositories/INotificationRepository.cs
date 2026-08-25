using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUtilisateurAsync(int idUtilisateur);
    Task<int> CompterNonLuesAsync(int idUtilisateur);
    Task<bool> MarquerLueAsync(int idNotification);
    Task<bool> MarquerToutesLuesAsync(int idUtilisateur);
}