using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Evaluations;

public partial class EvaluationsListViewModel : ViewModelBase
{
    private readonly IEvaluationService _evaluationService;
    private readonly IQuestionnaireService _questionnaireService;
    private readonly Action _openDashboard;
    private readonly Action<int> _openEvaluationPasser;
    private readonly Action<int> _openEvaluationResultat;
    private readonly string _role;
    private readonly int _userId;

    [ObservableProperty] private string _recherche = string.Empty;
    [ObservableProperty] private string _message = "Chargement...";

    public ObservableCollection<Evaluation> Evaluations { get; } = new();

    public EvaluationsListViewModel(
        IEvaluationService evaluationService,
        IQuestionnaireService questionnaireService,
        Action openDashboard,
        Action<int> openEvaluationPasser,
        Action<int> openEvaluationResultat,
        Action openFormations,
        Action openUtilisateurs,
        Action openSessions,
        Action openQuestionnaires,
        Action openStatistiques,
        string role = "",
        int userId = 0)
    {
        _evaluationService = evaluationService;
        _questionnaireService = questionnaireService;
        _openDashboard = openDashboard;
        _openEvaluationPasser = openEvaluationPasser;
        _openEvaluationResultat = openEvaluationResultat;
        _role = role;
        _userId = userId;
        _ = LoadAsync();
    }

    partial void OnRechercheChanged(string value)
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Evaluations.Clear();
        var list = _role == "Stagiaire"
            ? await _evaluationService.GetEvaluationsUtilisateurAsync(_userId)
            : await _evaluationService.GetEvaluationsAsync();
        if (!string.IsNullOrWhiteSpace(Recherche))
        {
            var term = Recherche.ToLowerInvariant();
            foreach (var e in list)
                if (e.UtilisateurNom.ToLowerInvariant().Contains(term) ||
                    e.QuestionnaireTitre.ToLowerInvariant().Contains(term) ||
                    e.Statut.ToLowerInvariant().Contains(term))
                    Evaluations.Add(e);
        }
        else
        {
            foreach (var e in list)
                Evaluations.Add(e);
        }

        Message = Evaluations.Count == 0 ? "Aucune evaluation trouvée." : $"{Evaluations.Count} evaluation(s)";
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync();
    [RelayCommand] private void OpenResultat(Evaluation evaluation) => _openEvaluationResultat(evaluation.IdEvaluation);
    [RelayCommand] private void OpenDashboard() => _openDashboard();
}
