using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface INotificationService
{
    Task<int> NotifierAsync(int idUtilisateur, string message);
    Task<IReadOnlyList<Notification>> GetNotificationsAsync(int idUtilisateur);
    Task<int> CompterNonLuesAsync(int idUtilisateur);
    Task<bool> MarquerLueAsync(int idNotification);
    Task<bool> MarquerToutesLuesAsync(int idUtilisateur);
}