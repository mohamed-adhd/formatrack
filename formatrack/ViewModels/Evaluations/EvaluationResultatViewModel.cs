using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Evaluations;

public partial class EvaluationResultatViewModel : ViewModelBase
{
    private readonly IEvaluationService _evaluationService;
    private readonly Action _openDashboard;

    [ObservableProperty] private Evaluation? _evaluation;
    [ObservableProperty] private string _message = "Chargement...";

    public EvaluationResultatViewModel(
        IEvaluationService evaluationService,
        Action openDashboard)
    {
        _evaluationService = evaluationService;
        _openDashboard = openDashboard;
    }

    public async Task InitializeAsync(int idEvaluation)
    {
        try
        {
            Evaluation = await _evaluationService.GetEvaluationAsync(idEvaluation);
            Message = Evaluation == null ? "Evaluation non trouvée." : string.Empty;
        }
        catch (Exception ex)
        {
            Message = $"Erreur de chargement : {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}
