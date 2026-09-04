using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Grades;

public partial class GradesViewModel : ViewModelBase
{
    private readonly IFormationService _formationService;
    private readonly IModuleService _moduleService;
    private readonly ISessionService _sessionService;
    private readonly INoteService _noteService;
    private readonly IUtilisateurService _utilisateurService;
    private bool _suppressAutoLoad;
    private CancellationTokenSource? _adminLoadCts;
    private IReadOnlyList<Session> _allSessions = Array.Empty<Session>();

    private static IReadOnlyList<Session>? _cachedSessions;
    private static IReadOnlyList<Formation>? _cachedFormations;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly object _cacheLock = new();

    [ObservableProperty] private string _role = "";
    [ObservableProperty] private string _departement = "";
    [ObservableProperty] private string _promotion = "";
    [ObservableProperty] private int _userId;
    [ObservableProperty] private bool _isMobile;
    [ObservableProperty] private string _message = "Chargement...";

    public bool IsFormateurView => Role == "Formateur";
    public bool IsAdminView => Role == "Administrateur";
    public bool IsStagiaireView => Role == "Stagiaire";
    public bool CanEdit => Role == "Formateur";

    [ObservableProperty] private Formation? _selectedFormation;
    [ObservableProperty] private string _selectedPromotion = "";
    [ObservableProperty] private Session? _selectedSession;
    [ObservableProperty] private Module? _selectedModule;

    [ObservableProperty] private Formation? _adminSelectedFormation;
    [ObservableProperty] private string _adminSelectedPromotion = "";
    [ObservableProperty] private string _adminSelectedSession = "";
    [ObservableProperty] private string _adminSelectedEtat = "";

    [ObservableProperty] private double _moyenneClasse;
    [ObservableProperty] private double _moyennePonderee;
    [ObservableProperty] private int _nbEtudiants;
    [ObservableProperty] private int _nbNotes;

    [ObservableProperty] private double _stagiaireMoyenne;
    [ObservableProperty] private int _stagiaireNbNotes;

    [ObservableProperty] private int _adminTotalNotes;
    [ObservableProperty] private double _adminMoyenne;

    public ObservableCollection<Formation> Formations { get; } = new();
    public ObservableCollection<string> Promotions { get; } = new();
    public ObservableCollection<Session> Sessions { get; } = new();
    public ObservableCollection<Module> Modules { get; } = new();
    public ObservableCollection<StudentGradeRow> StudentGrades { get; } = new();
    public ObservableCollection<StagiaireGradeRow> MyGrades { get; } = new();

    public ObservableCollection<Formation> AdminFormations { get; } = new();
    public ObservableCollection<string> AdminPromotions { get; } = new();
    public ObservableCollection<string> AdminSessions { get; } = new();
    public ObservableCollection<string> AdminEtats { get; } = new();
    public ObservableCollection<AdminNoteRow> AdminNotes { get; } = new();

    public GradesViewModel(
        IFormationService? formationService = null,
        IModuleService? moduleService = null,
        ISessionService? sessionService = null,
        INoteService? noteService = null,
        IUtilisateurService? utilisateurService = null,
        string role = "", string departement = "", string promotion = "", int userId = 0)
    {
        _formationService = formationService ?? new FormationService();
        _moduleService = moduleService ?? new ModuleService();
        _sessionService = sessionService ?? new SessionService();
        _noteService = noteService ?? new NoteService();
        _utilisateurService = utilisateurService ?? new UtilisateurService();
        Role = role;
        Departement = departement;
        Promotion = promotion;
        UserId = userId;

        if (IsStagiaireView)
            _ = LoadMyGradesAsync();
        else if (IsAdminView)
            _ = InitAdminAsync();
        else
            _ = LoadAsync();
    }

    private async Task LoadMyGradesAsync()
    {
        try
        {
            var notes = await Task.Run(() => _noteService.GetByStagiaireAsync(UserId));
            var rows = new List<StagiaireGradeRow>();
            foreach (var n in notes)
            {
                rows.Add(new StagiaireGradeRow
                {
                    ModuleTitre = n.ModuleTitre ?? $"Module #{n.IdModule}",
                    NoteValeur = n.NoteValeur,
                    Coefficient = n.ModuleCoefficient,
                    IdSession = n.IdSession
                });
            }
            MyGrades.Clear();
            foreach (var r in rows) MyGrades.Add(r);

            var withValues = rows.Where(g => g.NoteValeur > 0).ToList();
            StagiaireNbNotes = withValues.Count;
            StagiaireMoyenne = withValues.Count > 0
                ? withValues.Sum(g => g.NoteValeur * g.Coefficient) / withValues.Sum(g => g.Coefficient)
                : 0;
            Message = StagiaireNbNotes > 0
                ? $"{StagiaireNbNotes} note(s) • Moyenne: {StagiaireMoyenne:0.00}/20"
                : "Aucune note enregistrée.";
        }
        catch (Exception ex) { Message = $"Erreur : {ex.Message}"; }
    }

    private async Task LoadAsync()
    {
        try
        {
            var formations = await Task.Run(() => _formationService.GetFormationsAsync());
            Formations.Clear();
            foreach (var f in formations) Formations.Add(f);
            Message = $"{Formations.Count} formation(s) disponible(s)";
        }
        catch (Exception ex) { Message = $"Erreur : {ex.Message}"; }
    }

    private static async Task<IReadOnlyList<Session>> GetSessionsCachedAsync()
    {
        lock (_cacheLock)
        {
            if (_cachedSessions != null && DateTime.Now < _cacheExpiry)
                return _cachedSessions;
        }
        var result = await Task.Run(() =>
        {
            var svc = new SessionService();
            return svc.GetSessionsAsync();
        });
        lock (_cacheLock)
        {
            _cachedSessions = result;
            _cacheExpiry = DateTime.Now.AddMinutes(5);
        }
        return result;
    }

    private static async Task<IReadOnlyList<Formation>> GetFormationsCachedAsync()
    {
        lock (_cacheLock)
        {
            if (_cachedFormations != null && DateTime.Now < _cacheExpiry)
                return _cachedFormations;
        }
        var result = await Task.Run(() =>
        {
            var svc = new FormationService();
            return svc.GetFormationsAsync();
        });
        lock (_cacheLock)
        {
            _cachedFormations = result;
            _cacheExpiry = DateTime.Now.AddMinutes(5);
        }
        return result;
    }

    private async Task InitAdminAsync()
    {
        try
        {
            _suppressAutoLoad = true;
            _adminLoadCts?.Cancel();
            _adminLoadCts = new CancellationTokenSource();
            var ct = _adminLoadCts.Token;

            var sessionsTask = GetSessionsCachedAsync();
            var formationsTask = GetFormationsCachedAsync();
            await Task.WhenAll(sessionsTask, formationsTask);

            if (ct.IsCancellationRequested) return;

            _allSessions = sessionsTask.Result;
            var formations = formationsTask.Result;

            AdminFormations.Clear();
            AdminFormations.Add(new Formation { IdFormation = 0, Titre = "Toutes les formations" });
            foreach (var f in formations) AdminFormations.Add(f);

            AdminPromotions.Clear();
            AdminPromotions.Add("Toutes");
            AdminPromotions.Add("Promotion 2025");
            AdminPromotions.Add("Promotion 2026");

            AdminSessions.Clear();
            AdminSessions.Add("Toutes");
            AdminSessions.Add("Session 1 (Sep - Dec)");
            AdminSessions.Add("Session 2 (Jan - Avr)");

            AdminEtats.Clear();
            AdminEtats.Add("Tous");
            AdminEtats.Add("Militaire");
            AdminEtats.Add("Civil");

            _suppressAutoLoad = false;

            AdminSelectedFormation = AdminFormations[0];
            AdminSelectedPromotion = AdminPromotions[0];
            AdminSelectedSession = AdminSessions[0];
            AdminSelectedEtat = AdminEtats[0];

            await LoadAdminNotesAsync();
        }
        catch (Exception ex) { Message = $"Erreur : {ex.Message}"; }
    }

    partial void OnAdminSelectedFormationChanged(Formation? value) { if (!_suppressAutoLoad) _ = LoadAdminNotesAsync(); }
    partial void OnAdminSelectedPromotionChanged(string value) { if (!_suppressAutoLoad) _ = LoadAdminNotesAsync(); }
    partial void OnAdminSelectedSessionChanged(string value) { if (!_suppressAutoLoad) _ = LoadAdminNotesAsync(); }
    partial void OnAdminSelectedEtatChanged(string value) { if (!_suppressAutoLoad) _ = LoadAdminNotesAsync(); }

    private IEnumerable<int>? GetSessionIdsForFilter()
    {
        if (AdminSelectedSession == "Toutes" || string.IsNullOrEmpty(AdminSelectedSession))
            return null;

        bool isSession1 = AdminSelectedSession.Contains("Session 1");
        return _allSessions
            .Where(s => isSession1
                ? s.DateDebut.Month >= 9
                : s.DateDebut.Month >= 1 && s.DateDebut.Month <= 6)
            .Select(s => s.IdSession)
            .ToList();
    }

    private async Task LoadAdminNotesAsync()
    {
        _adminLoadCts?.Cancel();
        _adminLoadCts = new CancellationTokenSource();
        var ct = _adminLoadCts.Token;

        try
        {
            int? formationId = AdminSelectedFormation?.IdFormation == 0 ? null : AdminSelectedFormation?.IdFormation;
            string? promo = AdminSelectedPromotion == "Toutes" ? null : AdminSelectedPromotion;
            string? etatFilter = AdminSelectedEtat == "Tous" ? null : AdminSelectedEtat;
            var sessionIds = GetSessionIdsForFilter();

            var notes = await Task.Run(() => _noteService.GetAllNotesWithDetailsAsync(formationId, promo, sessionIds, etatFilter), ct);

            if (ct.IsCancellationRequested) return;

            var rows = new List<AdminNoteRow>(notes.Count);
            foreach (var n in notes)
            {
                rows.Add(new AdminNoteRow
                {
                    IdNote = n.IdNote,
                    StagiaireNom = n.StagiaireNom,
                    ModuleTitre = n.ModuleTitre,
                    Coefficient = n.ModuleCoefficient,
                    NoteValeur = n.NoteValeur,
                    NotePonderee = n.NoteValeur * n.ModuleCoefficient,
                    SessionTitre = n.SessionTitre,
                    Promotion = n.Promotion,
                    Departement = n.Departement,
                    FormationTitre = n.FormationTitre
                });
            }

            AdminNotes.Clear();
            foreach (var r in rows) AdminNotes.Add(r);

            AdminTotalNotes = AdminNotes.Count;
            var withValues = rows.Where(n => n.NoteValeur > 0).ToList();
            AdminMoyenne = withValues.Count > 0
                ? withValues.Sum(n => n.NoteValeur * n.Coefficient) / withValues.Sum(n => n.Coefficient)
                : 0;

            Message = $"{AdminTotalNotes} note(s) trouvée(s)";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Message = $"Erreur : {ex.Message}"; }
    }

    // ─── FORMATEUR CASCADE ────────────────────────────────

    partial void OnSelectedFormationChanged(Formation? value)
    {
        Promotions.Clear();
        SelectedPromotion = "";
        Sessions.Clear();
        SelectedSession = null;
        Modules.Clear();
        SelectedModule = null;
        StudentGrades.Clear();

        if (value != null)
        {
            Promotions.Add("Promotion 2025");
            Promotions.Add("Promotion 2026");
            if (!string.IsNullOrEmpty(Promotion) && Promotions.Contains(Promotion))
                SelectedPromotion = Promotion;
            else if (Promotions.Count > 0)
                SelectedPromotion = Promotions[0];
        }
    }

    partial void OnSelectedPromotionChanged(string value)
    {
        _ = LoadSessionsAsync();
    }

    private async Task LoadSessionsAsync()
    {
        if (SelectedFormation == null) return;
        Sessions.Clear();
        SelectedSession = null;
        Modules.Clear();
        SelectedModule = null;
        StudentGrades.Clear();

        var sessions = await Task.Run(() => _sessionService.GetSessionsAsync());
        foreach (var s in sessions.Where(s => s.IdFormation == SelectedFormation.IdFormation))
            Sessions.Add(s);

        if (Sessions.Count > 0)
            SelectedSession = Sessions[0];
    }

    partial void OnSelectedSessionChanged(Session? value)
    {
        _ = LoadModulesAsync();
    }

    private async Task LoadModulesAsync()
    {
        if (SelectedFormation == null) return;
        Modules.Clear();
        SelectedModule = null;
        StudentGrades.Clear();

        var modules = await Task.Run(() => _moduleService.GetByFormationAsync(SelectedFormation.IdFormation));
        foreach (var m in modules) Modules.Add(m);

        if (Modules.Count > 0)
            SelectedModule = Modules[0];
    }

    partial void OnSelectedModuleChanged(Module? value)
    {
        _ = LoadStudentGradesAsync();
    }

    private async Task LoadStudentGradesAsync()
    {
        StudentGrades.Clear();
        if (SelectedModule == null || SelectedSession == null) return;

        var (notes, students) = await Task.Run(() =>
        {
            var n = _noteService.GetByModuleSessionAsync(SelectedModule.IdModule, SelectedSession.IdSession).GetAwaiter().GetResult();
            var s = _utilisateurService.GetUtilisateursParDepartementAsync(Departement).GetAwaiter().GetResult();
            return (n, s);
        });

        var promoStudents = students.Where(s => s.Role == "Stagiaire" && s.Promotion == SelectedPromotion).ToList();

        foreach (var student in promoStudents)
        {
            var existingNote = notes.FirstOrDefault(n => n.IdStagiaire == student.IdUtilisateur);
            StudentGrades.Add(new StudentGradeRow
            {
                IdStagiaire = student.IdUtilisateur,
                NomComplet = student.NomComplet,
                NoteValeur = existingNote?.NoteValeur ?? 0,
                HasExistingNote = existingNote != null,
                IdNote = existingNote?.IdNote ?? 0,
                Coefficient = SelectedModule.Coefficient
            });
        }

        var gradesWithValues = StudentGrades.Where(g => g.HasExistingNote).ToList();
        NbEtudiants = StudentGrades.Count;
        NbNotes = gradesWithValues.Count;
        MoyenneClasse = gradesWithValues.Count > 0 ? gradesWithValues.Average(g => g.NoteValeur) : 0;
        MoyennePonderee = gradesWithValues.Count > 0
            ? gradesWithValues.Sum(g => g.NoteValeur * g.Coefficient) / gradesWithValues.Sum(g => g.Coefficient)
            : 0;

        Message = $"{NbNotes}/{NbEtudiants} notes saisies • {SelectedModule.Titre} (coef {SelectedModule.Coefficient})";
    }

    [RelayCommand]
    private async Task SaveAllGradesAsync()
    {
        if (SelectedModule == null || SelectedSession == null) return;

        var notesToSave = StudentGrades
            .Where(g => g.NoteValeur > 0 || g.HasExistingNote)
            .Select(g => new Note
            {
                IdNote = g.IdNote,
                IdStagiaire = g.IdStagiaire,
                IdModule = SelectedModule.IdModule,
                IdSession = SelectedSession.IdSession,
                NoteValeur = g.NoteValeur
            }).ToList();

        await _noteService.BulkSaveAsync(notesToSave, UserId);
        await LoadStudentGradesAsync();

        _ = formatrack.Services.CompositionRoot.Journal.JournalerAsync(UserId,
            $"Saisie bulk: {notesToSave.Count} notes",
            $"{SelectedModule.Titre} - {SelectedSession.Lieu}");

        Message = $"✓ {notesToSave.Count} note(s) enregistrée(s) avec succès.";
    }
}
