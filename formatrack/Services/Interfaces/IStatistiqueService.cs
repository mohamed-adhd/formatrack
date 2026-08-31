using System.Collections.Generic;
using System.Threading.Tasks;

namespace formatrack.Services.Interfaces;

public record DashboardStats(int Formations, int Sessions, int Utilisateurs, int Questionnaires, double TauxReussite);
public record StatistiqueFormation(string Formation, int Sessions, int Participants, double TauxReussite);
public record StatistiqueDepartement(int Formations, int Sessions, int Formateurs, int Stagiaires, double TauxReussite);
public record ClassementStagiaire(string NomComplet, int IdUtilisateur, double Moyenne, int NbEvaluations);

public interface IStatistiqueService
{
    Task<DashboardStats> GetDashboardStatsAsync();
    Task<IReadOnlyList<StatistiqueFormation>> GetStatistiquesFormationsAsync();
    Task<StatistiqueDepartement> GetStatistiquesDepartementAsync(string departement);
    Task<IReadOnlyList<ClassementStagiaire>> GetClassementParSessionAsync(int idSession);
    Task<double> GetMoyenneParSessionEtPromotionAsync(int idSession, string promotion);
    Task<double> GetTauxReussiteParPromotionAsync(string promotion);
}
