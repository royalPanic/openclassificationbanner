using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WpfBinding = System.Windows.Data.Binding;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace OpenClassBanner;

public sealed class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string colorText)
            return WpfBrushes.Black;

        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(colorText)!;
            return new SolidColorBrush(color);
        }
        catch (FormatException)
        {
            return WpfBrushes.Black;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is SolidColorBrush brush ? brush.Color.ToString() : WpfBinding.DoNothing;
}
