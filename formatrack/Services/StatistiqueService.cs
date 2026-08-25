using System;
using System.Collections.Generic;
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

    public async Task<IReadOnlyList<StatistiqueFormation>> GetStatistiquesFormationsAsync()
    {
        await AppDbContext.InitializeAsync();
        var resultats = new List<StatistiqueFormation>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT f.titre,
       COUNT(DISTINCT s.id_session) AS sessions,
       COUNT(DISTINCT p.id_participation) AS participants,
       COALESCE(AVG(CASE WHEN e.statut = 'Terminee' THEN e.pourcentage END), 0) AS taux
FROM formations f
LEFT JOIN sessions s ON s.id_formation = f.id_formation
LEFT JOIN participation p ON p.id_session = s.id_session
LEFT JOIN questionnaires q ON q.id_session = s.id_session
LEFT JOIN evaluations e ON e.id_questionnaire = q.id_questionnaire
GROUP BY f.id_formation
ORDER BY f.id_formation DESC;";

        await using var command = new SqliteCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            resultats.Add(new StatistiqueFormation(
                Formation: reader.GetString(0),
                Sessions: reader.GetInt32(1),
                Participants: reader.GetInt32(2),
                TauxReussite: Convert.ToDouble(reader.GetValue(3))));
        }

        return resultats;
    }

    private static async Task<int> CountAsync(SqliteConnection connection, string table)
    {
        await using var command = new SqliteCommand($"SELECT COUNT(*) FROM {table};", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }
}