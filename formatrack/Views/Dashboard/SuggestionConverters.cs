using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace formatrack.Views.Dashboard;

public class SuggestionPriorityConverter : IValueConverter
{
    public static readonly SuggestionPriorityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            1 => new SolidColorBrush(Color.Parse("#DC2626")),
            2 => new SolidColorBrush(Color.Parse("#F59E0B")),
            _ => new SolidColorBrush(Color.Parse("#3B82F6"))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class SuggestionTextConverter : IValueConverter
{
    public static readonly SuggestionTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isLu = value is true;
        return new SolidColorBrush(isLu ? Color.Parse("#9CA3AF") : Color.Parse("#1B2A4A"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class SuggestionDescOpacityConverter : IValueConverter
{
    public static readonly SuggestionDescOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? 0.5 : 1.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class SuggestionBadgeConverter : IValueConverter
{
    public static readonly SuggestionBadgeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            1 => new SolidColorBrush(Color.Parse("#DC2626")),
            2 => new SolidColorBrush(Color.Parse("#F59E0B")),
            _ => new SolidColorBrush(Color.Parse("#3B82F6"))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
