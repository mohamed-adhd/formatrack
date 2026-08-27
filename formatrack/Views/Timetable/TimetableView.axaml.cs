using Avalonia.Controls;
using Avalonia.Interactivity;
using formatrack.Models;
using formatrack.ViewModels.Timetable;

namespace formatrack.Views.Timetable;

public partial class TimetableView : UserControl
{
    public TimetableView()
    {
        InitializeComponent();
    }

    private void OnAfficherClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is EmploiDuTemps item && DataContext is TimetableViewModel vm)
            vm.SelectEmploiCommand.Execute(item);
    }

    private void OnTogglePublishClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is EmploiDuTemps item && DataContext is TimetableViewModel vm)
            vm.TogglePublishCommand.Execute(item);
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is EmploiDuTemps item && DataContext is TimetableViewModel vm)
            vm.DeleteEmploiCommand.Execute(item);
    }
}
