using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using formatrack.ViewModels.Shared;

namespace formatrack.Views.Shared;

public partial class ChatbotView : UserControl
{
    public ChatbotView()
    {
        InitializeComponent();
    }

    private void OnOverlayClick(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ChatbotViewModel vm)
            vm.IsOpen = false;
    }

    private void OnMessageKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ChatbotViewModel vm)
        {
            vm.SendMessageCommand.Execute(null);
            e.Handled = true;
        }
    }
}

public class ChatBubbleClassConverter : IValueConverter
{
    public static readonly ChatBubbleClassConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "msg-bubble-user" : "msg-bubble-bot";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ChatAlignmentConverter : IValueConverter
{
    public static readonly ChatAlignmentConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? HorizontalAlignment.Right : HorizontalAlignment.Left;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ChatTextConverter : IValueConverter
{
    public static readonly ChatTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Brushes.White : new SolidColorBrush(Color.Parse("#1F2937"));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StatusTextConverter : IValueConverter
{
    public static readonly StatusTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Hors ligne" : "En ligne";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StatusColorConverter : IValueConverter
{
    public static readonly StatusColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? new SolidColorBrush(Color.Parse("#EF4444")) : new SolidColorBrush(Color.Parse("#4ADE80"));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RecordingIconConverter : IValueConverter
{
    public static readonly RecordingIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "⏹" : "🎤";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RecordingColorConverter : IValueConverter
{
    public static readonly RecordingColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? new SolidColorBrush(Color.Parse("#EF4444")) : new SolidColorBrush(Color.Parse("#6B7280"));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
