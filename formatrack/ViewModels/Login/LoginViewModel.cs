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

    // Bind this to the error TextBlock's IsVisible in the view.
    [ObservableProperty]
    private bool _isErrorVisible;

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
