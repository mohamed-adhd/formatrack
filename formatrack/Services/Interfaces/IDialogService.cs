using System.Threading.Tasks;

namespace formatrack.Services.Interfaces;

public interface IDialogService
{
    Task InformerAsync(string titre, string message);
    Task<bool> ConfirmerAsync(string titre, string message);
}
