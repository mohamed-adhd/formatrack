namespace formatrack.Services.Interfaces;

public record FormationDecision(string Titre, string Priorite, double Score, string Justification);

public interface IDecisionSupportApiService
{
    Task<IReadOnlyList<FormationDecision>> RecommanderFormationsAsync();
}
