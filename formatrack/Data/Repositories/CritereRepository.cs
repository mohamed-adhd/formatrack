using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class CritereRepository : Repository<Critere>, ICritereRepository
{
    protected override string TableName => "criteres";
    protected override string IdColumn => "id_critere";
    protected override string InsertSql => @"INSERT INTO criteres (id_questionnaire, libelle, description, coefficient)
VALUES ($questionnaire, $libelle, $description, $coefficient)";
    protected override string UpdateSql => @"UPDATE criteres SET id_questionnaire=$questionnaire, libelle=$libelle,
description=$description, coefficient=$coefficient WHERE id_critere=$id";

    protected override Critere Map(SqliteDataReader r) => new()
    {
        IdCritere = Int(r, "id_critere"),
        IdQuestionnaire = Int(r, "id_questionnaire"),
        Libelle = Text(r, "libelle"),
        Description = Text(r, "description"),
        Coefficient = Double(r, "coefficient")
    };

    protected override void FillInsert(SqliteCommand c, Critere e) => Fill(c, e);
    protected override void FillUpdate(SqliteCommand c, Critere e) { c.Parameters.AddWithValue("$id", e.IdCritere); Fill(c, e); }
    private static void Fill(SqliteCommand c, Critere e)
    {
        c.Parameters.AddWithValue("$questionnaire", e.IdQuestionnaire);
        c.Parameters.AddWithValue("$libelle", e.Libelle);
        c.Parameters.AddWithValue("$description", Db(e.Description));
        c.Parameters.AddWithValue("$coefficient", e.Coefficient);
    }

    public async Task<IReadOnlyList<Critere>> GetByQuestionnaireAsync(int idQuestionnaire)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Critere>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand("SELECT * FROM criteres WHERE id_questionnaire=$id ORDER BY id_critere;", connection);
        command.Parameters.AddWithValue("$id", idQuestionnaire);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }
}
