using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repos;

    public NotificationService(INotificationRepository? repos = null)
        => _repos = repos ?? new NotificationRepository();

    public async Task<int> NotifierAsync(int idUtilisateur, string message)
        => await _repos.AddAsync(new Notification { IdUtilisateur = idUtilisateur, Message = message, Lue = false });

    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync(int idUtilisateur)
        => await _repos.GetByUtilisateurAsync(idUtilisateur);

    public async Task<int> CompterNonLuesAsync(int idUtilisateur)
        => await _repos.CompterNonLuesAsync(idUtilisateur);

    public async Task<bool> MarquerLueAsync(int idNotification)
        => await _repos.MarquerLueAsync(idNotification);

    public async Task<bool> MarquerToutesLuesAsync(int idUtilisateur)
        => await _repos.MarquerToutesLuesAsync(idUtilisateur);
}