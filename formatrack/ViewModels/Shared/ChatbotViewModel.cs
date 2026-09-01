using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using formatrack.Services;
using formatrack.Services.Interfaces;

namespace formatrack.ViewModels.Shared;

public partial class ChatbotViewModel : ViewModelBase
{
    private readonly IChatbotRagService _ragService = CompositionRoot.ChatbotRag;

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private string _userMessage = "";
    [ObservableProperty] private bool _isTyping;
    [ObservableProperty] private bool _isOffline;
    [ObservableProperty] private bool _hasApiKey;
    [ObservableProperty] private string _statusMessage = "";

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
