using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

/// <summary>
/// Generation et export de rapports. Export CSV natif (separateur ';' pour Excel francais) ;
/// les exports PDF/Excel riche peuvent etre ajoutes ou delegues au module Python via l'API.
/// </summary>
public class RapportService : IRapportService
{
    private readonly IRapportRepository _repos;
    private readonly string _dossier;

    public RapportService(IRapportRepository? repos = null)
    {
        _repos = repos ?? new RapportRepository();
        _dossier = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "formatrack", "Rapports");
    }

    public async Task<Rapport> GenererRapportAsync(string titre, string typeRapport, IReadOnlyList<string> colonnes, IReadOnlyList<IReadOnlyList<object?>> lignes, int? idUtilisateur = null)
    {
        Directory.CreateDirectory(_dossier);
        var chemin = Path.Combine(_dossier, $"{Sanitize(titre)}-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        await File.WriteAllTextAsync(chemin, ToCsv(colonnes, lignes), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var rapport = new Rapport
        {
            IdUtilisateur = idUtilisateur,
            Titre = titre,
            TypeRapport = string.IsNullOrWhiteSpace(typeRapport) ? "Statistique" : typeRapport,
            Format = "CSV",
            CheminFichier = chemin,
            DateGeneration = DateTime.Now
        };
        rapport.IdRapport = await _repos.AddAsync(rapport);
        return rapport;
    }

    public async Task<IReadOnlyList<Rapport>> GetRapportsAsync()
        => await _repos.GetAllAsync();

    public async Task<bool> SupprimerRapportAsync(int idRapport)
        => await _repos.DeleteAsync(idRapport);

    private static string Sanitize(string s)
    {
        var invalides = Path.GetInvalidFileNameChars();
        var propre = new string(s.Select(c => invalides.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(propre) ? "rapport" : propre;
    }

    private static string ToCsv(IReadOnlyList<string> colonnes, IReadOnlyList<IReadOnlyList<object?>> lignes)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", colonnes.Select(Esc)));

        foreach (var ligne in lignes)
        {
            var valeurs = colonnes.Count == 0
                ? ligne
                : ligne.Take(colonnes.Count).Concat(Enumerable.Repeat<object?>(null, Math.Max(0, colonnes.Count - ligne.Count))).ToList();
            sb.AppendLine(string.Join(";", valeurs.Select(v => Esc(v?.ToString() ?? string.Empty))));
        }

        return sb.ToString();
    }

    private static string Esc(string champ)
    {
        if (champ.Contains(';') || champ.Contains('"') || champ.Contains('\n') || champ.Contains('\r'))
            return "\"" + champ.Replace("\"", "\"\"") + "\"";
        return champ;
    }
}