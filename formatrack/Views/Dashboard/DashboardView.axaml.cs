using Avalonia.Controls;
using Avalonia.Input;
using formatrack.Models;
using formatrack.ViewModels.Dashboard;

namespace formatrack.Views.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private async void OnSuggestionClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is SuggestionAide suggestion && DataContext is DashboardViewModel vm)
        {
            await vm.DismissSuggestionCommand.ExecuteAsync(suggestion);
            vm.NavigateToSuggestionPage(suggestion.ActionPage);
        }
    }
}
