using System;

namespace formatrack.Models;

public class AbsenceRetard
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public int? SessionId { get; set; }
    public string Cours { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = "Absence"; // Absence or Retard
    public string Duree { get; set; } = "1 jour";
    public bool Justifiee { get; set; }
    public string Motif { get; set; } = string.Empty;
    public string Justification { get => Motif; set => Motif = value; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string StatutText => Justifiee ? "Justifiée" : (Type == "Retard" ? "Retard" : "Non justifiée");
    public bool IsAbsenceJustified => Type == "Absence" && Justifiee;
    public bool IsAbsenceUnjustified => Type == "Absence" && !Justifiee;
    public bool IsRetard => Type == "Retard";
}
