using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IEvaluationService
{
    Task<IReadOnlyList<Evaluation>> GetEvaluationsAsync();
    Task<IReadOnlyList<Evaluation>> GetEvaluationsUtilisateurAsync(int idUtilisateur);
    Task<IReadOnlyList<Evaluation>> GetEvaluationsParSessionAsync(int idSession);
    Task<Evaluation?> GetEvaluationAsync(int idEvaluation);
    Task<int> DemarrerEvaluationAsync(int idUtilisateur, int idQuestionnaire);
    Task<int> AjouterEvaluationAsync(Evaluation evaluation);
    Task<bool> EnregistrerReponsesAsync(int idEvaluation, IEnumerable<Reponse> reponses);
    Task<bool> TerminerEvaluationAsync(int idEvaluation);
    Task<bool> SupprimerEvaluationAsync(int idEvaluation);
}
