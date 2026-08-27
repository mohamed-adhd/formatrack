using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Utilisateurs;

public partial class UtilisateurFormViewModel : ViewModelBase
{
    private readonly IUtilisateurService _utilisateurService;
    private readonly Action<bool> _onCompleted;
    private readonly int? _idUtilisateur;
    private readonly Action _openDashboard;

    [ObservableProperty] private string _nom = string.Empty;
    [ObservableProperty] private string _prenom = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _motDePasse = string.Empty;
    [ObservableProperty] private string _role = string.Empty;
    [ObservableProperty] private string _departement = string.Empty;
    [ObservableProperty] private string _promotion = string.Empty;
    [ObservableProperty] private bool _actif = true;
    [ObservableProperty] private string _message = string.Empty;

    public ObservableCollection<string> RoleOptions { get; } = new()
    {
        "Administrateur",
        "ResponsableFormation",
        "ChefDepartement",
        "Formateur",
        "Stagiaire",
        "Decideur"
    };

    public ObservableCollection<string> DepartementOptions { get; } = new()
    {
        "Air",
        "Mer",
        "Terre"
    };

    public UtilisateurFormViewModel(
        IUtilisateurService utilisateurService,
        Action<bool> onCompleted,
        int? idUtilisateur = null,
        Action? openDashboard = null)
    {
        _utilisateurService = utilisateurService;
        _onCompleted = onCompleted;
        _idUtilisateur = idUtilisateur;
        _openDashboard = openDashboard ?? (() => { });
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_idUtilisateur.HasValue)
        {
            var user = await _utilisateurService.GetUtilisateurAsync(_idUtilisateur.Value);
            if (user != null)
            {
                Nom = user.Nom;
                Prenom = user.Prenom;
                Email = user.Email;
                Role = user.Role;
                Departement = user.Departement;
                Promotion = user.Promotion;
                Actif = user.Actif;
                Message = "Modification d'utilisateur";
            }
            else
            {
                Message = "Utilisateur non trouvé";
            }
        }
        else
        {
            Message = "Nouvel utilisateur";
            Role = "Stagiaire";
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Nom) || string.IsNullOrWhiteSpace(Prenom) ||
                string.IsNullOrWhiteSpace(Email))
            {
                Message = "Les champs Nom, Prenom et Email sont obligatoires";
                return;
            }

            if (!_idUtilisateur.HasValue && string.IsNullOrWhiteSpace(MotDePasse))
            {
                Message = "Le mot de passe est obligatoire pour un nouvel utilisateur";
                return;
            }

            var user = new Utilisateur
            {
                IdUtilisateur = _idUtilisateur ?? 0,
                Nom = Nom.Trim(),
                Prenom = Prenom.Trim(),
                Email = Email.Trim(),
                Role = string.IsNullOrWhiteSpace(Role) ? "Stagiaire" : Role.Trim(),
                Departement = Departement?.Trim() ?? string.Empty,
                Promotion = Promotion?.Trim() ?? string.Empty,
                Actif = Actif
            };

            int result = await _utilisateurService.EnregistrerUtilisateurAsync(
                user,
                string.IsNullOrWhiteSpace(MotDePasse) ? null : MotDePasse);

            if (result > 0)
            {
                var actionStr = _idUtilisateur.HasValue ? $"Modification de l'utilisateur {user.Prenom} {user.Nom}" : $"Création de l'utilisateur {user.Prenom} {user.Nom}";
                await formatrack.Services.CompositionRoot.Journal.JournalerAsync(null, actionStr, $"ID: {result}, Email: {user.Email}, Rôle: {user.Role}");
                _onCompleted?.Invoke(true);
            }
            else
            {
                Message = "Erreur lors de l'enregistrement";
            }
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => _onCompleted?.Invoke(false);

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}
