using System.Threading.Tasks;

namespace formatrack.Services.Interfaces;

public interface IChatbotRagService
{
    Task<ChatbotRagResponse> AskAsync(string query, string role, string promotion, string departement, string userName = "");
    Task<ChatbotRagResponse> TranscribeAsync(string audioFilePath, string lang = "fr");
    Task<bool> IndexKnowledgeBaseAsync();
    Task<bool> CheckApiStatusAsync();
}

public class ChatbotRagResponse
{
    public bool Success { get; set; }
    public string Answer { get; set; } = "";
    public string Error { get; set; } = "";
    public bool IsOffline { get; set; }
}
