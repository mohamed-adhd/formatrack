using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Questionnaires;

public partial class QuestionnairesListViewModel : ViewModelBase
{
    private readonly IQuestionnaireService _questionnaireService;
    private readonly ISessionService _sessionService;
    private readonly Action _openDashboard;
    private readonly Action<int?> _openQuestionnaireEditor;

    [ObservableProperty] private string _recherche = string.Empty;
    [ObservableProperty] private string _message = "Chargement...";

    public ObservableCollection<Questionnaire> Questionnaires { get; } = new();

    public QuestionnairesListViewModel(
        IQuestionnaireService questionnaireService,
        ISessionService sessionService,
        Action openDashboard,
        Action<int?> openQuestionnaireEditor,
        Action openFormations,
        Action openUtilisateurs,
        Action openSessions,
        Action openEvaluations,
        Action openStatistiques)
    {
        _questionnaireService = questionnaireService;
        _sessionService = sessionService;
        _openDashboard = openDashboard;
        _openQuestionnaireEditor = openQuestionnaireEditor;
        _ = LoadAsync();
    }

    partial void OnRechercheChanged(string value)
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Questionnaires.Clear();
        var list = await _questionnaireService.GetQuestionnairesAsync();
        if (!string.IsNullOrWhiteSpace(Recherche))
        {
            var term = Recherche.ToLowerInvariant();
            foreach (var q in list)
                if (q.Titre.ToLowerInvariant().Contains(term) ||
                    q.SessionTitre.ToLowerInvariant().Contains(term) ||
                    q.TypeEvaluation.ToLowerInvariant().Contains(term))
                    Questionnaires.Add(q);
        }
        else
        {
            foreach (var q in list)
                Questionnaires.Add(q);
        }

        Message = Questionnaires.Count == 0 ? "Aucun questionnaire trouvé." : $"{Questionnaires.Count} questionnaire(s)";
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync();
    [RelayCommand] private void OpenEditor(Questionnaire questionnaire) => _openQuestionnaireEditor(questionnaire.IdQuestionnaire);
    [RelayCommand] private void CreateNew() => _openQuestionnaireEditor(null);
    [RelayCommand] private void OpenDashboard() => _openDashboard();
}
