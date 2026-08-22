using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class SessionService : ISessionService
{
    public async Task<IReadOnlyList<Session>> GetProchainesSessionsAsync(int limite = 5)
    {
        await AppDbContext.InitializeAsync();
        var sessions = new List<Session>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT s.id_session, s.id_formation, f.titre, s.date_debut, s.date_fin,
       COALESCE(s.lieu,''), COALESCE(s.capacite,0), s.statut
FROM sessions s
JOIN formations f ON f.id_formation = s.id_formation
ORDER BY date(s.date_debut) ASC
LIMIT $limite;";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$limite", limite);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sessions.Add(new Session
            {
                IdSession = reader.GetInt32(0),
                IdFormation = reader.GetInt32(1),
                TitreFormation = reader.GetString(2),
                DateDebut = DateTime.Parse(reader.GetString(3)),
                DateFin = DateTime.Parse(reader.GetString(4)),
                Lieu = reader.GetString(5),
                Capacite = reader.GetInt32(6),
                Statut = reader.GetString(7)
            });
        }

        return sessions;
    }
}
