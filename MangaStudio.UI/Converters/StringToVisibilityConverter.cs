using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MangaStudio.UI.Converters;

// Collapses an element when the bound string is null or empty.
public class StringToVisibilityConverter : IValueConverter
{
    public static readonly StringToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value?.ToString())
            ? Visibility.Collapsed
            : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}