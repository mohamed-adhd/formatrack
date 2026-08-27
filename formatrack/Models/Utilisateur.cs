using System;

namespace formatrack.Models;

public class Utilisateur
{
    public int IdUtilisateur { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MotDePasseHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Stagiaire";
    public string Departement { get; set; } = string.Empty;
    public string Promotion { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public bool Actif { get; set; } = true;
    public string NomComplet => $"{Prenom} {Nom}".Trim();
}
