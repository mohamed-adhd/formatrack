using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Models;

namespace formatrack.Services.Interfaces;

public interface IJournalActiviteService
{
    Task<long> JournalerAsync(int? idUtilisateur, string action, string? details = null);
    Task<IReadOnlyList<JournalActivite>> GetActivitesAsync(int? idUtilisateur = null, DateTime? depuis = null, int limite = 200);
}