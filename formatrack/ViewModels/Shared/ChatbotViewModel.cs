using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Services;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Shared;

public partial class ChatbotViewModel : ViewModelBase
{
    private readonly IChatbotRagService _ragService = CompositionRoot.ChatbotRag;
    private Process? _recordingProcess;
    private string _recordingPath = "";
    private CancellationTokenSource? _recordTimeoutCts;

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private string _userMessage = "";
    [ObservableProperty] private bool _isTyping;
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private bool _hasApiKey;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private string _recordingTime = "";

    public string UserRole { get; set; } = "Stagiaire";
    public string UserPromotion { get; set; } = "";
    public string UserDepartement { get; set; } = "";
    public string UserName { get; set; } = "";

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ChatbotViewModel()
    {
        Messages.Add(new ChatMessage
        {
            Text = "Bonjour ! Je suis l'assistant IA du CMFPO. Je peux vous renseigner sur les formations, notes, sessions, emplois du temps et plus encore.\n\nPosez-moi votre question !",
            IsUser = false,
            Timestamp = DateTime.Now
        });
        _ = CheckApiStatusAsync();
    }

    private async Task CheckApiStatusAsync()
    {
        try
        {
            HasApiKey = await _ragService.CheckApiStatusAsync();
            if (!HasApiKey)
            {
                StatusMessage = "Clé API non configurée. Ajoutez votre clé dans system_aide_decision/.env";
                IsOffline = true;
            }
            else
            {
                _ = _ragService.IndexKnowledgeBaseAsync();
            }
        }
        catch
        {
            IsOffline = true;
            StatusMessage = "Service indisponible";
        }
    }

    public void SetUserContext(string role, string promotion, string departement, string userName = "")
    {
        UserRole = role;
        UserPromotion = promotion;
        UserDepartement = departement;
        UserName = userName;
    }

    [RelayCommand]
    private void ToggleChat() => IsOpen = !IsOpen;

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserMessage)) return;

        var userText = UserMessage.Trim();
        UserMessage = "";

        Messages.Add(new ChatMessage
        {
            Text = userText,
            IsUser = true,
            Timestamp = DateTime.Now
        });

        IsTyping = true;
        StatusMessage = "";

        try
        {
            var response = await _ragService.AskAsync(userText, UserRole, UserPromotion, UserDepartement, UserName);

            IsOffline = response.IsOffline;

            Messages.Add(new ChatMessage
            {
                Text = response.Success ? response.Answer : $"Erreur: {response.Error}",
                IsUser = false,
                Timestamp = DateTime.Now
            });

            if (response.IsOffline)
                StatusMessage = "Mode hors ligne — données limitées disponibles";
        }
        catch (Exception ex)
        {
            IsOffline = true;
            Messages.Add(new ChatMessage
            {
                Text = $"Désolé, une erreur est survenue: {ex.Message}",
                IsUser = false,
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            IsTyping = false;
        }
    }

    [RelayCommand]
    private async Task ToggleRecordingAsync()
    {
        if (IsRecording)
        {
            await StopRecordingAsync();
        }
        else
        {
            await StartRecordingAsync();
        }
    }

    private async Task StartRecordingAsync()
    {
        try
        {
            _recordingPath = Path.Combine(Path.GetTempPath(), $"cmfpo_rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -f pulse -i default -t 30 -ar 16000 -ac 1 \"{_recordingPath}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            _recordingProcess = Process.Start(startInfo);
            if (_recordingProcess == null)
            {
                StatusMessage = "Impossible de démarrer l'enregistrement.";
                return;
            }

            IsRecording = true;
            RecordingTime = "0s";
            StatusMessage = "🎤 Enregistrement en cours... (max 30s)";

            _recordTimeoutCts = new CancellationTokenSource();
            _ = UpdateRecordingTimerAsync(_recordTimeoutCts.Token);

            await Task.Delay(1500);

            if (_recordingProcess.HasExited)
            {
                var stderr = await _recordingProcess.StandardError.ReadToEndAsync();
                IsRecording = false;

                if (stderr.Contains("pulse", StringComparison.OrdinalIgnoreCase) || stderr.Contains("default", StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "PulseAudio indisponible, tentative ALSA...";
                    _recordingProcess = null;
                    await StartRecordingAlsaAsync();
                    return;
                }

                StatusMessage = $"Erreur microphone: {FormatFfmpegError(stderr)}";
                _recordingProcess = null;
                return;
            }
        }
        catch (Exception ex)
        {
            IsRecording = false;
            StatusMessage = $"Erreur: {ex.Message}";
            _recordingProcess?.Kill();
            _recordingProcess = null;
        }
    }

    private async Task StartRecordingAlsaAsync()
    {
        try
        {
            _recordingPath = Path.Combine(Path.GetTempPath(), $"cmfpo_rec_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -f alsa -i default -t 30 -ar 16000 -ac 1 \"{_recordingPath}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            _recordingProcess = Process.Start(startInfo);
            if (_recordingProcess == null)
            {
                StatusMessage = "Microphone introuvable. Vérifiez votre périphérique audio.";
                return;
            }

            IsRecording = true;
            RecordingTime = "0s";
            StatusMessage = "🎤 Enregistrement (ALSA)... (max 30s)";

            _recordTimeoutCts = new CancellationTokenSource();
            _ = UpdateRecordingTimerAsync(_recordTimeoutCts.Token);

            await Task.Delay(1500);

            if (_recordingProcess.HasExited)
            {
                var stderr = await _recordingProcess.StandardError.ReadToEndAsync();
                IsRecording = false;
                StatusMessage = $"Erreur microphone: {FormatFfmpegError(stderr)}";
                _recordingProcess = null;
            }
        }
        catch (Exception ex)
        {
            IsRecording = false;
            StatusMessage = $"Erreur: {ex.Message}";
            _recordingProcess?.Kill();
            _recordingProcess = null;
        }
    }

    private async Task StopRecordingAsync()
    {
        if (!IsRecording) return;

        _recordTimeoutCts?.Cancel();
        IsRecording = false;
        StatusMessage = "Transcription en cours...";

        try
        {
            if (_recordingProcess != null && !_recordingProcess.HasExited)
            {
                try
                {
                    await _recordingProcess.StandardInput.WriteAsync("q");
                    await _recordingProcess.StandardInput.FlushAsync();
                }
                catch { }

                var waitTask = _recordingProcess.WaitForExitAsync();
                var timeout = Task.Delay(5000);
                if (await Task.WhenAny(waitTask, timeout) != waitTask)
                {
                    try { _recordingProcess.Kill(); } catch { }
                    await Task.Delay(500);
                }
            }
        }
        catch { }
        finally
        {
            _recordingProcess = null;
        }

        await Task.Delay(300);

        if (!File.Exists(_recordingPath) || new FileInfo(_recordingPath).Length < 1000)
        {
            StatusMessage = "Aucun audio enregistré. Vérifiez votre microphone.";
            return;
        }

        try
        {
            var result = await _ragService.TranscribeAsync(_recordingPath);
            if (result.Success && !string.IsNullOrWhiteSpace(result.Answer))
            {
                UserMessage = result.Answer;
                StatusMessage = "Transcription terminée.";
            }
            else
            {
                StatusMessage = string.IsNullOrEmpty(result.Error)
                    ? "Aucun texte détecté dans l'enregistrement."
                    : $"Transcription: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur de transcription: {ex.Message}";
        }
        finally
        {
            try { File.Delete(_recordingPath); } catch { }
        }
    }

    private async Task UpdateRecordingTimerAsync(CancellationToken ct)
    {
        var startTime = DateTime.Now;
        while (!ct.IsCancellationRequested && IsRecording)
        {
            try
            {
                await Task.Delay(1000, ct);
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                RecordingTime = $"{(int)elapsed}s";
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static string FormatFfmpegError(string stderr)
    {
        if (string.IsNullOrEmpty(stderr)) return "Erreur inconnue";
        var lines = stderr.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("not found") || line.Contains("No such"))
                return "PulseAudio non disponible. Installez pulseaudio.";
            if (line.Contains("Permission denied"))
                return "Accès microphone refusé.";
            if (line.Contains("cannot open"))
                return "Microphone introuvable.";
        }
        return "Vérifiez que votre microphone est connecté.";
    }

    [RelayCommand]
    private async Task RefreshIndexAsync()
    {
        StatusMessage = "Mise à jour de la base de connaissances...";
        IsTyping = true;

        try
        {
            var ok = await _ragService.IndexKnowledgeBaseAsync();
            StatusMessage = ok ? "Base de connaissances mise à jour." : "Échec de la mise à jour.";
            HasApiKey = await _ragService.CheckApiStatusAsync();
        }
        catch
        {
            StatusMessage = "Erreur lors de la mise à jour.";
        }
        finally
        {
            IsTyping = false;
        }
    }
}

public class ChatMessage
{
    public string Text { get; set; } = "";
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; }
    public string TimeString => Timestamp.ToString("HH:mm");
}
