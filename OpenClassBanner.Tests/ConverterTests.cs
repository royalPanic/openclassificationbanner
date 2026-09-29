using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenClassBanner;

namespace OpenClassBanner.Tests;

[TestClass]
public sealed class ConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [TestMethod]
    public void HexColorConverterConvertsRgbAndArgbColors()
    {
        var converter = new HexColorToBrushConverter();

        var rgb = (SolidColorBrush)converter.Convert("#007A33", typeof(Brush), null!, Culture);
        var argb = (SolidColorBrush)converter.Convert("#80007A33", typeof(Brush), null!, Culture);

        Assert.AreEqual(Color.FromRgb(0, 122, 51), rgb.Color);
        Assert.AreEqual(Color.FromArgb(128, 0, 122, 51), argb.Color);
    }

    [TestMethod]
    public void HexColorConverterReturnsBlackForInvalidInput()
    {
        var converter = new HexColorToBrushConverter();

        Assert.AreSame(Brushes.Black, converter.Convert(null!, typeof(Brush), null!, Culture));
        Assert.AreSame(Brushes.Black, converter.Convert("not-a-color", typeof(Brush), null!, Culture));
    }

    [TestMethod]
    public void HexColorConverterConvertsBrushBackToColorText()
    {
        var converter = new HexColorToBrushConverter();
        var brush = new SolidColorBrush(Color.FromArgb(128, 0, 122, 51));

        Assert.AreEqual("#80007A33", converter.ConvertBack(brush, typeof(string), null!, Culture));
        Assert.AreSame(Binding.DoNothing, converter.ConvertBack(Brushes.Black, typeof(string), null!, Culture));
    }

    [TestMethod]
    public void FontSizeConverterScalesBottomPadding()
    {
        var converter = new FontSizeToPaddingConverter();

        var padding = (Thickness)converter.Convert([32d, 6d], typeof(Thickness), null!, Culture);

        Assert.AreEqual(new Thickness(0, 0, 0, 12), padding);
    }

    [TestMethod]
    public void FontSizeConverterUsesDefaultsForMissingOrInvalidValues()
    {
        var converter = new FontSizeToPaddingConverter();

        var padding = (Thickness)converter.Convert(["invalid"], typeof(Thickness), null!, Culture);

        Assert.AreEqual(new Thickness(0, 0, 0, 6), padding);
    }

    [TestMethod]
    public void FontSizeConverterClampsNegativeOffset()
    {
        var converter = new FontSizeToPaddingConverter();

        var padding = (Thickness)converter.Convert([16d, -10d], typeof(Thickness), null!, Culture);

        Assert.AreEqual(new Thickness(0, 0, 0, 0), padding);
    }

    [TestMethod]
    public void FontSizeConverterConvertBackReturnsTwoDoNothingValues()
    {
        var converter = new FontSizeToPaddingConverter();

        var values = converter.ConvertBack(new Thickness(), [typeof(double), typeof(double)], null!, Culture);

        Assert.AreEqual(2, values.Length);
        Assert.AreSame(Binding.DoNothing, values[0]);
        Assert.AreSame(Binding.DoNothing, values[1]);
    }
}
