namespace formatrack.Models;

public class Formation
{
    public int IdFormation { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Objectifs { get; set; } = string.Empty;
    public int DureeHeures { get; set; }
    public string TypeFormation { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public int NombreSessions { get; set; }
    public string Duree => $"{DureeHeures} h";
}
