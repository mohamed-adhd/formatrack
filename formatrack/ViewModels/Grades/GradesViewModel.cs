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

namespace formatrack.ViewModels.Grades;

public partial class GradesViewModel : ViewModelBase
{
    private readonly IFormationService _formationService;
    private readonly IModuleService _moduleService;
    private readonly ISessionService _sessionService;
    private readonly INoteService _noteService;
    private readonly IUtilisateurService _utilisateurService;

    [ObservableProperty] private string _role = "";
    [ObservableProperty] private string _departement = "";
    [ObservableProperty] private string _promotion = "";
    [ObservableProperty] private int _userId;
    [ObservableProperty] private bool _isMobile;
    [ObservableProperty] private string _message = "Chargement...";

    public bool IsFormateurView => Role == "Formateur";
    public bool IsAdminView => Role == "Administrateur";
    public bool CanEdit => Role == "Formateur";

    // Cascade selectors
    [ObservableProperty] private Formation? _selectedFormation;
    [ObservableProperty] private string _selectedPromotion = "";
    [ObservableProperty] private Session? _selectedSession;
    [ObservableProperty] private Module? _selectedModule;

    // Student grade grid data
    [ObservableProperty] private double _moyenneClasse;
    [ObservableProperty] private double _moyennePonderee;
    [ObservableProperty] private int _nbEtudiants;
    [ObservableProperty] private int _nbNotes;

    public ObservableCollection<Formation> Formations { get; } = new();
    public ObservableCollection<string> Promotions { get; } = new();
    public ObservableCollection<Session> Sessions { get; } = new();
    public ObservableCollection<Module> Modules { get; } = new();
    public ObservableCollection<StudentGradeRow> StudentGrades { get; } = new();

    public List<string> PromotionOptions { get; } = new() { "Promotion 2025", "Promotion 2026" };

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
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var formations = await _formationService.GetFormationsAsync();
            Formations.Clear();
            foreach (var f in formations) Formations.Add(f);
            Message = $"{Formations.Count} formation(s) disponible(s)";
        }
        catch (Exception ex) { Message = $"Erreur : {ex.Message}"; }
    }

    // Cascade: Formation → Promotions + Sessions
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

    // Cascade: Promotion → Sessions
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

        var sessions = await _sessionService.GetSessionsAsync();
        foreach (var s in sessions.Where(s => s.IdFormation == SelectedFormation.IdFormation))
            Sessions.Add(s);

        if (Sessions.Count > 0)
            SelectedSession = Sessions[0];
    }

    // Cascade: Session → Modules
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

        var modules = await _moduleService.GetByFormationAsync(SelectedFormation.IdFormation);
        foreach (var m in modules) Modules.Add(m);

        if (Modules.Count > 0)
            SelectedModule = Modules[0];
    }

    // Module selected → load student grades
    partial void OnSelectedModuleChanged(Module? value)
    {
        _ = LoadStudentGradesAsync();
    }

    private async Task LoadStudentGradesAsync()
    {
        StudentGrades.Clear();
        if (SelectedModule == null || SelectedSession == null) return;

        var notes = await _noteService.GetByModuleSessionAsync(SelectedModule.IdModule, SelectedSession.IdSession);
        var students = await _utilisateurService.GetUtilisateursParDepartementAsync(Departement);
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

        // Compute stats
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
