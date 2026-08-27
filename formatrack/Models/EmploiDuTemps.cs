using System;

namespace formatrack.Models;

public class EmploiDuTemps
{
    public int IdEmploi { get; set; }
    public int IdFormation { get; set; }
    public string TypeEmploi { get; set; } = "Hebdomadaire";
    public string Annee { get; set; } = string.Empty;
    public string Promotion { get; set; } = string.Empty;
    public string CheminImage { get; set; } = string.Empty;
    public DateTime DateUpload { get; set; } = DateTime.Now;
    public int UploadedBy { get; set; }
    public string Statut { get; set; } = "Brouillon";
    public string Description { get; set; } = string.Empty;

    // Joined fields
    public string FormationTitre { get; set; } = string.Empty;
    public string UploaderNom { get; set; } = string.Empty;
}
