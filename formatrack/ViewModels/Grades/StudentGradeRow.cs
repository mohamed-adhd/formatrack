using CommunityToolkit.Mvvm.ComponentModel;

namespace formatrack.ViewModels.Grades;

public partial class StudentGradeRow : ObservableObject
{
    public int IdStagiaire { get; set; }
    public string NomComplet { get; set; } = "";
    [ObservableProperty] private double _noteValeur;
    public bool HasExistingNote { get; set; }
    public int IdNote { get; set; }
    public double Coefficient { get; set; }
}
