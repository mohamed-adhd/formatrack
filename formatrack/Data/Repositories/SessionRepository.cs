using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class SessionRepository : Repository<Session>, ISessionRepository
{
    protected override string TableName => "sessions";
    protected override string IdColumn => "id_session";
    protected override string InsertSql => @"INSERT INTO sessions (id_formation, date_debut, date_fin, lieu, capacite, statut)
VALUES ($formation, $debut, $fin, $lieu, $capacite, $statut)";
    protected override string UpdateSql => @"UPDATE sessions SET id_formation=$formation, date_debut=$debut, date_fin=$fin,
lieu=$lieu, capacite=$capacite, statut=$statut WHERE id_session=$id";

    protected override Session Map(SqliteDataReader r) => new()
    {
        IdSession = Int(r, "id_session"),
        IdFormation = Int(r, "id_formation"),
        TitreFormation = Has(r, "titre_formation") ? Text(r, "titre_formation") : string.Empty,
        DateDebut = Date(r, "date_debut"),
        DateFin = Date(r, "date_fin"),
        Lieu = Text(r, "lieu"),
        Capacite = Int(r, "capacite"),
        Statut = Text(r, "statut")
    };

    protected override void FillInsert(SqliteCommand c, Session s) => Fill(c, s);
    protected override void FillUpdate(SqliteCommand c, Session s) { c.Parameters.AddWithValue("$id", s.IdSession); Fill(c, s); }
    private static void Fill(SqliteCommand c, Session s)
    {
        c.Parameters.AddWithValue("$formation", s.IdFormation);
        c.Parameters.AddWithValue("$debut", s.DateDebut.ToString("yyyy-MM-dd"));
        c.Parameters.AddWithValue("$fin", s.DateFin.ToString("yyyy-MM-dd"));
        c.Parameters.AddWithValue("$lieu", Db(s.Lieu));
        c.Parameters.AddWithValue("$capacite", s.Capacite);
        c.Parameters.AddWithValue("$statut", string.IsNullOrWhiteSpace(s.Statut) ? "Planifiee" : s.Statut);
    }

    public Task<IReadOnlyList<Session>> GetByFormationAsync(int idFormation) =>
        QueryAsync("WHERE s.id_formation = $id ORDER BY date(s.date_debut) DESC", c => c.Parameters.AddWithValue("$id", idFormation));

    public Task<IReadOnlyList<Session>> GetUpcomingAsync(int limite = 5) =>
        QueryAsync("ORDER BY date(s.date_debut) ASC LIMIT $limite", c => c.Parameters.AddWithValue("$limite", limite));

    private async Task<IReadOnlyList<Session>> QueryAsync(string clause, Action<SqliteCommand> bind)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Session>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        var sql = $@"SELECT s.*, f.titre AS titre_formation FROM sessions s
JOIN formations f ON f.id_formation = s.id_formation {clause};";
        await using var command = new SqliteCommand(sql, connection);
        bind(command);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }
}
