using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MangaStudio.UI.Converters;

// Converts a Serilog level string to a foreground brush for the log view.
public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Debug" => new SolidColorBrush(Color.FromRgb(148, 163, 184)), // slate-400
            "Warning" => new SolidColorBrush(Color.FromRgb(217, 119, 6)), // amber-600
            "Error" => new SolidColorBrush(Color.FromRgb(220, 38, 38)), // red-600
            "Fatal" => new SolidColorBrush(Color.FromRgb(153, 27, 27)), // red-800
            _ => new SolidColorBrush(Color.FromRgb(30, 41, 59))  // slate-800
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}