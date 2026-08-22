using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class QuestionRepository : Repository<Question>, IQuestionRepository
{
    protected override string TableName => "questions";
    protected override string IdColumn => "id_question";
    protected override string InsertSql => @"INSERT INTO questions (id_questionnaire, id_critere, enonce, type_question, bareme, ordre)
VALUES ($questionnaire, $critere, $enonce, $type, $bareme, $ordre)";
    protected override string UpdateSql => @"UPDATE questions SET id_questionnaire=$questionnaire, id_critere=$critere,
enonce=$enonce, type_question=$type, bareme=$bareme, ordre=$ordre WHERE id_question=$id";

    protected override Question Map(SqliteDataReader r) => new()
    {
        IdQuestion = Int(r, "id_question"),
        IdQuestionnaire = Int(r, "id_questionnaire"),
        IdCritere = r["id_critere"] == DBNull.Value ? null : Int(r, "id_critere"),
        Enonce = Text(r, "enonce"),
        TypeQuestion = Text(r, "type_question"),
        Bareme = Double(r, "bareme"),
        Ordre = Int(r, "ordre"),
        CritereLibelle = Has(r, "critere_libelle") ? Text(r, "critere_libelle") : string.Empty
    };

    protected override void FillInsert(SqliteCommand c, Question q) => Fill(c, q);
    protected override void FillUpdate(SqliteCommand c, Question q) { c.Parameters.AddWithValue("$id", q.IdQuestion); Fill(c, q); }
    private static void Fill(SqliteCommand c, Question q)
    {
        c.Parameters.AddWithValue("$questionnaire", q.IdQuestionnaire);
        c.Parameters.AddWithValue("$critere", Db(q.IdCritere));
        c.Parameters.AddWithValue("$enonce", q.Enonce);
        c.Parameters.AddWithValue("$type", string.IsNullOrWhiteSpace(q.TypeQuestion) ? "TexteLibre" : q.TypeQuestion);
        c.Parameters.AddWithValue("$bareme", q.Bareme);
        c.Parameters.AddWithValue("$ordre", q.Ordre);
    }

    public async Task<IReadOnlyList<Question>> GetByQuestionnaireAsync(int idQuestionnaire)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Question>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        const string sql = @"SELECT q.*, c.libelle AS critere_libelle FROM questions q
LEFT JOIN criteres c ON c.id_critere = q.id_critere
WHERE q.id_questionnaire=$id ORDER BY q.ordre, q.id_question;";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$id", idQuestionnaire);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }
}
