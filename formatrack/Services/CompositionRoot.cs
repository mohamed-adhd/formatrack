using formatrack.Services.Interfaces;

namespace formatrack.Services;

/// <summary>
/// Registre des dependances : chaque service est instancie par defaut avec ses repositories.
/// C'est le point de cablage des composants ; a remplacer par un conteneur DI reel si besoin.
/// </summary>
public static class CompositionRoot
{
    public static IAuthService Auth { get; } = new AuthService();
    public static IUtilisateurService Utilisateur { get; } = new UtilisateurService();
    public static IFormationService Formation { get; } = new FormationService();
    public static ISessionService Session { get; } = new SessionService();
    public static IQuestionnaireService Questionnaire { get; } = new QuestionnaireService();
    public static IEvaluationService Evaluation { get; } = new EvaluationService();
    public static IStatistiqueService Statistique { get; } = new StatistiqueService();
    public static IDialogService Dialog { get; } = new DialogService();
    public static INavigationService Navigation { get; } = new NavigationService();
    public static IDecisionSupportApiService Decision { get; } = new DecisionSupportApiService();
    public static IRapportService Rapport { get; } = new RapportService();
    public static IJournalActiviteService Journal { get; } = new JournalActiviteService();
    public static INotificationService Notification { get; } = new NotificationService();
    public static IAbsenceService Absence { get; } = new AbsenceService();
    public static IEmploiDuTempsService EmploiDuTemps { get; } = new EmploiDuTempsService();
    public static IModuleService Module { get; } = new ModuleService();
    public static INoteService Note { get; } = new NoteService();
    public static ISuggestionAideService SuggestionAide { get; } = new SuggestionAideService();
    public static IChatbotRagService ChatbotRag { get; } = new ChatbotRagService();
}