using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Utilisateurs;

public partial class UtilisateursListViewModel : ViewModelBase
{
    private readonly IUtilisateurService _utilisateurService;
    private readonly Action _openDashboard;
    private readonly Action<Utilisateur> _openUtilisateurDetail;
    private readonly Action<Utilisateur?> _openUtilisateurForm;

    [ObservableProperty] private string _recherche = string.Empty;
    [ObservableProperty] private string _message = "Chargement...";

    public ObservableCollection<Utilisateur> Utilisateurs { get; } = new();

    public UtilisateursListViewModel(
        IUtilisateurService utilisateurService,
        Action openDashboard,
        Action<Utilisateur> openUtilisateurDetail,
        Action<Utilisateur?> openUtilisateurForm,
        Action openFormations,
        Action openSessions,
        Action openEvaluations,
        Action openQuestionnaires,
        Action openStatistiques)
    {
        _utilisateurService = utilisateurService;
        _openDashboard = openDashboard;
        _openUtilisateurDetail = openUtilisateurDetail;
        _openUtilisateurForm = openUtilisateurForm;
        _ = LoadAsync();
    }

    partial void OnRechercheChanged(string value)
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Utilisateurs.Clear();
        var list = await _utilisateurService.GetUtilisateursAsync();
        if (!string.IsNullOrWhiteSpace(Recherche))
        {
            var term = Recherche.ToLowerInvariant();
            foreach (var u in list)
                if (u.Nom.ToLowerInvariant().Contains(term) ||
                    u.Prenom.ToLowerInvariant().Contains(term) ||
                    u.Email.ToLowerInvariant().Contains(term) ||
                    u.Role.ToLowerInvariant().Contains(term))
                    Utilisateurs.Add(u);
        }
        else
        {
            foreach (var u in list)
                Utilisateurs.Add(u);
        }

        Message = Utilisateurs.Count == 0 ? "Aucun utilisateur trouvé." : $"{Utilisateurs.Count} utilisateur(s)";
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private void OpenUtilisateurDetail(Utilisateur? utilisateur)
    {
        if (utilisateur == null) return;
        _openUtilisateurDetail(utilisateur);
    }

    [RelayCommand]
    private void EditUtilisateur(Utilisateur? utilisateur)
    {
        _openUtilisateurForm(utilisateur);
    }

    [RelayCommand]
    private async Task DeleteUtilisateur(Utilisateur utilisateur)
    {
        await _utilisateurService.SupprimerUtilisateurAsync(utilisateur.IdUtilisateur);
        await LoadAsync();
    }

    [RelayCommand]
    private void AddUtilisateur()
    {
        _openUtilisateurForm(null);
    }

    [RelayCommand]
    private void OpenDashboard() => _openDashboard();
}
