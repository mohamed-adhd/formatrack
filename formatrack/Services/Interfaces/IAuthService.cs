using System.Threading.Tasks;

namespace formatrack.Services.Interfaces;

public interface IAuthService
{
    Task<string?> AuthentifierAsync(string identifiant, string motDePasse);
}
