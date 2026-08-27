using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Utilisateurs;

public partial class UtilisateurDetailViewModel : ViewModelBase
{
    private readonly IUtilisateurService _utilisateurService;
    private readonly Action _backToList;
    private readonly Action<Utilisateur?> _editUtilisateur;
    private readonly Action _openDashboard;

    [ObservableProperty] private Utilisateur? _utilisateur;
    [ObservableProperty] private string _message = "Chargement...";

    public UtilisateurDetailViewModel(
        IUtilisateurService utilisateurService,
        Action backToList,
        Action<Utilisateur?> editUtilisateur,
        Action openDashboard)
    {
        _utilisateurService = utilisateurService;
        _backToList = backToList;
        _editUtilisateur = editUtilisateur;
        _openDashboard = openDashboard;
    }

    public async Task InitializeAsync(int idUtilisateur)
    {
        try
        {
            Utilisateur = await _utilisateurService.GetUtilisateurAsync(idUtilisateur);
            Message = Utilisateur == null ? "Utilisateur non trouvé." : string.Empty;
        }
        catch (Exception ex)
        {
            Message = $"Erreur de chargement : {ex.Message}";
        }
    }

    [RelayCommand]
    private void Edit()
    {
        if (Utilisateur != null)
            _editUtilisateur(Utilisateur);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (Utilisateur != null)
        {
            var ok = await _utilisateurService.SupprimerUtilisateurAsync(Utilisateur.IdUtilisateur);
            if (ok)
            {
                await formatrack.Services.CompositionRoot.Journal.JournalerAsync(null, $"Suppression de l'utilisateur {Utilisateur.Prenom} {Utilisateur.Nom}", $"ID: {Utilisateur.IdUtilisateur}, Rôle: {Utilisateur.Role}");
            }
            _backToList();
        }
    }

    [RelayCommand]
    private void BackToList() => _backToList();

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}
