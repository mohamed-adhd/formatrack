using System;

namespace formatrack.Models;

public class Rapport
{
    public int IdRapport { get; set; }
    public int? IdUtilisateur { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string TypeRapport { get; set; } = "Statistique";
    public string Format { get; set; } = "CSV";
    public string CheminFichier { get; set; } = string.Empty;
    public DateTime DateGeneration { get; set; } = DateTime.Now;
}