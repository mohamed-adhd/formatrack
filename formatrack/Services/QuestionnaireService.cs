using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class QuestionnaireService : IQuestionnaireService
{
    private readonly IQuestionnaireRepository _questionnaires;
    private readonly ICritereRepository _criteres;
    private readonly IQuestionRepository _questions;

    public QuestionnaireService(
        IQuestionnaireRepository? questionnaires = null,
        ICritereRepository? criteres = null,
        IQuestionRepository? questions = null)
    {
        _questionnaires = questionnaires ?? new QuestionnaireRepository();
        _criteres = criteres ?? new CritereRepository();
        _questions = questions ?? new QuestionRepository();
    }

    public async Task<IReadOnlyList<Questionnaire>> GetQuestionnairesAsync()
        => await _questionnaires.GetAllAsync();

    public async Task<IReadOnlyList<Questionnaire>> GetQuestionnairesSessionAsync(int idSession)
        => await _questionnaires.GetBySessionAsync(idSession);

    public async Task<Questionnaire?> GetQuestionnaireAsync(int idQuestionnaire)
        => await _questionnaires.GetByIdAsync(idQuestionnaire);

    public async Task<int> EnregistrerQuestionnaireAsync(Questionnaire questionnaire)
        => questionnaire.IdQuestionnaire > 0
            ? await _questionnaires.UpdateAsync(questionnaire) ? questionnaire.IdQuestionnaire : 0
            : await _questionnaires.AddAsync(questionnaire);

    public async Task<bool> SupprimerQuestionnaireAsync(int idQuestionnaire)
    {
        foreach (var question in await GetQuestionsAsync(idQuestionnaire))
            await _questions.DeleteAsync(question.IdQuestion);
        foreach (var critere in await GetCriteresAsync(idQuestionnaire))
            await _criteres.DeleteAsync(critere.IdCritere);
        return await _questionnaires.DeleteAsync(idQuestionnaire);
    }

    public async Task<IReadOnlyList<Critere>> GetCriteresAsync(int idQuestionnaire)
        => await _criteres.GetByQuestionnaireAsync(idQuestionnaire);

    public async Task<int> EnregistrerCritereAsync(Critere critere)
        => critere.IdCritere > 0
            ? await _criteres.UpdateAsync(critere) ? critere.IdCritere : 0
            : await _criteres.AddAsync(critere);

    public async Task<bool> SupprimerCritereAsync(int idCritere)
        => await _criteres.DeleteAsync(idCritere);

    public async Task<IReadOnlyList<Question>> GetQuestionsAsync(int idQuestionnaire)
        => await _questions.GetByQuestionnaireAsync(idQuestionnaire);

    public async Task<int> EnregistrerQuestionAsync(Question question)
        => question.IdQuestion > 0
            ? await _questions.UpdateAsync(question) ? question.IdQuestion : 0
            : await _questions.AddAsync(question);

    public async Task<bool> SupprimerQuestionAsync(int idQuestion)
        => await _questions.DeleteAsync(idQuestion);
}