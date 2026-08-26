using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Sessions;

public partial class SessionsListViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly IFormationService _formationService;
    private readonly Action _openDashboard;
    private readonly Action<int> _openSessionDetail;
    private readonly Action _openSessionCreate;

    [ObservableProperty] private string _recherche = string.Empty;
    [ObservableProperty] private string _message = "Chargement...";

    public ObservableCollection<Session> Sessions { get; } = new();

    public SessionsListViewModel(
        ISessionService sessionService,
        IFormationService formationService,
        Action openDashboard,
        Action<int> openSessionDetail,
        Action openSessionCreate,
        Action openFormations,
        Action openUtilisateurs,
        Action openEvaluations,
        Action openQuestionnaires,
        Action openStatistiques)
    {
        _sessionService = sessionService;
        _formationService = formationService;
        _openDashboard = openDashboard;
        _openSessionDetail = openSessionDetail;
        _openSessionCreate = openSessionCreate;
        _ = LoadAsync();
    }

    partial void OnRechercheChanged(string value)
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Sessions.Clear();
        var list = await _sessionService.GetSessionsAsync();
        if (!string.IsNullOrWhiteSpace(Recherche))
        {
            var term = Recherche.ToLowerInvariant();
            foreach (var s in list)
                if (s.TitreFormation.ToLowerInvariant().Contains(term) ||
                    s.Lieu.ToLowerInvariant().Contains(term) ||
                    s.Statut.ToLowerInvariant().Contains(term))
                    Sessions.Add(s);
        }
        else
        {
            foreach (var s in list)
                Sessions.Add(s);
        }

        Message = Sessions.Count == 0 ? "Aucune session trouvée." : $"{Sessions.Count} session(s)";
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync();
    [RelayCommand] private void OpenDetail(Session session) => _openSessionDetail(session.IdSession);
    [RelayCommand] private void OpenCreate() => _openSessionCreate();
    [RelayCommand] private void OpenDashboard() => _openDashboard();
}
