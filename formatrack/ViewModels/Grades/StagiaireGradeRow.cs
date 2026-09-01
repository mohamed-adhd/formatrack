using CommunityToolkit.Mvvm.ComponentModel;

namespace formatrack.ViewModels.Grades;

public partial class StagiaireGradeRow : ObservableObject
{
    public string ModuleTitre { get; set; } = "";
    [ObservableProperty] private double _noteValeur;
    public double Coefficient { get; set; }
    public int IdSession { get; set; }
    public double NotePonderee => NoteValeur * Coefficient;
}
