namespace formatrack.Services.Interfaces;

public interface INavigationService
{
    event Action<object?>? CurrentPageChanged;
    object? CurrentPage { get; }
    void NavigateTo(object page);
    void Clear();
}
