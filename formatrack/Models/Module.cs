namespace formatrack.Models;

public class Module
{
    public int IdModule { get; set; }
    public int IdFormation { get; set; }
    public string Titre { get; set; } = string.Empty;
    public int CreditHoraire { get; set; }
    public int NbExamen { get; set; }
    public double Coefficient { get; set; } = 1.0;
    public bool EstCommum { get; set; }
    public string FormationTitre { get; set; } = string.Empty;
}
