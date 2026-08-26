using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Questionnaires;

public partial class QuestionEditorViewModel : ViewModelBase
{
    private readonly IQuestionnaireService _questionnaireService;
    private readonly Action _openDashboard;

    [ObservableProperty] private int _idQuestionnaire;
    [ObservableProperty] private string _enonce = string.Empty;
    [ObservableProperty] private string _typeQuestion = string.Empty;
    [ObservableProperty] private double _bareme;
    [ObservableProperty] private int _ordre;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private Question? _selectedQuestion;

    public ObservableCollection<Question> Questions { get; } = new();
    public ObservableCollection<string> TypeQuestionOptions { get; } = new() { "TexteLibre", "QCM", "VraiFaux", "Notation" };

    public QuestionEditorViewModel(
        IQuestionnaireService questionnaireService,
        int idQuestionnaire,
        Action openDashboard)
    {
        _questionnaireService = questionnaireService;
        _openDashboard = openDashboard;
        IdQuestionnaire = idQuestionnaire;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Questions.Clear();
        var list = await _questionnaireService.GetQuestionsAsync(IdQuestionnaire);
        foreach (var q in list)
            Questions.Add(q);

        Message = Questions.Count == 0 ? "Aucune question." : $"{Questions.Count} question(s)";
    }

    [RelayCommand]
    private async Task AddQuestion()
    {
        if (string.IsNullOrWhiteSpace(Enonce))
        {
            Message = "L'énnoncé est obligatoire";
            return;
        }

        var q = new Question
        {
            IdQuestionnaire = IdQuestionnaire,
            Enonce = Enonce.Trim(),
            TypeQuestion = string.IsNullOrWhiteSpace(TypeQuestion) ? "TexteLibre" : TypeQuestion.Trim(),
            Bareme = Bareme,
            Ordre = Questions.Count + 1
        };

        await _questionnaireService.EnregistrerQuestionAsync(q);
        Enonce = string.Empty;
        Bareme = 0;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteQuestion(Question question)
    {
        await _questionnaireService.SupprimerQuestionAsync(question.IdQuestion);
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}
