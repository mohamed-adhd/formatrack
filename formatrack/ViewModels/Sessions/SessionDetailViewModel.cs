using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Sessions;

public partial class SessionDetailViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly Action _backToList;
    private readonly Action<Session> _editSession;
    private readonly Action _openDashboard;

    [ObservableProperty] private Session? _session;
    [ObservableProperty] private string _message = "Chargement...";

    public SessionDetailViewModel(
        ISessionService sessionService,
        Action backToList,
        Action<Session> editSession,
        Action openDashboard)
    {
        _sessionService = sessionService;
        _backToList = backToList;
        _editSession = editSession;
        _openDashboard = openDashboard;
    }

    public async Task InitializeAsync(int idSession)
    {
        try
        {
            Session = await _sessionService.GetSessionAsync(idSession);
            Message = Session == null ? "Session non trouvée." : string.Empty;
        }
        catch (Exception ex)
        {
            Message = $"Erreur de chargement : {ex.Message}";
        }
    }

    [RelayCommand]
    private void Edit()
    {
        if (Session != null)
            _editSession(Session);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (Session != null)
        {
            var ok = await _sessionService.SupprimerSessionAsync(Session.IdSession);
            if (ok)
            {
                await formatrack.Services.CompositionRoot.Journal.JournalerAsync(null, $"Suppression de la session {Session.TitreFormation}", $"ID: {Session.IdSession}, Lieu: {Session.Lieu}");
            }
            _backToList();
        }
    }

    [RelayCommand]
    private void BackToList() => _backToList();

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}
