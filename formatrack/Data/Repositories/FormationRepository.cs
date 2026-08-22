using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class FormationRepository : Repository<Formation>, IFormationRepository
{
    protected override string TableName => "formations";
    protected override string IdColumn => "id_formation";
    protected override string InsertSql => @"INSERT INTO formations (titre, description, objectifs, duree_heures, type_formation, statut)
VALUES ($titre, $description, $objectifs, $duree, $type, $statut)";
    protected override string UpdateSql => @"UPDATE formations SET titre=$titre, description=$description, objectifs=$objectifs,
duree_heures=$duree, type_formation=$type, statut=$statut WHERE id_formation=$id";

    protected override Formation Map(SqliteDataReader r) => new()
    {
        IdFormation = Int(r, "id_formation"),
        Titre = Text(r, "titre"),
        Description = Text(r, "description"),
        Objectifs = Text(r, "objectifs"),
        DureeHeures = Int(r, "duree_heures"),
        TypeFormation = Text(r, "type_formation"),
        Statut = Text(r, "statut"),
        NombreSessions = Has(r, "nombre_sessions") ? Int(r, "nombre_sessions") : 0
    };

    protected override void FillInsert(SqliteCommand c, Formation f) => Fill(c, f);
    protected override void FillUpdate(SqliteCommand c, Formation f) { c.Parameters.AddWithValue("$id", f.IdFormation); Fill(c, f); }
    private static void Fill(SqliteCommand c, Formation f)
    {
        c.Parameters.AddWithValue("$titre", f.Titre);
        c.Parameters.AddWithValue("$description", Db(f.Description));
        c.Parameters.AddWithValue("$objectifs", Db(f.Objectifs));
        c.Parameters.AddWithValue("$duree", f.DureeHeures);
        c.Parameters.AddWithValue("$type", Db(f.TypeFormation));
        c.Parameters.AddWithValue("$statut", string.IsNullOrWhiteSpace(f.Statut) ? "Planifiee" : f.Statut);
    }

    public async Task<IReadOnlyList<Formation>> SearchAsync(string? recherche)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Formation>();
        await using var connection = new SqliteConnection(AppDbContext.ConnectionString);
        await connection.OpenAsync();
        const string sql = @"
SELECT f.*, COUNT(s.id_session) AS nombre_sessions
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
            items.Add(Map(reader));
        return items;
    }
}
