using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Services;

namespace formatrack.Data;

public static class AppDbContext
{
    public static string DatabasePath => Path.Combine(AppContext.BaseDirectory, "Assets", "database.db");
    public static string ConnectionString => $"Data Source={DatabasePath}";

    public static async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();

        var sql = @"
CREATE TABLE IF NOT EXISTS utilisateurs (
 id_utilisateur INTEGER PRIMARY KEY AUTOINCREMENT, nom TEXT NOT NULL, prenom TEXT NOT NULL,
 email TEXT NOT NULL UNIQUE, mot_de_passe_hash TEXT NOT NULL, role TEXT NOT NULL,
 date_creation TEXT NOT NULL DEFAULT (datetime('now')), actif INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS formations (
 id_formation INTEGER PRIMARY KEY AUTOINCREMENT, titre TEXT NOT NULL, description TEXT,
 objectifs TEXT, duree_heures INTEGER NOT NULL, type_formation TEXT, statut TEXT NOT NULL DEFAULT 'Planifiee');
CREATE TABLE IF NOT EXISTS sessions (
 id_session INTEGER PRIMARY KEY AUTOINCREMENT, id_formation INTEGER NOT NULL,
 date_debut TEXT NOT NULL, date_fin TEXT NOT NULL, lieu TEXT, capacite INTEGER,
 statut TEXT NOT NULL DEFAULT 'Planifiee');
CREATE TABLE IF NOT EXISTS questionnaires (
 id_questionnaire INTEGER PRIMARY KEY AUTOINCREMENT, id_session INTEGER NOT NULL,
 titre TEXT NOT NULL, description TEXT, type_evaluation TEXT, date_creation TEXT NOT NULL DEFAULT (datetime('now')),
 statut TEXT NOT NULL DEFAULT 'Brouillon');
CREATE TABLE IF NOT EXISTS evaluations (
 id_evaluation INTEGER PRIMARY KEY AUTOINCREMENT, id_utilisateur INTEGER NOT NULL, id_questionnaire INTEGER NOT NULL,
 date_passage TEXT, score_total REAL, pourcentage REAL, statut TEXT NOT NULL DEFAULT 'EnCours');
CREATE TABLE IF NOT EXISTS participation (
 id_participation INTEGER PRIMARY KEY AUTOINCREMENT, id_utilisateur INTEGER NOT NULL, id_session INTEGER NOT NULL,
 role_participation TEXT NOT NULL, date_inscription TEXT NOT NULL DEFAULT (datetime('now')));";
        await using (var command = new SqliteCommand(sql, connection))
            await command.ExecuteNonQueryAsync();

        await SeedAsync(connection);
    }

    private static async Task SeedAsync(SqliteConnection connection)
    {
        await using var count = new SqliteCommand("SELECT COUNT(*) FROM utilisateurs;", connection);
        if ((long)(await count.ExecuteScalarAsync() ?? 0L) > 0)
            return;

        var hash = PasswordHasher.Hash("admin123");
        var sql = @"
INSERT INTO utilisateurs (nom, prenom, email, mot_de_passe_hash, role) VALUES
 ('Admin','SEFAD','admin@sefad.local',$hash,'Administrateur'),
 ('Mansouri','Yasmine','formatrice@sefad.local',$hash,'Formateur'),
 ('Ben Ali','Karim','stagiaire@sefad.local',$hash,'Stagiaire');
INSERT INTO formations (titre, description, objectifs, duree_heures, type_formation, statut) VALUES
 ('Developpement Web ASP.NET','Bases et pratiques pour construire une application web professionnelle.','Maitriser MVC, l acces aux donnees et la securite.',40,'Technique','EnCours'),
 ('Gestion de Projet Agile','Planification, suivi et pilotage iteratif des projets.','Structurer un backlog et suivre les indicateurs.',24,'Management','Planifiee'),
 ('Business Intelligence','Tableaux de bord, indicateurs et aide a la decision.','Transformer les donnees de formation en decisions.',32,'Decisionnel','Planifiee');
INSERT INTO sessions (id_formation, date_debut, date_fin, lieu, capacite, statut) VALUES
 (1,'2026-09-01','2026-09-12','Salle A',18,'EnCours'),
 (2,'2026-10-05','2026-10-09','Salle B',20,'Planifiee'),
 (3,'2026-11-02','2026-11-10','Lab BI',16,'Planifiee');
INSERT INTO questionnaires (id_session, titre, description, type_evaluation, statut) VALUES
 (1,'Evaluation a chaud ASP.NET','Satisfaction et comprehension immediate.','AChaud','Publie'),
 (2,'Evaluation Agile','Preparation du questionnaire.','AChaud','Brouillon');
INSERT INTO participation (id_utilisateur, id_session, role_participation) VALUES
 (2,1,'Formateur'), (3,1,'Stagiaire');";
        await using var seed = new SqliteCommand(sql, connection);
        seed.Parameters.AddWithValue("$hash", hash);
        await seed.ExecuteNonQueryAsync();
    }
}
