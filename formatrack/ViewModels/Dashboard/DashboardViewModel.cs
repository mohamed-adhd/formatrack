using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Dashboard;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IStatistiqueService _statistiqueService;
    private readonly ISessionService _sessionService;
    private readonly Action _openFormations;
    private readonly Action _logout;

    [ObservableProperty] private string _role = string.Empty;
    [ObservableProperty] private int _formationsCount;
    [ObservableProperty] private int _sessionsCount;
    [ObservableProperty] private int _utilisateursCount;
    [ObservableProperty] private int _questionnairesCount;
    [ObservableProperty] private string _tauxReussite = "0 %";
    [ObservableProperty] private string _message = "Chargement...";

    public ObservableCollection<Session> ProchainesSessions { get; } = new();

    public DashboardViewModel(IStatistiqueService statistiqueService, ISessionService sessionService, Action openFormations, Action logout, string role)
    {
        _statistiqueService = statistiqueService;
        _sessionService = sessionService;
        _openFormations = openFormations;
        _logout = logout;
        Role = role;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var stats = await _statistiqueService.GetDashboardStatsAsync();
        FormationsCount = stats.Formations;
        SessionsCount = stats.Sessions;
        UtilisateursCount = stats.Utilisateurs;
        QuestionnairesCount = stats.Questionnaires;
        TauxReussite = $"{stats.TauxReussite:0.#} %";

        ProchainesSessions.Clear();
        foreach (var session in await _sessionService.GetProchainesSessionsAsync())
            ProchainesSessions.Add(session);

        Message = ProchainesSessions.Count == 0 ? "Aucune session planifiee." : "Sessions a suivre";
    }

    [RelayCommand] private void OpenFormations() => _openFormations();
    [RelayCommand] private void Logout() => _logout();
}
