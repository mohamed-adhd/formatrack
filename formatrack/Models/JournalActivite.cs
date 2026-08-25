using System;

namespace formatrack.Models;

public class JournalActivite
{
    public int IdJournal { get; set; }
    public int? IdUtilisateur { get; set; }
    public string UtilisateurNom { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime DateAction { get; set; } = DateTime.Now;
}