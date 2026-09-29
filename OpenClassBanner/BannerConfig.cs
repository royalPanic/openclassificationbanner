namespace OpenClassBanner;

public sealed class BannerConfig
{
    public string Name { get; set; } = "Example Configuration";
    public string BannerText { get; init; } = "UNCLASSIFIED";
    public string LeftText { get; set; } = string.Empty;
    public string CenterText { get; set; } = "UNCLASSIFIED";
    public string RightText { get; set; } = string.Empty;
    public string BackgroundColor { get; init; } = "#007A33";
    public string ForegroundColor { get; init; } = "#FFFFFF";
    public int HeightPx { get; init; } = 36;
    public double FontSize { get; init; } = 16;
    public double VerticalTextOffsetPx { get; init; } = 6;
    public string FontFamily { get; init; } = "Segoe UI";
}
