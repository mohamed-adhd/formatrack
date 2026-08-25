using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class JournalActiviteService : IJournalActiviteService
{
    private readonly IJournalActiviteRepository _repos;

    public JournalActiviteService(IJournalActiviteRepository? repos = null)
        => _repos = repos ?? new JournalActiviteRepository();

    public async Task<long> JournalerAsync(int? idUtilisateur, string action, string? details = null)
        => await _repos.AddAsync(new JournalActivite
        {
            IdUtilisateur = idUtilisateur,
            Action = action,
            Details = details ?? string.Empty,
            DateAction = DateTime.Now
        });

    public async Task<IReadOnlyList<JournalActivite>> GetActivitesAsync(int? idUtilisateur = null, DateTime? depuis = null, int limite = 200)
    {
        if (idUtilisateur.HasValue)
            return await _repos.GetByUtilisateurAsync(idUtilisateur.Value, limite);
        if (depuis.HasValue)
            return await _repos.GetDepuisAsync(depuis.Value, limite);
        return await _repos.GetRecentsAsync(limite);
    }
}