using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace formatrack.Services.Interfaces;

public record FormationDecision(string Titre, string Priorite, double Score, string Justification);
public record StatistiqueFormationDetail(string Formation, int EvaluationsTerminees, int Participants, double Moyenne, double Mediane, double EcartType, double TauxReussite);
public record IndicateursGlobaux(int Formations, int Sessions, double MoyenneGlobale, double TauxReussiteGlobale);
public record PointTendance(string Periode, double Moyenne);
public record ScoreMulticritere(double Score, double Coherence);
public record RapportGenererResultat(string TypeRapport, string Titre, string CheminFichier, DateTime DateGeneration);

public interface IDecisionSupportApiService
{
    Task<IReadOnlyList<FormationDecision>> RecommanderFormationsAsync(int idFormation);
    Task<StatistiqueFormationDetail?> GetStatistiquesFormationAsync(int idFormation);
    Task<IndicateursGlobaux?> GetIndicateursGlobauxAsync();
    Task<IReadOnlyList<PointTendance>> AnalyserTendancesAsync(int? idFormation = null, DateTime? debut = null, DateTime? fin = null);
    Task<ScoreMulticritere?> ScoreMulticriteresAsync(IReadOnlyDictionary<string, double> poids, IReadOnlyDictionary<string, double> scores);
    Task<RapportGenererResultat?> GenererRapportAsync(string titre, string typeRapport, IReadOnlyDictionary<string, object?>? parametres = null);
}