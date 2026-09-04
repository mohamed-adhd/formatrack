using System;

namespace formatrack.Models;

public class Note
{
    public int IdNote { get; set; }
    public int IdStagiaire { get; set; }
    public int IdModule { get; set; }
    public int IdSession { get; set; }
    public double NoteValeur { get; set; }
    public DateTime DateSaisie { get; set; } = DateTime.Now;
    public int SaisiPar { get; set; }

    // Joined fields
    public string StagiaireNom { get; set; } = string.Empty;
    public string ModuleTitre { get; set; } = string.Empty;
    public double ModuleCoefficient { get; set; } = 1.0;
    public string SessionTitre { get; set; } = string.Empty;

    // Admin view joined fields
    public int IdFormation { get; set; }
    public string FormationTitre { get; set; } = string.Empty;
    public string Departement { get; set; } = string.Empty;
    public string Promotion { get; set; } = string.Empty;
}
