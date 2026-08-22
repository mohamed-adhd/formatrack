using System;

namespace formatrack.Models;

public class Session
{
    public int IdSession { get; set; }
    public int IdFormation { get; set; }
    public string TitreFormation { get; set; } = string.Empty;
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public string Lieu { get; set; } = string.Empty;
    public int Capacite { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string Periode => $"{DateDebut:dd/MM/yyyy} - {DateFin:dd/MM/yyyy}";
}
