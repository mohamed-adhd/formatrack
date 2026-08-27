namespace formatrack.Models;

public class Question
{
    public int IdQuestion { get; set; }
    public int IdQuestionnaire { get; set; }
    public int? IdCritere { get; set; }
    public string Enonce { get; set; } = string.Empty;
    public string TypeQuestion { get; set; } = "TexteLibre";
    public double Bareme { get; set; }
    public int Ordre { get; set; }
    public string CritereLibelle { get; set; } = string.Empty;

    private Critere? _selectedCritere;
    public Critere? SelectedCritere
    {
        get => _selectedCritere;
        set
        {
            _selectedCritere = value;
            IdCritere = value?.IdCritere;
            if (value != null)
            {
                CritereLibelle = value.Libelle;
            }
        }
    }
}
