using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class UtilisateurRepository : Repository<Utilisateur>, IUtilisateurRepository
{
    protected override string TableName => "utilisateurs";
    protected override string IdColumn => "id_utilisateur";
    protected override string InsertSql => @"INSERT INTO utilisateurs (nom, prenom, email, mot_de_passe_hash, role, actif)
VALUES ($nom, $prenom, $email, $hash, $role, $actif)";
    protected override string UpdateSql => @"UPDATE utilisateurs SET nom=$nom, prenom=$prenom, email=$email,
mot_de_passe_hash=$hash, role=$role, actif=$actif WHERE id_utilisateur=$id";

    protected override Utilisateur Map(SqliteDataReader r) => new()
    {
        IdUtilisateur = Int(r, "id_utilisateur"),
        Nom = Text(r, "nom"),
        Prenom = Text(r, "prenom"),
        Email = Text(r, "email"),
        MotDePasseHash = Text(r, "mot_de_passe_hash"),
        Role = Text(r, "role"),
        DateCreation = Date(r, "date_creation"),
        Actif = Int(r, "actif") == 1
    };

    protected override void FillInsert(SqliteCommand c, Utilisateur u) => Fill(c, u);
    protected override void FillUpdate(SqliteCommand c, Utilisateur u) { c.Parameters.AddWithValue("$id", u.IdUtilisateur); Fill(c, u); }
    private static void Fill(SqliteCommand c, Utilisateur u)
    {
        c.Parameters.AddWithValue("$nom", u.Nom);
        c.Parameters.AddWithValue("$prenom", u.Prenom);
        c.Parameters.AddWithValue("$email", u.Email);
        c.Parameters.AddWithValue("$hash", u.MotDePasseHash);
        c.Parameters.AddWithValue("$role", string.IsNullOrWhiteSpace(u.Role) ? "Stagiaire" : u.Role);
        c.Parameters.AddWithValue("$actif", u.Actif ? 1 : 0);
    }

    public async Task<Utilisateur?> GetByEmailAsync(string email)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand("SELECT * FROM utilisateurs WHERE email = $email LIMIT 1;", connection);
        command.Parameters.AddWithValue("$email", email);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<bool> SetActifAsync(int idUtilisateur, bool actif)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand("UPDATE utilisateurs SET actif=$actif WHERE id_utilisateur=$id;", connection);
        command.Parameters.AddWithValue("$id", idUtilisateur);
        command.Parameters.AddWithValue("$actif", actif ? 1 : 0);
        return await command.ExecuteNonQueryAsync() > 0;
    }
}
