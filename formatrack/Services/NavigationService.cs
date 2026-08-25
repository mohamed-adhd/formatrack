using System;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class NavigationService : INavigationService
{
    public event Action<object?>? CurrentPageChanged;

    public object? CurrentPage { get; private set; }

    public void NavigateTo(object page)
    {
        CurrentPage = page;
        CurrentPageChanged?.Invoke(page);
    }

    public void Clear()
    {
        CurrentPage = null;
        CurrentPageChanged?.Invoke(null);
    }
}