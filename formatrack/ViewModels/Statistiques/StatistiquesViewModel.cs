using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Statistiques;

public partial class StatistiquesViewModel : ViewModelBase
{
    private readonly IStatistiqueService _statistiqueService;
    private readonly IFormationService _formationService;
    private readonly ISessionService _sessionService;
    private readonly Action _openDashboard;
    private readonly Action _openRapport;

    [ObservableProperty] private int _formationsCount;
    [ObservableProperty] private int _sessionsCount;
    [ObservableProperty] private int _utilisateursCount;
    [ObservableProperty] private int _questionnairesCount;
    [ObservableProperty] private string _tauxReussite = "0 %";
    [ObservableProperty] private string _message = "Chargement...";

    public StatistiquesViewModel(
        IStatistiqueService statistiqueService,
        IFormationService formationService,
        ISessionService sessionService,
        Action openDashboard,
        Action openRapport,
        Action openFormations,
        Action openUtilisateurs,
        Action openSessions,
        Action openEvaluations,
        Action openQuestionnaires)
    {
        _statistiqueService = statistiqueService;
        _formationService = formationService;
        _sessionService = sessionService;
        _openDashboard = openDashboard;
        _openRapport = openRapport;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var stats = await _statistiqueService.GetDashboardStatsAsync();
            FormationsCount = stats.Formations;
            SessionsCount = stats.Sessions;
            UtilisateursCount = stats.Utilisateurs;
            QuestionnairesCount = stats.Questionnaires;
            TauxReussite = $"{stats.TauxReussite:0.#} %";
            Message = "Statistiques et indicateurs de performance";
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenRapport() => _openRapport();

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}
