using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class QuestionnaireRepository : Repository<Questionnaire>, IQuestionnaireRepository
{
    protected override string TableName => "questionnaires";
    protected override string IdColumn => "id_questionnaire";
    protected override string InsertSql => @"INSERT INTO questionnaires (id_session, titre, description, type_evaluation, statut)
VALUES ($session, $titre, $description, $type, $statut)";
    protected override string UpdateSql => @"UPDATE questionnaires SET id_session=$session, titre=$titre, description=$description,
type_evaluation=$type, statut=$statut WHERE id_questionnaire=$id";

    protected override Questionnaire Map(SqliteDataReader r) => new()
    {
        IdQuestionnaire = Int(r, "id_questionnaire"),
        IdSession = Int(r, "id_session"),
        Titre = Text(r, "titre"),
        Description = Text(r, "description"),
        TypeEvaluation = Text(r, "type_evaluation"),
        DateCreation = Date(r, "date_creation"),
        Statut = Text(r, "statut"),
        SessionTitre = Has(r, "session_titre") ? Text(r, "session_titre") : string.Empty,
        NombreQuestions = Has(r, "nombre_questions") ? Int(r, "nombre_questions") : 0
    };

    protected override void FillInsert(SqliteCommand c, Questionnaire q) => Fill(c, q);
    protected override void FillUpdate(SqliteCommand c, Questionnaire q) { c.Parameters.AddWithValue("$id", q.IdQuestionnaire); Fill(c, q); }
    private static void Fill(SqliteCommand c, Questionnaire q)
    {
        c.Parameters.AddWithValue("$session", q.IdSession);
        c.Parameters.AddWithValue("$titre", q.Titre);
        c.Parameters.AddWithValue("$description", Db(q.Description));
        c.Parameters.AddWithValue("$type", Db(q.TypeEvaluation));
        c.Parameters.AddWithValue("$statut", string.IsNullOrWhiteSpace(q.Statut) ? "Brouillon" : q.Statut);
    }

    public Task<IReadOnlyList<Questionnaire>> GetBySessionAsync(int idSession) =>
        QueryAsync("WHERE q.id_session = $session", c => c.Parameters.AddWithValue("$session", idSession));

    public override Task<IReadOnlyList<Questionnaire>> GetAllAsync() => QueryAsync(string.Empty, _ => { });

    private async Task<IReadOnlyList<Questionnaire>> QueryAsync(string where, Action<SqliteCommand> bind)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Questionnaire>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        var sql = $@"SELECT q.*, f.titre AS session_titre, COUNT(ques.id_question) AS nombre_questions
FROM questionnaires q
JOIN sessions s ON s.id_session = q.id_session
JOIN formations f ON f.id_formation = s.id_formation
LEFT JOIN questions ques ON ques.id_questionnaire = q.id_questionnaire
{where}
GROUP BY q.id_questionnaire
ORDER BY q.id_questionnaire DESC;";
        await using var command = new SqliteCommand(sql, connection);
        bind(command);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }
}
