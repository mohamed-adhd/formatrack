using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Evaluations;

public partial class EvaluationPasserViewModel : ViewModelBase
{
    private readonly IEvaluationService _evaluationService;
    private readonly IQuestionnaireService _questionnaireService;
    private readonly Action<int> _onEvaluationComplete;
    private readonly Action _openDashboard;

    [ObservableProperty] private Questionnaire? _questionnaire;
    [ObservableProperty] private Question? _currentQuestion;
    [ObservableProperty] private string _reponseTexte = string.Empty;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalQuestions;
    [ObservableProperty] private string _message = "Chargement...";
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private double _progressPercentage;

    public bool IsLastQuestion => CurrentIndex >= TotalQuestions - 1;
    public bool IsFirstQuestion => CurrentIndex == 0;

    private List<Question> _questions = new();
    private Dictionary<int, string> _reponses = new();
    private int _idEvaluation;

    public ObservableCollection<Question> Questions { get; } = new();

    public EvaluationPasserViewModel(
        IEvaluationService evaluationService,
        IQuestionnaireService questionnaireService,
        Action<int> onEvaluationComplete,
        Action openDashboard)
    {
        _evaluationService = evaluationService;
        _questionnaireService = questionnaireService;
        _onEvaluationComplete = onEvaluationComplete;
        _openDashboard = openDashboard;
    }

    public async Task InitializeAsync(int idQuestionnaire, int idUtilisateur = 1)
    {
        try
        {
            Questionnaire = await _questionnaireService.GetQuestionnaireAsync(idQuestionnaire);
            var qList = await _questionnaireService.GetQuestionsAsync(idQuestionnaire);
            _questions = new List<Question>(qList);
            TotalQuestions = _questions.Count;

            if (TotalQuestions > 0)
            {
                _idEvaluation = await _evaluationService.DemarrerEvaluationAsync(idUtilisateur, idQuestionnaire);
                CurrentIndex = 0;
                CurrentQuestion = _questions[0];
                Message = $"Question {CurrentIndex + 1} sur {TotalQuestions}";
                UpdateProgress();
            }
            else
            {
                Message = "Aucune question disponible pour ce questionnaire.";
            }
        }
        catch (Exception ex)
        {
            Message = $"Erreur d'initialisation : {ex.Message}";
        }
    }

    private void UpdateProgress()
    {
        ProgressPercentage = TotalQuestions > 0 ? ((double)(CurrentIndex + 1) / TotalQuestions) * 100.0 : 0;
        OnPropertyChanged(nameof(IsLastQuestion));
        OnPropertyChanged(nameof(IsFirstQuestion));
    }

    [RelayCommand]
    private void SetRating(string value)
    {
        ReponseTexte = value;
    }

    [RelayCommand]
    private void NextQuestion()
    {
        if (CurrentQuestion == null) return;

        _reponses[CurrentQuestion.IdQuestion] = ReponseTexte;
        ReponseTexte = string.Empty;

        if (CurrentIndex < TotalQuestions - 1)
        {
            CurrentIndex++;
            CurrentQuestion = _questions[CurrentIndex];
            Message = $"Question {CurrentIndex + 1} sur {TotalQuestions}";

            if (_reponses.ContainsKey(CurrentQuestion.IdQuestion))
                ReponseTexte = _reponses[CurrentQuestion.IdQuestion];

            UpdateProgress();
        }
    }

    [RelayCommand]
    private void PreviousQuestion()
    {
        if (CurrentIndex > 0)
        {
            if (CurrentQuestion != null)
                _reponses[CurrentQuestion.IdQuestion] = ReponseTexte;

            CurrentIndex--;
            CurrentQuestion = _questions[CurrentIndex];
            Message = $"Question {CurrentIndex + 1} sur {TotalQuestions}";

            if (_reponses.ContainsKey(CurrentQuestion.IdQuestion))
                ReponseTexte = _reponses[CurrentQuestion.IdQuestion];
            else
                ReponseTexte = string.Empty;

            UpdateProgress();
        }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (CurrentQuestion != null)
            _reponses[CurrentQuestion.IdQuestion] = ReponseTexte;

        var reponses = new List<Reponse>();
        foreach (var q in _questions)
        {
            if (_reponses.TryGetValue(q.IdQuestion, out var contenu))
            {
                reponses.Add(new Reponse
                {
                    IdQuestion = q.IdQuestion,
                    Contenu = contenu,
                    ScoreObtenu = 0
                });
            }
        }

        await _evaluationService.EnregistrerReponsesAsync(_idEvaluation, reponses);
        await _evaluationService.TerminerEvaluationAsync(_idEvaluation);

        IsCompleted = true;
        Message = "Évaluation finalisée avec succès.";
        _onEvaluationComplete(_idEvaluation);
    }

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}

