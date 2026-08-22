using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly Action<string> _onLoginSuccess;

    [ObservableProperty] private string _identifiant = string.Empty;
    [ObservableProperty] private string _motDePasse = string.Empty;
    [ObservableProperty] private string _messageErreur = string.Empty;
    [ObservableProperty] private bool _isErrorVisible;
    [ObservableProperty] private bool _isPasswordVisible;

    public char PasswordChar => IsPasswordVisible ? '\0' : '*';
    public string EyeIcon => IsPasswordVisible ? "Masquer" : "Voir";

    public LoginViewModel(IAuthService authService, Action<string>? onLoginSuccess = null)
    {
        _authService = authService;
        _onLoginSuccess = onLoginSuccess ?? (_ => { });
    }

    partial void OnIsPasswordVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordChar));
        OnPropertyChanged(nameof(EyeIcon));
    }

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

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

        _onLoginSuccess(role);
    }

    [RelayCommand]
    private void CreerCompte()
    {
        MessageErreur = "Creation de compte a finaliser dans le module utilisateurs.";
        IsErrorVisible = true;
    }

    [RelayCommand]
    private void Quitter() => Environment.Exit(0);
}
