using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface ISuggestionAideRepository
{
    Task<IReadOnlyList<SuggestionAide>> GetAllAsync();
    Task<IReadOnlyList<SuggestionAide>> GetUnreadAsync();
    Task MarkAsReadAsync(int id);
    Task MarkAllAsReadAsync();
}
