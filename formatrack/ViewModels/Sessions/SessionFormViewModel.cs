using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Sessions;

public partial class SessionFormViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly IFormationService _formationService;
    private readonly Action<bool> _onCompleted;
    private readonly int? _idSession;
    private readonly Action _openDashboard;

    [ObservableProperty] private int _idFormation;
    [ObservableProperty] private string _titreFormation = string.Empty;
    [ObservableProperty] private Formation? _selectedFormation;
    [ObservableProperty] private DateTimeOffset? _dateDebutOffset = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset? _dateFinOffset = DateTimeOffset.Now.AddDays(7);
    [ObservableProperty] private string _lieu = "Amphithéâtre Moncey - EMS";
    [ObservableProperty] private int _capacite = 30;
    [ObservableProperty] private string _statut = "Planifiee";
    [ObservableProperty] private string _message = string.Empty;

    public ObservableCollection<Formation> Formations { get; } = new();

    public ObservableCollection<string> StatutOptions { get; } = new()
    {
        "Planifiee",
        "En cours",
        "Terminee",
        "Annulee"
    };

    public SessionFormViewModel(
        ISessionService sessionService,
        IFormationService formationService,
        Action<bool> onCompleted,
        int? idSession = null,
        Action? openDashboard = null)
    {
        _sessionService = sessionService;
        _formationService = formationService;
        _onCompleted = onCompleted;
        _idSession = idSession;
        _openDashboard = openDashboard ?? (() => { });
        _ = InitializeAsync();
    }

    partial void OnSelectedFormationChanged(Formation? value)
    {
        if (value != null)
        {
            IdFormation = value.IdFormation;
            TitreFormation = value.Titre;
        }
    }

    private async Task InitializeAsync()
    {
        var formations = await _formationService.GetFormationsAsync();
        Formations.Clear();
        foreach (var f in formations)
            Formations.Add(f);

        if (_idSession.HasValue)
        {
            var session = await _sessionService.GetSessionAsync(_idSession.Value);
            if (session != null)
            {
                IdFormation = session.IdFormation;
                TitreFormation = session.TitreFormation;
                SelectedFormation = Formations.FirstOrDefault(f => f.IdFormation == IdFormation);
                DateDebutOffset = new DateTimeOffset(session.DateDebut);
                DateFinOffset = new DateTimeOffset(session.DateFin);
                Lieu = session.Lieu;
                Capacite = session.Capacite;
                Statut = session.Statut;
                Message = "Modification de la Session";
            }
            else
            {
                Message = "Session non trouvée";
            }
        }
        else
        {
            Message = "Planification d'une Nouvelle Session";
            Statut = "Planifiee";
            if (Formations.Count > 0)
            {
                SelectedFormation = Formations[0];
            }
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            if (SelectedFormation != null)
            {
                IdFormation = SelectedFormation.IdFormation;
                TitreFormation = SelectedFormation.Titre;
            }

            if (IdFormation <= 0)
            {
                Message = "Veuillez sélectionner une formation";
                return;
            }

            var session = new Session
            {
                IdSession = _idSession ?? 0,
                IdFormation = IdFormation,
                TitreFormation = TitreFormation.Trim(),
                DateDebut = DateDebutOffset?.DateTime ?? DateTime.Today,
                DateFin = DateFinOffset?.DateTime ?? DateTime.Today.AddDays(7),
                Lieu = string.IsNullOrWhiteSpace(Lieu) ? "École d'État-Major" : Lieu.Trim(),
                Capacite = Capacite <= 0 ? 25 : Capacite,
                Statut = string.IsNullOrWhiteSpace(Statut) ? "Planifiee" : Statut.Trim()
            };

            int result = await _sessionService.EnregistrerSessionAsync(session);
            if (result > 0)
                _onCompleted?.Invoke(true);
            else
                Message = "Erreur lors de l'enregistrement";
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

