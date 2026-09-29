using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenClassBanner;

namespace OpenClassBanner.Tests;

[TestClass]
public sealed class BannerConfigTests
{
    [TestMethod]
    public void NewConfigUsesExpectedDefaults()
    {
        var config = new BannerConfig();

        Assert.AreEqual("Example Configuration", config.Name);
        Assert.AreEqual("UNCLASSIFIED", config.BannerText);
        Assert.AreEqual("UNCLASSIFIED", config.CenterText);
        Assert.AreEqual(string.Empty, config.LeftText);
        Assert.AreEqual(string.Empty, config.RightText);
        Assert.AreEqual("#007A33", config.BackgroundColor);
        Assert.AreEqual("#FFFFFF", config.ForegroundColor);
        Assert.AreEqual(36, config.HeightPx);
        Assert.AreEqual(16, config.FontSize);
        Assert.AreEqual(6, config.VerticalTextOffsetPx);
        Assert.AreEqual("Segoe UI", config.FontFamily);
    }
}
