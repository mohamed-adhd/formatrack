namespace formatrack.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainWindowViewModel  : ViewModelBase
{
    [ObservableProperty] private ViewModelBase current_page;
    [ObservableProperty] private MainWindowViewModel _main;

    public MainWindowViewModel()
    {
        Current_page = new LoginViewModel();
    }
}