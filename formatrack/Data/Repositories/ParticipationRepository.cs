using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class ParticipationRepository : Repository<Participation>, IParticipationRepository
{
    protected override string TableName => "participation";
    protected override string IdColumn => "id_participation";
    protected override string InsertSql => @"INSERT INTO participation (id_utilisateur, id_session, role_participation)
VALUES ($utilisateur, $session, $role)";
    protected override string UpdateSql => @"UPDATE participation SET id_utilisateur=$utilisateur, id_session=$session,
role_participation=$role WHERE id_participation=$id";

    protected override Participation Map(SqliteDataReader r) => new()
    {
        IdParticipation = Int(r, "id_participation"),
        IdUtilisateur = Int(r, "id_utilisateur"),
        IdSession = Int(r, "id_session"),
        RoleParticipation = Text(r, "role_participation"),
        DateInscription = Date(r, "date_inscription"),
        UtilisateurNom = Has(r, "utilisateur_nom") ? Text(r, "utilisateur_nom") : string.Empty,
        SessionTitre = Has(r, "session_titre") ? Text(r, "session_titre") : string.Empty
    };

    protected override void FillInsert(SqliteCommand c, Participation p) => Fill(c, p);
    protected override void FillUpdate(SqliteCommand c, Participation p) { c.Parameters.AddWithValue("$id", p.IdParticipation); Fill(c, p); }
    private static void Fill(SqliteCommand c, Participation p)
    {
        c.Parameters.AddWithValue("$utilisateur", p.IdUtilisateur);
        c.Parameters.AddWithValue("$session", p.IdSession);
        c.Parameters.AddWithValue("$role", string.IsNullOrWhiteSpace(p.RoleParticipation) ? "Stagiaire" : p.RoleParticipation);
    }

    public Task<IReadOnlyList<Participation>> GetBySessionAsync(int idSession) =>
        QueryAsync("WHERE p.id_session=$id", c => c.Parameters.AddWithValue("$id", idSession));

    public Task<IReadOnlyList<Participation>> GetByUtilisateurAsync(int idUtilisateur) =>
        QueryAsync("WHERE p.id_utilisateur=$id", c => c.Parameters.AddWithValue("$id", idUtilisateur));

    public override Task<IReadOnlyList<Participation>> GetAllAsync() => QueryAsync(string.Empty, _ => { });

    private async Task<IReadOnlyList<Participation>> QueryAsync(string where, Action<SqliteCommand> bind)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Participation>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        var sql = $@"SELECT p.*, (u.prenom || ' ' || u.nom) AS utilisateur_nom, f.titre AS session_titre
FROM participation p
JOIN utilisateurs u ON u.id_utilisateur = p.id_utilisateur
JOIN sessions s ON s.id_session = p.id_session
JOIN formations f ON f.id_formation = s.id_formation
{where}
ORDER BY p.id_participation DESC;";
        await using var command = new SqliteCommand(sql, connection);
        bind(command);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }
}
