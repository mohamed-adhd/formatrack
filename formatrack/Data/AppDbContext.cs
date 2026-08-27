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
 departement TEXT NOT NULL DEFAULT '', promotion TEXT NOT NULL DEFAULT '',
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
CREATE TABLE IF NOT EXISTS criteres (
 id_critere INTEGER PRIMARY KEY AUTOINCREMENT, id_questionnaire INTEGER NOT NULL,
 libelle TEXT NOT NULL, description TEXT, coefficient REAL NOT NULL DEFAULT 1.0);
CREATE TABLE IF NOT EXISTS questions (
 id_question INTEGER PRIMARY KEY AUTOINCREMENT, id_questionnaire INTEGER NOT NULL, id_critere INTEGER,
 enonce TEXT NOT NULL, type_question TEXT NOT NULL, bareme REAL NOT NULL DEFAULT 0, ordre INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS evaluations (
 id_evaluation INTEGER PRIMARY KEY AUTOINCREMENT, id_utilisateur INTEGER NOT NULL, id_questionnaire INTEGER NOT NULL,
 date_passage TEXT, score_total REAL, pourcentage REAL, statut TEXT NOT NULL DEFAULT 'EnCours');
CREATE TABLE IF NOT EXISTS reponses (
 id_reponse INTEGER PRIMARY KEY AUTOINCREMENT, id_evaluation INTEGER NOT NULL, id_question INTEGER NOT NULL,
 contenu TEXT, est_correcte INTEGER, score_obtenu REAL);
CREATE TABLE IF NOT EXISTS participation (
 id_participation INTEGER PRIMARY KEY AUTOINCREMENT, id_utilisateur INTEGER NOT NULL, id_session INTEGER NOT NULL,
 role_participation TEXT NOT NULL, date_inscription TEXT NOT NULL DEFAULT (datetime('now')));
CREATE INDEX IF NOT EXISTS idx_sessions_formation ON sessions (id_formation);
CREATE INDEX IF NOT EXISTS idx_questionnaires_session ON questionnaires (id_session);
CREATE INDEX IF NOT EXISTS idx_questions_questionnaire ON questions (id_questionnaire);
CREATE INDEX IF NOT EXISTS idx_evaluations_utilisateur ON evaluations (id_utilisateur);
CREATE INDEX IF NOT EXISTS idx_evaluations_questionnaire ON evaluations (id_questionnaire);
CREATE INDEX IF NOT EXISTS idx_reponses_evaluation ON reponses (id_evaluation);
CREATE INDEX IF NOT EXISTS idx_participation_utilisateur ON participation (id_utilisateur);
CREATE INDEX IF NOT EXISTS idx_participation_session ON participation (id_session);
CREATE TABLE IF NOT EXISTS rapports (
 id_rapport INTEGER PRIMARY KEY AUTOINCREMENT, id_utilisateur INTEGER,
 titre TEXT NOT NULL, type_rapport TEXT NOT NULL, format TEXT NOT NULL DEFAULT 'CSV',
 chemin_fichier TEXT, date_generation TEXT NOT NULL DEFAULT (datetime('now')));
CREATE TABLE IF NOT EXISTS journal_activite (
 id_journal INTEGER PRIMARY KEY AUTOINCREMENT, id_utilisateur INTEGER,
 action TEXT NOT NULL, details TEXT, date_action TEXT NOT NULL DEFAULT (datetime('now')));
CREATE TABLE IF NOT EXISTS notifications (
 id_notification INTEGER PRIMARY KEY AUTOINCREMENT, id_utilisateur INTEGER NOT NULL,
 message TEXT NOT NULL, lue INTEGER NOT NULL DEFAULT 0, date_creation TEXT NOT NULL DEFAULT (datetime('now')));
CREATE TABLE IF NOT EXISTS absences_retards (
 id INTEGER PRIMARY KEY AUTOINCREMENT, utilisateur_id INTEGER NOT NULL, session_id INTEGER,
 cours TEXT NOT NULL, date TEXT NOT NULL, type TEXT NOT NULL, duree TEXT,
 justifiee INTEGER NOT NULL DEFAULT 0, motif TEXT NOT NULL DEFAULT '',
 created_at TEXT NOT NULL DEFAULT (datetime('now')),
 FOREIGN KEY (utilisateur_id) REFERENCES utilisateurs(id_utilisateur) ON DELETE CASCADE);
CREATE INDEX IF NOT EXISTS idx_journal_utilisateur ON journal_activite (id_utilisateur);
CREATE INDEX IF NOT EXISTS idx_notifications_utilisateur ON notifications (id_utilisateur);
CREATE TABLE IF NOT EXISTS emplois_du_temps (
 id_emploi INTEGER PRIMARY KEY AUTOINCREMENT, id_formation INTEGER NOT NULL,
 type_emploi TEXT NOT NULL, annee TEXT NOT NULL, promotion TEXT NOT NULL DEFAULT '',
 chemin_image TEXT NOT NULL, date_upload TEXT NOT NULL DEFAULT (datetime('now')),
 uploaded_by INTEGER NOT NULL, statut TEXT NOT NULL DEFAULT 'Brouillon',
 description TEXT NOT NULL DEFAULT '',
 FOREIGN KEY (id_formation) REFERENCES formations(id_formation) ON DELETE CASCADE,
 FOREIGN KEY (uploaded_by) REFERENCES utilisateurs(id_utilisateur));
CREATE INDEX IF NOT EXISTS idx_emplois_formation ON emplois_du_temps (id_formation);";
        await using (var command = new SqliteCommand(sql, connection))
            await command.ExecuteNonQueryAsync();

        await EnsureColumnsAsync(connection);
        await SeedAsync(connection);
    }

    // Migrations "a chaud" : ajoute les colonnes manquantes aux tables deja creees.
    private static async Task EnsureColumnsAsync(SqliteConnection connection)
    {
        await EnsureColumnAsync(connection, "questionnaires", "note_maximale",
            "ALTER TABLE questionnaires ADD COLUMN note_maximale REAL NOT NULL DEFAULT 20.0;");
        await EnsureColumnAsync(connection, "questionnaires", "duree_minutes",
            "ALTER TABLE questionnaires ADD COLUMN duree_minutes INTEGER;");
        await EnsureColumnAsync(connection, "evaluations", "score_maximum",
            "ALTER TABLE evaluations ADD COLUMN score_maximum REAL;");
        await EnsureColumnAsync(connection, "utilisateurs", "departement",
            "ALTER TABLE utilisateurs ADD COLUMN departement TEXT NOT NULL DEFAULT '';");
        await EnsureColumnAsync(connection, "utilisateurs", "promotion",
            "ALTER TABLE utilisateurs ADD COLUMN promotion TEXT NOT NULL DEFAULT '';");
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string table, string column, string alterSql)
    {
        await using var pragma = new SqliteCommand($"PRAGMA table_info({table});", connection);
        await using var reader = await pragma.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return;
        }

        await using var alter = new SqliteCommand(alterSql, connection);
        await alter.ExecuteNonQueryAsync();
    }

    private static async Task SeedAsync(SqliteConnection connection)
    {
        await using (var count = new SqliteCommand("SELECT COUNT(*) FROM utilisateurs;", connection))
        {
            if ((long)(await count.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var hash = PasswordHasher.Hash("admin123");

                // ===== UTILISATEURS (18) =====
                var sqlUsers = @"
INSERT INTO utilisateurs (nom, prenom, email, mot_de_passe_hash, role, departement, promotion) VALUES
 ('Admin','SEFAD','admin@sefad.local',$hash,'Administrateur','',''),
 ('Harbi','Ali','chefdep@sefad.local',$hash,'ChefDepartement','Terre',''),
 ('Nacer','Djamel','chefdep2@sefad.local',$hash,'ChefDepartement','Air',''),
 ('Mansouri','Yasmine','formatrice@sefad.local',$hash,'Formateur','Terre','Promotion 2026'),
 ('Tlemcani','Omar','formateur2@sefad.local',$hash,'Formateur','Terre','Promotion 2025'),
 ('Bensalem','Rania','formateur3@sefad.local',$hash,'Formateur','Air','Promotion 2026'),
 ('Hadj','Ahmed','resp.formation@sefad.local',$hash,'ResponsableFormation','',''),
 ('Bouzid','Mourad','decideur@sefad.local',$hash,'Decideur','',''),
 ('Ben Ali','Karim','stagiaire@sefad.local',$hash,'Stagiaire','Terre','Promotion 2026'),
 ('Khelifi','Ahmed','stagiaire2@sefad.local',$hash,'Stagiaire','Terre','Promotion 2026'),
 ('Bouazizi','Fatma','stagiaire3@sefad.local',$hash,'Stagiaire','Terre','Promotion 2026'),
 ('Mansour','Sami','stagiaire4@sefad.local',$hash,'Stagiaire','Terre','Promotion 2026'),
 ('Zouari','Mohamed','stagiaire5@sefad.local',$hash,'Stagiaire','Terre','Promotion 2025'),
 ('Trabelsi','Souad','stagiaire6@sefad.local',$hash,'Stagiaire','Terre','Promotion 2025'),
 ('Gharbi','Youssef','stagiaire7@sefad.local',$hash,'Stagiaire','Terre','Promotion 2025'),
 ('Jaziri','Nadia','stagiaire8@sefad.local',$hash,'Stagiaire','Terre','Promotion 2025'),
 ('Mebarki','Tarek','stagiaire9@sefad.local',$hash,'Stagiaire','Air','Promotion 2026'),
 ('Cherif','Amina','stagiaire10@sefad.local',$hash,'Stagiaire','Air','Promotion 2026');";
                await using var seedUsers = new SqliteCommand(sqlUsers, connection);
                seedUsers.Parameters.AddWithValue("$hash", hash);
                await seedUsers.ExecuteNonQueryAsync();

                // ===== FORMATIONS (5) =====
                var sqlFormations = @"
INSERT INTO formations (titre, description, objectifs, duree_heures, type_formation, statut) VALUES
 ('Developpement Web ASP.NET','Maitrise de l''ecosysteme ASP.NET Core pour construire des applications web robustes.','Maitriser MVC, Razor Pages, Entity Framework Core et la securite.',40,'Technique','EnCours'),
 ('Gestion de Projet Agile','Planification, suivi et pilotage iteratif avec Scrum et Kanban.','Structurer un backlog, animer des daily et suivre les indicateurs Agile.',24,'Management','Planifiee'),
 ('Business Intelligence','Conception de tableaux de bord et analyse decisionnelle.','Transformer les donnees de formation en decisions strategiques.',32,'Decisionnel','Planifiee'),
 ('Tactique Interarmes','Entrainement a la planification et coordination interarmes.','Maitriser les phases de preparatif, execution et compte-rendu tactique.',48,'Operationnel','EnCours'),
 ('Renseignement et Communication','Techniques de collecte, traitement et diffusion du renseignement.','Acquerir les methods de renseignement operationnel et communication chiffrée.',36,'Technique','Terminee');";
                await using var seedFormations = new SqliteCommand(sqlFormations, connection);
                await seedFormations.ExecuteNonQueryAsync();

                // ===== SESSIONS (7) =====
                var sqlSessions = @"
INSERT INTO sessions (id_formation, date_debut, date_fin, lieu, capacite, statut) VALUES
 (1,'2026-09-01','2026-09-12','Salle A - Batiment Principal',18,'EnCours'),
 (2,'2026-10-05','2026-10-09','Salle B - Batiment Secondaire',20,'Planifiee'),
 (3,'2026-11-02','2026-11-10','Laboratoire BI',16,'Planifiee'),
 (4,'2026-08-15','2026-08-28','Salle C - Auditorium',15,'EnCours'),
 (5,'2026-06-01','2026-06-12','Salle D - Conference',20,'Terminee'),
 (1,'2027-01-10','2027-01-21','Salle A - Batiment Principal',18,'Planifiee'),
 (4,'2027-02-01','2027-02-14','Terrain d''entrainement',15,'Planifiee');";
                await using var seedSessions = new SqliteCommand(sqlSessions, connection);
                await seedSessions.ExecuteNonQueryAsync();

                // ===== QUESTIONNAIRES (8) =====
                var sqlQuestionnaires = @"
INSERT INTO questionnaires (id_session, titre, description, type_evaluation, statut) VALUES
 (1,'Eval a chaud - ASP.NET','Satisfaction et comprehension immediate du module ASP.NET.','AChaud','Publie'),
 (1,'Eval a froid - ASP.NET','Mesure de retention des acquis 3 mois apres la formation.','AFroid','Brouillon'),
 (2,'Eval a chaud - Agile','Satisfaction et comprehension immediate du module Agile.','AChaud','Brouillon'),
 (3,'Eval a chaud - BI','Satisfaction et comprehension immediate du module BI.','AChaud','Brouillon'),
 (4,'Eval a chaud - Tactique','Satisfaction et comprehension immediate du module tactique.','AChaud','Publie'),
 (4,'Eval a froid - Tactique','Mesure de retention des acquis tactiques 2 mois apres.','AFroid','Brouillon'),
 (5,'Eval a chaud - Renseignement','Satisfaction et comprehension immediate du module renseignement.','AChaud','Publie'),
 (5,'Eval a froid - Renseignement','Mesure de retention des acquis en renseignement.','AFroid','Publie');";
                await using var seedQ = new SqliteCommand(sqlQuestionnaires, connection);
                await seedQ.ExecuteNonQueryAsync();

                // ===== CRITERES (6) =====
                var sqlCriteres = @"
INSERT INTO criteres (id_questionnaire, libelle, description, coefficient) VALUES
 (1,'Contenu pedagogique','Pertinence et richesse du contenu presente.',1.2),
 (1,'Animation et supports','Qualite de l''animation et des supports visuels.',1.0),
 (5,'Contenu tactique','Pertinence du scenario tactique et de la doctrine.',1.5),
 (5,'Mise en situation','Realisme et qualite des exercices pratiques.',1.2),
 (7,'Contenu renseignement','Qualite des supports de renseignement et de la methodologie.',1.3),
 (7,'Analyse operationnelle','Pertinence de l''analyse et des recommandations.',1.0);";
                await using var seedC = new SqliteCommand(sqlCriteres, connection);
                await seedC.ExecuteNonQueryAsync();

                // ===== QUESTIONS (12) =====
                var sqlQuestions = @"
INSERT INTO questions (id_questionnaire, id_critere, enonce, type_question, bareme, ordre) VALUES
 (1,1,'Le contenu repond-il aux objectifs annonces ?','Echelle',5,1),
 (1,2,'Le formateur explique-t-il clairement les notions ?','Echelle',5,2),
 (1,NULL,'Commentaires et suggestions d''amelioration','TexteLibre',0,3),
 (5,3,'Le scenario tactique est-il realiste et coherent ?','Echelle',5,1),
 (5,4,'Les exercices pratiques sont-ils pertinents ?','Echelle',5,2),
 (5,NULL,'Suggestions d''amelioration pour la prochaine session','TexteLibre',0,3),
 (7,5,'Les supports de renseignement sont-ils clairs et complets ?','Echelle',5,1),
 (7,6,'L''analyse operationnelle est-elle pertinente ?','Echelle',5,2),
 (7,NULL,'Commentaires supplementaires','TexteLibre',0,3),
 (2,1,'Le contenu a-t-il ete correctement retenu ?','Echelle',5,1),
 (2,2,'Les connaissances sont-elles applicables en situation reelle ?','Echelle',5,2),
 (2,NULL,'Exemples concrets d''application en poste','TexteLibre',0,3);";
                await using var seedQst = new SqliteCommand(sqlQuestions, connection);
                await seedQst.ExecuteNonQueryAsync();

                // ===== PARTICIPATION (14) =====
                var sqlPart = @"
INSERT INTO participation (id_utilisateur, id_session, role_participation) VALUES
 (4,1,'Formateur'), (9,1,'Stagiaire'), (10,1,'Stagiaire'), (11,1,'Stagiaire'), (12,1,'Stagiaire'),
 (6,4,'Formateur'), (9,4,'Stagiaire'), (10,4,'Stagiaire'), (17,4,'Stagiaire'), (18,4,'Stagiaire'),
 (6,5,'Formateur'), (9,5,'Stagiaire'), (13,5,'Stagiaire'), (14,5,'Stagiaire'),
 (5,5,'Formateur'), (15,5,'Stagiaire'), (16,5,'Stagiaire'),
 (5,2,'Formateur'), (13,2,'Stagiaire'), (14,2,'Stagiaire'),
 (6,3,'Formateur'), (17,3,'Stagiaire'), (18,3,'Stagiaire');";
                await using var seedPart = new SqliteCommand(sqlPart, connection);
                await seedPart.ExecuteNonQueryAsync();

                // ===== EVALUATIONS (23) =====
                var sqlEvals = @"
INSERT INTO evaluations (id_utilisateur, id_questionnaire, date_passage, score_total, score_maximum, pourcentage, statut) VALUES
 (9,1,'2026-08-22',16.0,20.0,80.0,'Terminee'),
 (10,1,'2026-08-22',14.0,20.0,70.0,'Terminee'),
 (11,1,'2026-08-22',17.5,20.0,87.5,'Terminee'),
 (12,1,'2026-08-22',12.0,20.0,60.0,'Terminee'),
 (9,5,'2026-08-27',15.0,20.0,75.0,'Terminee'),
 (10,5,'2026-08-27',13.5,20.0,67.5,'Terminee'),
 (17,5,'2026-08-27',16.5,20.0,82.5,'Terminee'),
 (18,5,'2026-08-27',11.0,20.0,55.0,'Terminee'),
 (9,7,'2026-06-10',18.0,20.0,90.0,'Terminee'),
 (13,7,'2026-06-10',14.0,20.0,70.0,'Terminee'),
 (14,7,'2026-06-10',16.0,20.0,80.0,'Terminee'),
 (15,7,'2026-06-10',10.0,20.0,50.0,'Terminee'),
 (16,7,'2026-06-10',18.5,20.0,92.5,'Terminee'),
 (9,8,'2026-07-15',17.0,20.0,85.0,'Terminee'),
 (13,8,'2026-07-15',12.5,20.0,62.5,'Terminee'),
 (17,8,'2026-07-15',15.0,20.0,75.0,'Terminee'),
 (10,5,'2026-08-27',14.5,20.0,72.5,'Terminee'),
 (11,5,'2026-08-27',14.5,20.0,72.5,'Terminee'),
 (12,5,'2026-08-27',9.5,20.0,47.5,'Terminee'),
 (18,7,'2026-06-10',13.0,20.0,65.0,'Terminee'),
 (10,7,'2026-06-10',15.5,20.0,77.5,'Terminee'),
 (17,7,'2026-06-10',17.0,20.0,85.0,'Terminee'),
 (11,7,'2026-06-10',12.0,20.0,60.0,'Terminee');";
                await using var seedEvals = new SqliteCommand(sqlEvals, connection);
                await seedEvals.ExecuteNonQueryAsync();
            }
        }

        // ===== ABSENCES & RETARDS (18) =====
        await using (var countAbs = new SqliteCommand("SELECT COUNT(*) FROM absences_retards;", connection))
        {
            if ((long)(await countAbs.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlAbs = @"
INSERT INTO absences_retards (utilisateur_id, session_id, cours, date, type, duree, justifiee, motif) VALUES
 (9,1,'Exercice tactique en simulateur','24/08/2026','Absence','1 jour',0,''),
 (9,1,'Doctrine interarmes - Module B','20/08/2026','Absence','1 jour',1,'Certificat medical valide par le medecin de garnison'),
 (9,1,'Transmissions chiffrées','15/08/2026','Retard','15 min',1,'Retard du convoi ferroviaire (justificatif fourni)'),
 (10,1,'Developpement ASP.NET - Seance 3','22/08/2026','Absence','1 demi-journee',0,''),
 (10,4,'Tactique interarmes - Phase 1','18/08/2026','Retard','30 min',1,'Embouteillage sur l''axe principal'),
 (11,1,'Developpement ASP.NET - Seance 5','25/08/2026','Absence','1 jour',1,'Permission exceptionnelle accordee par la hiérarchie'),
 (11,4,'Exercice tactique nocturne','22/08/2026','Retard','20 min',1,'Retard bus de transport'),
 (12,1,'Developpement ASP.NET - Seance 2','19/08/2026','Absence','1 jour',0,''),
 (12,4,'Tactique interarmes - Planification','20/08/2026','Absence','1 demi-journee',1,'Raison familiale urgente'),
 (13,5,'Renseignement - Collecte','03/06/2026','Retard','10 min',1,'Retard justifie'),
 (14,5,'Renseignement - Traitement','05/06/2026','Absence','1 jour',1,'Certificat medical'),
 (15,5,'Renseignement - Analyse','07/06/2026','Absence','1 jour',0,''),
 (16,5,'Renseignement - Communication','09/06/2026','Retard','25 min',1,'Convoyage charge'),
 (17,4,'Exercice tactique interarmes','19/08/2026','Absence','1 jour',1,'Mission de service exterieur'),
 (17,3,'BI - Modelisation','05/11/2026','Retard','15 min',0,''),
 (18,4,'Tactique interarmes - Preparation','17/08/2026','Absence','1 demi-journee',0,''),
 (18,3,'BI - Visualisation','07/11/2026','Retard','20 min',1,'Conditions meteorologiques'),
 (9,4,'Tactique interarmes - Execution','25/08/2026','Retard','10 min',1,'Retard mineur justifie');";
                await using var seedAbs = new SqliteCommand(sqlAbs, connection);
                await seedAbs.ExecuteNonQueryAsync();
            }
        }

        // ===== NOTIFICATIONS =====
        await using (var countNotif = new SqliteCommand("SELECT COUNT(*) FROM notifications;", connection))
        {
            if ((long)(await countNotif.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlNotif = @"
INSERT INTO notifications (id_utilisateur, message, lue, date_creation) VALUES
 (9,'Alerte : Absence non justifiee detectee le 24/08/2026 - Exercice tactique en simulateur.',0,'2026-08-24 14:00:00'),
 (9,'Nouvelle note publiee : 16.0/20 en Developpement ASP.NET (evaluation a chaud).',0,'2026-08-22 16:30:00'),
 (9,'Evaluation a froid planifiee : Retention des acquis ASP.NET le 15/11/2026.',1,'2026-08-20 09:00:00'),
 (9,'Note de service : Mise a jour du reglement interieur de l''Ecole d''Etat-Major.',1,'2026-08-18 08:00:00'),
 (10,'Absence non justifiee enregistree le 22/08/2026 - Developpement ASP.NET.',0,'2026-08-22 10:00:00'),
 (10,'Votre note en Tactique Interarmes : 13.5/20 (moyenne classe : 13.2/20).',0,'2026-08-27 15:00:00'),
 (11,'Justification acceptee pour l''absence du 25/08/2026.',1,'2026-08-26 11:00:00'),
 (12,'Alerte : Absence non justifiee le 19/08/2026 - Developpement ASP.NET.',0,'2026-08-19 10:00:00'),
 (12,'Note en Tactique Interarmes : 9.5/20 - Des cours de renforcement sont proposes.',0,'2026-08-27 15:30:00'),
 (4,'Nouvelles inscriptions : 4 stagiaires dans la session ASP.NET.',1,'2026-08-15 09:00:00'),
 (4,'Evaluation a chaud terminee pour 4 stagiaires - Module ASP.NET.',1,'2026-08-22 17:00:00'),
 (2,'Rapport mensuel : 3 absences non justifiees dans le departement Terre.',0,'2026-08-31 08:00:00'),
 (2,'Session Tactique Interarmes en cours - 4 stagiaires inscrits.',1,'2026-08-15 08:00:00'),
 (8,'Tableau de bord mis a jour : Taux de reussite global a 74.3%.',0,'2026-08-27 18:00:00'),
 (7,'Nouvelle formation planifiee : Developpement Web ASP.NET (session 2) - Janvier 2027.',1,'2026-08-20 10:00:00');";
                await using var seedNotif = new SqliteCommand(sqlNotif, connection);
                await seedNotif.ExecuteNonQueryAsync();
            }
        }

        // ===== JOURNAL D'ACTIVITE =====
        await using (var countJournal = new SqliteCommand("SELECT COUNT(*) FROM journal_activite;", connection))
        {
            if ((long)(await countJournal.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlJournal = @"
INSERT INTO journal_activite (id_utilisateur, action, details, date_action) VALUES
 (1,'Connexion administrateur','Connexion reussie depuis le poste Admin-01','2026-08-15 08:00:00'),
 (4,'Publication evaluation','Publication de l''evaluation a chaud ASP.NET pour la session 1','2026-08-15 09:30:00'),
 (4,'Saisie de note','Note saisie pour Ben Ali Karim : 16.0/20 en ASP.NET','2026-08-22 16:00:00'),
 (4,'Saisie de note','Note saisie pour Khelifi Ahmed : 14.0/20 en ASP.NET','2026-08-22 16:05:00'),
 (4,'Saisie de note','Note saisie pour Bouazizi Fatma : 17.5/20 en ASP.NET','2026-08-22 16:10:00'),
 (4,'Saisie de note','Note saisie pour Mansour Sami : 12.0/20 en ASP.NET','2026-08-22 16:15:00'),
 (6,'Publication evaluation','Publication de l''evaluation a chaud Tactique pour la session 4','2026-08-15 10:00:00'),
 (9,'Justification absence','Justification soumise pour absence du 20/08/2026 - Certificat medical','2026-08-20 14:00:00'),
 (2,'Validation justification','Justification de Ben Ali Karim validee pour absence du 20/08/2026','2026-08-21 09:00:00'),
 (1,'Configuration systeme','Creation des comptes utilisateurs et attribution des roles','2026-06-01 08:00:00');";
                await using var seedJournal = new SqliteCommand(sqlJournal, connection);
                await seedJournal.ExecuteNonQueryAsync();
            }
        }
    }
}
