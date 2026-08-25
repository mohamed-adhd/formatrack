using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Data.Repositories;

public interface IJournalActiviteRepository : IRepository<JournalActivite>
{
    Task<IReadOnlyList<JournalActivite>> GetRecentsAsync(int limite = 200);
    Task<IReadOnlyList<JournalActivite>> GetByUtilisateurAsync(int idUtilisateur, int limite = 200);
    Task<IReadOnlyList<JournalActivite>> GetDepuisAsync(DateTime depuis, int limite = 200);
}