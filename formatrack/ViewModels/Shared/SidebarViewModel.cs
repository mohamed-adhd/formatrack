using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace formatrack.ViewModels.Shared;

public partial class SidebarViewModel : ViewModelBase
{
    private readonly Action _openDashboard;
    private readonly Action _openUtilisateurs;
    private readonly Action _openFormations;
    private readonly Action _openSessions;
    private readonly Action _openEvaluations;
    private readonly Action _openQuestionnaires;
    private readonly Action _openStatistiques;
    private readonly Action _logout;

    [ObservableProperty] private string _role = "Administrateur";
    [ObservableProperty] private string _activePage = "Dashboard";
    [ObservableProperty] private string _subTitle = "Tableau de bord";
    [ObservableProperty] private string _userName = "Administrateur";
    [ObservableProperty] private string _userEmail = "admin@sefad.local";
    [ObservableProperty] private string _userInitials = "AD";

    public bool IsDashboardActive => ActivePage == "Dashboard";
    public bool IsFormationsActive => ActivePage == "Formations";
    public bool IsSessionsActive => ActivePage == "Sessions";
    public bool IsUtilisateursActive => ActivePage == "Utilisateurs";
    public bool IsQuestionnairesActive => ActivePage == "Questionnaires";
    public bool IsEvaluationsActive => ActivePage == "Evaluations";
    public bool IsStatistiquesActive => ActivePage == "Statistiques";

    public bool CanManageUsers => Role == "Administrateur" || Role == "ResponsableFormation";

    public SidebarViewModel(
        string role,
        string subTitle,
        Action openDashboard,
        Action openUtilisateurs,
        Action openFormations,
        Action openSessions,
        Action openEvaluations,
        Action openQuestionnaires,
        Action openStatistiques,
        Action logout,
        string userName = "Utilisateur",
        string userEmail = "")
    {
        Role = string.IsNullOrWhiteSpace(role) ? "Administrateur" : role;
        SubTitle = subTitle;
        _openDashboard = openDashboard;
        _openUtilisateurs = openUtilisateurs;
        _openFormations = openFormations;
        _openSessions = openSessions;
        _openEvaluations = openEvaluations;
        _openQuestionnaires = openQuestionnaires;
        _openStatistiques = openStatistiques;
        _logout = logout;

        UserName = string.IsNullOrWhiteSpace(userName) ? (Role == "Administrateur" ? "Colonel Direction" : "Utilisateur") : userName;
        UserEmail = string.IsNullOrWhiteSpace(userEmail) ? $"{Role.ToLowerInvariant()}@sefad.local" : userEmail;
        UserInitials = GetInitials(UserName);
    }

    partial void OnActivePageChanged(string value)
    {
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsFormationsActive));
        OnPropertyChanged(nameof(IsSessionsActive));
        OnPropertyChanged(nameof(IsUtilisateursActive));
        OnPropertyChanged(nameof(IsQuestionnairesActive));
        OnPropertyChanged(nameof(IsEvaluationsActive));
        OnPropertyChanged(nameof(IsStatistiquesActive));
    }

    partial void OnRoleChanged(string value)
    {
        OnPropertyChanged(nameof(CanManageUsers));
    }

    partial void OnUserNameChanged(string value)
    {
        UserInitials = GetInitials(value);
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "EM";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Length >= 2 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    [RelayCommand] private void NavigateDashboard() { ActivePage = "Dashboard"; _openDashboard(); }
    [RelayCommand] private void NavigateUtilisateurs() { ActivePage = "Utilisateurs"; _openUtilisateurs(); }
    [RelayCommand] private void NavigateFormations() { ActivePage = "Formations"; _openFormations(); }
    [RelayCommand] private void NavigateSessions() { ActivePage = "Sessions"; _openSessions(); }
    [RelayCommand] private void NavigateEvaluations() { ActivePage = "Evaluations"; _openEvaluations(); }
    [RelayCommand] private void NavigateQuestionnaires() { ActivePage = "Questionnaires"; _openQuestionnaires(); }
    [RelayCommand] private void NavigateStatistiques() { ActivePage = "Statistiques"; _openStatistiques(); }
    [RelayCommand] private void Logout() => _logout();
}

