using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class ModuleRepository : Repository<Module>, IModuleRepository
{
    protected override string TableName => "modules";
    protected override string IdColumn => "id_module";

    protected override string InsertSql => @"INSERT INTO modules (id_formation, titre, credit_horaire, nb_examen, coefficient, est_commum)
 VALUES ($id_formation, $titre, $credit_horaire, $nb_examen, $coefficient, $est_commum)";

    protected override string UpdateSql => @"UPDATE modules SET id_formation=$id_formation, titre=$titre, credit_horaire=$credit_horaire,
 nb_examen=$nb_examen, coefficient=$coefficient, est_commum=$est_commum WHERE id_module=$id_module";

    protected override Module Map(SqliteDataReader r) => new()
    {
        IdModule = Int(r, "id_module"),
        IdFormation = Int(r, "id_formation"),
        Titre = Text(r, "titre"),
        CreditHoraire = Int(r, "credit_horaire"),
        NbExamen = Int(r, "nb_examen"),
        Coefficient = Double(r, "coefficient"),
        EstCommum = Has(r, "est_commum") && Int(r, "est_commum") == 1
    };

    protected override void FillInsert(SqliteCommand c, Module m)
    {
        c.Parameters.AddWithValue("$id_formation", m.IdFormation);
        c.Parameters.AddWithValue("$titre", m.Titre);
        c.Parameters.AddWithValue("$credit_horaire", m.CreditHoraire);
        c.Parameters.AddWithValue("$nb_examen", m.NbExamen);
        c.Parameters.AddWithValue("$coefficient", m.Coefficient);
        c.Parameters.AddWithValue("$est_commum", m.EstCommum ? 1 : 0);
    }

    protected override void FillUpdate(SqliteCommand c, Module m)
    {
        c.Parameters.AddWithValue("$id_module", m.IdModule);
        c.Parameters.AddWithValue("$id_formation", m.IdFormation);
        c.Parameters.AddWithValue("$titre", m.Titre);
        c.Parameters.AddWithValue("$credit_horaire", m.CreditHoraire);
        c.Parameters.AddWithValue("$nb_examen", m.NbExamen);
        c.Parameters.AddWithValue("$coefficient", m.Coefficient);
        c.Parameters.AddWithValue("$est_commum", m.EstCommum ? 1 : 0);
    }

    public async Task<IReadOnlyList<Module>> GetByFormationAsync(int idFormation)
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Module>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($@"SELECT * FROM {TableName} WHERE id_formation=$id OR est_commum=1 ORDER BY est_commum, titre;", conn);
        cmd.Parameters.AddWithValue("$id", idFormation);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }

    public async Task<IReadOnlyList<Module>> GetCommunsAsync()
    {
        await AppDbContext.InitializeAsync();
        var items = new List<Module>();
        await using var conn = new SqliteConnection(AppDbContext.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqliteCommand($"SELECT * FROM {TableName} WHERE est_commum=1 ORDER BY titre;", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) items.Add(Map(r));
        return items;
    }
}
