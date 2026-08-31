using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using formatrack.Services;

namespace formatrack.Data;

public static class AppDbContext
{
    private static bool _initialized;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public static string DatabasePath => Path.Combine(AppContext.BaseDirectory, "Assets", "database.db");
    public static string ConnectionString => $"Data Source={DatabasePath}";

    public static async Task InitializeAsync()
    {
        if (_initialized) return;
        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
            await using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();

            await using (var pragmas = new SqliteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON; PRAGMA temp_store=MEMORY;", connection))
                await pragmas.ExecuteNonQueryAsync();

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
CREATE INDEX IF NOT EXISTS idx_emplois_formation ON emplois_du_temps (id_formation);
CREATE TABLE IF NOT EXISTS suggestions_aide (
 id INTEGER PRIMARY KEY AUTOINCREMENT, titre TEXT NOT NULL,
 description TEXT NOT NULL, priorite INTEGER NOT NULL DEFAULT 3,
 categorie TEXT NOT NULL, action_page TEXT NOT NULL,
 action_params TEXT DEFAULT '', est_lu INTEGER NOT NULL DEFAULT 0,
 date_generation TEXT NOT NULL DEFAULT (datetime('now'))
);
CREATE TABLE IF NOT EXISTS modules (
 id_module INTEGER PRIMARY KEY AUTOINCREMENT, id_formation INTEGER NOT NULL,
 titre TEXT NOT NULL, credit_horaire INTEGER NOT NULL DEFAULT 0,
 nb_examen INTEGER NOT NULL DEFAULT 1, coefficient REAL NOT NULL DEFAULT 1.0,
 est_commum INTEGER NOT NULL DEFAULT 0,
 FOREIGN KEY (id_formation) REFERENCES formations(id_formation) ON DELETE CASCADE);
CREATE INDEX IF NOT EXISTS idx_modules_formation ON modules (id_formation);
CREATE TABLE IF NOT EXISTS notes (
 id_note INTEGER PRIMARY KEY AUTOINCREMENT, id_stagiaire INTEGER NOT NULL,
 id_module INTEGER NOT NULL, id_session INTEGER NOT NULL,
 note REAL NOT NULL, date_saisie TEXT NOT NULL DEFAULT (datetime('now')),
 saisi_par INTEGER NOT NULL,
 FOREIGN KEY (id_stagiaire) REFERENCES utilisateurs(id_utilisateur),
 FOREIGN KEY (id_module) REFERENCES modules(id_module),
 FOREIGN KEY (id_session) REFERENCES sessions(id_session),
 FOREIGN KEY (saisi_par) REFERENCES utilisateurs(id_utilisateur));
CREATE INDEX IF NOT EXISTS idx_notes_stagiaire ON notes (id_stagiaire);
CREATE INDEX IF NOT EXISTS idx_notes_module ON notes (id_module);
CREATE INDEX IF NOT EXISTS idx_notes_session ON notes (id_session);";
            await using (var command = new SqliteCommand(sql, connection))
                await command.ExecuteNonQueryAsync();

            await EnsureColumnsAsync(connection);
            await SeedAsync(connection);

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
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

                // ===== FORMATIONS (3) =====
                var sqlFormations = @"
INSERT INTO formations (titre, description, objectifs, duree_heures, type_formation, statut) VALUES
 ('Telecoms et Reseaux','Formation aux techniques de telecommunications et reseaux pour les officiers d''etat-major.','Maitriser les infrastructures reseaux, les protocoles et les systemes de communication.',120,'Technique','EnCours'),
 ('Maintenance des Micro-ordinateurs','Formation a la maintenance et au deploiement des systemes informatiques.','Maitriser le diagnostic, la reparation et la configuration des equipements informatiques.',90,'Technique','EnCours'),
 ('Informatique de Gestion (Soutien)','Formation en informatique de gestion pour les besoins d''un etat-major.','Developper les competences en bases de donnees, developpement et administration reseau.',100,'Technique','Planifiee');";
                await using var seedFormations = new SqliteCommand(sqlFormations, connection);
                await seedFormations.ExecuteNonQueryAsync();

                // ===== SESSIONS (6) =====
                var sqlSessions = @"
INSERT INTO sessions (id_formation, date_debut, date_fin, lieu, capacite, statut) VALUES
 (1,'2026-09-01','2026-12-15','Salle Telecoms A',18,'EnCours'),
 (1,'2027-01-10','2027-04-30','Salle Telecoms A',18,'Planifiee'),
 (2,'2026-09-01','2026-12-15','Salle Maintenance B',20,'EnCours'),
 (2,'2027-01-10','2027-04-30','Salle Maintenance B',20,'Planifiee'),
 (3,'2026-09-01','2026-12-15','Salle Info C',16,'Planifiee'),
 (3,'2027-01-10','2027-04-30','Salle Info C',16,'Planifiee');";
                await using var seedSessions = new SqliteCommand(sqlSessions, connection);
                await seedSessions.ExecuteNonQueryAsync();

                // ===== MODULES COMMUNS (10) =====
                var sqlModulesCommuns = @"
INSERT INTO modules (id_formation, titre, credit_horaire, nb_examen, coefficient, est_commum) VALUES
 (1,'Arabe',30,1,1.0,1),(1,'Francais',30,1,1.0,1),(1,'Anglais',30,1,1.0,1),
 (1,'Gestion 1',20,1,2.0,1),(1,'Gestion 2',20,1,2.0,1),
 (1,'Legislation',15,1,1.0,1),(1,'Securite et sante au travail',10,1,1.0,1),
 (1,'Concepts qualite',10,1,1.0,1),(1,'Education environnementale',10,1,1.0,1),
 (1,'Education physique',20,1,1.0,1),
 (2,'Arabe',30,1,1.0,1),(2,'Francais',30,1,1.0,1),(2,'Anglais',30,1,1.0,1),
 (2,'Gestion 1',20,1,2.0,1),(2,'Gestion 2',20,1,2.0,1),
 (2,'Legislation',15,1,1.0,1),(2,'Securite et sante au travail',10,1,1.0,1),
 (2,'Concepts qualite',10,1,1.0,1),(2,'Education environnementale',10,1,1.0,1),
 (2,'Education physique',20,1,1.0,1),
 (3,'Arabe',30,1,1.0,1),(3,'Francais',30,1,1.0,1),(3,'Anglais',30,1,1.0,1),
 (3,'Gestion 1',20,1,2.0,1),(3,'Gestion 2',20,1,2.0,1),
 (3,'Legislation',15,1,1.0,1),(3,'Securite et sante au travail',10,1,1.0,1),
 (3,'Concepts qualite',10,1,1.0,1),(3,'Education environnementale',10,1,1.0,1),
 (3,'Education physique',20,1,1.0,1);";
                await using var seedMC = new SqliteCommand(sqlModulesCommuns, connection);
                await seedMC.ExecuteNonQueryAsync();

                // ===== MODULES FORMATION 1 — Telecoms/Reseaux (34) =====
                var sqlMod1 = @"
INSERT INTO modules (id_formation, titre, credit_horaire, nb_examen, coefficient, est_commum) VALUES
 (1,'Profession et Formation',15,1,2.0,0),(1,'Electricite Electromagnatisme',30,2,3.0,0),
 (1,'Systemes de Cablage',25,1,3.0,0),(1,'Reseaux NGN',35,2,4.0,0),
 (1,'Reseaux Operationnels Tunisiens',30,2,4.0,0),(1,'Microprocesseurs',30,2,3.0,0),
 (1,'Systemes AFV',25,1,3.0,0),(1,'Securite Informatique',20,1,2.0,0),
 (1,'Maintenance des Equipements',30,2,3.0,0),(1,'Deploiement Reseaux Cellulaire',35,2,4.0,0),
 (1,'Anglais Technique',15,1,1.0,0),(1,'Projet Pédagogique PPI',40,1,15.0,0),
 (1,'Stage',200,1,15.0,0),(1,'Topographie',20,1,2.0,0),
 (1,'Transmission Numerique',25,1,3.0,0),(1,'Optique et Fibre Optique',20,1,2.0,0),
 (1,'Systemes Embarques',25,1,3.0,0),(1,'Programmation Reseaux',30,2,3.0,0),
 (1,'Protocoles IP',25,1,3.0,0),(1,'Switching et Routing',30,2,4.0,0),
 (1,'Qualite de Service (QoS)',20,1,2.0,0),(1,'Virtualisation Reseaux',20,1,2.0,0),
 (1,'Securite Reseaux',25,1,3.0,0),(1,'Administration Systemes',25,1,3.0,0),
 (1,'Cloud Computing',20,1,2.0,0),(1,'IoT et Objets Connectes',15,1,2.0,0),
 (1,'5G et Reseaux Futurs',20,1,2.0,0),(1,'Cable Sous-Marin',15,1,2.0,0),
 (1,'Reseaux Militaires',25,1,3.0,0),(1,'Cryptographie',20,1,2.0,0),
 (1,'Forensics Numerique',20,1,2.0,0),(1,'Simulation Reseaux',15,1,1.0,0),
 (1,'Travail en Equipe',10,1,1.0,0),(1,'Communication Professionnelle',15,1,1.0,0);";
                await using var seedM1 = new SqliteCommand(sqlMod1, connection);
                await seedM1.ExecuteNonQueryAsync();

                // ===== MODULES FORMATION 2 — Maintenance Micro-ordinateurs (22) =====
                var sqlMod2 = @"
INSERT INTO modules (id_formation, titre, credit_horaire, nb_examen, coefficient, est_commum) VALUES
 (2,'Metier et Formation',15,1,2.0,0),(2,'Electricite de base',25,1,2.0,0),
 (2,'Semi-conducteurs',20,1,2.0,0),(2,'Electronique Analogique',30,2,3.0,0),
 (2,'Electronique Numerique',30,2,3.0,0),(2,'Informatique generale',20,1,2.0,0),
 (2,'Unite centrale et peripheriques',30,2,3.0,0),(2,'Montage et configuration PC',30,2,3.0,0),
 (2,'Microprocesseurs',30,2,3.0,0),(2,'Microcontroleurs',20,1,2.0,0),
 (2,'Maintenance Hard des PC',35,2,4.0,0),(2,'Maintenance Soft des PC',35,2,4.0,0),
 (2,'Reseaux poste-a-poste',20,1,2.0,0),(2,'Depannage bureautique',25,1,3.0,0),
 (2,'Stage en milieu de travail',200,1,15.0,0),(2,'Soudure et CABlage',15,1,1.0,0),
 (2,'Alimentations a impulsions',20,1,2.0,0),(2,'Impression et Peripheriques',15,1,2.0,0),
 (2,'Onduleurs et Protection',15,1,2.0,0),(2,'Diagnostic Puce',20,1,2.0,0),
 (2,'Techniques de Mesure',15,1,1.0,0),(2,'Documentation Technique',10,1,1.0,0);";
                await using var seedM2 = new SqliteCommand(sqlMod2, connection);
                await seedM2.ExecuteNonQueryAsync();

                // ===== MODULES FORMATION 3 — Informatique de Gestion (32) =====
                var sqlMod3 = @"
INSERT INTO modules (id_formation, titre, credit_horaire, nb_examen, coefficient, est_commum) VALUES
 (3,'Fonction de travail',15,1,2.0,0),(3,'Algorithmes et Programmation',40,2,4.0,0),
 (3,'Bases de Donnees',35,2,4.0,0),(3,'Securite d''un Systeme Informatique',20,1,2.0,0),
 (3,'Reseaux Locaux',25,1,3.0,0),(3,'Administration reseau',25,1,3.0,0),
 (3,'Developpement d''applications locales',35,2,4.0,0),(3,'Gestion d''un parc informatique',20,1,2.0,0),
 (3,'Stage d''observation',50,1,5.0,0),(3,'Stage d''integration',200,1,15.0,0),
 (3,'Systeme d''information',20,1,2.0,0),(3,'Methode Agile',15,1,2.0,0),
 (3,'Langage C/C++',30,2,3.0,0),(3,'Langage Java',30,2,3.0,0),
 (3,'Langage Python',25,1,3.0,0),(3,'HTML/CSS/JavaScript',25,1,2.0,0),
 (3,'Framework Web',30,2,3.0,0),(3,'ERP et CRM',20,1,2.0,0),
 (3,'Business Intelligence',20,1,2.0,0),(3,'Gestion de Projet IT',15,1,2.0,0),
 (3,'Qualite Logicielle',15,1,1.0,0),(3,'Virtualisation et Conteneurs',20,1,2.0,0),
 (3,'Administration BDD',25,1,3.0,0),(3,'Developpement Mobile',25,1,2.0,0),
 (3,'API et Microservices',20,1,2.0,0),(3,'DevOps et CI/CD',15,1,2.0,0),
 (3,'UML et Merise',20,1,2.0,0),(3,'Logique et Structure',15,1,1.0,0),
 (3,'Anglais Informatique',15,1,1.0,0),(3,'Communication Technique',15,1,1.0,0),
 (3,'Travail en Equipe',10,1,1.0,0),(3,'Culture Numerique',10,1,1.0,0);";
                await using var seedM3 = new SqliteCommand(sqlMod3, connection);
                await seedM3.ExecuteNonQueryAsync();

                // ===== QUESTIONNAIRES (6) =====
                var sqlQuestionnaires = @"
INSERT INTO questionnaires (id_session, titre, description, type_evaluation, statut) VALUES
 (1,'Eval a chaud - Telecoms Session 1','Satisfaction et comprehension immediate.','AChaud','Publie'),
 (1,'Eval a froid - Telecoms Session 1','Retention des acquis 3 mois apres.','AFroid','Brouillon'),
 (3,'Eval a chaud - Maintenance Session 1','Satisfaction et comprehension immediate.','AChaud','Publie'),
 (3,'Eval a froid - Maintenance Session 1','Retention des acquis 3 mois apres.','AFroid','Brouillon'),
 (5,'Eval a chaud - Info Session 1','Satisfaction et comprehension immediate.','AChaud','Publie'),
 (5,'Eval a froid - Info Session 1','Retention des acquis 3 mois apres.','AFroid','Brouillon');";
                await using var seedQ = new SqliteCommand(sqlQuestionnaires, connection);
                await seedQ.ExecuteNonQueryAsync();

                // ===== PARTICIPATION (18) =====
                var sqlPart = @"
INSERT INTO participation (id_utilisateur, id_session, role_participation) VALUES
 (4,1,'Formateur'), (9,1,'Stagiaire'), (10,1,'Stagiaire'), (11,1,'Stagiaire'), (12,1,'Stagiaire'),
 (5,1,'Formateur'), (13,1,'Stagiaire'), (14,1,'Stagiaire'), (15,1,'Stagiaire'), (16,1,'Stagiaire'),
 (6,3,'Formateur'), (9,3,'Stagiaire'), (10,3,'Stagiaire'), (17,3,'Stagiaire'), (18,3,'Stagiaire'),
 (4,5,'Formateur'), (13,5,'Stagiaire'), (14,5,'Stagiaire'), (15,5,'Stagiaire'), (16,5,'Stagiaire');";
                await using var seedPart = new SqliteCommand(sqlPart, connection);
                await seedPart.ExecuteNonQueryAsync();

                // ===== NOTES / GRADES (sample grades for Formation 1, Session 1, modules 11-20) =====
                var sqlNotes = @"
INSERT INTO notes (id_stagiaire, id_module, id_session, note, saisi_par) VALUES
 (9,11,1,14.5,4),(9,12,1,16.0,4),(9,13,1,12.0,4),(9,14,1,15.5,4),(9,15,1,13.0,4),
 (9,16,1,17.0,4),(9,17,1,14.0,4),(9,18,1,11.5,4),(9,19,1,16.5,4),(9,20,1,15.0,4),
 (10,11,1,12.0,4),(10,12,1,14.5,4),(10,13,1,10.5,4),(10,14,1,13.0,4),(10,15,1,11.0,4),
 (10,16,1,15.5,4),(10,17,1,12.5,4),(10,18,1,9.0,4),(10,19,1,14.0,4),(10,20,1,13.5,4),
 (11,11,1,16.0,4),(11,12,1,18.0,4),(11,13,1,14.0,4),(11,14,1,17.5,4),(11,15,1,15.0,4),
 (11,16,1,19.0,4),(11,17,1,16.0,4),(11,18,1,13.5,4),(11,19,1,18.5,4),(11,20,1,17.0,4),
 (12,11,1,10.0,4),(12,12,1,12.5,4),(12,13,1,8.5,4),(12,14,1,11.0,4),(12,15,1,9.5,4),
 (12,16,1,13.0,4),(12,17,1,10.5,4),(12,18,1,7.0,4),(12,19,1,12.0,4),(12,20,1,11.5,4);";
                await using var seedNotes = new SqliteCommand(sqlNotes, connection);
                await seedNotes.ExecuteNonQueryAsync();
            }
        }

        // ===== EMPLOIS DU TEMPS (2 sample) =====
        await using (var countEmplois = new SqliteCommand("SELECT COUNT(*) FROM emplois_du_temps;", connection))
        {
            if ((long)(await countEmplois.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlEmplois = @"
INSERT INTO emplois_du_temps (id_formation, type_emploi, annee, promotion, chemin_image, uploaded_by, statut, description) VALUES
 (1,'Hebdomadaire','2025-2026','Promotion 2026','avares://formatrack/Assets/emplois_du_temps/timetable.jpg',1,'Publie','Emploi du temps hebdomadaire - Classe 2GT12'),
 (1,'Annuel','2025-2026','Promotion 2026','avares://formatrack/Assets/emplois_du_temps/timetable-mensuel.jpg',1,'Publie','Chronogramme annuel - Semaines type GS/MS');";
                await using var seedEmplois = new SqliteCommand(sqlEmplois, connection);
                await seedEmplois.ExecuteNonQueryAsync();
            }
        }

        // ===== ABSENCES & RETARDS (12) =====
        await using (var countAbs = new SqliteCommand("SELECT COUNT(*) FROM absences_retards;", connection))
        {
            if ((long)(await countAbs.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlAbs = @"
INSERT INTO absences_retards (utilisateur_id, session_id, cours, date, type, duree, justifiee, motif) VALUES
 (9,1,'Electricite Electromagnatisme','24/08/2026','Absence','1 jour',0,''),
 (9,1,'Reseaux NGN','20/08/2026','Absence','1 jour',1,'Certificat medical valide'),
 (10,1,'Microprocesseurs','22/08/2026','Retard','30 min',1,'Retard transport'),
 (11,1,'Systemes de Cablage','25/08/2026','Absence','1 jour',1,'Permission accordee'),
 (12,1,'Profession et Formation','19/08/2026','Absence','1 jour',0,''),
 (17,3,'Electricite de base','18/08/2026','Retard','20 min',1,'Embouteillage'),
 (18,3,'Semi-conducteurs','17/08/2026','Absence','1 jour',0,''),
 (13,5,'Algorithmes','03/09/2026','Retard','15 min',1,'Retard justifie'),
 (14,5,'Bases de Donnees','05/09/2026','Absence','1 jour',1,'Certificat medical'),
 (15,5,'Reseaux Locaux','07/09/2026','Absence','1 jour',0,''),
 (16,5,'Administration reseau','09/09/2026','Retard','25 min',1,'Convoyage'),
 (9,3,'Montage et configuration PC','25/08/2026','Retard','10 min',1,'Retard mineur');";
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
 (9,'Note publiee : 14.5/20 en Electricite Electromagnatisme (Telecoms - Session 1).',0,'2026-08-22 16:30:00'),
 (9,'Note publiee : 16.0/20 en Reseaux NGN.',0,'2026-08-22 16:35:00'),
 (10,'Alerte : Absence non justifiee le 22/08/2026 - Microprocesseurs.',0,'2026-08-22 10:00:00'),
 (12,'Note en Tactique : 9.5/20 - Cours de renforcement proposes.',0,'2026-08-27 15:30:00'),
 (4,'Nouvelles inscriptions : 4 stagiaires dans Telecoms Session 1.',1,'2026-08-15 09:00:00'),
 (2,'Rapport mensuel : 2 absences non justifiees dans le departement Terre.',0,'2026-08-31 08:00:00'),
 (8,'Tableau de bord mis a jour : Taux de reussite global a 74.3%.',0,'2026-08-27 18:00:00');";
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
 (4,'Publication evaluation','Publication evaluation a chaud Telecoms Session 1','2026-08-15 09:30:00'),
 (4,'Saisie de note','Saisie bulk: 40 notes pour Telecoms Session 1','2026-08-22 16:00:00'),
 (4,'Saisie de note','Note saisie pour Ben Ali Karim : 14.5/20 en Electricite','2026-08-22 16:00:00'),
 (6,'Publication evaluation','Publication evaluation a chaud Maintenance Session 1','2026-08-15 10:00:00'),
 (9,'Justification absence','Justification soumise pour absence du 20/08/2026','2026-08-20 14:00:00'),
 (1,'Configuration systeme','Creation des comptes utilisateurs et attribution des roles','2026-06-01 08:00:00');";
                await using var seedJournal = new SqliteCommand(sqlJournal, connection);
                await seedJournal.ExecuteNonQueryAsync();
            }
        }
    }
}
