using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WpfBinding = System.Windows.Data.Binding;

namespace OpenClassBanner;

public sealed class FontSizeToPaddingConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var fontSize = values.Length > 0 && values[0] is double size ? size : 16;
        var baseOffset = values.Length > 1 && values[1] is double offset ? offset : 6;
        var scaledOffset = Math.Max(0, baseOffset * fontSize / 16);
        return new Thickness(0, 0, 0, scaledOffset);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        [WpfBinding.DoNothing, WpfBinding.DoNothing];
}