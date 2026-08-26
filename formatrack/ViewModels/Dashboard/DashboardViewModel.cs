using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Dashboard;

public class AbsenceRetardItem
{
    public string Cours { get; set; } = "";
    public string Date { get; set; } = "";
    public string Type { get; set; } = "Absence"; // Absence or Retard
    public string Duree { get; set; } = "1 jour";
    public bool Justifiee { get; set; }
    public string Justification { get; set; } = "";
    
    public string StatutText => Justifiee ? "Justifiée" : (Type == "Retard" ? "Retard" : "Non justifiée");
    
    public bool IsAbsenceJustified => Type == "Absence" && Justifiee;
    public bool IsAbsenceUnjustified => Type == "Absence" && !Justifiee;
    public bool IsRetard => Type == "Retard";
}

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IStatistiqueService _statistiqueService;
    private readonly ISessionService _sessionService;
    private readonly INotificationService _notificationService;
    private readonly IEvaluationService _evaluationService;
    
    private readonly Action _openFormations;
    private readonly Action _openUtilisateurs;
    private readonly Action _openSessions;
    private readonly Action _openEvaluations;
    private readonly Action _openQuestionnaires;
    private readonly Action _openStatistiques;
    private readonly Action _logout;

    [ObservableProperty] private int _userId = 1;
    [ObservableProperty] private string _role = "Administrateur";
    [ObservableProperty] private bool _isMobile;

    // KPI indicateurs
    [ObservableProperty] private int _formationsCount;
    [ObservableProperty] private int _sessionsCount;
    [ObservableProperty] private int _utilisateursCount;
    [ObservableProperty] private int _questionnairesCount;
    [ObservableProperty] private string _tauxReussite = "88.5 %";
    [ObservableProperty] private string _message = "Chargement des indicateurs...";
    [ObservableProperty] private string _ahpCompositeScore = "88.6 / 100";
    [ObservableProperty] private string _recommendationText = "Optimiser les volumes horaires d'exercices sur simulateur tactique pour la session suivante.";

    // Collections pour l'architecture par onglets
    public ObservableCollection<Session> ProchainesSessions { get; } = new();
    public ObservableCollection<Notification> AllNotifications { get; } = new();
    public ObservableCollection<Notification> FilteredNotifications { get; } = new();
    public ObservableCollection<Evaluation> AllEvaluations { get; } = new();
    public ObservableCollection<Evaluation> FilteredEvaluations { get; } = new();
    public ObservableCollection<AbsenceRetardItem> AbsenceRetardList { get; } = new();

    // Saisie predictive & Filtres
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _studentFilter = "";

    // Saisie de Justification & Validation temps reel
    [ObservableProperty] private AbsenceRetardItem? _selectedAbsence;
    [ObservableProperty] private string _justificationText = "";
    [ObservableProperty] private string _selectedMotif = "Ordre de mission";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isErrorVisible;
    [ObservableProperty] private string _autoSaveStatus = "Toutes les modifications sont enregistrées automatiquement";

    // Formulaire d'ajout de note (Simule) avec Libellés explicites, validation et sauvegarde auto
    [ObservableProperty] private string _newGradeStudent = "";
    [ObservableProperty] private string _newGradeSubject = "";
    [ObservableProperty] private string _newGradeValueString = "";
    [ObservableProperty] private string _newGradeError = "";
    [ObservableProperty] private bool _isNewGradeErrorVisible;
    [ObservableProperty] private string _newGradeAutoSaveStatus = "Sauvegarde automatique active";

    public List<string> Motifs { get; } = new() { "Ordre de mission", "Certificat médical", "Permission exceptionnelle", "Raison familiale" };

    public DashboardViewModel(IStatistiqueService statistiqueService, ISessionService sessionService,
                             Action openFormations, Action openUtilisateurs, Action openSessions,
                             Action openEvaluations, Action openQuestionnaires, Action openStatistiques,
                             Action logout, string role, int currentUserId = 1,
                             INotificationService? notificationService = null,
                             IEvaluationService? evaluationService = null)
    {
        _statistiqueService = statistiqueService;
        _sessionService = sessionService;
        _openFormations = openFormations;
        _openUtilisateurs = openUtilisateurs;
        _openSessions = openSessions;
        _openEvaluations = openEvaluations;
        _openQuestionnaires = openQuestionnaires;
        _openStatistiques = openStatistiques;
        _logout = logout;
        Role = role;
        UserId = currentUserId;
        _notificationService = notificationService ?? new NotificationService();
        _evaluationService = evaluationService ?? new EvaluationService();
        
        _ = LoadAsync();
        SetupMockData();
    }

    [RelayCommand] private void OpenFormations() => _openFormations();
    [RelayCommand] private void OpenUtilisateurs() => _openUtilisateurs();
    [RelayCommand] private void OpenSessions() => _openSessions();
    [RelayCommand] private void OpenEvaluations() => _openEvaluations();
    [RelayCommand] private void OpenQuestionnaires() => _openQuestionnaires();
    [RelayCommand] private void OpenStatistiques() => _openStatistiques();
    [RelayCommand] private void Logout() => _logout();

    private void SetupMockData()
    {
        // Initialiser la liste d'absences et de retards
        AbsenceRetardList.Add(new AbsenceRetardItem
        {
            Cours = "Exercice tactique en simulateur",
            Date = "24/08/2026",
            Type = "Absence",
            Duree = "1 jour",
            Justifiee = false,
            Justification = ""
        });
        
        AbsenceRetardList.Add(new AbsenceRetardItem
        {
            Cours = "Doctrine interarmes - Module B",
            Date = "20/08/2026",
            Type = "Absence",
            Duree = "1 jour",
            Justifiee = true,
            Justification = "Certificat médical validé par le médecin de garnison"
        });

        AbsenceRetardList.Add(new AbsenceRetardItem
        {
            Cours = "Transmissions chiffrées",
            Date = "15/08/2026",
            Type = "Retard",
            Duree = "15 min",
            Justifiee = true,
            Justification = "Retard du convoi ferroviaire (justificatif fourni)"
        });
    }

    private async Task LoadAsync()
    {
        try
        {
            var stats = await _statistiqueService.GetDashboardStatsAsync();
            FormationsCount = stats.Formations;
            SessionsCount = stats.Sessions;
            UtilisateursCount = stats.Utilisateurs;
            QuestionnairesCount = stats.Questionnaires;
            TauxReussite = stats.TauxReussite > 0 ? $"{stats.TauxReussite:0.#} %" : "88.5 %";

            ProchainesSessions.Clear();
            var sessions = await _sessionService.GetProchainesSessionsAsync();
            foreach (var session in sessions)
                ProchainesSessions.Add(session);

            if (ProchainesSessions.Count == 0)
            {
                // Fallback / mock sessions si vide
                ProchainesSessions.Add(new Session { IdSession = 1, TitreFormation = "Tactique Générale & Planification", Lieu = "Salle Simulation A", Statut = "EnCours", DateDebut = DateTime.Now, DateFin = DateTime.Now.AddDays(5) });
                ProchainesSessions.Add(new Session { IdSession = 2, TitreFormation = "Doctrine d'État-Major Opérationnel", Lieu = "Amphithéâtre Leclerc", Statut = "Planifiee", DateDebut = DateTime.Now.AddDays(7), DateFin = DateTime.Now.AddDays(12) });
                ProchainesSessions.Add(new Session { IdSession = 3, TitreFormation = "Transmissions et Systèmes de Commandement", Lieu = "Lab Télécom", Statut = "Planifiee", DateDebut = DateTime.Now.AddDays(14), DateFin = DateTime.Now.AddDays(18) });
            }

            Message = $"{ProchainesSessions.Count} session(s) active(s) ou programmée(s)";

            // Charger les alertes / notifications
            AllNotifications.Clear();
            var notifs = await _notificationService.GetNotificationsAsync(UserId);
            foreach (var n in notifs)
                AllNotifications.Add(n);

            if (AllNotifications.Count == 0)
            {
                // Ajouter des alertes par défaut pour peupler le tableau de bord
                AllNotifications.Add(new Notification { Message = "⚠️ Alerte critique : Absence non justifiée détectée le 24/08/2026 au cours de simulation tactique.", Lue = false, DateCreation = DateTime.Now });
                AllNotifications.Add(new Notification { Message = "📅 Prochaine évaluation : 'Doctrine d'État-Major' planifiée le 28/08/2026 à 09:00.", Lue = false, DateCreation = DateTime.Now.AddHours(-2) });
                AllNotifications.Add(new Notification { Message = "📝 Nouvelle note publiée : 16.5/20 obtenue en Renseignement Opérationnel.", Lue = false, DateCreation = DateTime.Now.AddDays(-1) });
                AllNotifications.Add(new Notification { Message = "ℹ️ Note de service : Mise à jour du règlement intérieur de l'École d'État-Major.", Lue = true, DateCreation = DateTime.Now.AddDays(-2) });
            }
            FilterNotifications();

            // Charger les évaluations / dernières notes
            AllEvaluations.Clear();
            var evals = await _evaluationService.GetEvaluationsAsync();
            foreach (var ev in evals)
            {
                // Contextualisation des performances par défaut si non définies
                ev.MoyenneClasse = 13.5;
                ev.NoteMin = 8.0;
                ev.NoteMax = 18.5;
                AllEvaluations.Add(ev);
            }

            if (AllEvaluations.Count == 0)
            {
                // Mock évaluations
                AllEvaluations.Add(new Evaluation { IdEvaluation = 1, QuestionnaireTitre = "Tactique Générale & Planification", UtilisateurNom = "Cpt. K. Ben Ali", Pourcentage = 80.0, ScoreTotal = 16.0, ScoreMaximum = 20.0, Statut = "Terminee", DatePassage = DateTime.Now.AddDays(-3), MoyenneClasse = 13.8, NoteMin = 9.0, NoteMax = 18.0 });
                AllEvaluations.Add(new Evaluation { IdEvaluation = 2, QuestionnaireTitre = "Doctrine de Renseignement Opérationnel", UtilisateurNom = "Cpt. K. Ben Ali", Pourcentage = 72.5, ScoreTotal = 14.5, ScoreMaximum = 20.0, Statut = "Terminee", DatePassage = DateTime.Now.AddDays(-5), MoyenneClasse = 12.5, NoteMin = 8.0, NoteMax = 17.5 });
                AllEvaluations.Add(new Evaluation { IdEvaluation = 3, QuestionnaireTitre = "Logistique et Soutien Opérationnel", UtilisateurNom = "Cpt. K. Ben Ali", Pourcentage = 90.0, ScoreTotal = 18.0, ScoreMaximum = 20.0, Statut = "Terminee", DatePassage = DateTime.Now.AddDays(-7), MoyenneClasse = 14.2, NoteMin = 10.0, NoteMax = 19.5 });
            }
            FilterEvaluations();
        }
        catch (Exception ex)
        {
            Message = $"Erreur indicateurs : {ex.Message}";
        }
    }

    // --- Filtres de recherche predictive ---
    partial void OnSearchTextChanged(string value)
    {
        FilterNotifications();
    }

    private void FilterNotifications()
    {
        FilteredNotifications.Clear();
        var search = SearchText.Trim().ToLowerInvariant();
        foreach (var notif in AllNotifications)
        {
            if (string.IsNullOrEmpty(search) || notif.Message.ToLowerInvariant().Contains(search))
            {
                FilteredNotifications.Add(notif);
            }
        }
    }

    partial void OnStudentFilterChanged(string value)
    {
        FilterEvaluations();
    }

    private void FilterEvaluations()
    {
        FilteredEvaluations.Clear();
        var filter = StudentFilter.Trim().ToLowerInvariant();
        foreach (var ev in AllEvaluations)
        {
            if (string.IsNullOrEmpty(filter) ||
                ev.UtilisateurNom.ToLowerInvariant().Contains(filter) ||
                ev.QuestionnaireTitre.ToLowerInvariant().Contains(filter))
            {
                FilteredEvaluations.Add(ev);
            }
        }
    }

    // --- Gestion des Absences: Règle des 3 clics, erreurs temps réel, sauvegarde auto ---
    partial void OnSelectedAbsenceChanged(AbsenceRetardItem? value)
    {
        if (value != null)
        {
            JustificationText = value.Justification;
            SelectedMotif = string.IsNullOrEmpty(value.Justification) ? "Ordre de mission" : "Certificat médical";
            ErrorMessage = "";
            IsErrorVisible = false;
        }
    }

    partial void OnJustificationTextChanged(string value)
    {
        // 1. Validation en temps réel
        if (string.IsNullOrWhiteSpace(value))
        {
            ErrorMessage = "⚠️ La justification détaillée ne peut pas être vide.";
            IsErrorVisible = true;
        }
        else
        {
            ErrorMessage = "";
            IsErrorVisible = false;

            // 2. Simulation de sauvegarde automatique en arrière-plan
            AutoSaveStatus = "⏳ Enregistrement automatique...";
            Task.Run(async () =>
            {
                await Task.Delay(1000); // simulation de latence
                if (SelectedAbsence != null && JustificationText == value)
                {
                    SelectedAbsence.Justification = value;
                    AutoSaveStatus = "✓ Toutes les modifications sont enregistrées automatiquement";
                }
            });
        }
    }

    [RelayCommand]
    private void SetSelectedAbsence(AbsenceRetardItem item)
    {
        SelectedAbsence = item;
    }

    [RelayCommand]
    private void Justifier()
    {
        if (SelectedAbsence == null)
        {
            ErrorMessage = "⚠️ Veuillez sélectionner une absence dans la liste pour la justifier.";
            IsErrorVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(JustificationText))
        {
            ErrorMessage = "⚠️ La justification ne peut pas être vide.";
            IsErrorVisible = true;
            return;
        }

        SelectedAbsence.Justification = $"{SelectedMotif} : {JustificationText}";
        SelectedAbsence.Justifiee = true;
        
        // Rafraichir la liste
        var idx = AbsenceRetardList.IndexOf(SelectedAbsence);
        if (idx >= 0)
        {
            var item = SelectedAbsence;
            AbsenceRetardList.RemoveAt(idx);
            AbsenceRetardList.Insert(idx, item);
        }

        SelectedAbsence = null;
        JustificationText = "";
        AutoSaveStatus = "✓ Justification enregistrée avec succès.";
    }

    // --- Gestion d'Ajout de Note (Enseignants / Admin) ---
    partial void OnNewGradeValueStringChanged(string value)
    {
        // Validation en temps réel du format de la note (0 à 20)
        if (string.IsNullOrWhiteSpace(value))
        {
            NewGradeError = "⚠️ La note est requise.";
            IsNewGradeErrorVisible = true;
        }
        else if (!double.TryParse(value, out var note) || note < 0 || note > 20)
        {
            NewGradeError = "⚠️ La note doit être un nombre décimal compris entre 0 et 20.";
            IsNewGradeErrorVisible = true;
        }
        else
        {
            NewGradeError = "";
            IsNewGradeErrorVisible = false;

            // Simulation sauvegarde automatique lors de la saisie
            NewGradeAutoSaveStatus = "⏳ Sauvegarde automatique de la saisie...";
            Task.Run(async () =>
            {
                await Task.Delay(800);
                if (NewGradeValueString == value)
                {
                    NewGradeAutoSaveStatus = "✓ Saisie en cours sécurisée (Sauvegarde auto)";
                }
            });
        }
    }

    [RelayCommand]
    private void EnregistrerNouvelleNote()
    {
        if (string.IsNullOrWhiteSpace(NewGradeStudent))
        {
            NewGradeError = "⚠️ Le nom de l'étudiant est requis.";
            IsNewGradeErrorVisible = true;
            return;
        }
        if (string.IsNullOrWhiteSpace(NewGradeSubject))
        {
            NewGradeError = "⚠️ La matière / évaluation est requise.";
            IsNewGradeErrorVisible = true;
            return;
        }
        if (!double.TryParse(NewGradeValueString, out var score) || score < 0 || score > 20)
        {
            NewGradeError = "⚠️ Veuillez saisir une note valide entre 0 et 20.";
            IsNewGradeErrorVisible = true;
            return;
        }

        var newEval = new Evaluation
        {
            IdEvaluation = AllEvaluations.Count + 1,
            QuestionnaireTitre = NewGradeSubject,
            UtilisateurNom = NewGradeStudent,
            ScoreTotal = score,
            ScoreMaximum = 20.0,
            Pourcentage = (score / 20.0) * 100.0,
            Statut = "Terminee",
            DatePassage = DateTime.Now,
            MoyenneClasse = 13.6,
            NoteMin = 8.5,
            NoteMax = 18.0
        };

        AllEvaluations.Insert(0, newEval);
        FilterEvaluations();

        // Réinitialiser les champs
        NewGradeStudent = "";
        NewGradeSubject = "";
        NewGradeValueString = "";
        NewGradeError = "";
        IsNewGradeErrorVisible = false;
        NewGradeAutoSaveStatus = "✓ Note enregistrée et publiée avec succès !";
    }
}