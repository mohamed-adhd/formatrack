using CommunityToolkit.Mvvm.ComponentModel;
using formatrack.Models;
using formatrack.Services;
using formatrack.Services.Interfaces;
using formatrack.ViewModels.Dashboard;
using formatrack.ViewModels.Formations;
using formatrack.ViewModels.Utilisateurs;
using formatrack.ViewModels.Sessions;
using formatrack.ViewModels.Evaluations;
using formatrack.ViewModels.Questionnaires;
using formatrack.ViewModels.Statistiques;
using formatrack.ViewModels.Timetable;
using formatrack.ViewModels.Grades;
using formatrack.ViewModels.Shared;

namespace formatrack.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IFormationService _formationService;
    private readonly ISessionService _sessionService;
    private readonly IStatistiqueService _statistiqueService;
    private readonly IUtilisateurService _utilisateurService;
    private readonly IEvaluationService _evaluationService;
    private readonly IQuestionnaireService _questionnaireService;

    public ChatbotViewModel Chatbot { get; } = new();

    [ObservableProperty] private bool _isLoggedIn;
    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private SidebarViewModel _sidebar;
    [ObservableProperty] private string _currentBreadcrumb = "Tableau de bord";
    [ObservableProperty] private string _currentPageTitle = "Tableau de bord";
    [ObservableProperty] private string _currentUserName = "Colonel Direction";
    [ObservableProperty] private string _currentUserRole = "Administrateur";
    [ObservableProperty] private string _currentUserEmail = "admin@sefad.local";
    [ObservableProperty] private int _currentUserId = 1;
    [ObservableProperty] private string _currentUserDepartement = "";
    [ObservableProperty] private string _currentUserPromotion = "";
    [ObservableProperty] private bool _isMobile;

    partial void OnIsMobileChanged(bool value)
    {
        if (CurrentPage is DashboardViewModel dashboard)
        {
            dashboard.IsMobile = value;
        }
    }

    public MainWindowViewModel() : this(new AuthService())
    {
    }

    public MainWindowViewModel(IAuthService authService)
    {
        _authService = authService;
        _formationService = new FormationService();
        _sessionService = new SessionService();
        _statistiqueService = new StatistiqueService();
        _utilisateurService = new UtilisateurService();
        _evaluationService = new EvaluationService();
        _questionnaireService = new QuestionnaireService();

        Sidebar = new SidebarViewModel(
            string.Empty, string.Empty,
            OpenDashboardAction, OpenUtilisateurs, OpenFormations,
            OpenSessions, OpenEvaluations, OpenQuestionnaires,
            OpenStatistiques, Logout,
            openTimetable: OpenTimetable,
            openGrades: OpenGrades);

        CurrentPage = new LoginViewModel(_authService, (role, email) => OpenDashboard(role, email));
    }

    private void UpdateSidebar(string activePage, string subTitle)
    {
        Sidebar.Role = CurrentUserRole;
        Sidebar.ActivePage = activePage;
        Sidebar.SubTitle = subTitle;
        Sidebar.UserName = CurrentUserName;
        Sidebar.UserEmail = CurrentUserEmail;
    }

    public async void OpenDashboard(string role, string email = "")
    {
        IsLoggedIn = true;
        CurrentUserRole = string.IsNullOrWhiteSpace(role) ? "Administrateur" : role;
        CurrentUserEmail = string.IsNullOrWhiteSpace(email) ? $"{CurrentUserRole.ToLowerInvariant()}@sefad.local" : email;

        // Fetch user details from database
        var user = await _utilisateurService.GetUtilisateurParEmailAsync(CurrentUserEmail);
        if (user != null)
        {
            CurrentUserName = user.NomComplet;
            CurrentUserRole = user.Role;
            CurrentUserId = user.IdUtilisateur;
            CurrentUserDepartement = user.Departement;
            CurrentUserPromotion = user.Promotion;
        }
        else
        {
            // Fallback for unknown users
            if (CurrentUserEmail.Contains("admin") || CurrentUserRole == "Administrateur")
            {
                CurrentUserName = "Colonel H. Mansour";
                CurrentUserRole = "Administrateur";
                CurrentUserId = 1;
            }
            else if (CurrentUserEmail.Contains("chefdep") || CurrentUserRole == "ChefDepartement")
            {
                CurrentUserName = "Cdt. A. Harbi";
                CurrentUserRole = "ChefDepartement";
                CurrentUserId = 4;
            }
            else if (CurrentUserEmail.Contains("format") || CurrentUserRole == "Formateur")
            {
                CurrentUserName = "Cdt. Y. Mansouri";
                CurrentUserRole = "Formateur";
                CurrentUserId = 2;
            }
            else if (CurrentUserEmail.Contains("stagiaire") || CurrentUserRole == "Stagiaire")
            {
                CurrentUserName = "Cpt. K. Ben Ali";
                CurrentUserRole = "Stagiaire";
                CurrentUserId = 3;
            }
            else if (CurrentUserEmail.Contains("resp.formation") || CurrentUserRole == "ResponsableFormation")
            {
                CurrentUserName = "Cdt. A. Hadj";
                CurrentUserRole = "ResponsableFormation";
                CurrentUserId = 7;
            }
            else if (CurrentUserEmail.Contains("decideur") || CurrentUserRole == "Decideur")
            {
                CurrentUserName = "Maj. M. Bouzid";
                CurrentUserRole = "Decideur";
                CurrentUserId = 8;
            }
            else
            {
                CurrentUserName = "Utilisateur EMS";
                CurrentUserId = 1;
            }
        }

        CurrentBreadcrumb = "Accueil / Tableau de bord";
        CurrentPageTitle = "Tableau de bord";
        UpdateSidebar("Dashboard", "Tableau de bord");

        Chatbot.SetUserContext(CurrentUserRole, CurrentUserPromotion, CurrentUserDepartement, CurrentUserName);

        CurrentPage = new DashboardViewModel(_statistiqueService, _sessionService,
            OpenFormations, OpenUtilisateurs, OpenSessions,
            OpenEvaluations, OpenQuestionnaires, OpenStatistiques, Logout, CurrentUserRole, CurrentUserId,
            utilisateurService: _utilisateurService,
            departement: CurrentUserDepartement,
            promotion: CurrentUserPromotion,
            openGrades: OpenGrades,
            openTimetable: OpenTimetable)
        {
            IsMobile = IsMobile
        };
    }

    public void OpenFormations()
    {
        CurrentBreadcrumb = "Accueil / Formations";
        CurrentPageTitle = "Gestion des Formations";
        UpdateSidebar("Formations", "Gestion formations");

        CurrentPage = new FormationsListViewModel(
            _formationService,
            OpenDashboardAction,
            OpenFormationDetail,
            OpenFormationCreate,
            OpenUtilisateurs,
            OpenSessions,
            OpenEvaluations,
            OpenQuestionnaires,
            OpenStatistiques);
    }

    public void OpenUtilisateurs()
    {
        CurrentBreadcrumb = "Accueil / Utilisateurs";
        CurrentPageTitle = "Gestion des Utilisateurs";
        UpdateSidebar("Utilisateurs", "Gestion utilisateurs");

        CurrentPage = new UtilisateursListViewModel(
            _utilisateurService,
            OpenDashboardAction,
            OpenUtilisateurDetail,
            OpenUtilisateurForm,
            OpenFormations,
            OpenSessions,
            OpenEvaluations,
            OpenQuestionnaires,
            OpenStatistiques);
    }

    public void OpenSessions()
    {
        CurrentBreadcrumb = "Accueil / Sessions";
        CurrentPageTitle = "Gestion des Sessions Pédagogiques";
        UpdateSidebar("Sessions", "Gestion sessions");

        CurrentPage = new SessionsListViewModel(
            _sessionService,
            _formationService,
            OpenDashboardAction,
            OpenSessionDetail,
            OpenSessionCreate,
            OpenFormations,
            OpenUtilisateurs,
            OpenEvaluations,
            OpenQuestionnaires,
            OpenStatistiques);
    }

    public void OpenEvaluations()
    {
        CurrentBreadcrumb = "Accueil / Évaluations";
        CurrentPageTitle = "Suivi des Évaluations";
        UpdateSidebar("Evaluations", "Gestion evaluations");

        CurrentPage = new EvaluationsListViewModel(
            _evaluationService,
            _questionnaireService,
            OpenDashboardAction,
            OpenEvaluationPasser,
            OpenEvaluationResultat,
            OpenFormations,
            OpenUtilisateurs,
            OpenSessions,
            OpenQuestionnaires,
            OpenStatistiques,
            CurrentUserRole,
            CurrentUserId);
    }

    public void OpenQuestionnaires()
    {
        CurrentBreadcrumb = "Accueil / Questionnaires";
        CurrentPageTitle = "Conception des Questionnaires";
        UpdateSidebar("Questionnaires", "Gestion questionnaires");

        CurrentPage = new QuestionnairesListViewModel(
            _questionnaireService,
            _sessionService,
            OpenDashboardAction,
            OpenQuestionnaireEditor,
            OpenFormations,
            OpenUtilisateurs,
            OpenSessions,
            OpenEvaluations,
            OpenStatistiques);
    }

    public void OpenStatistiques()
    {
        CurrentBreadcrumb = "Accueil / Statistiques & Aide à la Décision";
        CurrentPageTitle = "Statistiques & Aide à la Décision";
        UpdateSidebar("Statistiques", "Statistiques & rapports");

        CurrentPage = new StatistiquesViewModel(
            _statistiqueService,
            _formationService,
            _sessionService,
            OpenDashboardAction,
            OpenRapport,
            OpenFormations,
            OpenUtilisateurs,
            OpenSessions,
            OpenEvaluations,
            OpenQuestionnaires);
    }

    public void OpenTimetable()
    {
        CurrentBreadcrumb = "Accueil / Emploi du Temps";
        CurrentPageTitle = "Emploi du Temps & Chronogramme";
        UpdateSidebar("Timetable", "Emploi du temps");

        CurrentPage = new TimetableViewModel(
            departement: CurrentUserDepartement,
            promotion: CurrentUserPromotion,
            userId: CurrentUserId,
            role: CurrentUserRole)
        {
            IsMobile = IsMobile
        };
    }

    public void OpenGrades()
    {
        CurrentBreadcrumb = "Accueil / Notes";
        CurrentPageTitle = "Notes & Saisie des Bulletins";
        UpdateSidebar("Grades", "Gestion des notes");

        CurrentPage = new GradesViewModel(
            departement: CurrentUserDepartement,
            promotion: CurrentUserPromotion,
            userId: CurrentUserId,
            role: CurrentUserRole)
        {
            IsMobile = IsMobile
        };
    }

    private void OpenDashboardAction()
    {
        OpenDashboard(CurrentUserRole, CurrentUserEmail);
    }

    // --- Formation navigation ---
    private void OpenFormationDetail(int idFormation)
    {
        CurrentBreadcrumb = "Accueil / Formations / Détail";
        CurrentPageTitle = "Détail de la Formation";
        UpdateSidebar("Formations", "Détail formation");

        var vm = new FormationDetailViewModel(
            _formationService,
            OpenFormations,
            OpenFormationEdit,
            DeleteFormation,
            OpenUtilisateurs,
            OpenSessions,
            OpenEvaluations,
            OpenQuestionnaires,
            OpenStatistiques);
        CurrentPage = vm;
        _ = vm.InitializeAsync(idFormation);
    }

    private void OpenFormationEdit(Formation formation)
    {
        CurrentBreadcrumb = "Accueil / Formations / Modification";
        CurrentPageTitle = "Modifier la Formation";
        UpdateSidebar("Formations", "Modifier formation");

        var vm = new FormationFormViewModel(
            _formationService,
            (bool saved) =>
            {
                if (saved)
                    OpenFormations();
                else
                    OpenFormationDetail(formation.IdFormation);
            },
            formation.IdFormation,
            OpenDashboardAction,
            OpenUtilisateurs,
            OpenSessions,
            OpenEvaluations,
            OpenQuestionnaires,
            OpenStatistiques);
        CurrentPage = vm;
    }

    private void OpenFormationCreate()
    {
        CurrentBreadcrumb = "Accueil / Formations / Nouvelle Formation";
        CurrentPageTitle = "Créer une Formation";
        UpdateSidebar("Formations", "Nouvelle formation");

        var vm = new FormationFormViewModel(
            _formationService,
            (bool saved) =>
            {
                if (saved)
                    OpenFormations();
                else
                    OpenFormations();
            },
            null,
            OpenDashboardAction,
            OpenUtilisateurs,
            OpenSessions,
            OpenEvaluations,
            OpenQuestionnaires,
            OpenStatistiques);
        CurrentPage = vm;
    }

    private async void DeleteFormation(int idFormation)
    {
        var formation = await _formationService.GetFormationAsync(idFormation);
        var nom = formation?.Titre ?? idFormation.ToString();
        var ok = await _formationService.SupprimerFormationAsync(idFormation);
        if (ok)
        {
            await formatrack.Services.CompositionRoot.Journal.JournalerAsync(null, $"Suppression de la formation {nom}", $"ID: {idFormation}");
        }
        OpenFormations();
    }

    // --- Utilisateur navigation ---
    private void OpenUtilisateurDetail(Utilisateur utilisateur)
    {
        CurrentBreadcrumb = "Accueil / Utilisateurs / Profil";
        CurrentPageTitle = "Fiche Utilisateur";
        UpdateSidebar("Utilisateurs", "Détail utilisateur");

        var vm = new UtilisateurDetailViewModel(
            _utilisateurService,
            OpenUtilisateurs,
            OpenUtilisateurForm,
            OpenDashboardAction);
        CurrentPage = vm;
        _ = vm.InitializeAsync(utilisateur.IdUtilisateur);
    }

    private void OpenUtilisateurForm(Utilisateur? utilisateur)
    {
        CurrentBreadcrumb = utilisateur == null ? "Accueil / Utilisateurs / Nouvel Utilisateur" : "Accueil / Utilisateurs / Modification";
        CurrentPageTitle = utilisateur == null ? "Créer un Utilisateur" : "Modifier l'Utilisateur";
        UpdateSidebar("Utilisateurs", utilisateur == null ? "Nouvel utilisateur" : "Modifier utilisateur");

        var vm = new UtilisateurFormViewModel(
            _utilisateurService,
            (bool saved) =>
            {
                if (saved)
                    OpenUtilisateurs();
                else if (utilisateur != null)
                    OpenUtilisateurDetail(utilisateur);
                else
                    OpenUtilisateurs();
            },
            utilisateur?.IdUtilisateur,
            OpenDashboardAction);
        CurrentPage = vm;
    }

    // --- Session navigation ---
    private void OpenSessionDetail(int idSession)
    {
        CurrentBreadcrumb = "Accueil / Sessions / Détail";
        CurrentPageTitle = "Détail de la Session";
        UpdateSidebar("Sessions", "Détail session");

        var vm = new SessionDetailViewModel(
            _sessionService,
            OpenSessions,
            OpenSessionEdit,
            OpenDashboardAction);
        CurrentPage = vm;
        _ = vm.InitializeAsync(idSession);
    }

    private void OpenSessionCreate()
    {
        CurrentBreadcrumb = "Accueil / Sessions / Nouvelle Session";
        CurrentPageTitle = "Planifier une Session";
        UpdateSidebar("Sessions", "Nouvelle session");

        var vm = new SessionFormViewModel(
            _sessionService,
            _formationService,
            (bool saved) =>
            {
                if (saved)
                    OpenSessions();
                else
                    OpenSessions();
            },
            null,
            OpenDashboardAction);
        CurrentPage = vm;
    }

    private void OpenSessionEdit(Session session)
    {
        CurrentBreadcrumb = "Accueil / Sessions / Modification";
        CurrentPageTitle = "Modifier la Session";
        UpdateSidebar("Sessions", "Modifier session");

        var vm = new SessionFormViewModel(
            _sessionService,
            _formationService,
            (bool saved) =>
            {
                if (saved)
                    OpenSessions();
                else
                    OpenSessionDetail(session.IdSession);
            },
            session.IdSession,
            OpenDashboardAction);
        CurrentPage = vm;
    }

    // --- Evaluation navigation ---
    private void OpenEvaluationPasser(int idQuestionnaire)
    {
        CurrentBreadcrumb = "Accueil / Évaluations / Passer l'Évaluation";
        CurrentPageTitle = "Formulaire d'Évaluation";
        UpdateSidebar("Evaluations", "Passer évaluation");

        var vm = new EvaluationPasserViewModel(
            _evaluationService,
            _questionnaireService,
            (int idEvaluation) => OpenEvaluationResultat(idEvaluation),
            OpenEvaluations);
        CurrentPage = vm;
        _ = vm.InitializeAsync(idQuestionnaire, CurrentUserId);
    }

    private void OpenEvaluationResultat(int idEvaluation)
    {
        CurrentBreadcrumb = "Accueil / Évaluations / Résultat";
        CurrentPageTitle = "Résultat de l'Évaluation";
        UpdateSidebar("Evaluations", "Résultat évaluation");

        var vm = new EvaluationResultatViewModel(
            _evaluationService,
            OpenEvaluations);
        CurrentPage = vm;
        _ = vm.InitializeAsync(idEvaluation);
    }

    // --- Questionnaire navigation ---
    private void OpenQuestionnaireEditor(int? idQuestionnaire)
    {
        CurrentBreadcrumb = idQuestionnaire.HasValue ? "Accueil / Questionnaires / Édition & Questions" : "Accueil / Questionnaires / Nouveau Questionnaire";
        CurrentPageTitle = idQuestionnaire.HasValue ? "Éditeur de Questionnaire" : "Créer un Questionnaire";
        UpdateSidebar("Questionnaires", "Éditeur questionnaire");

        var vm = new QuestionnaireEditorViewModel(
            _questionnaireService,
            _sessionService,
            (bool saved) =>
            {
                if (saved)
                    OpenQuestionnaires();
                else
                    OpenQuestionnaires();
            },
            idQuestionnaire,
            OpenDashboardAction);
        CurrentPage = vm;
    }

    // --- Rapport navigation ---
    private void OpenRapport()
    {
        CurrentBreadcrumb = "Accueil / Statistiques / Rapport Officiel";
        CurrentPageTitle = "Rapport Officiel SEFAD";
        UpdateSidebar("Statistiques", "Rapport officiel");

        var vm = new RapportViewModel(
            _statistiqueService,
            _formationService,
            OpenStatistiques);
        CurrentPage = vm;
    }

    private void Logout()
    {
        IsLoggedIn = false;
        CurrentUserRole = string.Empty;
        CurrentUserName = string.Empty;
        CurrentUserEmail = string.Empty;
        CurrentPage = new LoginViewModel(_authService, (role, email) => OpenDashboard(role, email));
    }
}

