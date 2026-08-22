namespace formatrack.Services.Interfaces;
using formatrack.Models;

public interface IQuestionnaireService
{
    Task<IReadOnlyList<Questionnaire>> GetQuestionnairesAsync();
    Task<IReadOnlyList<Questionnaire>> GetQuestionnairesSessionAsync(int idSession);
    Task<Questionnaire?> GetQuestionnaireAsync(int idQuestionnaire);
    Task<int> EnregistrerQuestionnaireAsync(Questionnaire questionnaire);
    Task<bool> SupprimerQuestionnaireAsync(int idQuestionnaire);
    Task<IReadOnlyList<Critere>> GetCriteresAsync(int idQuestionnaire);
    Task<int> EnregistrerCritereAsync(Critere critere);
    Task<bool> SupprimerCritereAsync(int idCritere);
    Task<IReadOnlyList<Question>> GetQuestionsAsync(int idQuestionnaire);
    Task<int> EnregistrerQuestionAsync(Question question);
    Task<bool> SupprimerQuestionAsync(int idQuestion);
}
