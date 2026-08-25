using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    protected override string TableName => "notifications";
    protected override string IdColumn => "id_notification";
    protected override string InsertSql => @"INSERT INTO notifications (id_utilisateur, message, lue, date_creation)
VALUES ($utilisateur, $message, $lue, $date)";
    protected override string UpdateSql => @"UPDATE notifications SET id_utilisateur=$utilisateur, message=$message,
lue=$lue, date_creation=$date WHERE id_notification=$id";

    protected override Notification Map(SqliteDataReader r) => new()
    {
        IdNotification = Int(r, "id_notification"),
        IdUtilisateur = Int(r, "id_utilisateur"),
        Message = Text(r, "message"),
        Lue = Int(r, "lue") == 1,
        DateCreation = Date(r, "date_creation")
    };

    protected override void FillInsert(SqliteCommand c, Notification e) => Fill(c, e);
    protected override void FillUpdate(SqliteCommand c, Notification e) { c.Parameters.AddWithValue("$id", e.IdNotification); Fill(c, e); }
    private static void Fill(SqliteCommand c, Notification e)
    {
        c.Parameters.AddWithValue("$utilisateur", e.IdUtilisateur);
        c.Parameters.AddWithValue("$message", e.Message);
        c.Parameters.AddWithValue("$lue", e.Lue ? 1 : 0);
        c.Parameters.AddWithValue("$date", e.DateCreation.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    public async Task<IReadOnlyList<Notification>> GetByUtilisateurAsync(int idUtilisateur)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Notification>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand(
            "SELECT * FROM notifications WHERE id_utilisateur=$id ORDER BY date_creation DESC, id_notification DESC;", connection);
        command.Parameters.AddWithValue("$id", idUtilisateur);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            items.Add(Map(reader));
        return items;
    }

    public async Task<int> CompterNonLuesAsync(int idUtilisateur)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand(
            "SELECT COUNT(*) FROM notifications WHERE id_utilisateur=$id AND lue=0;", connection);
        command.Parameters.AddWithValue("$id", idUtilisateur);
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<bool> MarquerLueAsync(int idNotification)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand("UPDATE notifications SET lue=1 WHERE id_notification=$id;", connection);
        command.Parameters.AddWithValue("$id", idNotification);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> MarquerToutesLuesAsync(int idUtilisateur)
    {
        await AppDbContext.InitializeAsync();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqliteCommand("UPDATE notifications SET lue=1 WHERE id_utilisateur=$id;", connection);
        command.Parameters.AddWithValue("$id", idUtilisateur);
        return await command.ExecuteNonQueryAsync() > 0;
    }
}