using Avalonia.Controls;
using Avalonia.Interactivity;

namespace formatrack.Views;

public partial class LoginView : UserControl
{
    private const int DefaultWidth = 1280;
    private const int DefaultHeight = 720;

    private bool _isFullScreen;

    public LoginView()
    {
        InitializeComponent();
    }

    private void OnToggleFullScreenClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window is null)
            return;

        var button = sender as Button;

        if (!_isFullScreen)
        {
            window.WindowState = WindowState.FullScreen;
            if (button is not null)
                button.Content = "Quitter le plein écran";
        }
        else
        {
            window.WindowState = WindowState.Normal;
            window.Width = DefaultWidth;
            window.Height = DefaultHeight;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (button is not null)
                button.Content = "Plein écran";
        }

        _isFullScreen = !_isFullScreen;
    }
}
