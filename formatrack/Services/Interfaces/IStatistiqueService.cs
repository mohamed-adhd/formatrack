using System.Threading.Tasks;

namespace formatrack.Services.Interfaces;

public record DashboardStats(int Formations, int Sessions, int Utilisateurs, int Questionnaires, double TauxReussite);

public interface IStatistiqueService
{
    Task<DashboardStats> GetDashboardStatsAsync();
}
