using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class EvaluationRepository : Repository<Evaluation>, IEvaluationRepository
{
    protected override string TableName => "evaluations";
    protected override string IdColumn => "id_evaluation";
    protected override string InsertSql => @"INSERT INTO evaluations (id_utilisateur, id_questionnaire, date_passage, score_total, pourcentage, statut)
VALUES ($utilisateur, $questionnaire, $date, $score, $pourcentage, $statut)";
    protected override string UpdateSql => @"UPDATE evaluations SET id_utilisateur=$utilisateur, id_questionnaire=$questionnaire,
date_passage=$date, score_total=$score, pourcentage=$pourcentage, statut=$statut WHERE id_evaluation=$id";

    protected override Evaluation Map(SqliteDataReader r) => new()
    {
        IdEvaluation = Int(r, "id_evaluation"),
        IdUtilisateur = Int(r, "id_utilisateur"),
        IdQuestionnaire = Int(r, "id_questionnaire"),
        DatePassage = NullableDate(r, "date_passage"),
        ScoreTotal = NullableDouble(r, "score_total"),
        Pourcentage = NullableDouble(r, "pourcentage"),
        Statut = Text(r, "statut"),
        UtilisateurNom = Has(r, "utilisateur_nom") ? Text(r, "utilisateur_nom") : string.Empty,
        QuestionnaireTitre = Has(r, "questionnaire_titre") ? Text(r, "questionnaire_titre") : string.Empty
    };

    protected override void FillInsert(SqliteCommand c, Evaluation e) => Fill(c, e);
    protected override void FillUpdate(SqliteCommand c, Evaluation e) { c.Parameters.AddWithValue("$id", e.IdEvaluation); Fill(c, e); }
    private static void Fill(SqliteCommand c, Evaluation e)
    {
        c.Parameters.AddWithValue("$utilisateur", e.IdUtilisateur);
        c.Parameters.AddWithValue("$questionnaire", e.IdQuestionnaire);
        c.Parameters.AddWithValue("$date", e.DatePassage?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        c.Parameters.AddWithValue("$score", Db(e.ScoreTotal));
        c.Parameters.AddWithValue("$pourcentage", Db(e.Pourcentage));
        c.Parameters.AddWithValue("$statut", string.IsNullOrWhiteSpace(e.Statut) ? "EnCours" : e.Statut);
    }

    public Task<IReadOnlyList<Evaluation>> GetByUtilisateurAsync(int idUtilisateur) =>
        QueryAsync("WHERE e.id_utilisateur=$id", c => c.Parameters.AddWithValue("$id", idUtilisateur));

    public override Task<IReadOnlyList<Evaluation>> GetAllAsync() => QueryAsync(string.Empty, _ => { });

    public async Task<bool> TerminerAsync(int idEvaluation, double scoreTotal, double pourcentage)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        const string sql = @"UPDATE evaluations SET score_total=$score, pourcentage=$pourcentage, statut='Terminee',
date_passage=COALESCE(date_passage, datetime('now')) WHERE id_evaluation=$id;";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", idEvaluation);
        command.Parameters.AddWithValue("$score", scoreTotal);
        command.Parameters.AddWithValue("$pourcentage", pourcentage);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    private async Task<IReadOnlyList<Evaluation>> QueryAsync(string where, Action<SqliteCommand> bind)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Evaluation>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        var sql = $@"SELECT e.*, (u.prenom || ' ' || u.nom) AS utilisateur_nom, q.titre AS questionnaire_titre
FROM evaluations e
JOIN utilisateurs u ON u.id_utilisateur = e.id_utilisateur
JOIN questionnaires q ON q.id_questionnaire = e.id_questionnaire
{where}
ORDER BY e.id_evaluation DESC;";
        await using var command = new SqliteCommand(sql, connection);
        bind(command);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }
}
