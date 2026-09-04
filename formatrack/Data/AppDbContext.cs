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
 etat TEXT NOT NULL DEFAULT 'Militaire',
 date_creation TEXT NOT NULL DEFAULT (datetime('now')), actif INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS formations (
 id_formation INTEGER PRIMARY KEY AUTOINCREMENT, titre TEXT NOT NULL, description TEXT,
 objectifs TEXT, duree_heures INTEGER NOT NULL, type_formation TEXT, statut TEXT NOT NULL DEFAULT 'Planifiee');
CREATE TABLE IF NOT EXISTS sessions (
 id_session INTEGER PRIMARY KEY AUTOINCREMENT, id_formation INTEGER NOT NULL,
 date_debut TEXT NOT NULL, date_fin TEXT NOT NULL, lieu TEXT, capacite INTEGER,
 statut TEXT NOT NULL DEFAULT 'Planifiee');
DROP TABLE IF EXISTS reponses;
DROP TABLE IF EXISTS evaluations;
DROP TABLE IF EXISTS questions;
DROP TABLE IF EXISTS criteres;
DROP TABLE IF EXISTS questionnaires;
CREATE TABLE questionnaires (
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

            // Auto-index RAG knowledge base in background after seeding
            _ = Task.Run(async () =>
            {
                try
                {
                    var ragService = new Services.ChatbotRagService();
                    await ragService.IndexKnowledgeBaseAsync();
                }
                catch { }
            });
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
        await EnsureColumnAsync(connection, "utilisateurs", "etat",
            "ALTER TABLE utilisateurs ADD COLUMN etat TEXT NOT NULL DEFAULT 'Militaire';");
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
        var hash = PasswordHasher.Hash("admin123");

        async Task<bool> IsEmpty(string table)
        {
            await using var cmd = new SqliteCommand($"SELECT COUNT(*) FROM {table};", connection);
            return (long)(await cmd.ExecuteScalarAsync() ?? 0L) == 0;
        }

        // ===== UTILISATEURS (21) =====
        if (await IsEmpty("utilisateurs"))
        {
            var sqlUsers = @"
INSERT INTO utilisateurs (nom, prenom, email, mot_de_passe_hash, role, departement, promotion, etat) VALUES
 ('Admin','SEFAD','admin@sefad.local',$hash,'Administrateur','','','Militaire'),
 ('Harbi','Ali','chefdep@sefad.local',$hash,'ChefDepartement','Terre','','Militaire'),
 ('Nacer','Djamel','chefdep2@sefad.local',$hash,'ChefDepartement','Air','','Militaire'),
 ('Bouchamaoui','Sami','chefdep3@sefad.local',$hash,'ChefDepartement','Marine','','Militaire'),
 ('Mansouri','Yasmine','formatrice@sefad.local',$hash,'Formateur','Terre','Promotion 2026','Militaire'),
 ('Tlemcani','Omar','formateur2@sefad.local',$hash,'Formateur','Terre','Promotion 2025','Militaire'),
 ('Bensalem','Rania','formateur3@sefad.local',$hash,'Formateur','Air','Promotion 2026','Civil'),
 ('Ferjani','Walid','formateur4@sefad.local',$hash,'Formateur','Air','Promotion 2025','Civil'),
 ('Mejri','Sonia','formateur5@sefad.local',$hash,'Formateur','Terre','Promotion 2026','Militaire'),
 ('Hadj','Ahmed','resp.formation@sefad.local',$hash,'ResponsableFormation','','','Militaire'),
 ('Bouzid','Mourad','decideur@sefad.local',$hash,'Decideur','','','Militaire'),
 ('Ben Ali','Karim','stagiaire@sefad.local',$hash,'Stagiaire','Terre','Promotion 2026','Militaire'),
 ('Khelifi','Ahmed','stagiaire2@sefad.local',$hash,'Stagiaire','Terre','Promotion 2026','Civil'),
 ('Bouazizi','Fatma','stagiaire3@sefad.local',$hash,'Stagiaire','Terre','Promotion 2026','Militaire'),
 ('Mansour','Sami','stagiaire4@sefad.local',$hash,'Stagiaire','Terre','Promotion 2026','Civil'),
 ('Ben Salah','Imed','stagiaire5@sefad.local',$hash,'Stagiaire','Terre','Promotion 2025','Militaire'),
 ('Chatti','Amira','stagiaire6@sefad.local',$hash,'Stagiaire','Terre','Promotion 2025','Civil'),
 ('Mebarki','Tarek','stagiaire7@sefad.local',$hash,'Stagiaire','Air','Promotion 2026','Militaire'),
 ('Cherif','Amina','stagiaire8@sefad.local',$hash,'Stagiaire','Air','Promotion 2026','Civil'),
 ('Drissi','Rachid','stagiaire9@sefad.local',$hash,'Stagiaire','Air','Promotion 2025','Militaire'),
 ('Zayeni','Houda','stagiaire10@sefad.local',$hash,'Stagiaire','Air','Promotion 2025','Civil');";
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
 (1,'2026-09-01','2026-12-15','Salle Telecoms A',20,'EnCours'),
 (1,'2027-01-10','2027-04-30','Salle Telecoms A',20,'Planifiee'),
 (2,'2026-09-01','2026-12-15','Salle Maintenance B',20,'EnCours'),
 (2,'2027-01-10','2027-04-30','Salle Maintenance B',20,'Planifiee'),
 (3,'2026-09-01','2026-12-15','Salle Info C',18,'Planifiee'),
 (3,'2027-01-10','2027-04-30','Salle Info C',18,'Planifiee');";
                await using var seedSessions = new SqliteCommand(sqlSessions, connection);
                await seedSessions.ExecuteNonQueryAsync();

                // ===== MODULES — COMMON (10 per formation) =====
                var sqlModCommuns = @"
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
                await using var seedMC = new SqliteCommand(sqlModCommuns, connection);
                await seedMC.ExecuteNonQueryAsync();

                // ===== MODULES FORMATION 1 — Telecoms (id 11-44) =====
                var sqlMod1 = @"
INSERT INTO modules (id_formation, titre, credit_horaire, nb_examen, coefficient, est_commum) VALUES
 (1,'Profession et Formation',15,1,2.0,0),(1,'Electricite Electromagnatisme',30,2,3.0,0),
 (1,'Systemes de Cablage',25,1,3.0,0),(1,'Reseaux NGN',35,2,4.0,0),
 (1,'Reseaux Operationnels Tunisiens',30,2,4.0,0),(1,'Microprocesseurs',30,2,3.0,0),
 (1,'Systemes AFV',25,1,3.0,0),(1,'Securite Informatique',20,1,2.0,0),
 (1,'Maintenance des Equipements',30,2,3.0,0),(1,'Deploiement Reseaux Cellulaire',35,2,4.0,0),
 (1,'Anglais Technique',15,1,1.0,0),(1,'Projet Pedagogique PPI',40,1,15.0,0),
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

                // ===== MODULES FORMATION 2 — Maintenance (id 55-76) =====
                var sqlMod2 = @"
INSERT INTO modules (id_formation, titre, credit_horaire, nb_examen, coefficient, est_commum) VALUES
 (2,'Metier et Formation',15,1,2.0,0),(2,'Electricite de base',25,1,2.0,0),
 (2,'Semi-conducteurs',20,1,2.0,0),(2,'Electronique Analogique',30,2,3.0,0),
 (2,'Electronique Numerique',30,2,3.0,0),(2,'Informatique generale',20,1,2.0,0),
 (2,'Unite centrale et peripheriques',30,2,3.0,0),(2,'Montage et configuration PC',30,2,3.0,0),
 (2,'Microprocesseurs',30,2,3.0,0),(2,'Microcontroleurs',20,1,2.0,0),
 (2,'Maintenance Hard des PC',35,2,4.0,0),(2,'Maintenance Soft des PC',35,2,4.0,0),
 (2,'Reseaux poste-a-poste',20,1,2.0,0),(2,'Depannage bureautique',25,1,3.0,0),
 (2,'Stage en milieu de travail',200,1,15.0,0),(2,'Soudure et Cablage',15,1,1.0,0),
 (2,'Alimentations a impulsions',20,1,2.0,0),(2,'Impression et Peripheriques',15,1,2.0,0),
 (2,'Onduleurs et Protection',15,1,2.0,0),(2,'Diagnostic Puce',20,1,2.0,0),
 (2,'Techniques de Mesure',15,1,1.0,0),(2,'Documentation Technique',10,1,1.0,0);";
                await using var seedM2 = new SqliteCommand(sqlMod2, connection);
                await seedM2.ExecuteNonQueryAsync();

                // ===== MODULES FORMATION 3 — Info (id 81-112) =====
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

                // ===== PARTICIPATIONS =====
                var sqlPart = @"
INSERT INTO participation (id_utilisateur, id_session, role_participation) VALUES
 (5,1,'Formateur'),(6,1,'Formateur'),
 (12,1,'Stagiaire'),(13,1,'Stagiaire'),(14,1,'Stagiaire'),(15,1,'Stagiaire'),(16,1,'Stagiaire'),
 (17,1,'Stagiaire'),(18,1,'Stagiaire'),(19,1,'Stagiaire'),(20,1,'Stagiaire'),(21,1,'Stagiaire'),
 (7,3,'Formateur'),(8,3,'Formateur'),
 (12,3,'Stagiaire'),(13,3,'Stagiaire'),(14,3,'Stagiaire'),(15,3,'Stagiaire'),(16,3,'Stagiaire'),
 (17,3,'Stagiaire'),(18,3,'Stagiaire'),(19,3,'Stagiaire'),(20,3,'Stagiaire'),(21,3,'Stagiaire');";
                await using var seedPart = new SqliteCommand(sqlPart, connection);
                await seedPart.ExecuteNonQueryAsync();
        }

        if (await IsEmpty("questionnaires"))
        {
                // ===== QUESTIONNAIRES (8) =====
                var sqlQ = @"
INSERT INTO questionnaires (id_session, titre, description, type_evaluation, note_maximale, duree_minutes, statut) VALUES
 (1,'Examen Partiel Telecoms - Octobre 2026','Evaluation mi-parcours sur les modules fondamentaux.','Partielle',20.0,120,'Publie'),
 (1,'Examen Final Telecoms - Decembre 2026','Evaluation finale couvrant l''ensemble du programme.','Finale',20.0,180,'Brouillon'),
 (3,'Examen Partiel Maintenance - Octobre 2026','Evaluation mi-parcours maintenance.','Partielle',20.0,120,'Publie'),
 (3,'Examen Final Maintenance - Decembre 2026','Evaluation finale maintenance.','Finale',20.0,180,'Brouillon'),
 (1,'Quiz Telecoms - Reseaux NGN','Quiz rapide sur les reseaux de nouvelle generation.','Quiz',10.0,30,'Publie'),
 (1,'Evaluation a chaud - Telecoms','Satisfaction et comprehension immediate.','AChaud',20.0,60,'Publie'),
 (3,'Evaluation a chaud - Maintenance','Satisfaction et comprehension immediate.','AChaud',20.0,60,'Publie'),
 (5,'Evaluation a chaud - Informatique','Satisfaction et comprehension immediate.','AChaud',20.0,60,'Publie');";
                await using var seedQ = new SqliteCommand(sqlQ, connection);
                await seedQ.ExecuteNonQueryAsync();

                // ===== CRITERES =====
                var sqlCrit = @"
INSERT INTO criteres (id_questionnaire, libelle, description, coefficient) VALUES
 (1,'QCM','Questions a choix multiple',2.0),(1,'Exercices pratiques','Mise en situation equipements',3.0),(1,'Dissertation','Developpement ecrit',1.0),
 (3,'QCM Maintenance','Questions theoriques',2.0),(3,'Diagnostic pratique','Diagnostic sur PC reel',3.0),(3,'Rapport technique','Compte-rendu intervention',1.0),
 (5,'Quiz Reseaux','Questions rapides NGN',1.0);";
                await using var seedCrit = new SqliteCommand(sqlCrit, connection);
                await seedCrit.ExecuteNonQueryAsync();

                // ===== QUESTIONS =====
                var sqlQuest = @"
INSERT INTO questions (id_questionnaire, id_critere, enonce, type_question, bareme, ordre) VALUES
 (1,1,'Quel protocole est utilise pour l''adressage IP v4?','QCM',2.0,1),
 (1,1,'Combien de couches contient le modele OSI?','QCM',2.0,2),
 (1,1,'Quel est le role principal d''un switch?','QCM',2.0,3),
 (1,1,'Bande passante maximale Ethernet Gigabit?','QCM',2.0,4),
 (1,2,'Configurer un reseau local avec 3 sous-reseaux.','Exercice',6.0,5),
 (1,2,'Diagnostiquer un probleme de connectivite reseau.','Exercice',6.0,6),
 (1,3,'Avantages et inconvenients des topologies maillage vs etoile.','Dissertation',4.0,7),
 (3,4,'Quel composant gere l''allocation des ressources CPU?','QCM',2.0,1),
 (3,4,'Que signifie le sigle BIOS?','QCM',2.0,2),
 (3,4,'Quel outil permet de tester la memoire RAM?','QCM',2.0,3),
 (3,5,'Diagnostiquer un PC qui ne demarre pas.','Exercice',8.0,4),
 (3,5,'Remplacer un disque dur et reinstallere le systeme.','Exercice',6.0,5),
 (3,6,'Rapport de maintenance pour remplacement de carte mere.','Rapport',4.0,6),
 (5,8,'Qu''est-ce qu''un reseau NGN?','QCM',2.0,1),
 (5,8,'Quel protocole VoIP est le plus utilise?','QCM',2.0,2),
 (5,8,'Difference circuit switching et packet switching?','QCM',2.0,3),
 (5,8,'Citez 3 services convergents d''un NGN.','QCM',2.0,4),
 (5,8,'Role du SIP dans la telephonie IP?','QCM',2.0,5);";
                await using var seedQuest = new SqliteCommand(sqlQuest, connection);
                await seedQuest.ExecuteNonQueryAsync();

                // ===== NOTES / GRADES (50 — 5 per student) =====
                var sqlNotes = @"
INSERT INTO notes (id_stagiaire, id_module, id_session, note, saisi_par) VALUES
 (12,11,1,14.5,5),(12,12,1,16.0,5),(12,13,1,12.0,5),(12,14,1,15.5,5),(12,15,1,13.0,5),
 (13,11,1,12.0,5),(13,12,1,14.5,5),(13,13,1,10.5,5),(13,14,1,13.0,5),(13,15,1,11.0,5),
 (14,11,1,16.0,5),(14,12,1,18.0,5),(14,13,1,14.0,5),(14,14,1,17.5,5),(14,15,1,15.0,5),
 (15,11,1,10.0,5),(15,12,1,12.5,5),(15,13,1,8.5,5),(15,14,1,11.0,5),(15,15,1,9.5,5),
 (16,11,1,15.5,5),(16,12,1,17.0,5),(16,13,1,13.0,5),(16,14,1,16.0,5),(16,15,1,14.0,5),
 (17,11,1,13.0,5),(17,12,1,15.5,5),(17,13,1,11.5,5),(17,14,1,14.0,5),(17,15,1,12.0,5),
 (18,11,1,17.0,5),(18,12,1,18.5,5),(18,13,1,15.0,5),(18,14,1,17.0,5),(18,15,1,16.0,5),
 (19,11,1,11.5,5),(19,12,1,13.0,5),(19,13,1,9.5,5),(19,14,1,12.0,5),(19,15,1,10.5,5),
 (20,11,1,14.0,5),(20,12,1,16.0,5),(20,13,1,12.5,5),(20,14,1,15.0,5),(20,15,1,13.5,5),
 (21,11,1,16.5,5),(21,12,1,18.0,5),(21,13,1,14.5,5),(21,14,1,17.0,5),(21,15,1,15.5,5);";
                await using var seedNotes = new SqliteCommand(sqlNotes, connection);
                await seedNotes.ExecuteNonQueryAsync();

                // ===== EVALUATIONS (10) =====
                var sqlEval = @"
INSERT INTO evaluations (id_utilisateur, id_questionnaire, date_passage, score_total, pourcentage, score_maximum, statut) VALUES
 (12,1,'2026-10-15 09:00:00',14.5,72.5,20.0,'Terminee'),
 (13,1,'2026-10-15 09:00:00',12.0,60.0,20.0,'Terminee'),
 (14,1,'2026-10-15 09:00:00',16.0,80.0,20.0,'Terminee'),
 (15,1,'2026-10-15 09:00:00',11.0,55.0,20.0,'Terminee'),
 (16,1,'2026-10-15 09:00:00',15.5,77.5,20.0,'Terminee'),
 (17,1,'2026-10-15 09:00:00',13.5,67.5,20.0,'Terminee'),
 (18,1,'2026-10-15 09:00:00',17.0,85.0,20.0,'Terminee'),
 (19,1,'2026-10-15 09:00:00',10.5,52.5,20.0,'Terminee'),
 (20,1,'2026-10-15 09:00:00',14.0,70.0,20.0,'Terminee'),
 (21,1,'2026-10-15 09:00:00',16.5,82.5,20.0,'Terminee');";
                await using var seedEval = new SqliteCommand(sqlEval, connection);
                await seedEval.ExecuteNonQueryAsync();

                // ===== REPONSES =====
                var sqlRep = @"
INSERT INTO reponses (id_evaluation, id_question, contenu, est_correcte, score_obtenu) VALUES
 (1,1,'IPv4',1,2.0),(1,2,'7 couches',1,2.0),(1,3,'Filtrage et routage',0,0.0),(1,4,'1 Gbps',1,2.0),
 (1,5,'Config reussie',1,6.0),(1,6,'Diagnostic termine',0,1.5),(1,7,'Bon developpement',1,1.0),
 (2,1,'IPv4',1,2.0),(2,2,'7 couches',1,2.0),(2,3,'Connexion',0,0.0),(2,4,'100 Mbps',0,0.0),
 (2,5,'Exercice partiel',1,4.0),(2,6,'Diagnostic partiel',0,2.0),(2,7,'Incomplet',0,2.0),
 (3,1,'IPv4',1,2.0),(3,2,'7 couches',1,2.0),(3,3,'Filtrage et routage',1,2.0),(3,4,'1 Gbps',1,2.0),
 (3,5,'Config excellente',1,6.0),(3,6,'Diagnostic complet',1,1.0),(3,7,'Bon developpement',0,1.0);";
                await using var seedRep = new SqliteCommand(sqlRep, connection);
                await seedRep.ExecuteNonQueryAsync();
        }

        // ===== EMPLOIS DU TEMPS (4) =====
        await using (var countEmplois = new SqliteCommand("SELECT COUNT(*) FROM emplois_du_temps;", connection))
        {
            if ((long)(await countEmplois.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlEmplois = @"
INSERT INTO emplois_du_temps (id_formation, type_emploi, annee, promotion, chemin_image, uploaded_by, statut, description) VALUES
 (1,'Hebdomadaire','2026-2027','Promotion 2026','avares://formatrack/Assets/emplois_du_temps/timetable.jpg',1,'Publie','Emploi du temps hebdomadaire - Telecoms Promo 2026'),
 (1,'Annuel','2026-2027','Promotion 2026','avares://formatrack/Assets/emplois_du_temps/timetable-mensuel.jpg',1,'Publie','Chronogramme annuel - Telecoms Promo 2026'),
 (2,'Hebdomadaire','2026-2027','Promotion 2026','avares://formatrack/Assets/emplois_du_temps/timetable.jpg',1,'Publie','Emploi du temps hebdomadaire - Maintenance Promo 2026'),
 (2,'Annuel','2026-2027','Promotion 2025','avares://formatrack/Assets/emplois_du_temps/timetable-mensuel.jpg',1,'Publie','Chronogramme annuel - Maintenance Promo 2025');";
                await using var seedEmplois = new SqliteCommand(sqlEmplois, connection);
                await seedEmplois.ExecuteNonQueryAsync();
            }
        }

        // ===== ABSENCES & RETARDS (15) =====
        await using (var countAbs = new SqliteCommand("SELECT COUNT(*) FROM absences_retards;", connection))
        {
            if ((long)(await countAbs.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlAbs = @"
INSERT INTO absences_retards (utilisateur_id, session_id, cours, date, type, duree, justifiee, motif) VALUES
 (12,1,'Electricite Electromagnatisme','2026-09-15','Absence','1 jour',1,'Certificat medical'),
 (12,1,'Reseaux NGN','2026-10-01','Retard','30 min',1,'Retard transport'),
 (13,1,'Microprocesseurs','2026-09-22','Absence','1 jour',0,''),
 (13,1,'Systemes de Cablage','2026-10-05','Retard','20 min',1,'Embouteillage'),
 (14,1,'Profession et Formation','2026-09-18','Absence','1 jour',1,'Permission accordee'),
 (15,1,'Reseaux Operationnels','2026-09-25','Absence','1 jour',0,''),
 (15,1,'Securite Informatique','2026-10-08','Retard','15 min',1,'Retard mineur'),
 (16,1,'Deploiement Reseaux','2026-09-20','Absence','1 jour',1,'Certificat medical'),
 (17,1,'Programmation Reseaux','2026-10-03','Retard','25 min',1,'Convoyage'),
 (18,1,'Protocoles IP','2026-09-28','Absence','1 jour',0,''),
 (19,1,'Switching et Routing','2026-10-10','Retard','10 min',1,'Retard mineur'),
 (20,1,'Cloud Computing','2026-10-02','Absence','1 jour',1,'Permission'),
 (21,1,'IoT et Objets Connectes','2026-09-30','Absence','1 jour',0,''),
 (12,1,'Arabe','2026-09-10','Retard','15 min',1,'Transport'),
 (14,1,'Anglais','2026-10-12','Absence','1 jour',1,'Raison familiale');";
                await using var seedAbs = new SqliteCommand(sqlAbs, connection);
                await seedAbs.ExecuteNonQueryAsync();
            }
        }

        // ===== NOTIFICATIONS (24) =====
        await using (var countNotif = new SqliteCommand("SELECT COUNT(*) FROM notifications;", connection))
        {
            if ((long)(await countNotif.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlNotif = @"
INSERT INTO notifications (id_utilisateur, message, lue, date_creation) VALUES
 (12,'Note publiee : 14.5/20 en Electricite Electromagnatisme.',1,'2026-10-15 16:30:00'),
 (12,'Note publiee : 16.0/20 en Reseaux NGN.',1,'2026-10-15 16:35:00'),
 (12,'Evaluation Partielle Telecoms terminee : 72.5%.',0,'2026-10-15 17:00:00'),
 (13,'Alerte : Absence non justifiee le 22/09/2026 - Microprocesseurs.',0,'2026-09-22 10:00:00'),
 (13,'Note publiee : 12.0/20 en Electricite Electromagnatisme.',0,'2026-10-15 16:30:00'),
 (14,'Note publiee : 16.0/20 en Electricite Electromagnatisme - Excellent!',0,'2026-10-15 16:30:00'),
 (14,'Quiz NGN : 9/10 - Bravo!',0,'2026-09-20 11:00:00'),
 (15,'Alerte : Absence non justifiee le 25/09/2026 - Reseaux Op.',0,'2026-09-25 10:00:00'),
 (15,'Note publiee : 10.0/20 en Electricite Electromagnatisme.',0,'2026-10-15 16:30:00'),
 (16,'Note publiee : 15.5/20 en Electricite Electromagnatisme.',0,'2026-10-15 16:30:00'),
 (17,'Note publiee : 13.5/20 en Electricite Electromagnatisme.',0,'2026-10-15 16:30:00'),
 (18,'Note publiee : 17.0/20 en Electricite Electromagnatisme - Tres bien!',0,'2026-10-15 16:30:00'),
 (18,'Alerte : Absence non justifiee le 28/09/2026 - Protocoles IP.',0,'2026-09-28 10:00:00'),
 (19,'Note publiee : 10.5/20 en Electricite Electromagnatisme.',0,'2026-10-15 16:30:00'),
 (20,'Note publiee : 14.0/20 en Electricite Electromagnatisme.',0,'2026-10-15 16:30:00'),
 (21,'Note publiee : 16.5/20 en Electricite Electromagnatisme.',0,'2026-10-15 16:30:00'),
 (21,'Alerte : Absence non justifiee le 30/09/2026 - IoT.',0,'2026-09-30 10:00:00'),
 (5,'Nouvelles inscriptions : 10 stagiaires dans Telecoms Session 1.',1,'2026-08-15 09:00:00'),
 (5,'Notes saisies : 40 notes pour Telecoms Session 1.',1,'2026-10-15 16:00:00'),
 (2,'Rapport mensuel : 5 absences non justifiees dans le departement Terre.',0,'2026-10-01 08:00:00'),
 (2,'Evaluation Partielle Telecoms terminee - 10 stagiaires.',0,'2026-10-15 18:00:00'),
 (7,'Notes Maintenance saisies : 8 stagiaires.',0,'2026-10-16 17:00:00'),
 (10,'Tableau de bord mis a jour : Taux de reussite global a 71.2%.',0,'2026-10-16 18:00:00'),
 (11,'Alerte : 3 stagiaires en dessous de 10/20 de moyenne generale.',0,'2026-10-16 18:30:00');";
                await using var seedNotif = new SqliteCommand(sqlNotif, connection);
                await seedNotif.ExecuteNonQueryAsync();
            }
        }

        // ===== JOURNAL D'ACTIVITE (24) =====
        await using (var countJournal = new SqliteCommand("SELECT COUNT(*) FROM journal_activite;", connection))
        {
            if ((long)(await countJournal.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlJournal = @"
INSERT INTO journal_activite (id_utilisateur, action, details, date_action) VALUES
 (1,'Connexion administrateur','Connexion reussie depuis le poste Admin-01','2026-08-15 08:00:00'),
 (1,'Configuration systeme','Creation des comptes utilisateurs et attribution des roles','2026-06-01 08:00:00'),
 (1,'Import emplois du temps','Import timetable.jpg et timetable-mensuel.jpg','2026-08-15 08:30:00'),
 (5,'Connexion formateur','Connexion depuis poste Form-01','2026-09-01 08:15:00'),
 (5,'Publication evaluation','Publication Examen Partiel Telecoms Octobre 2026','2026-10-10 09:00:00'),
 (5,'Saisie de notes','Saisie bulk: 50 notes pour Telecoms Session 1','2026-10-15 16:00:00'),
 (7,'Connexion formateur','Connexion depuis poste Form-03','2026-09-01 08:25:00'),
 (12,'Connexion stagiaire','Connexion depuis poste ST-01','2026-09-01 08:30:00'),
 (12,'Passage evaluation','Passage Examen Partiel Telecoms - 14.5/20','2026-10-15 09:00:00'),
 (12,'Justification absence','Justification soumise pour absence du 15/09/2026','2026-09-15 14:00:00'),
 (14,'Connexion stagiaire','Connexion depuis poste ST-03','2026-09-01 08:40:00'),
 (14,'Passage evaluation','Passage Examen Partiel Telecoms - 16.0/20','2026-10-15 09:00:00'),
 (18,'Connexion stagiaire','Connexion depuis poste ST-07','2026-09-01 08:45:00'),
 (2,'Analyse absence','Analyse absences departement Terre - 3 non justifiees','2026-10-01 08:00:00'),
 (10,'Rapport generation','Generation rapport officiel Q3 2026','2026-10-01 09:00:00'),
 (1,'Mise a jour systeme','Ajout des promotions 2025 et 2026','2026-06-15 08:00:00');";
                await using var seedJournal = new SqliteCommand(sqlJournal, connection);
                await seedJournal.ExecuteNonQueryAsync();
            }
        }

        // ===== SUGGESTIONS AIDE (5) =====
        await using (var countSugg = new SqliteCommand("SELECT COUNT(*) FROM suggestions_aide;", connection))
        {
            if ((long)(await countSugg.ExecuteScalarAsync() ?? 0L) == 0)
            {
                var sqlSugg = @"
INSERT INTO suggestions_aide (titre, description, priorite, categorie, action_page, action_params, est_lu) VALUES
 ('Stagiaire en difficulte','Khelifi Ahmed a une moyenne inferieure a 10/20. Soutien pedagogique recommande.',2,'Alerte','Grades','filter=low',0),
 ('Absences non justifiees elevees','5 absences non justifiees dans le departement Terre ce mois.',3,'Absences','Absences','dept=Terre',0),
 ('Taux de reussite en baisse','Promotion 2026 a 71.2% de reussite, en baisse.',2,'Statistiques','Statistiques','promo=2026',0),
 ('Evaluation en attente','Examen final Telecoms Dec 2026 sans questions definies.',1,'Urgence','Questionnaires','id=2',0),
 ('Module a renforcer','Electricite Electromagnatisme: 62% reussite - hours de renforcement.',2,'Pedagogie','Grades','module=12',0);";
                await using var seedSugg = new SqliteCommand(sqlSugg, connection);
                await seedSugg.ExecuteNonQueryAsync();
            }
        }
    }
}
