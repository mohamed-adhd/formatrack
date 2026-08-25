using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

/// <summary>
/// Client HTTP vers le module d'aide a la decision (service Flask du PFE).
/// Tous les appels tolerant les erreurs reseau : en cas d'echec le resultat est vide/null
/// afin de ne pas faire planter l'UI tant que le service analytique n'est pas deploye.
/// </summary>
public class DecisionSupportApiService : IDecisionSupportApiService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;
    private string? _token;

    public DecisionSupportApiService(string? baseUrl = null)
    {
        baseUrl ??= Environment.GetEnvironmentVariable("SEFAD_ANALYTICS_URL") ?? "http://localhost:5000";
        _client = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute), Timeout = TimeSpan.FromSeconds(5) };
    }

    public void SetToken(string? token) => _token = token;

    public async Task<IReadOnlyList<FormationDecision>> RecommanderFormationsAsync(int idFormation)
    {
        var json = await EnvoyerAsync(HttpMethod.Get, $"/api/recommandations/{idFormation}");
        if (json is null) return Array.Empty<FormationDecision>();

        try
        {
            return ExtraireTableau(json).Select(el => new FormationDecision(
                Titre: Str(el, "titre"),
                Priorite: Str(el, "priorite"),
                Score: Num(el, "score"),
                Justification: Str(el, "justification"))).ToList();
        }
        catch
        {
            return Array.Empty<FormationDecision>();
        }
    }

    public async Task<StatistiqueFormationDetail?> GetStatistiquesFormationAsync(int idFormation)
    {
        var json = await EnvoyerAsync(HttpMethod.Get, $"/api/stats/formation/{idFormation}");
        return json is null ? null : ParseObjet(json, el => new StatistiqueFormationDetail(
            Formation: Str(el, "formation"),
            EvaluationsTerminees: Int(el, "evaluations_terminees"),
            Participants: Int(el, "participants"),
            Moyenne: Num(el, "moyenne"),
            Mediane: Num(el, "mediane"),
            EcartType: Num(el, "ecart_type"),
            TauxReussite: Num(el, "taux_reussite")));
    }

    public async Task<IndicateursGlobaux?> GetIndicateursGlobauxAsync()
    {
        var json = await EnvoyerAsync(HttpMethod.Get, "/api/stats/global");
        return json is null ? null : ParseObjet(json, el => new IndicateursGlobaux(
            Formations: Int(el, "formations"),
            Sessions: Int(el, "sessions"),
            MoyenneGlobale: Num(el, "moyenne_globale"),
            TauxReussiteGlobale: Num(el, "taux_reussite_globale")));
    }

    public async Task<IReadOnlyList<PointTendance>> AnalyserTendancesAsync(int? idFormation = null, DateTime? debut = null, DateTime? fin = null)
    {
        var corps = new
        {
            id_formation = idFormation,
            debut = debut?.ToString("yyyy-MM-dd"),
            fin = fin?.ToString("yyyy-MM-dd")
        };
        var json = await EnvoyerAsync(HttpMethod.Post, "/api/analyse/tendances", corps);
        if (json is null) return Array.Empty<PointTendance>();

        try
        {
            return ExtraireTableau(json).Select(el => new PointTendance(
                Periode: Str(el, "periode"),
                Moyenne: Num(el, "moyenne"))).ToList();
        }
        catch
        {
            return Array.Empty<PointTendance>();
        }
    }

    public async Task<ScoreMulticritere?> ScoreMulticriteresAsync(IReadOnlyDictionary<string, double> poids, IReadOnlyDictionary<string, double> scores)
    {
        var json = await EnvoyerAsync(HttpMethod.Post, "/api/scoring/multicriteres", new { poids, scores });
        return json is null ? null : ParseObjet(json, el => new ScoreMulticritere(
            Score: Num(el, "score"),
            Coherence: Num(el, "coherence")));
    }

    public async Task<RapportGenererResultat?> GenererRapportAsync(string titre, string typeRapport, IReadOnlyDictionary<string, object?>? parametres = null)
    {
        var json = await EnvoyerAsync(HttpMethod.Post, "/api/rapport/generer", new { titre, type_rapport = typeRapport, parametres });
        return json is null ? null : ParseObjet(json, el => new RapportGenererResultat(
            TypeRapport: Str(el, "type_rapport"),
            Titre: Str(el, "titre"),
            CheminFichier: Str(el, "chemin_fichier"),
            DateGeneration: DateTime.TryParse(Str(el, "date_generation"), out var d) ? d : DateTime.Now));
    }

    private async Task<string?> EnvoyerAsync(HttpMethod method, string chemin, object? corps = null)
    {
        try
        {
            using var requete = new HttpRequestMessage(method, chemin);
            if (!string.IsNullOrWhiteSpace(_token))
                requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            if (corps is not null)
                requete.Content = JsonContent.Create(corps, options: Json);

            using var reponse = await _client.SendAsync(requete);
            if (!reponse.IsSuccessStatusCode)
                return null;
            return await reponse.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SEFAD] Appel API impossible: {ex.Message}");
            return null;
        }
    }

    private static List<JsonElement> ExtraireTableau(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return new List<JsonElement>();
        return doc.RootElement.EnumerateArray().ToList();
    }

    private static T? ParseObjet<T>(string json, Func<JsonElement, T> read) where T : class
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;
            return read(doc.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : string.Empty;

    private static double Num(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0d;

    private static int Int(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var v))
        {
            if (v.ValueKind == JsonValueKind.Number) return v.GetInt32();
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var i)) return i;
        }
        return 0;
    }
}