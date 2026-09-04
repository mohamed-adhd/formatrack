using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class NoteRepository : Repository<Note>, INoteRepository
{
    protected override string TableName => "notes";
    protected override string IdColumn => "id_note";

    protected override string InsertSql => @"INSERT INTO notes (id_stagiaire, id_module, id_session, note, saisi_par)
 VALUES ($id_stagiaire, $id_module, $id_session, $note, $saisi_par)";

    protected override string UpdateSql => @"UPDATE notes SET note=$note, date_saisie=datetime('now') WHERE id_note=$id_note";

    protected override Note Map(SqliteDataReader r) => new()
    {
        IdNote = Int(r, "id_note"),
        IdStagiaire = Int(r, "id_stagiaire"),
        IdModule = Int(r, "id_module"),
        IdSession = Int(r, "id_session"),
        NoteValeur = Double(r, "note"),
        DateSaisie = Has(r, "date_saisie") ? Date(r, "date_saisie") : DateTime.Now,
        SaisiPar = Int(r, "saisi_par")
    };

    protected override void FillInsert(SqliteCommand c, Note n)
    {
        c.Parameters.AddWithValue("$id_stagiaire", n.IdStagiaire);
        c.Parameters.AddWithValue("$id_module", n.IdModule);
        c.Parameters.AddWithValue("$id_session", n.IdSession);
        c.Parameters.AddWithValue("$note", n.NoteValeur);
        c.Parameters.AddWithValue("$saisi_par", n.SaisiPar);
    }

    protected override void FillUpdate(SqliteCommand c, Note n)
    {
        c.Parameters.AddWithValue("$id_note", n.IdNote);
        c.Parameters.AddWithValue("$note", n.NoteValeur);
    }

    public async Task<IReadOnlyList<Note>> GetByModuleSessionAsync(int idModule, int idSession)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Note>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($@"SELECT n.*, u.nom || ' ' || u.prenom AS stagiaire_nom
 FROM {TableName} n INNER JOIN utilisateurs u ON n.id_stagiaire = u.id_utilisateur
 WHERE n.id_module=$mod AND n.id_session=$sess ORDER BY u.nom;", conn);
        cmd.Parameters.AddWithValue("$mod", idModule);
        cmd.Parameters.AddWithValue("$sess", idSession);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var note = Map(r);
            note.StagiaireNom = Text(r, "stagiaire_nom");
            items.Add(note);
        }
        return items;
    }

    public async Task<IReadOnlyList<Note>> GetByStagiaireAsync(int idStagiaire)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Note>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($@"SELECT n.*, m.titre AS module_titre, m.coefficient AS module_coef
 FROM {TableName} n INNER JOIN modules m ON n.id_module = m.id_module
 WHERE n.id_stagiaire=$id ORDER BY n.date_saisie DESC;", conn);
        cmd.Parameters.AddWithValue("$id", idStagiaire);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var note = Map(r);
            note.ModuleTitre = Text(r, "module_titre");
            note.ModuleCoefficient = Double(r, "module_coef");
            items.Add(note);
        }
        return items;
    }

    public async Task<IReadOnlyList<Note>> GetBySessionAsync(int idSession)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Note>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($@"SELECT n.*, u.nom || ' ' || u.prenom AS stagiaire_nom, m.titre AS module_titre, m.coefficient AS module_coef
 FROM {TableName} n
 INNER JOIN utilisateurs u ON n.id_stagiaire = u.id_utilisateur
 INNER JOIN modules m ON n.id_module = m.id_module
 WHERE n.id_session=$sess ORDER BY u.nom, m.titre;", conn);
        cmd.Parameters.AddWithValue("$sess", idSession);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var note = Map(r);
            note.StagiaireNom = Text(r, "stagiaire_nom");
            note.ModuleTitre = Text(r, "module_titre");
            note.ModuleCoefficient = Double(r, "module_coef");
            items.Add(note);
        }
        return items;
    }

    public async Task<IReadOnlyList<Note>> GetAllNotesWithDetailsAsync(int? idFormation = null, string? promotion = null, IEnumerable<int>? sessionIds = null, string? etat = null)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Note>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();

        var where = new List<string>();
        var parameters = new Dictionary<string, object>();

        if (idFormation.HasValue)
        {
            where.Add("m.id_formation = $formation");
            parameters["$formation"] = idFormation.Value;
        }
        if (!string.IsNullOrEmpty(promotion))
        {
            where.Add("u.promotion = $promo");
            parameters["$promo"] = promotion;
        }
        if (sessionIds != null)
        {
            var ids = sessionIds.ToList();
            if (ids.Count > 0)
            {
                var placeholders = string.Join(",", ids.Select((_, i) => $"$sess{i}"));
                where.Add($"n.id_session IN ({placeholders})");
                for (int i = 0; i < ids.Count; i++)
                    parameters[$"$sess{i}"] = ids[i];
            }
        }
        if (!string.IsNullOrEmpty(etat))
        {
            where.Add("u.etat = $etat");
            parameters["$etat"] = etat;
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
        var sql = $@"SELECT n.*, u.nom || ' ' || u.prenom AS stagiaire_nom, u.departement, u.promotion,
         m.titre AS module_titre, m.coefficient AS module_coef, m.id_formation,
         f.titre AS formation_titre, s.date_debut, s.date_fin, s.lieu
         FROM {TableName} n
         INNER JOIN utilisateurs u ON n.id_stagiaire = u.id_utilisateur
         INNER JOIN modules m ON n.id_module = m.id_module
         INNER JOIN formations f ON m.id_formation = f.id_formation
         INNER JOIN sessions s ON n.id_session = s.id_session
         {whereClause}
         ORDER BY u.nom, m.titre;";

        await using var cmd = new SqliteCommand(sql, conn);
        foreach (var p in parameters)
            cmd.Parameters.AddWithValue(p.Key, p.Value);

        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var note = Map(r);
            note.StagiaireNom = Text(r, "stagiaire_nom");
            note.ModuleTitre = Text(r, "module_titre");
            note.ModuleCoefficient = Double(r, "module_coef");
            note.SessionTitre = Text(r, "lieu");
            items.Add(note);
        }
        return items;
    }

    public async Task<Note?> GetUniqueAsync(int idStagiaire, int idModule, int idSession)
    {
        await AppDbContext.InitializeAsync();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($@"SELECT * FROM {TableName}
 WHERE id_stagiaire=$stag AND id_module=$mod AND id_session=$sess LIMIT 1;", conn);
        cmd.Parameters.AddWithValue("$stag", idStagiaire);
        cmd.Parameters.AddWithValue("$mod", idModule);
        cmd.Parameters.AddWithValue("$sess", idSession);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task BulkSaveAsync(IEnumerable<Note> notes)
    {
        await AppDbContext.InitializeAsync();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        foreach (var note in notes)
        {
            var existing = await GetUniqueAsync(note.IdStagiaire, note.IdModule, note.IdSession);
            if (existing != null)
            {
                existing.NoteValeur = note.NoteValeur;
                await UpdateAsync(existing);
            }
            else
            {
                await AddAsync(note);
            }
        }
    }
}
