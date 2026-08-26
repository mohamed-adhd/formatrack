using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Formations;

public partial class FormationDetailViewModel : ViewModelBase
{
    private readonly IFormationService _formationService;
    private readonly Action _backToDashboard;
    private readonly Action<Formation> _editFormation;
    private readonly Action<int> _deleteFormation;
    private readonly Action _openUtilisateurs;
    private readonly Action _openSessions;
    private readonly Action _openEvaluations;
    private readonly Action _openQuestionnaires;
    private readonly Action _logout;

    [ObservableProperty] private Formation? _formation;
    [ObservableProperty] private string _message = "Chargement...";

    public FormationDetailViewModel(IFormationService formationService,
                                   Action backToDashboard,
                                   Action<Formation> editFormation,
                                   Action<int> deleteFormation,
                                   Action openUtilisateurs,
                                   Action openSessions,
                                   Action openEvaluations,
                                   Action openQuestionnaires,
                                   Action logout)
    {
        _formationService = formationService;
        _backToDashboard = backToDashboard;
        _editFormation = editFormation;
        _deleteFormation = deleteFormation;
        _openUtilisateurs = openUtilisateurs;
        _openSessions = openSessions;
        _openEvaluations = openEvaluations;
        _openQuestionnaires = openQuestionnaires;
        _logout = logout;
    }

    public async Task InitializeAsync(int idFormation)
    {
        try
        {
            Formation = await _formationService.GetFormationAsync(idFormation);
            Message = Formation == null ? "Formation non trouvée." : string.Empty;
        }
        catch (Exception ex)
        {
            Message = $"Erreur de chargement : {ex.Message}";
        }
    }

    [RelayCommand]
    private void Edit()
    {
        if (Formation != null)
            _editFormation(Formation);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (Formation != null)
        {
            // TODO: Show confirmation dialog
            var result = true; // Placeholder for actual dialog result
            if (result)
            {
                await _formationService.SupprimerFormationAsync(Formation.IdFormation);
                _backToDashboard();
            }
        }
    }

    [RelayCommand]
    private void BackToDashboard() => _backToDashboard();

    [RelayCommand]
    private void OpenUtilisateurs() => _openUtilisateurs();

    [RelayCommand]
    private void OpenSessions() => _openSessions();

    [RelayCommand]
    private void OpenEvaluations() => _openEvaluations();

    [RelayCommand]
    private void OpenQuestionnaires() => _openQuestionnaires();

    [RelayCommand]
    private void Logout() => _logout();
}