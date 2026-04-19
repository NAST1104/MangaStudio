using System.Globalization;
using System.Windows.Data;

namespace MangaStudio.UI.Converters;

// Converts an enum value to bool for RadioButton IsChecked bindings.
// ConverterParameter is the enum member name as a string.
// Example: {Binding OutputFormat, Converter=..., ConverterParameter=WebP}
// Returns true when OutputFormat == ExportFormat.WebP
public class EnumToBoolConverter : IValueConverter
{
    public static readonly EnumToBoolConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter is not null)
            return Enum.Parse(targetType, parameter.ToString()!);

        return Binding.DoNothing;
    }
}