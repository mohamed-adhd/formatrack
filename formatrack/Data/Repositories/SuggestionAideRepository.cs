using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class SuggestionAideRepository : ISuggestionAideRepository
{
    private const string TableName = "suggestions_aide";

    private static SuggestionAide Map(SqliteDataReader r) => new()
    {
        Id = Convert.ToInt32(r["id"]),
        Titre = r["titre"]?.ToString() ?? "",
        Description = r["description"]?.ToString() ?? "",
        Priorite = Convert.ToInt32(r["priorite"]),
        Categorie = r["categorie"]?.ToString() ?? "",
        ActionPage = r["action_page"]?.ToString() ?? "",
        ActionParams = r["action_params"]?.ToString() ?? "",
        EstLu = Convert.ToInt32(r["est_lu"]) == 1,
        DateGeneration = DateTime.TryParse(r["date_generation"]?.ToString(), out var dt) ? dt : DateTime.Now
    };

    public async Task<IReadOnlyList<SuggestionAide>> GetAllAsync()
    {
        await AppDbContext.InitializeAsync();
        var items = new List<SuggestionAide>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand(
            $"SELECT * FROM {TableName} ORDER BY priorite ASC, date_generation DESC;", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }

    public async Task<IReadOnlyList<SuggestionAide>> GetUnreadAsync()
    {
        await AppDbContext.InitializeAsync();
        var items = new List<SuggestionAide>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand(
            $"SELECT * FROM {TableName} WHERE est_lu = 0 ORDER BY priorite ASC, date_generation DESC;", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }

    public async Task MarkAsReadAsync(int id)
    {
        await AppDbContext.InitializeAsync();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand(
            $"UPDATE {TableName} SET est_lu = 1 WHERE id = $id;", conn);
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkAllAsReadAsync()
    {
        await AppDbContext.InitializeAsync();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand(
            $"UPDATE {TableName} SET est_lu = 1;", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
