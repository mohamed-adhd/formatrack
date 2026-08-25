using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Data;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public class RapportRepository : Repository<Rapport>, IRapportRepository
{
    protected override string TableName => "rapports";
    protected override string IdColumn => "id_rapport";
    protected override string InsertSql => @"INSERT INTO rapports (id_utilisateur, titre, type_rapport, format, chemin_fichier, date_generation)
VALUES ($utilisateur, $titre, $type, $format, $chemin, $date)";
    protected override string UpdateSql => @"UPDATE rapports SET id_utilisateur=$utilisateur, titre=$titre, type_rapport=$type,
format=$format, chemin_fichier=$chemin, date_generation=$date WHERE id_rapport=$id";

    protected override Rapport Map(SqliteDataReader r) => new()
    {
        IdRapport = Int(r, "id_rapport"),
        IdUtilisateur = r["id_utilisateur"] == DBNull.Value ? null : Int(r, "id_utilisateur"),
        Titre = Text(r, "titre"),
        TypeRapport = Text(r, "type_rapport"),
        Format = Text(r, "format"),
        CheminFichier = Text(r, "chemin_fichier"),
        DateGeneration = Date(r, "date_generation")
    };

    protected override void FillInsert(SqliteCommand c, Rapport e) => Fill(c, e);
    protected override void FillUpdate(SqliteCommand c, Rapport e) { c.Parameters.AddWithValue("$id", e.IdRapport); Fill(c, e); }
    private static void Fill(SqliteCommand c, Rapport e)
    {
        c.Parameters.AddWithValue("$utilisateur", Db(e.IdUtilisateur));
        c.Parameters.AddWithValue("$titre", e.Titre);
        c.Parameters.AddWithValue("$type", string.IsNullOrWhiteSpace(e.TypeRapport) ? "Statistique" : e.TypeRapport);
        c.Parameters.AddWithValue("$format", string.IsNullOrWhiteSpace(e.Format) ? "CSV" : e.Format);
        c.Parameters.AddWithValue("$chemin", Db(e.CheminFichier));
        c.Parameters.AddWithValue("$date", e.DateGeneration.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}