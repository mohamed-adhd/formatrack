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

    public async Task<StatistiqueDepartement> GetStatistiquesDepartementAsync(string departement)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT
  (SELECT COUNT(DISTINCT s.id_session) FROM sessions s
   JOIN questionnaires q ON q.id_session = s.id_session
   JOIN evaluations e ON e.id_questionnaire = q.id_questionnaire
   JOIN utilisateurs u ON u.id_utilisateur = e.id_utilisateur
   WHERE u.departement = $dep) AS sessions,
  (SELECT COUNT(*) FROM utilisateurs WHERE departement = $dep AND role = 'Formateur') AS formateurs,
  (SELECT COUNT(*) FROM utilisateurs WHERE departement = $dep AND role = 'Stagiaire') AS stagiaires,
  COALESCE((SELECT AVG(e.pourcentage) FROM evaluations e
   JOIN utilisateurs u ON u.id_utilisateur = e.id_utilisateur
   WHERE u.departement = $dep AND e.statut = 'Terminee'), 0) AS taux;";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$dep", departement);
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new StatistiqueDepartement(
                Formations: await CountFormationsDepartementAsync(connection, departement),
                Sessions: reader.GetInt32(0),
                Formateurs: reader.GetInt32(1),
                Stagiaires: reader.GetInt32(2),
                TauxReussite: Convert.ToDouble(reader.GetValue(3)));
        }
        return new StatistiqueDepartement(0, 0, 0, 0, 0);
    }

    public async Task<IReadOnlyList<ClassementStagiaire>> GetClassementParSessionAsync(int idSession)
    {
        await AppDbContext.InitializeAsync();
        var resultats = new List<ClassementStagiaire>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT u.id_utilisateur, u.prenom || ' ' || u.nom AS nom_complet,
       COALESCE(AVG(e.pourcentage), 0) AS moyenne,
       COUNT(e.id_evaluation) AS nb_evals
FROM utilisateurs u
JOIN participation p ON p.id_utilisateur = u.id_utilisateur AND p.id_session = $sid
LEFT JOIN evaluations e ON e.id_utilisateur = u.id_utilisateur AND e.statut = 'Terminee'
LEFT JOIN questionnaires q ON q.id_questionnaire = e.id_questionnaire AND q.id_session = $sid
WHERE u.role = 'Stagiaire'
GROUP BY u.id_utilisateur
ORDER BY moyenne DESC;";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$sid", idSession);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            resultats.Add(new ClassementStagiaire(
                NomComplet: reader.GetString(1),
                IdUtilisateur: reader.GetInt32(0),
                Moyenne: Convert.ToDouble(reader.GetValue(2)),
                NbEvaluations: reader.GetInt32(3)));
        }
        return resultats;
    }

    public async Task<double> GetMoyenneParSessionEtPromotionAsync(int idSession, string promotion)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT COALESCE(AVG(e.pourcentage), 0)
FROM evaluations e
JOIN utilisateurs u ON u.id_utilisateur = e.id_utilisateur
JOIN questionnaires q ON q.id_questionnaire = e.id_questionnaire
WHERE q.id_session = $sid AND u.promotion = $promo AND e.statut = 'Terminee';";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$sid", idSession);
        command.Parameters.AddWithValue("$promo", promotion);
        return Convert.ToDouble(await command.ExecuteScalarAsync() ?? 0d);
    }

    private static async Task<int> CountFormationsDepartementAsync(SqliteConnection connection, string departement)
    {
        const string sql = @"
SELECT COUNT(DISTINCT f.id_formation) FROM formations f
JOIN sessions s ON s.id_formation = f.id_formation
JOIN questionnaires q ON q.id_session = s.id_session
JOIN evaluations e ON e.id_questionnaire = q.id_questionnaire
JOIN utilisateurs u ON u.id_utilisateur = e.id_utilisateur
WHERE u.departement = $dep;";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$dep", departement);
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }

    private static async Task<int> CountAsync(SqliteConnection connection, string table)
    {
        await using var command = new SqliteCommand($"SELECT COUNT(*) FROM {table};", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }
}