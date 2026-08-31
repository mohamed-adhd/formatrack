using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using formatrack.Data.Repositories;
using formatrack.Models;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class SuggestionAideService : ISuggestionAideService
{
    private readonly ISuggestionAideRepository _repo;

    public SuggestionAideService(ISuggestionAideRepository? repo = null)
    {
        _repo = repo ?? new SuggestionAideRepository();
    }

    public async Task<IReadOnlyList<SuggestionAide>> GetAllSuggestionsAsync()
        => await _repo.GetAllAsync();

    public async Task<IReadOnlyList<SuggestionAide>> GetUnreadSuggestionsAsync()
        => await _repo.GetUnreadAsync();

    public async Task<int> RunEngineAsync()
    {
        try
        {
            var dbPath = Path.Combine(AppContext.BaseDirectory, "Assets", "database.db");
            if (!File.Exists(dbPath)) return 0;

            var engineDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "system_aide_decision");
            var enginePy = Path.Combine(engineDir, "engine.py");

            if (!File.Exists(enginePy))
            {
                engineDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system_aide_decision");
                enginePy = Path.Combine(engineDir, "engine.py");
            }

            if (!File.Exists(enginePy)) return 0;

            var startInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{enginePy}\" \"{dbPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = engineDir
            };

            using var process = Process.Start(startInfo);
            if (process == null) return 0;

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                System.Diagnostics.Debug.WriteLine($"Engine error: {error}");
            }

            return await _repo.GetAllAsync().ContinueWith(t => t.Result.Count);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to run engine: {ex.Message}");
            return 0;
        }
    }

    public async Task MarkAsReadAsync(int id)
        => await _repo.MarkAsReadAsync(id);

    public async Task MarkAllAsReadAsync()
        => await _repo.MarkAllAsReadAsync();
}
