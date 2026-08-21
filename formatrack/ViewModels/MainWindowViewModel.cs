using CommunityToolkit.Mvvm.ComponentModel;

namespace formatrack.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    public MainWindowViewModel()
    {
        _currentPage = new LoginViewModel();
    }
}
