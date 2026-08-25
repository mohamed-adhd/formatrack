using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class EvaluationService : IEvaluationService
{
    private readonly IEvaluationRepository _evaluations;
    private readonly IReponseRepository _reponses;
    private readonly IQuestionRepository _questions;
    private readonly IQuestionnaireRepository _questionnaires;

    public EvaluationService(
        IEvaluationRepository? evaluations = null,
        IReponseRepository? reponses = null,
        IQuestionRepository? questions = null,
        IQuestionnaireRepository? questionnaires = null)
    {
        _evaluations = evaluations ?? new EvaluationRepository();
        _reponses = reponses ?? new ReponseRepository();
        _questions = questions ?? new QuestionRepository();
        _questionnaires = questionnaires ?? new QuestionnaireRepository();
    }

    public async Task<IReadOnlyList<Evaluation>> GetEvaluationsAsync()
        => await _evaluations.GetAllAsync();

    public async Task<IReadOnlyList<Evaluation>> GetEvaluationsUtilisateurAsync(int idUtilisateur)
        => await _evaluations.GetByUtilisateurAsync(idUtilisateur);

    public async Task<Evaluation?> GetEvaluationAsync(int idEvaluation)
        => await _evaluations.GetByIdAsync(idEvaluation);

    public async Task<int> DemarrerEvaluationAsync(int idUtilisateur, int idQuestionnaire)
    {
        var questionnaire = await _questionnaires.GetByIdAsync(idQuestionnaire);
        return await _evaluations.AddAsync(new Evaluation
        {
            IdUtilisateur = idUtilisateur,
            IdQuestionnaire = idQuestionnaire,
            DatePassage = DateTime.Now,
            Statut = "EnCours",
            ScoreTotal = 0,
            Pourcentage = 0
        });
    }

    public async Task<bool> EnregistrerReponsesAsync(int idEvaluation, IEnumerable<Reponse> reponses)
    {
        var ok = true;
        foreach (var reponse in reponses)
        {
            reponse.IdEvaluation = idEvaluation;
            if (reponse.IdReponse > 0)
                ok &= await _reponses.UpdateAsync(reponse);
            else
                reponse.IdReponse = await _reponses.AddAsync(reponse);
        }

        return ok;
    }

    public async Task<bool> TerminerEvaluationAsync(int idEvaluation)
    {
        var evaluation = await _evaluations.GetByIdAsync(idEvaluation);
        if (evaluation is null)
            return false;

        var questions = await _questions.GetByQuestionnaireAsync(evaluation.IdQuestionnaire);
        var parQuestion = (await _reponses.GetByEvaluationAsync(idEvaluation))
            .ToDictionary(r => r.IdQuestion, r => r);

        var scoreTotal = 0d;
        var baremeTotal = 0d;

        foreach (var question in questions)
        {
            if (question.Bareme > 0)
                baremeTotal += question.Bareme;

            if (!parQuestion.TryGetValue(question.IdQuestion, out var reponse))
                continue;

            double? score = reponse.ScoreObtenu;
            if (score is null && reponse.EstCorrecte.HasValue)
                score = reponse.EstCorrecte.Value ? question.Bareme : 0d;

            reponse.ScoreObtenu = score ?? 0d;
            await _reponses.UpdateAsync(reponse);
            scoreTotal += reponse.ScoreObtenu ?? 0d;
        }

        var pourcentage = baremeTotal > 0 ? Math.Round(scoreTotal / baremeTotal * 100, 2) : 0d;
        return await _evaluations.TerminerAsync(idEvaluation, Math.Round(scoreTotal, 2), pourcentage);
    }

    public async Task<bool> SupprimerEvaluationAsync(int idEvaluation)
    {
        foreach (var reponse in await _reponses.GetByEvaluationAsync(idEvaluation))
            await _reponses.DeleteAsync(reponse.IdReponse);
        return await _evaluations.DeleteAsync(idEvaluation);
    }
}