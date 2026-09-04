using CommunityToolkit.Mvvm.ComponentModel;

namespace formatrack.ViewModels.Grades;

public partial class AdminNoteRow : ObservableObject
{
    public int IdNote { get; set; }
    public string StagiaireNom { get; set; } = "";
    public string ModuleTitre { get; set; } = "";
    public double Coefficient { get; set; }
    public double NoteValeur { get; set; }
    public double NotePonderee { get; set; }
    public string SessionTitre { get; set; } = "";
    public string Promotion { get; set; } = "";
    public string Departement { get; set; } = "";
    public string FormationTitre { get; set; } = "";
}
