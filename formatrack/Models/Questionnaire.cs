using System;

namespace formatrack.Models;

public class Questionnaire
{
    public int IdQuestionnaire { get; set; }
    public int IdSession { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TypeEvaluation { get; set; } = "AChaud";
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public string Statut { get; set; } = "Brouillon";
    public string SessionTitre { get; set; } = string.Empty;
    public int NombreQuestions { get; set; }
}
