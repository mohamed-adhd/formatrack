using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class FormationService : IFormationService
{
    public async Task<IReadOnlyList<Formation>> GetFormationsAsync(string? recherche = null)
    {
        await AppDbContext.InitializeAsync();
        var formations = new List<Formation>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT f.id_formation, f.titre, COALESCE(f.description,''), COALESCE(f.objectifs,''),
       f.duree_heures, COALESCE(f.type_formation,''), f.statut, COUNT(s.id_session)
FROM formations f
LEFT JOIN sessions s ON s.id_formation = f.id_formation
WHERE $q IS NULL OR f.titre LIKE $like OR f.type_formation LIKE $like OR f.statut LIKE $like
GROUP BY f.id_formation
ORDER BY f.id_formation DESC;";
        await using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("$q", string.IsNullOrWhiteSpace(recherche) ? DBNull.Value : recherche.Trim());
        command.Parameters.AddWithValue("$like", $"%{recherche?.Trim()}%");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            formations.Add(new Formation
            {
                IdFormation = reader.GetInt32(0),
                Titre = reader.GetString(1),
                Description = reader.GetString(2),
                Objectifs = reader.GetString(3),
                DureeHeures = reader.GetInt32(4),
                TypeFormation = reader.GetString(5),
                Statut = reader.GetString(6),
                NombreSessions = reader.GetInt32(7)
            });
        }

        return formations;
    }
}
