using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Formations;

public partial class FormationFormViewModel : ViewModelBase
{
    private readonly IFormationService _formationService;
    private readonly Action<bool> _onCompleted; // true if saved, false if cancelled
    private readonly int? _idFormation;
    private readonly Action _backToDashboard;
    private readonly Action _openUtilisateurs;
    private readonly Action _openSessions;
    private readonly Action _openEvaluations;
    private readonly Action _openQuestionnaires;
    private readonly Action _logout;

    [ObservableProperty] private string _titre = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _objectifs = string.Empty;
    [ObservableProperty] private int _dureeHeures;
    [ObservableProperty] private string _typeFormation = string.Empty;
    [ObservableProperty] private string _statut = string.Empty;
    [ObservableProperty] private string _message = string.Empty;

    public ObservableCollection<string> TypeFormationOptions { get; } = new()
    {
        "Presentielle",
        "Distancielle",
        "Hybride",
        "E-learning"
    };

    public ObservableCollection<string> StatutOptions { get; } = new()
    {
        "Planifiee",
        "En cours",
        "Terminee",
        "Annulee"
    };

    public FormationFormViewModel(IFormationService formationService, Action<bool> onCompleted, int? idFormation = null,
                                 Action? backToDashboard = null, Action? openUtilisateurs = null, Action? openSessions = null,
                                 Action? openEvaluations = null, Action? openQuestionnaires = null, Action? logout = null)
    {
        _formationService = formationService;
        _onCompleted = onCompleted;
        _idFormation = idFormation;
        _backToDashboard = backToDashboard ?? (() => { });
        _openUtilisateurs = openUtilisateurs ?? (() => { });
        _openSessions = openSessions ?? (() => { });
        _openEvaluations = openEvaluations ?? (() => { });
        _openQuestionnaires = openQuestionnaires ?? (() => { });
        _logout = logout ?? (() => { });
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_idFormation.HasValue)
        {
            var formation = await _formationService.GetFormationAsync(_idFormation.Value);
            if (formation != null)
            {
                Titre = formation.Titre;
                Description = formation.Description;
                Objectifs = formation.Objectifs;
                DureeHeures = formation.DureeHeures;
                TypeFormation = formation.TypeFormation;
                Statut = formation.Statut;
                Message = "Modification de formation";
            }
            else
            {
                Message = "Formation non trouvée";
            }
        }
        else
        {
            Message = "Nouvelle formation";
            // Set default values
            Statut = "Planifiee";
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Titre))
            {
                Message = "Le titre est obligatoire";
                return;
            }

            var formation = new Formation
            {
                IdFormation = _idFormation ?? 0,
                Titre = Titre.Trim(),
                Description = Description.Trim(),
                Objectifs = Objectifs.Trim(),
                DureeHeures = DureeHeures,
                TypeFormation = TypeFormation.Trim(),
                Statut = string.IsNullOrWhiteSpace(Statut) ? "Planifiee" : Statut.Trim()
            };

            int result = await _formationService.EnregistrerFormationAsync(formation);
            if (result > 0)
            {
                var actionStr = _idFormation.HasValue ? $"Modification de la formation {formation.Titre}" : $"Création de la formation {formation.Titre}";
                await formatrack.Services.CompositionRoot.Journal.JournalerAsync(null, actionStr, $"ID: {result}, Durée: {formation.DureeHeures}h, Type: {formation.TypeFormation}");
                _onCompleted?.Invoke(true); // Saved successfully
            }
            else
            {
                Message = "Erreur lors de l'enregistrement";
            }
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _onCompleted?.Invoke(false); // Cancelled
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