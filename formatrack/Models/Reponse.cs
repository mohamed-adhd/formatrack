namespace formatrack.Models;

public class Reponse
{
    public int IdReponse { get; set; }
    public int IdEvaluation { get; set; }
    public int IdQuestion { get; set; }
    public string Contenu { get; set; } = string.Empty;
    public bool? EstCorrecte { get; set; }
    public double? ScoreObtenu { get; set; }
    public string QuestionEnonce { get; set; } = string.Empty;
}
