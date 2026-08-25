using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using formatrack.Services.Interfaces;

namespace formatrack.Services;

public class DialogService : IDialogService
{
    public Task InformerAsync(string titre, string message) => ShowAsync(titre, message, confirmation: false);
    public Task<bool> ConfirmerAsync(string titre, string message) => ShowAsync(titre, message, confirmation: true);

    private static async Task<bool> ShowAsync(string titre, string message, bool confirmation)
    {
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is null)
        {
            Console.WriteLine($"[Dialog] {titre}: {message}");
            return !confirmation;
        }

        var tcs = new TaskCompletionSource<bool>();
        Window boite = null!;

        var boutons = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        boite = new Window
        {
            Title = titre,
            Width = 400,
            Height = 200,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 20,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    boutons
                }
            }
        };

        if (confirmation)
        {
            var oui = new Button { Content = "Oui", Width = 90, IsDefault = true };
            var non = new Button { Content = "Non", Width = 90 };
            oui.Click += (_, _) => { tcs.TrySetResult(true); boite.Close(); };
            non.Click += (_, _) => { tcs.TrySetResult(false); boite.Close(); };
            boutons.Children.Add(oui);
            boutons.Children.Add(non);
        }
        else
        {
            var ok = new Button { Content = "OK", Width = 90, IsDefault = true };
            ok.Click += (_, _) => { tcs.TrySetResult(true); boite.Close(); };
            boutons.Children.Add(ok);
        }

        await boite.ShowDialog(owner);
        return await tcs.Task;
    }
}