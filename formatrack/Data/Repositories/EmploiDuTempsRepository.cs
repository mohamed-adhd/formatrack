using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class EmploiDuTempsRepository : Repository<EmploiDuTemps>, IEmploiDuTempsRepository
{
    protected override string TableName => "emplois_du_temps";
    protected override string IdColumn => "id_emploi";

    protected override string InsertSql => @"INSERT INTO emplois_du_temps (id_formation, type_emploi, annee, promotion, chemin_image, uploaded_by, statut, description)
 VALUES ($id_formation, $type_emploi, $annee, $promotion, $chemin_image, $uploaded_by, $statut, $description)";

    protected override string UpdateSql => @"UPDATE emplois_du_temps SET id_formation=$id_formation, type_emploi=$type_emploi, annee=$annee, promotion=$promotion,
 chemin_image=$chemin_image, statut=$statut, description=$description WHERE id_emploi=$id_emploi";

    protected override EmploiDuTemps Map(SqliteDataReader r) => new()
    {
        IdEmploi = Int(r, "id_emploi"),
        IdFormation = Int(r, "id_formation"),
        TypeEmploi = Text(r, "type_emploi"),
        Annee = Text(r, "annee"),
        Promotion = Text(r, "promotion"),
        CheminImage = Text(r, "chemin_image"),
        DateUpload = Has(r, "date_upload") ? Date(r, "date_upload") : DateTime.Now,
        UploadedBy = Int(r, "uploaded_by"),
        Statut = Text(r, "statut"),
        Description = Text(r, "description")
    };

    protected override void FillInsert(SqliteCommand c, EmploiDuTemps e)
    {
        c.Parameters.AddWithValue("$id_formation", e.IdFormation);
        c.Parameters.AddWithValue("$type_emploi", e.TypeEmploi);
        c.Parameters.AddWithValue("$annee", e.Annee);
        c.Parameters.AddWithValue("$promotion", e.Promotion);
        c.Parameters.AddWithValue("$chemin_image", e.CheminImage);
        c.Parameters.AddWithValue("$uploaded_by", e.UploadedBy);
        c.Parameters.AddWithValue("$statut", e.Statut);
        c.Parameters.AddWithValue("$description", e.Description);
    }

    protected override void FillUpdate(SqliteCommand c, EmploiDuTemps e)
    {
        c.Parameters.AddWithValue("$id_emploi", e.IdEmploi);
        c.Parameters.AddWithValue("$id_formation", e.IdFormation);
        c.Parameters.AddWithValue("$type_emploi", e.TypeEmploi);
        c.Parameters.AddWithValue("$annee", e.Annee);
        c.Parameters.AddWithValue("$promotion", e.Promotion);
        c.Parameters.AddWithValue("$chemin_image", e.CheminImage);
        c.Parameters.AddWithValue("$statut", e.Statut);
        c.Parameters.AddWithValue("$description", e.Description);
    }

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByFormationAsync(int idFormation)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<EmploiDuTemps>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($"SELECT * FROM {TableName} WHERE id_formation=$id ORDER BY annee DESC, type_emploi;", conn);
        cmd.Parameters.AddWithValue("$id", idFormation);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByTypeAsync(string typeEmploi)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<EmploiDuTemps>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($"SELECT * FROM {TableName} WHERE type_emploi=$type AND statut='Publie' ORDER BY annee DESC;", conn);
        cmd.Parameters.AddWithValue("$type", typeEmploi);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }

    public async Task<IReadOnlyList<EmploiDuTemps>> GetPubliesAsync()
    {
        await AppDbContext.InitializeAsync();
        var items = new List<EmploiDuTemps>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($"SELECT * FROM {TableName} WHERE statut='Publie' ORDER BY date_upload DESC;", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByDepartementAsync(string departement)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<EmploiDuTemps>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($@"SELECT e.* FROM {TableName} e
 INNER JOIN formations f ON e.id_formation = f.id_formation
 WHERE e.statut='Publie' ORDER BY e.annee DESC;", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByPromotionAsync(string promotion)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<EmploiDuTemps>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($"SELECT * FROM {TableName} WHERE promotion=$promo AND statut='Publie' ORDER BY annee DESC;", conn);
        cmd.Parameters.AddWithValue("$promo", promotion);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }

    public async Task<IReadOnlyList<EmploiDuTemps>> GetByUploaderAsync(int uploadedBy)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<EmploiDuTemps>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($"SELECT * FROM {TableName} WHERE uploaded_by=$id ORDER BY date_upload DESC;", conn);
        cmd.Parameters.AddWithValue("$id", uploadedBy);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }
}
