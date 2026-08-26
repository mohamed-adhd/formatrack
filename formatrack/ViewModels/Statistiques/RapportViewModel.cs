using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Statistiques;

public partial class RapportViewModel : ViewModelBase
{
    private readonly IStatistiqueService _statistiqueService;
    private readonly IFormationService _formationService;
    private readonly Action _openDashboard;

    [ObservableProperty] private string _titre = "Rapport statistique";
    [ObservableProperty] private string _message = "Generation du rapport...";
    [ObservableProperty] private string _rapportContenu = string.Empty;
    [ObservableProperty] private int _formationsCount;
    [ObservableProperty] private int _sessionsCount;
    [ObservableProperty] private int _utilisateursCount;
    [ObservableProperty] private string _tauxReussite = "0 %";

    public RapportViewModel(
        IStatistiqueService statistiqueService,
        IFormationService formationService,
        Action openDashboard)
    {
        _statistiqueService = statistiqueService;
        _formationService = formationService;
        _openDashboard = openDashboard;
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
            TauxReussite = $"{stats.TauxReussite:0.#} %";

            RapportContenu = $"""
                === RAPPORT STATISTIQUE SEFAD ===
                Date de generation: {DateTime.Now:dd/MM/yyyy HH:mm}

                --- RESUME GLOBAL ---
                Nombre de formations: {FormationsCount}
                Nombre de sessions: {SessionsCount}
                Nombre d'utilisateurs: {UtilisateursCount}
                Taux de reussite moyen: {TauxReussite}

                --- ANALYSE ---
                Ce rapport presente une vue d'ensemble des performances
                du systeme d'evaluation de la formation.

                Les indicateurs cles montrent une evolution positive
                des resultats d'evaluation sur la periode analysee.
                """;

            Message = "Rapport genere avec succes";
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}
