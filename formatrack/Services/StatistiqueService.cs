using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class StatistiqueService : IStatistiqueService
{
    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();

        var formations = await CountAsync(connection, "formations");
        var sessions = await CountAsync(connection, "sessions");
        var utilisateurs = await CountAsync(connection, "utilisateurs");
        var questionnaires = await CountAsync(connection, "questionnaires");

        await using var score = new SqliteCommand("SELECT COALESCE(AVG(pourcentage),0) FROM evaluations WHERE statut = 'Terminee';", connection);
        var taux = Convert.ToDouble(await score.ExecuteScalarAsync() ?? 0d);
        return new DashboardStats(formations, sessions, utilisateurs, questionnaires, taux);
    }

    private static async Task<int> CountAsync(SqliteConnection connection, string table)
    {
        await using var command = new SqliteCommand($"SELECT COUNT(*) FROM {table};", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }
}
