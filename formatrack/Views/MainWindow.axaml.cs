using Avalonia.Controls;

namespace formatrack.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty)
        {
            if (DataContext is ViewModels.MainWindowViewModel vm)
            {
                vm.IsMobile = Bounds.Width < 850; // Use 850 as threshold to fit the 260px sidebar and a decent content area
            }
        }
    }
}