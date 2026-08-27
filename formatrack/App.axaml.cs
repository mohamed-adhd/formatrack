using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using formatrack.Data;
using formatrack.Services;
using formatrack.ViewModels;
using formatrack.Views;
using formatrack.Services.Interfaces;
namespace formatrack;

public partial class App : Application
{
    public static IAuthService authService = new AuthService();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _ = AppDbContext.InitializeAsync();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(authService),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}