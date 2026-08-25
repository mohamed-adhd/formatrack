using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class JournalActiviteRepository : Repository<JournalActivite>, IJournalActiviteRepository
{
    protected override string TableName => "journal_activite";
    protected override string IdColumn => "id_journal";
    protected override string InsertSql => @"INSERT INTO journal_activite (id_utilisateur, action, details, date_action)
VALUES ($utilisateur, $action, $details, $date)";
    protected override string UpdateSql => @"UPDATE journal_activite SET id_utilisateur=$utilisateur, action=$action,
details=$details, date_action=$date WHERE id_journal=$id";

    protected override JournalActivite Map(SqliteDataReader r) => new()
    {
        IdJournal = Int(r, "id_journal"),
        IdUtilisateur = r["id_utilisateur"] == DBNull.Value ? null : Int(r, "id_utilisateur"),
        UtilisateurNom = Has(r, "utilisateur_nom") ? Text(r, "utilisateur_nom") : string.Empty,
        Action = Text(r, "action"),
        Details = Text(r, "details"),
        DateAction = Date(r, "date_action")
    };

    protected override void FillInsert(SqliteCommand c, JournalActivite e) => Fill(c, e);
    protected override void FillUpdate(SqliteCommand c, JournalActivite e) { c.Parameters.AddWithValue("$id", e.IdJournal); Fill(c, e); }
    private static void Fill(SqliteCommand c, JournalActivite e)
    {
        c.Parameters.AddWithValue("$utilisateur", Db(e.IdUtilisateur));
        c.Parameters.AddWithValue("$action", e.Action);
        c.Parameters.AddWithValue("$details", Db(e.Details));
        c.Parameters.AddWithValue("$date", e.DateAction.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    public Task<IReadOnlyList<JournalActivite>> GetRecentsAsync(int limite = 200)
        => QueryAsync(string.Empty, c => c.Parameters.AddWithValue("$limite", limite));

    public Task<IReadOnlyList<JournalActivite>> GetByUtilisateurAsync(int idUtilisateur, int limite = 200)
        => QueryAsync("WHERE j.id_utilisateur = $id", c =>
        {
            c.Parameters.AddWithValue("$id", idUtilisateur);
            c.Parameters.AddWithValue("$limite", limite);
        });

    public Task<IReadOnlyList<JournalActivite>> GetDepuisAsync(DateTime depuis, int limite = 200)
        => QueryAsync("WHERE j.date_action >= $depuis", c =>
        {
            c.Parameters.AddWithValue("$depuis", depuis.ToString("yyyy-MM-dd HH:mm:ss"));
            c.Parameters.AddWithValue("$limite", limite);
        });

    private async Task<IReadOnlyList<JournalActivite>> QueryAsync(string clause, Action<SqliteCommand> bind)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<JournalActivite>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        var sql = $@"SELECT j.*, COALESCE(u.prenom || ' ' || u.nom, '') AS utilisateur_nom
FROM journal_activite j
LEFT JOIN utilisateurs u ON u.id_utilisateur = j.id_utilisateur
{clause}
ORDER BY j.date_action DESC, j.id_journal DESC
LIMIT $limite;";
        await using var command = new SqliteCommand(sql, connection);
        bind(command);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }
}