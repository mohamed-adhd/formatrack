namespace formatrack.Services.Interfaces;
using formatrack.Models;

public interface IEvaluationService
{
    Task<IReadOnlyList<Evaluation>> GetEvaluationsAsync();
    Task<IReadOnlyList<Evaluation>> GetEvaluationsUtilisateurAsync(int idUtilisateur);
    Task<Evaluation?> GetEvaluationAsync(int idEvaluation);
    Task<int> DemarrerEvaluationAsync(int idUtilisateur, int idQuestionnaire);
    Task<bool> EnregistrerReponsesAsync(int idEvaluation, IEnumerable<Reponse> reponses);
    Task<bool> TerminerEvaluationAsync(int idEvaluation);
    Task<bool> SupprimerEvaluationAsync(int idEvaluation);
}
