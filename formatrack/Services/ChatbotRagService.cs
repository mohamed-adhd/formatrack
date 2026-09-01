using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class ChatbotRagService : IChatbotRagService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static string EngineDir =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "system_aide_decision");

    private static string EngineDirFallback =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system_aide_decision");

    private static string DbPath =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "database.db");

    private string ResolveEngineDir()
    {
        if (File.Exists(Path.Combine(EngineDir, "engine.py"))) return EngineDir;
        if (File.Exists(Path.Combine(EngineDirFallback, "engine.py"))) return EngineDirFallback;
        return EngineDir;
    }

    private async Task<(int exitCode, string stdout, string stderr)> RunPythonAsync(
        string args, int timeoutMs = 30000)
    {
        var engineDir = ResolveEngineDir();
        var startInfo = new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = engineDir
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null) return (1, "", "Failed to start python3");

            using var cts = new System.Threading.CancellationTokenSource(timeoutMs);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { }
                return (2, "", "Timeout");
            }

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            return (process.ExitCode, stdout, stderr);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return (3, "", "python3 not found");
        }
        catch (Exception ex)
        {
            return (4, "", ex.Message);
        }
    }

    public async Task<ChatbotRagResponse> AskAsync(string query, string role, string promotion, string departement, string userName = "")
    {
        if (!File.Exists(DbPath))
            return new ChatbotRagResponse { Success = false, Error = "Base de données introuvable." };

        var args = $"-m rag.chat \"{DbPath}\" \"{query.Replace("\"", "\\\"")}\" \"{role}\" \"{promotion}\" \"{departement}\" \"{userName.Replace("\"", "\\\"")}\"";
        var (exitCode, stdout, stderr) = await RunPythonAsync(args, timeoutMs: 20000);

        if (exitCode == 3)
            return new ChatbotRagResponse
            {
                Success = false, IsOffline = true,
                Error = "Python3 n'est pas installé. Installez Python3 pour utiliser l'assistant."
            };

        if (exitCode == 2)
            return new ChatbotRagResponse
            {
                Success = false, IsOffline = true,
                Error = "Le service IA a pris trop de temps. Veuillez réessayer."
            };

        if (exitCode != 0)
            return new ChatbotRagResponse
            {
                Success = false, IsOffline = true,
                Error = $"Erreur du service: {stderr.Trim()}"
            };

        try
        {
            var result = JsonSerializer.Deserialize<JsonElement>(stdout.Trim());
            var answer = result.GetProperty("answer").GetString() ?? "";
            var isOffline = answer.Contains("indisponible") || answer.Contains("Erreur") || answer.Contains("temporairement");
            return new ChatbotRagResponse { Success = true, Answer = answer, IsOffline = isOffline };
        }
        catch
        {
            return new ChatbotRagResponse
            {
                Success = false, IsOffline = true,
                Error = $"Réponse invalide du service: {stdout.Trim()}"
            };
        }
    }

    public async Task<ChatbotRagResponse> TranscribeAsync(string audioFilePath, string lang = "fr")
    {
        if (!File.Exists(audioFilePath))
            return new ChatbotRagResponse { Success = false, Error = "Fichier audio introuvable." };

        var args = $"-m transcribe.vosk_transcribe \"{audioFilePath}\" \"{lang}\"";
        var (exitCode, stdout, stderr) = await RunPythonAsync(args, timeoutMs: 15000);

        if (exitCode == 3)
            return new ChatbotRagResponse
            {
                Success = false, IsOffline = true,
                Error = "Python3 n'est pas installé."
            };

        if (exitCode != 0)
            return new ChatbotRagResponse
            {
                Success = false, IsOffline = true,
                Error = $"Erreur de transcription: {stderr.Trim()}"
            };

        try
        {
            var result = JsonSerializer.Deserialize<JsonElement>(stdout.Trim());
            var text = result.GetProperty("text").GetString() ?? "";
            var error = result.TryGetProperty("error", out var errProp) ? errProp.GetString() : null;

            if (!string.IsNullOrEmpty(error))
                return new ChatbotRagResponse { Success = false, Error = error };

            return new ChatbotRagResponse { Success = true, Answer = text };
        }
        catch
        {
            return new ChatbotRagResponse { Success = false, Error = $"Réponse invalide: {stdout.Trim()}" };
        }
    }

    public async Task<bool> IndexKnowledgeBaseAsync()
    {
        if (!File.Exists(DbPath)) return false;

        var args = $"-m rag.seed \"{DbPath}\"";
        var (exitCode, stdout, stderr) = await RunPythonAsync(args, timeoutMs: 60000);
        return exitCode == 0;
    }

    public async Task<bool> CheckApiStatusAsync()
    {
        var (exitCode, stdout, stderr) = await RunPythonAsync(
            "-c \"import openai; from pathlib import Path; "
            + "p=Path('system_aide_decision/.env'); "
            + "k=[l.split('=',1)[1].strip() for l in p.read_text().splitlines() if l.startswith('API_KEY=') and 'your-api' not in l] if p.exists() else []; "
            + "print('ok' if k else 'no-key')\"",
            timeoutMs: 5000);

        return exitCode == 0 && stdout.Trim() == "ok";
    }
}
