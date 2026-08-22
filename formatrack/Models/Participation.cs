using System;

namespace formatrack.Models;

public class Participation
{
    public int IdParticipation { get; set; }
    public int IdUtilisateur { get; set; }
    public int IdSession { get; set; }
    public string RoleParticipation { get; set; } = "Stagiaire";
    public DateTime DateInscription { get; set; } = DateTime.Now;
    public string UtilisateurNom { get; set; } = string.Empty;
    public string SessionTitre { get; set; } = string.Empty;
}
