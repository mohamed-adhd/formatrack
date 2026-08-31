using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface ISuggestionAideService
{
    Task<IReadOnlyList<SuggestionAide>> GetAllSuggestionsAsync();
    Task<IReadOnlyList<SuggestionAide>> GetUnreadSuggestionsAsync();
    Task<int> RunEngineAsync();
    Task MarkAsReadAsync(int id);
    Task MarkAllAsReadAsync();
}
