using CommunityToolkit.Mvvm.ComponentModel;
using formatrack.Services;
using formatrack.Services.Interfaces;
using formatrack.ViewModels.Dashboard;
using formatrack.ViewModels.Formations;

namespace formatrack.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IFormationService _formationService;
    private readonly ISessionService _sessionService;
    private readonly IStatistiqueService _statistiqueService;
    private string _role = string.Empty;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    public MainWindowViewModel() : this(new AuthService())
    {
    }

    public MainWindowViewModel(IAuthService authService)
    {
        _authService = authService;
        _formationService = new FormationService();
        _sessionService = new SessionService();
        _statistiqueService = new StatistiqueService();
        _currentPage = new LoginViewModel(_authService, OpenDashboard);
    }

    private void OpenDashboard(string role)
    {
        _role = role;
        CurrentPage = new DashboardViewModel(_statistiqueService, _sessionService, OpenFormations, Logout, _role);
    }

    private void OpenFormations()
    {
        CurrentPage = new FormationsListViewModel(_formationService, () => OpenDashboard(_role));
    }

    private void Logout()
    {
        CurrentPage = new LoginViewModel(_authService, OpenDashboard);
    }
}
