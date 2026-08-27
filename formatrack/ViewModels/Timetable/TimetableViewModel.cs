using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Models;
using formatrack.Services;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Timetable;

public partial class TimetableViewModel : ViewModelBase
{
    private readonly IEmploiDuTempsService _emploiService;
    private readonly IFormationService _formationService;
    private readonly IUtilisateurService _utilisateurService;
    private bool _suppressToggle;

    [ObservableProperty] private string _role = "";
    [ObservableProperty] private string _departement = "";
    [ObservableProperty] private string _promotion = "";
    [ObservableProperty] private int _userId;
    [ObservableProperty] private bool _isMobile;
    [ObservableProperty] private string _message = "Chargement...";

    public bool IsStagiaireView => Role == "Stagiaire";
    public bool IsFormateurView => Role == "Formateur";
    public bool IsChefDepView => Role == "ChefDepartement";
    public bool IsAdminView => Role == "Administrateur";
    public bool IsResponsableView => Role == "ResponsableFormation";
    public bool IsDecideurView => Role == "Decideur";
    public bool CanManage => Role is "Administrateur" or "ResponsableFormation";

    [ObservableProperty] private bool _showWeekly = true;
    [ObservableProperty] private bool _showYearly;

    [ObservableProperty] private EmploiDuTemps? _selectedEmploi;
    [ObservableProperty] private string _selectedImagePath = "";
    [ObservableProperty] private double _zoomLevel = 1.0;

    [ObservableProperty] private Formation? _selectedFormation;
    [ObservableProperty] private string _selectedTypeEmploi = "Hebdomadaire";
    [ObservableProperty] private string _selectedAnnee = "1ère année";
    [ObservableProperty] private string _uploadDescription = "";
    [ObservableProperty] private string _uploadError = "";
    [ObservableProperty] private bool _isUploadErrorVisible;
    [ObservableProperty] private string? _pendingImagePath;

    public ObservableCollection<EmploiDuTemps> EmploisList { get; } = new();
    public ObservableCollection<EmploiDuTemps> FilteredEmplois { get; } = new();
    public ObservableCollection<Formation> Formations { get; } = new();

    public List<string> TypesEmploi { get; } = new() { "Hebdomadaire", "Annuel" };
    public List<string> Annees { get; } = new() { "1ère année", "2ème année", "3ème année", "4ème année" };

    public TimetableViewModel(
        IEmploiDuTempsService? emploiService = null,
        IFormationService? formationService = null,
        IUtilisateurService? utilisateurService = null,
        string role = "", string departement = "", string promotion = "", int userId = 0)
    {
        _emploiService = emploiService ?? new EmploiDuTempsService();
        _formationService = formationService ?? new FormationService();
        _utilisateurService = utilisateurService ?? new UtilisateurService();
        Role = role;
        Departement = departement;
        Promotion = promotion;
        UserId = userId;
        _ = LoadAsync();
    }

    partial void OnShowWeeklyChanged(bool value)
    {
        if (_suppressToggle) return;
        _suppressToggle = true;
        ShowYearly = !value;
        FilterByType();
        _suppressToggle = false;
    }

    partial void OnShowYearlyChanged(bool value)
    {
        if (_suppressToggle) return;
        _suppressToggle = true;
        ShowWeekly = !value;
        FilterByType();
        _suppressToggle = false;
    }

    partial void OnSelectedEmploiChanged(EmploiDuTemps? value)
    {
        if (value != null && !string.IsNullOrEmpty(value.CheminImage) && File.Exists(value.CheminImage))
            SelectedImagePath = value.CheminImage;
        else
            SelectedImagePath = "";
    }

    private async Task LoadAsync()
    {
        try
        {
            if (CanManage)
            {
                var formations = await _formationService.GetFormationsAsync();
                if (formations != null)
                {
                    Formations.Clear();
                    foreach (var f in formations)
                        Formations.Add(f);
                }
            }

            var emplois = await _emploiService.GetByRoleAsync(Role, Departement, Promotion, UserId);

            if (emplois != null && emplois.Count > 0)
            {
                var allFormations = await _formationService.GetFormationsAsync();
                var formDict = allFormations?.ToDictionary(f => f.IdFormation, f => f.Titre)
                               ?? new Dictionary<int, string>();

                foreach (var e in emplois)
                {
                    if (formDict.TryGetValue(e.IdFormation, out var titre))
                        e.FormationTitre = titre;
                }
            }

            EmploisList.Clear();
            if (emplois != null)
            {
                foreach (var e in emplois)
                    EmploisList.Add(e);
            }

            FilterByType();
            Message = $"{EmploisList.Count} emploi(s) du temps disponible(s)";
        }
        catch (Exception ex)
        {
            Message = $"Erreur : {ex.Message}";
        }
    }

    private void FilterByType()
    {
        FilteredEmplois.Clear();
        var type = ShowWeekly ? "Hebdomadaire" : "Annuel";
        if (EmploisList.Count > 0)
        {
            foreach (var e in EmploisList.Where(x => x.TypeEmploi == type))
                FilteredEmplois.Add(e);
        }

        if (FilteredEmplois.Count > 0 && SelectedEmploi == null)
            SelectedEmploi = FilteredEmplois[0];
        else if (FilteredEmplois.Count == 0)
            SelectedEmploi = null;
    }

    [RelayCommand]
    private void SelectEmploi(EmploiDuTemps item) => SelectedEmploi = item;

    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(ZoomLevel + 0.25, 3.0);

    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(ZoomLevel - 0.25, 0.25);

    [RelayCommand]
    private void ResetZoom() => ZoomLevel = 1.0;

    [RelayCommand]
    private async Task PickImageAsync()
    {
        var desktop = App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var topLevel = desktop?.MainWindow;
        if (topLevel == null) return;

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionner un emploi du temps",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" } }
            }
        });

        if (result != null && result.Count > 0)
        {
            var srcPath = result[0].Path.LocalPath;
            var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "emplois_du_temps");
            Directory.CreateDirectory(assetsDir);
            var destName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(srcPath)}";
            var destPath = Path.Combine(assetsDir, destName);
            File.Copy(srcPath, destPath, true);
            PendingImagePath = destPath;
            UploadError = "";
            IsUploadErrorVisible = false;
        }
    }

    [RelayCommand]
    private async Task UploadEmploiAsync()
    {
        if (SelectedFormation == null)
        {
            UploadError = "Veuillez sélectionner une formation.";
            IsUploadErrorVisible = true;
            return;
        }
        if (string.IsNullOrEmpty(PendingImagePath))
        {
            UploadError = "Veuillez sélectionner une image.";
            IsUploadErrorVisible = true;
            return;
        }

        var emploi = new EmploiDuTemps
        {
            IdFormation = SelectedFormation.IdFormation,
            TypeEmploi = SelectedTypeEmploi,
            Annee = SelectedAnnee,
            Promotion = Promotion,
            CheminImage = PendingImagePath,
            UploadedBy = UserId,
            Statut = "Brouillon",
            Description = UploadDescription
        };

        var id = await _emploiService.AjouterAsync(emploi);
        emploi.IdEmploi = id;
        emploi.FormationTitre = SelectedFormation.Titre;
        EmploisList.Insert(0, emploi);
        FilterByType();

        PendingImagePath = null;
        UploadDescription = "";
        Message = $"Emploi du temps ajouté avec succès ({SelectedTypeEmploi} - {SelectedAnnee})";

        _ = formatrack.Services.CompositionRoot.Journal.JournalerAsync(UserId,
            $"Ajout emploi du temps : {SelectedTypeEmploi} {SelectedAnnee}",
            $"Formation: {SelectedFormation.Titre}");
    }

    [RelayCommand]
    private async Task TogglePublishAsync(EmploiDuTemps item)
    {
        if (item.Statut == "Publie")
        {
            await _emploiService.DepublierAsync(item.IdEmploi);
            item.Statut = "Brouillon";
            Message = "Emploi du temps dépublié.";
        }
        else
        {
            await _emploiService.PublierAsync(item.IdEmploi);
            item.Statut = "Publie";
            Message = "Emploi du temps publié.";
        }

        var idx = EmploisList.IndexOf(item);
        if (idx >= 0) { EmploisList.RemoveAt(idx); EmploisList.Insert(idx, item); }
        FilterByType();
    }

    [RelayCommand]
    private async Task DeleteEmploiAsync(EmploiDuTemps item)
    {
        await _emploiService.SupprimerAsync(item.IdEmploi);
        EmploisList.Remove(item);
        FilterByType();
        if (SelectedEmploi == item)
            SelectedEmploi = FilteredEmplois.FirstOrDefault();
        Message = "Emploi du temps supprimé.";

        _ = formatrack.Services.CompositionRoot.Journal.JournalerAsync(UserId,
            $"Suppression emploi du temps #{item.IdEmploi}",
            $"{item.TypeEmploi} - {item.Annee}");
    }
}
