using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace formatrack.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _identifiant = string.Empty;

    [ObservableProperty]
    private string _motDePasse = string.Empty;

    [ObservableProperty]
    private string _messageErreur = string.Empty;

    [ObservableProperty]
    private bool _isErrorVisible;

    [ObservableProperty]
    private bool _isPasswordVisible;

    // Default masked with "*"; becomes '\0' (no masking) when toggled.
    public char PasswordChar => IsPasswordVisible ? '\0' : '*';

    // Simple text glyph for the toggle button (swap for an icon font/SVG later if you want).
    public string EyeIcon => IsPasswordVisible ? "🙈" : "👁";

    partial void OnIsPasswordVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordChar));
        OnPropertyChanged(nameof(EyeIcon));
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private void SeConnecter()
    {
        // TODO: brancher sur AuthService.
        IsErrorVisible = true;
        MessageErreur = "Identifiant ou mot de passe incorrect.";
    }

    [RelayCommand]
    private void CreerCompte()
    {
        // TODO: naviguer vers l'écran d'inscription.
    }

    [RelayCommand]
    private void Quitter()
    {
        // TODO: fermer l'application (ex: Environment.Exit(0) ou fermeture de la fenêtre).
    }
}
