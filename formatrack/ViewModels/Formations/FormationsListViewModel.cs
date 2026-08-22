using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Formations;

public partial class FormationsListViewModel : ViewModelBase
{
    private readonly IFormationService _formationService;
    private readonly Action _backToDashboard;

    [ObservableProperty] private string _recherche = string.Empty;
    [ObservableProperty] private string _message = "Chargement...";

    public ObservableCollection<Formation> Formations { get; } = new();

    public FormationsListViewModel(IFormationService formationService, Action backToDashboard)
    {
        _formationService = formationService;
        _backToDashboard = backToDashboard;
        _ = LoadAsync();
    }

    partial void OnRechercheChanged(string value)
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Formations.Clear();
        foreach (var formation in await _formationService.GetFormationsAsync(Recherche))
            Formations.Add(formation);

        Message = Formations.Count == 0 ? "Aucune formation trouvee." : $"{Formations.Count} formation(s)";
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync();
    [RelayCommand] private void BackToDashboard() => _backToDashboard();
}
