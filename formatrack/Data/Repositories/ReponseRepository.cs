using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class ReponseRepository : Repository<Reponse>, IReponseRepository
{
    protected override string TableName => "reponses";
    protected override string IdColumn => "id_reponse";
    protected override string InsertSql => @"INSERT INTO reponses (id_evaluation, id_question, contenu, est_correcte, score_obtenu)
VALUES ($evaluation, $question, $contenu, $correcte, $score)";
    protected override string UpdateSql => @"UPDATE reponses SET id_evaluation=$evaluation, id_question=$question,
contenu=$contenu, est_correcte=$correcte, score_obtenu=$score WHERE id_reponse=$id";

    protected override Reponse Map(SqliteDataReader r) => new()
    {
        IdReponse = Int(r, "id_reponse"),
        IdEvaluation = Int(r, "id_evaluation"),
        IdQuestion = Int(r, "id_question"),
        Contenu = Text(r, "contenu"),
        EstCorrecte = r["est_correcte"] == DBNull.Value ? null : Int(r, "est_correcte") == 1,
        ScoreObtenu = NullableDouble(r, "score_obtenu"),
        QuestionEnonce = Has(r, "question_enonce") ? Text(r, "question_enonce") : string.Empty
    };

    protected override void FillInsert(SqliteCommand c, Reponse r) => Fill(c, r);
    protected override void FillUpdate(SqliteCommand c, Reponse r) { c.Parameters.AddWithValue("$id", r.IdReponse); Fill(c, r); }
    private static void Fill(SqliteCommand c, Reponse r)
    {
        c.Parameters.AddWithValue("$evaluation", r.IdEvaluation);
        c.Parameters.AddWithValue("$question", r.IdQuestion);
        c.Parameters.AddWithValue("$contenu", Db(r.Contenu));
        c.Parameters.AddWithValue("$correcte", Db(r.EstCorrecte));
        c.Parameters.AddWithValue("$score", Db(r.ScoreObtenu));
    }

    public async Task<IReadOnlyList<Reponse>> GetByEvaluationAsync(int idEvaluation)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Reponse>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        const string sql = @"SELECT r.*, q.enonce AS question_enonce FROM reponses r
JOIN questions q ON q.id_question = r.id_question
WHERE r.id_evaluation=$id ORDER BY q.ordre, r.id_reponse;";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", idEvaluation);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }
}
