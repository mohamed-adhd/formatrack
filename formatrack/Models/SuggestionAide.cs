using System;

namespace formatrack.Models;

public class SuggestionAide
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priorite { get; set; } = 3;
    public string Categorie { get; set; } = string.Empty;
    public string ActionPage { get; set; } = string.Empty;
    public string ActionParams { get; set; } = string.Empty;
    public bool EstLu { get; set; }
    public DateTime DateGeneration { get; set; } = DateTime.Now;

    public string PrioriteLabel => Priorite switch
    {
        1 => "Critique",
        2 => "Attention",
        _ => "Info"
    };
}
