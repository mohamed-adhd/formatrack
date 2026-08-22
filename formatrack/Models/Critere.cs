namespace formatrack.Models;

public class Critere
{
    public int IdCritere { get; set; }
    public int IdQuestionnaire { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Coefficient { get; set; } = 1d;
}
