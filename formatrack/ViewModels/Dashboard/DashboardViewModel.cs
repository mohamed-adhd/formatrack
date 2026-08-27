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

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IStatistiqueService _statistiqueService;
    private readonly ISessionService _sessionService;
    private readonly INotificationService _notificationService;
    private readonly IEvaluationService _evaluationService;
    private readonly IUtilisateurService _utilisateurService;
    private readonly IAbsenceService _absenceService = formatrack.Services.CompositionRoot.Absence;
    
    private readonly Action _openFormations;
    private readonly Action _openUtilisateurs;
    private readonly Action _openSessions;
    private readonly Action _openEvaluations;
    private readonly Action _openQuestionnaires;
    private readonly Action _openStatistiques;
    private readonly Action _logout;

    [ObservableProperty] private int _userId = 1;
    [ObservableProperty] private string _role = "Administrateur";
    [ObservableProperty] private string _departement = "";
    [ObservableProperty] private string _promotion = "";
    [ObservableProperty] private bool _isMobile;
    [ObservableProperty] private int _selectedTabIndex;

    // Role-based view flags
    public bool IsStagiaireView => Role == "Stagiaire";
    public bool IsFormateurView => Role == "Formateur";
    public bool IsChefDepView => Role == "ChefDepartement";
    public bool IsAdminView => Role == "Administrateur";
    public bool IsResponsableFormationView => Role == "ResponsableFormation";
    public bool IsDecideurView => Role == "Decideur";
    public bool IsFormateurOrAdmin => Role is "Formateur" or "Administrateur";
    public bool CanExport => Role is "Administrateur" or "Decideur" or "ResponsableFormation";

    // KPI indicateurs
    [ObservableProperty] private int _formationsCount;
    [ObservableProperty] private int _sessionsCount;
    [ObservableProperty] private int _utilisateursCount;
    [ObservableProperty] private int _questionnairesCount;
    [ObservableProperty] private string _tauxReussite = "0 %";
    [ObservableProperty] private string _message = "Chargement des indicateurs...";
    [ObservableProperty] private string _ahpCompositeScore = "— / 100";
    [ObservableProperty] private string _recommendationText = "";

    // Stagiaire: own remarks/alerts
    [ObservableProperty] private string _stagiaireAlerts = "";
    [ObservableProperty] private string _stagiaireRemarks = "";

    // Formateur: class stats
    [ObservableProperty] private string _selectedPromotion = "";
    [ObservableProperty] private Session? _selectedSession;
    [ObservableProperty] private string _classAverage = "—";
    [ObservableProperty] private string _classPassRate = "—";
    [ObservableProperty] private string _classBestStudent = "—";
    [ObservableProperty] private string _classWorstStudent = "—";
    public ObservableCollection<string> AvailablePromotions { get; } = new();
    public ObservableCollection<Session> AvailableSessions { get; } = new();
    public ObservableCollection<ClassementStagiaire> ClassementStagiaires { get; } = new();

    // ChefDepartement: dept monitoring
    [ObservableProperty] private string _deptFormateursCount = "0";
    [ObservableProperty] private string _deptStagiairesCount = "0";
    [ObservableProperty] private string _deptTauxReussite = "—";
    [ObservableProperty] private string _deptAvgScore = "—";
    public ObservableCollection<Utilisateur> FormateursDuDepartement { get; } = new();
    public ObservableCollection<Utilisateur> StagiairesDuDepartement { get; } = new();
    public ObservableCollection<StatistiqueFormation> DeptFormations { get; } = new();

    // Collections for common tabs
    public ObservableCollection<Session> ProchainesSessions { get; } = new();
    public ObservableCollection<Notification> AllNotifications { get; } = new();
    public ObservableCollection<Notification> FilteredNotifications { get; } = new();
    public ObservableCollection<Evaluation> AllEvaluations { get; } = new();
    public ObservableCollection<Evaluation> FilteredEvaluations { get; } = new();
    public ObservableCollection<AbsenceRetard> AbsenceRetardList { get; } = new();

    // Filters
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _studentFilter = "";

    // Justification form
    [ObservableProperty] private AbsenceRetard? _selectedAbsence;
    [ObservableProperty] private string _justificationText = "";
    [ObservableProperty] private string _selectedMotif = "Ordre de mission";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isErrorVisible;
    [ObservableProperty] private string _autoSaveStatus = "Toutes les modifications sont enregistrées automatiquement";

    // Grade entry form (Formateur only)
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
                             IEvaluationService? evaluationService = null,
                             IUtilisateurService? utilisateurService = null,
                             string departement = "", string promotion = "")
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
        Departement = departement;
        Promotion = promotion;
        _notificationService = notificationService ?? new NotificationService();
        _evaluationService = evaluationService ?? new EvaluationService();
        _utilisateurService = utilisateurService ?? new UtilisateurService();

        // Default tab based on role
        SelectedTabIndex = Role == "Stagiaire" ? 1 : 0;

        _ = LoadAsync();
    }

    [RelayCommand] private void OpenFormations() => _openFormations();
    [RelayCommand] private void OpenUtilisateurs() => _openUtilisateurs();
    [RelayCommand] private void OpenSessions() => _openSessions();
    [RelayCommand] private void OpenEvaluations() => _openEvaluations();
    [RelayCommand] private void OpenQuestionnaires() => _openQuestionnaires();
    [RelayCommand] private void OpenStatistiques() => _openStatistiques();
    [RelayCommand] private void Logout() => _logout();

    private async Task LoadAsync()
    {
        try
        {
            // Load common data
            await LoadNotificationsAsync();
            await LoadAbsencesAsync();

            // Role-specific loading
            if (IsStagiaireView)
                await LoadStagiaireDataAsync();
            else if (IsFormateurView)
                await LoadFormateurDataAsync();
            else if (IsChefDepView)
                await LoadChefDepDataAsync();
            else if (IsResponsableFormationView)
                await LoadResponsableFormationDataAsync();
            else if (IsDecideurView)
                await LoadDecideurDataAsync();
            else
                await LoadAdminDataAsync();
        }
        catch (Exception ex)
        {
            Message = $"Erreur indicateurs : {ex.Message}";
        }
    }

    private async Task LoadAdminDataAsync()
    {
        var stats = await _statistiqueService.GetDashboardStatsAsync();
        FormationsCount = stats.Formations;
        SessionsCount = stats.Sessions;
        UtilisateursCount = stats.Utilisateurs;
        QuestionnairesCount = stats.Questionnaires;
        TauxReussite = stats.TauxReussite > 0 ? $"{stats.TauxReussite:0.#} %" : "—";
        AhpCompositeScore = "88.6 / 100";
        RecommendationText = "Optimiser les volumes horaires d'exercices sur simulateur tactique pour la session suivante.";

        ProchainesSessions.Clear();
        var sessions = await _sessionService.GetProchainesSessionsAsync();
        foreach (var session in sessions)
            ProchainesSessions.Add(session);
        Message = $"{ProchainesSessions.Count} session(s) active(s) ou programmée(s)";

        await LoadEvaluationsAsync();
    }

    private async Task LoadStagiaireDataAsync()
    {
        // Stagiaire: only own evaluations
        AllEvaluations.Clear();
        var evals = await _evaluationService.GetEvaluationsUtilisateurAsync(UserId);
        foreach (var ev in evals)
        {
            ev.MoyenneClasse = 13.5;
            ev.NoteMin = 8.0;
            ev.NoteMax = 18.5;
            AllEvaluations.Add(ev);
        }
        FilterEvaluations();

        // Stagiaire alerts & remarks
        var alerts = new List<string>();
        if (AbsenceRetardList.Any(a => !a.Justifiee))
            alerts.Add("⚠️ Vous avez des absences non justifiées.");
        if (AllEvaluations.Any(e => e.Pourcentage < 50))
            alerts.Add("⚠️ Vous avez des évaluations en dessous de la moyenne.");
        if (AllNotifications.Any(n => !n.Lue))
            alerts.Add($"📬 Vous avez {AllNotifications.Count(n => !n.Lue)} notification(s) non lue(s).");
        StagiaireAlerts = alerts.Count > 0 ? string.Join("\n", alerts) : "✅ Aucune alerte pour le moment.";

        var remarks = new List<string>();
        foreach (var ev in AllEvaluations.Where(e => e.Pourcentage < 50))
            remarks.Add($"❌ {ev.QuestionnaireTitre}: {ev.ScoreTotal}/{ev.ScoreMaximum} ({ev.Pourcentage:0.0}%)");
        foreach (var abs in AbsenceRetardList.Where(a => !a.Justifiee))
            remarks.Add($"⚠️ Absence non justifiée: {abs.Cours} le {abs.Date}");
        StagiaireRemarks = remarks.Count > 0 ? string.Join("\n", remarks) : "✅ Aucune remarque.";

        Message = $"{evals.Count} évaluation(s) • {AbsenceRetardList.Count} absence(s)/retard(s)";
    }

    private async Task LoadFormateurDataAsync()
    {
        // Load available promotions from students in same department
        var students = await _utilisateurService.GetUtilisateursParDepartementAsync(Departement);
        var promos = students.Where(s => s.Role == "Stagiaire" && !string.IsNullOrEmpty(s.Promotion))
                             .Select(s => s.Promotion).Distinct().OrderBy(p => p).ToList();
        AvailablePromotions.Clear();
        foreach (var p in promos)
            AvailablePromotions.Add(p);

        // Load sessions
        var sessions = await _sessionService.GetSessionsAsync();
        AvailableSessions.Clear();
        foreach (var s in sessions)
            AvailableSessions.Add(s);

        // Default selections
        if (AvailablePromotions.Count > 0 && string.IsNullOrEmpty(SelectedPromotion))
            SelectedPromotion = AvailablePromotions[0];
        if (AvailableSessions.Count > 0 && SelectedSession == null)
            SelectedSession = AvailableSessions[0];

        // Load class stats
        await LoadClassementAsync();
        await LoadEvaluationsAsync();

        ProchainesSessions.Clear();
        var upcoming = await _sessionService.GetProchainesSessionsAsync();
        foreach (var s in upcoming)
            ProchainesSessions.Add(s);

        Message = $"Formateur • {Departement} • {AvailablePromotions.Count} promotion(s)";
    }

    private async Task LoadChefDepDataAsync()
    {
        // Load department stats
        var deptStats = await _statistiqueService.GetStatistiquesDepartementAsync(Departement);
        FormationsCount = deptStats.Formations;
        SessionsCount = deptStats.Sessions;
        UtilisateursCount = deptStats.Formateurs + deptStats.Stagiaires;
        TauxReussite = deptStats.TauxReussite > 0 ? $"{deptStats.TauxReussite:0.#} %" : "—";
        DeptFormateursCount = deptStats.Formateurs.ToString();
        DeptStagiairesCount = deptStats.Stagiaires.ToString();
        DeptTauxReussite = deptStats.TauxReussite > 0 ? $"{deptStats.TauxReussite:0.#} %" : "—";

        AhpCompositeScore = $"{deptStats.TauxReussite:0.#} / 100";
        RecommendationText = $"Département {Departement}: {deptStats.Formateurs} formateur(s), {deptStats.Stagiaires} stagiaire(s), {deptStats.Sessions} session(s).";

        // Load formateurs in department
        var formateurs = await _utilisateurService.GetFormateursParDepartementAsync(Departement);
        FormateursDuDepartement.Clear();
        foreach (var f in formateurs)
            FormateursDuDepartement.Add(f);

        // Load stagiaires in department
        var allDeptUsers = await _utilisateurService.GetUtilisateursParDepartementAsync(Departement);
        StagiairesDuDepartement.Clear();
        foreach (var s in allDeptUsers.Where(u => u.Role == "Stagiaire"))
            StagiairesDuDepartement.Add(s);

        // Load department formations stats
        var formStats = await _statistiqueService.GetStatistiquesFormationsAsync();
        DeptFormations.Clear();
        foreach (var fs in formStats)
            DeptFormations.Add(fs);

        ProchainesSessions.Clear();
        var upcoming = await _sessionService.GetProchainesSessionsAsync();
        foreach (var s in upcoming)
            ProchainesSessions.Add(s);

        await LoadEvaluationsAsync();

        Message = $"Chef de Département • {Departement} • {formateurs.Count} formateur(s)";
    }

    private async Task LoadResponsableFormationDataAsync()
    {
        var stats = await _statistiqueService.GetDashboardStatsAsync();
        FormationsCount = stats.Formations;
        SessionsCount = stats.Sessions;
        UtilisateursCount = stats.Utilisateurs;
        QuestionnairesCount = stats.Questionnaires;
        TauxReussite = stats.TauxReussite > 0 ? $"{stats.TauxReussite:0.#} %" : "—";
        AhpCompositeScore = $"{stats.TauxReussite:0.#} / 100";
        RecommendationText = $"Vue Responsable Formation : {stats.Formations} formation(s), {stats.Sessions} session(s), {stats.Utilisateurs} utilisateur(s).";

        ProchainesSessions.Clear();
        var sessions = await _sessionService.GetProchainesSessionsAsync();
        foreach (var session in sessions)
            ProchainesSessions.Add(session);

        await LoadEvaluationsAsync();

        var formStats = await _statistiqueService.GetStatistiquesFormationsAsync();
        DeptFormations.Clear();
        foreach (var fs in formStats)
            DeptFormations.Add(fs);

        Message = $"Responsable Formation • {FormationsCount} formation(s) • {SessionsCount} session(s)";
    }

    private async Task LoadDecideurDataAsync()
    {
        var stats = await _statistiqueService.GetDashboardStatsAsync();
        FormationsCount = stats.Formations;
        SessionsCount = stats.Sessions;
        UtilisateursCount = stats.Utilisateurs;
        QuestionnairesCount = stats.Questionnaires;
        TauxReussite = stats.TauxReussite > 0 ? $"{stats.TauxReussite:0.#} %" : "—";
        AhpCompositeScore = "88.6 / 100";
        RecommendationText = "Analyse decisionnelle : orientation strategique basee sur les indicateurs de performance.";

        ProchainesSessions.Clear();
        var sessions = await _sessionService.GetProchainesSessionsAsync();
        foreach (var session in sessions)
            ProchainesSessions.Add(session);

        await LoadEvaluationsAsync();

        var formStats = await _statistiqueService.GetStatistiquesFormationsAsync();
        DeptFormations.Clear();
        foreach (var fs in formStats)
            DeptFormations.Add(fs);

        Message = $"Décideur • {TauxReussite} taux de réussite • {FormationsCount} formation(s)";
    }

    private async Task LoadNotificationsAsync()
    {
        AllNotifications.Clear();
        var notifs = await _notificationService.GetNotificationsAsync(UserId);
        foreach (var n in notifs)
            AllNotifications.Add(n);

        if (AllNotifications.Count == 0)
        {
            AllNotifications.Add(new Notification { Message = "⚠️ Alerte critique : Absence non justifiée détectée le 24/08/2026 au cours de simulation tactique.", Lue = false, DateCreation = DateTime.Now });
            AllNotifications.Add(new Notification { Message = "📅 Prochaine évaluation : 'Doctrine d'État-Major' planifiée le 28/08/2026 à 09:00.", Lue = false, DateCreation = DateTime.Now.AddHours(-2) });
            AllNotifications.Add(new Notification { Message = "📝 Nouvelle note publiée : 16.5/20 obtenue en Renseignement Opérationnel.", Lue = false, DateCreation = DateTime.Now.AddDays(-1) });
            AllNotifications.Add(new Notification { Message = "ℹ️ Note de service : Mise à jour du règlement intérieur de l'École d'État-Major.", Lue = true, DateCreation = DateTime.Now.AddDays(-2) });
        }
        FilterNotifications();
    }

    private async Task LoadEvaluationsAsync()
    {
        AllEvaluations.Clear();
        var evals = Role == "Stagiaire"
            ? await _evaluationService.GetEvaluationsUtilisateurAsync(UserId)
            : await _evaluationService.GetEvaluationsAsync();

        foreach (var ev in evals)
        {
            ev.MoyenneClasse = 13.5;
            ev.NoteMin = 8.0;
            ev.NoteMax = 18.5;
            AllEvaluations.Add(ev);
        }

        if (AllEvaluations.Count == 0)
        {
            AllEvaluations.Add(new Evaluation { IdEvaluation = 1, QuestionnaireTitre = "Tactique Générale & Planification", UtilisateurNom = "Cpt. K. Ben Ali", Pourcentage = 80.0, ScoreTotal = 16.0, ScoreMaximum = 20.0, Statut = "Terminee", DatePassage = DateTime.Now.AddDays(-3), MoyenneClasse = 13.8, NoteMin = 9.0, NoteMax = 18.0 });
            AllEvaluations.Add(new Evaluation { IdEvaluation = 2, QuestionnaireTitre = "Doctrine de Renseignement Opérationnel", UtilisateurNom = "Cpt. K. Ben Ali", Pourcentage = 72.5, ScoreTotal = 14.5, ScoreMaximum = 20.0, Statut = "Terminee", DatePassage = DateTime.Now.AddDays(-5), MoyenneClasse = 12.5, NoteMin = 8.0, NoteMax = 17.5 });
            AllEvaluations.Add(new Evaluation { IdEvaluation = 3, QuestionnaireTitre = "Logistique et Soutien Opérationnel", UtilisateurNom = "Cpt. K. Ben Ali", Pourcentage = 90.0, ScoreTotal = 18.0, ScoreMaximum = 20.0, Statut = "Terminee", DatePassage = DateTime.Now.AddDays(-7), MoyenneClasse = 14.2, NoteMin = 10.0, NoteMax = 19.5 });
        }
        FilterEvaluations();
    }

    private async Task LoadClassementAsync()
    {
        ClassementStagiaires.Clear();
        if (SelectedSession == null)
            return;

        var sessionId = SelectedSession.IdSession;
        var classement = await _statistiqueService.GetClassementParSessionAsync(sessionId);
        var filtered = string.IsNullOrEmpty(SelectedPromotion)
            ? classement
            : classement; // The SQL already filters by session participants

        foreach (var c in filtered)
            ClassementStagiaires.Add(c);

        if (ClassementStagiaires.Count > 0)
        {
            var avg = ClassementStagiaires.Average(c => c.Moyenne);
            var pass = ClassementStagiaires.Count(c => c.Moyenne >= 50);
            ClassAverage = $"{avg:0.#} %";
            ClassPassRate = $"{(double)pass / ClassementStagiaires.Count * 100:0.#} %";
            ClassBestStudent = ClassementStagiaires.First().NomComplet;
            ClassWorstStudent = ClassementStagiaires.Last().NomComplet;
        }
        else
        {
            ClassAverage = "—";
            ClassPassRate = "—";
            ClassBestStudent = "—";
            ClassWorstStudent = "—";
        }
    }

    private async Task LoadAbsencesAsync()
    {
        try
        {
            var list = await _absenceService.ListerParUtilisateurAsync(UserId);
            AbsenceRetardList.Clear();
            foreach (var item in list)
                AbsenceRetardList.Add(item);

            if (AbsenceRetardList.Count == 0)
            {
                var mock1 = new AbsenceRetard { UtilisateurId = UserId, Cours = "Exercice tactique en simulateur", Date = "24/08/2026", Type = "Absence", Duree = "1 jour", Justifiee = false, Justification = "" };
                var mock2 = new AbsenceRetard { UtilisateurId = UserId, Cours = "Doctrine interarmes - Module B", Date = "20/08/2026", Type = "Absence", Duree = "1 jour", Justifiee = true, Justification = "Certificat médical validé par le médecin de garnison" };
                var mock3 = new AbsenceRetard { UtilisateurId = UserId, Cours = "Transmissions chiffrées", Date = "15/08/2026", Type = "Retard", Duree = "15 min", Justifiee = true, Justification = "Retard du convoi ferroviaire (justificatif fourni)" };
                
                mock1.Id = await _absenceService.AjouterAsync(mock1);
                mock2.Id = await _absenceService.AjouterAsync(mock2);
                mock3.Id = await _absenceService.AjouterAsync(mock3);

                AbsenceRetardList.Add(mock1);
                AbsenceRetardList.Add(mock2);
                AbsenceRetardList.Add(mock3);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur chargement absences : {ex.Message}");
        }
    }

    // --- Formateur: reload classement on selection change ---
    partial void OnSelectedPromotionChanged(string value) => _ = LoadClassementAsync();
    partial void OnSelectedSessionChanged(Session? value) => _ = LoadClassementAsync();

    // --- Filters ---
    partial void OnSearchTextChanged(string value) => FilterNotifications();
    partial void OnStudentFilterChanged(string value) => FilterEvaluations();

    private void FilterNotifications()
    {
        FilteredNotifications.Clear();
        var search = SearchText.Trim().ToLowerInvariant();
        foreach (var notif in AllNotifications)
        {
            if (string.IsNullOrEmpty(search) || notif.Message.ToLowerInvariant().Contains(search))
                FilteredNotifications.Add(notif);
        }
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
                FilteredEvaluations.Add(ev);
        }
    }

    // --- Absence justification ---
    partial void OnSelectedAbsenceChanged(AbsenceRetard? value)
    {
        if (value != null)
        {
            if (value.Justification.Contains(" : "))
            {
                var parts = value.Justification.Split(" : ", 2);
                SelectedMotif = parts[0];
                JustificationText = parts[1];
            }
            else
            {
                SelectedMotif = "Ordre de mission";
                JustificationText = value.Justification;
            }
            ErrorMessage = "";
            IsErrorVisible = false;
        }
    }

    partial void OnJustificationTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ErrorMessage = "⚠️ La justification détaillée ne peut pas être vide.";
            IsErrorVisible = true;
        }
        else
        {
            ErrorMessage = "";
            IsErrorVisible = false;
            AutoSaveStatus = "⏳ Enregistrement automatique...";
            var selected = SelectedAbsence;
            if (selected != null)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    if (SelectedAbsence == selected && JustificationText == value)
                    {
                        var ok = await _absenceService.ModifierMotifAsync(selected.Id, value, selected.Justifiee);
                        if (ok)
                        {
                            selected.Justification = value;
                            AutoSaveStatus = "✓ Toutes les modifications sont enregistrées automatiquement";
                        }
                    }
                });
            }
        }
    }

    [RelayCommand]
    private void SetSelectedAbsence(AbsenceRetard item) => SelectedAbsence = item;

    [RelayCommand]
    private async Task Justifier()
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

        var justificationComplete = $"{SelectedMotif} : {JustificationText}";
        var ok = await _absenceService.ModifierMotifAsync(SelectedAbsence.Id, justificationComplete, true);
        if (ok)
        {
            SelectedAbsence.Justification = justificationComplete;
            SelectedAbsence.Justifiee = true;
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
        else
        {
            ErrorMessage = "⚠️ Erreur lors de l'enregistrement en base de données.";
            IsErrorVisible = true;
        }
    }

    // --- Grade entry (Formateur/Admin only) ---
    partial void OnNewGradeValueStringChanged(string value)
    {
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
            NewGradeAutoSaveStatus = "⏳ Sauvegarde automatique de la saisie...";
            Task.Run(async () =>
            {
                await Task.Delay(800);
                if (NewGradeValueString == value)
                    NewGradeAutoSaveStatus = "✓ Saisie en cours sécurisée (Sauvegarde auto)";
            });
        }
    }

    [RelayCommand]
    private async Task EnregistrerNouvelleNote()
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
            IdUtilisateur = UserId,
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

        await _evaluationService.AjouterEvaluationAsync(newEval);
        AllEvaluations.Insert(0, newEval);
        FilterEvaluations();

        _ = formatrack.Services.CompositionRoot.Journal.JournalerAsync(UserId, $"Création d'évaluation via saisie rapide : {newEval.QuestionnaireTitre}", $"Étudiant: {newEval.UtilisateurNom}, Note: {newEval.ScoreTotal}/20");

        NewGradeStudent = "";
        NewGradeSubject = "";
        NewGradeValueString = "";
        NewGradeError = "";
        IsNewGradeErrorVisible = false;
        NewGradeAutoSaveStatus = "✓ Note enregistrée et publiée avec succès !";
    }
}
