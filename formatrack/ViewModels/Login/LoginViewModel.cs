using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly Action<string, string> _onLoginSuccess;

    [ObservableProperty] private string _identifiant = "admin@sefad.local";
    [ObservableProperty] private string _motDePasse = "admin123";
    [ObservableProperty] private string _messageErreur = string.Empty;
    [ObservableProperty] private bool _isErrorVisible;
    [ObservableProperty] private bool _isPasswordVisible;

    public char PasswordChar => IsPasswordVisible ? '\0' : '●';
    public string EyeIcon => IsPasswordVisible ? "Masquer" : "Afficher";

    public LoginViewModel(IAuthService authService, Action<string, string>? onLoginSuccess = null)
    {
        _authService = authService;
        _onLoginSuccess = onLoginSuccess ?? ((_, _) => { });
    }

    public LoginViewModel(IAuthService authService, Action<string> onLoginSuccessLegacy)
    {
        _authService = authService;
        _onLoginSuccess = (role, _) => onLoginSuccessLegacy(role);
    }

    partial void OnIsPasswordVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordChar));
        OnPropertyChanged(nameof(EyeIcon));
    }

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void FillAdmin()
    {
        Identifiant = "admin@sefad.local";
        MotDePasse = "admin123";
        IsErrorVisible = false;
    }

    [RelayCommand]
    private void FillFormateur()
    {
        Identifiant = "formatrice@sefad.local";
        MotDePasse = "admin123";
        IsErrorVisible = false;
    }

    [RelayCommand]
    private void FillStagiaire()
    {
        Identifiant = "stagiaire@sefad.local";
        MotDePasse = "admin123";
        IsErrorVisible = false;
    }

    [RelayCommand]
    private async Task SeConnecterAsync()
    {
        IsErrorVisible = false;
        var role = await _authService.AuthentifierAsync(Identifiant, MotDePasse);

        if (role is null)
        {
            MessageErreur = "Identifiant ou mot de passe incorrect.";
            IsErrorVisible = true;
            return;
        }

        _onLoginSuccess(role, Identifiant);
    }

    [RelayCommand]
    private void Quitter() => Environment.Exit(0);
}

