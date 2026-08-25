using System;

namespace formatrack.Models;

public class Evaluation
{
    public int IdEvaluation { get; set; }
    public int IdUtilisateur { get; set; }
    public int IdQuestionnaire { get; set; }
    public DateTime? DatePassage { get; set; }
    public double? ScoreTotal { get; set; }
    public double? ScoreMaximum { get; set; }
    public double? Pourcentage { get; set; }
    public string Statut { get; set; } = "EnCours";
    public string UtilisateurNom { get; set; } = string.Empty;
    public string QuestionnaireTitre { get; set; } = string.Empty;
}
