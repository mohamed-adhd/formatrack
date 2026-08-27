using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Questionnaires;

public partial class QuestionnaireEditorViewModel : ViewModelBase
{
    private readonly IQuestionnaireService _questionnaireService;
    private readonly ISessionService _sessionService;
    private readonly Action<bool> _onCompleted;
    private int? _idQuestionnaire;
    private readonly Action _openDashboard;

    [ObservableProperty] private string _titre = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _typeEvaluation = "AChaud";
    [ObservableProperty] private double _noteMaximale = 20;
    [ObservableProperty] private int? _dureeMinutes = 30;
    [ObservableProperty] private string _statut = "Publie";
    [ObservableProperty] private int _idSession;
    [ObservableProperty] private Session? _selectedSession;
    [ObservableProperty] private string _message = string.Empty;

    // New Question Inline Creator
    [ObservableProperty] private string _newQuestionEnonce = string.Empty;
    [ObservableProperty] private string _newQuestionType = "Notation";
    [ObservableProperty] private double _newQuestionBareme = 5.0;
    [ObservableProperty] private Critere? _selectedNewQuestionCritere;

    // Critere Creator
    [ObservableProperty] private string _newCritereLibelle = string.Empty;
    [ObservableProperty] private double _newCritereCoefficient = 1.0;

    public ObservableCollection<Session> Sessions { get; } = new();
    public ObservableCollection<Question> Questions { get; } = new();
    public ObservableCollection<Critere> Criteres { get; } = new();
    public ObservableCollection<string> TypeEvaluationOptions { get; } = new() { "AChaud", "AFroid", "Competences" };
    public ObservableCollection<string> StatutOptions { get; } = new() { "Brouillon", "Publie", "Archive" };
    public ObservableCollection<string> TypeQuestionOptions { get; } = new() { "Notation", "QCM", "TexteLibre", "VraiFaux" };

    public bool HasExistingQuestions => _idQuestionnaire.HasValue;

    public QuestionnaireEditorViewModel(
        IQuestionnaireService questionnaireService,
        ISessionService sessionService,
        Action<bool> onCompleted,
        int? idQuestionnaire = null,
        Action? openDashboard = null)
    {
        _questionnaireService = questionnaireService;
        _sessionService = sessionService;
        _onCompleted = onCompleted;
        _idQuestionnaire = idQuestionnaire;
        _openDashboard = openDashboard ?? (() => { });
        _ = InitializeAsync();
    }

    partial void OnSelectedSessionChanged(Session? value)
    {
        if (value != null)
        {
            IdSession = value.IdSession;
        }
    }

    private async Task InitializeAsync()
    {
        var sessions = await _sessionService.GetSessionsAsync();
        Sessions.Clear();
        foreach (var s in sessions)
            Sessions.Add(s);

        if (_idQuestionnaire.HasValue)
        {
            var q = await _questionnaireService.GetQuestionnaireAsync(_idQuestionnaire.Value);
            if (q != null)
            {
                Titre = q.Titre;
                Description = q.Description;
                TypeEvaluation = q.TypeEvaluation;
                NoteMaximale = q.NoteMaximale;
                DureeMinutes = q.DureeMinutes;
                Statut = q.Statut;
                IdSession = q.IdSession;
                SelectedSession = Sessions.FirstOrDefault(s => s.IdSession == IdSession);
                Message = "Édition du questionnaire & gestion des questions";
            }
            await LoadCriteresAsync();
            await LoadQuestionsAsync();
        }
        else
        {
            Message = "Nouveau Questionnaire d'Évaluation";
            Statut = "Publie";
            TypeEvaluation = "AChaud";
            if (Sessions.Count > 0)
                SelectedSession = Sessions[0];
        }
    }

    private async Task LoadCriteresAsync()
    {
        if (!_idQuestionnaire.HasValue) return;
        Criteres.Clear();
        var criteres = await _questionnaireService.GetCriteresAsync(_idQuestionnaire.Value);
        foreach (var c in criteres)
            Criteres.Add(c);
    }

    private async Task LoadQuestionsAsync()
    {
        if (!_idQuestionnaire.HasValue) return;
        Questions.Clear();
        var questions = await _questionnaireService.GetQuestionsAsync(_idQuestionnaire.Value);
        foreach (var quest in questions)
        {
            quest.SelectedCritere = Criteres.FirstOrDefault(c => c.IdCritere == quest.IdCritere);
            Questions.Add(quest);
        }
        OnPropertyChanged(nameof(HasExistingQuestions));
    }

    [RelayCommand]
    private async Task AddQuestionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewQuestionEnonce))
        {
            Message = "Veuillez saisir l'énoncé de la question";
            return;
        }

        if (!_idQuestionnaire.HasValue || _idQuestionnaire.Value == 0)
        {
            // Auto save questionnaire first
            await SaveInternalAsync();
        }

        if (_idQuestionnaire.HasValue && _idQuestionnaire.Value > 0)
        {
            var q = new Question
            {
                IdQuestionnaire = _idQuestionnaire.Value,
                Enonce = NewQuestionEnonce.Trim(),
                TypeQuestion = string.IsNullOrWhiteSpace(NewQuestionType) ? "Notation" : NewQuestionType.Trim(),
                Bareme = NewQuestionBareme <= 0 ? 5.0 : NewQuestionBareme,
                Ordre = Questions.Count + 1,
                IdCritere = SelectedNewQuestionCritere?.IdCritere
            };
            await _questionnaireService.EnregistrerQuestionAsync(q);
            NewQuestionEnonce = string.Empty;
            SelectedNewQuestionCritere = null;
            await LoadQuestionsAsync();
            Message = "Question ajoutée avec succès.";
        }
    }

    [RelayCommand]
    private async Task AddCritereAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCritereLibelle))
        {
            Message = "Veuillez saisir le libellé du critère";
            return;
        }

        if (!_idQuestionnaire.HasValue || _idQuestionnaire.Value == 0)
        {
            // Auto save questionnaire first
            await SaveInternalAsync();
        }

        if (_idQuestionnaire.HasValue && _idQuestionnaire.Value > 0)
        {
            var c = new Critere
            {
                IdQuestionnaire = _idQuestionnaire.Value,
                Libelle = NewCritereLibelle.Trim(),
                Coefficient = NewCritereCoefficient <= 0 ? 1.0 : NewCritereCoefficient,
                Description = string.Empty
            };
            await _questionnaireService.EnregistrerCritereAsync(c);
            NewCritereLibelle = string.Empty;
            NewCritereCoefficient = 1.0;
            await LoadCriteresAsync();
            await LoadQuestionsAsync(); // refresh references
            Message = "Critère ajouté avec succès.";
        }
    }

    [RelayCommand]
    private async Task DeleteCritereAsync(Critere critere)
    {
        if (critere == null) return;
        await _questionnaireService.SupprimerCritereAsync(critere.IdCritere);
        await LoadCriteresAsync();
        await LoadQuestionsAsync();
        Message = "Critère supprimé.";
    }

    [RelayCommand]
    private async Task DeleteQuestionAsync(Question question)
    {
        if (question == null) return;
        await _questionnaireService.SupprimerQuestionAsync(question.IdQuestion);
        await LoadQuestionsAsync();
    }

    private async Task<bool> SaveInternalAsync()
    {
        if (SelectedSession != null)
            IdSession = SelectedSession.IdSession;

        if (string.IsNullOrWhiteSpace(Titre))
        {
            Message = "Le titre du questionnaire est obligatoire";
            return false;
        }

        var q = new Questionnaire
        {
            IdQuestionnaire = _idQuestionnaire ?? 0,
            IdSession = IdSession,
            Titre = Titre.Trim(),
            Description = Description.Trim(),
            TypeEvaluation = string.IsNullOrWhiteSpace(TypeEvaluation) ? "AChaud" : TypeEvaluation.Trim(),
            NoteMaximale = NoteMaximale <= 0 ? 20 : NoteMaximale,
            DureeMinutes = DureeMinutes,
            Statut = string.IsNullOrWhiteSpace(Statut) ? "Publie" : Statut.Trim()
        };

        int result = await _questionnaireService.EnregistrerQuestionnaireAsync(q);
        if (result > 0)
        {
            if (!_idQuestionnaire.HasValue)
                _idQuestionnaire = result;

            foreach (var question in Questions)
            {
                question.IdCritere = question.SelectedCritere?.IdCritere;
                await _questionnaireService.EnregistrerQuestionAsync(question);
            }
            return true;
        }
        Message = "Erreur lors de l'enregistrement";
        return false;
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            var success = await SaveInternalAsync();
            if (success)
                _onCompleted?.Invoke(true);
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => _onCompleted?.Invoke(false);

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}

