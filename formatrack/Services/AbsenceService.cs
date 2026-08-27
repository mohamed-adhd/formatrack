using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class AbsenceService : IAbsenceService
{
    private readonly string _connectionString;

    public AbsenceService(string? connectionString = null)
    {
        _connectionString = connectionString ?? AppDbContext.ConnectionString;
    }

    public async Task<IReadOnlyList<AbsenceRetard>> ListerParUtilisateurAsync(int idUtilisateur)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<AbsenceRetard>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT id, utilisateur_id, session_id, cours, date, type, duree, justifiee, motif, created_at
            FROM absences_retards
            WHERE utilisateur_id = @utilisateur_id
            ORDER BY date DESC, id DESC;";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@utilisateur_id", idUtilisateur);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new AbsenceRetard
            {
                Id = reader.GetInt32(0),
                UtilisateurId = reader.GetInt32(1),
                SessionId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Cours = reader.GetString(3),
                Date = reader.GetString(4),
                Type = reader.GetString(5),
                Duree = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Justifiee = reader.GetInt32(7) == 1,
                Motif = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                CreatedAt = DateTime.Parse(reader.GetString(9))
            });
        }

        return items;
    }

    public async Task<int> AjouterAsync(AbsenceRetard item)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO absences_retards (utilisateur_id, session_id, cours, date, type, duree, justifiee, motif, created_at)
            VALUES (@utilisateur_id, @session_id, @cours, @date, @type, @duree, @justifiee, @motif, @created_at);
            SELECT last_insert_rowid();";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@utilisateur_id", item.UtilisateurId);
        command.Parameters.AddWithValue("@session_id", (object?)item.SessionId ?? DBNull.Value);
        command.Parameters.AddWithValue("@cours", item.Cours);
        command.Parameters.AddWithValue("@date", item.Date);
        command.Parameters.AddWithValue("@type", item.Type);
        command.Parameters.AddWithValue("@duree", (object?)item.Duree ?? DBNull.Value);
        command.Parameters.AddWithValue("@justifiee", item.Justifiee ? 1 : 0);
        command.Parameters.AddWithValue("@motif", item.Motif);
        command.Parameters.AddWithValue("@created_at", item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<bool> ModifierMotifAsync(int id, string motif, bool justifiee)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            UPDATE absences_retards
            SET motif = @motif, justifiee = @justifiee
            WHERE id = @id;";

        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@motif", motif);
        command.Parameters.AddWithValue("@justifiee", justifiee ? 1 : 0);
        command.Parameters.AddWithValue("@id", id);

        var rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }
}
