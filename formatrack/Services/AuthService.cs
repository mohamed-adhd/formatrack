using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class AuthService : IAuthService
{
    private readonly string _connectionString;

    public AuthService(string? connectionString = null)
    {
        if (connectionString is not null)
        {
            _connectionString = connectionString;
            return;
        }

        _connectionString = AppDbContext.ConnectionString;
    }

    public async Task<string?> AuthentifierAsync(string identifiant, string motDePasse)
    {
        if (string.IsNullOrWhiteSpace(identifiant) || string.IsNullOrWhiteSpace(motDePasse))
            return null;

        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT role, mot_de_passe_hash, actif
            FROM utilisateurs
            WHERE email = @identifiant
            LIMIT 1;";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@identifiant", identifiant);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null; // Aucun utilisateur trouve pour cet identifiant

        var role = reader.GetString(0);
        var hashStocke = reader.GetString(1);
        var estActif = reader.GetInt64(2) == 1;

        if (!estActif)
            return null; // Compte desactive

        if (!PasswordHasher.Verify(motDePasse, hashStocke))
            return null; // Mot de passe incorrect

        return role;
    }
}
