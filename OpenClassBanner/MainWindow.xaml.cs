using System.Drawing;
using System.Windows;
using System.Windows.Interop;

namespace OpenClassBanner;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Rectangle _monitorBounds;
    private AppBarHelper? _appBar;
    private BannerConfig _config;

    public MainWindow(Rectangle monitorBounds, BannerConfig config)
    {
        InitializeComponent();
        _monitorBounds = monitorBounds;
        _config = config;
        UpdateWindowHeight();
        DataContext = config;
        SourceInitialized += OnSourceInitialized;
        Closed += (_, _) => _appBar?.Dispose();
    }

    public void ApplyConfig(BannerConfig config)
    {
        _config = config;
        UpdateWindowHeight();
        DataContext = config;
        _appBar?.UpdateHeight(config.HeightPx);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var source = (HwndSource)PresentationSource.FromVisual(this)!;
        UpdateWindowHeight();
        _appBar = new AppBarHelper(source.Handle, source, _monitorBounds, _config.HeightPx);
        if (!_appBar.Register())
            Logger.Warn($"AppBar registration failed for monitor bounds {_monitorBounds}; displaying an overlay fallback.");
    }

    private void UpdateWindowHeight()
    {
        var dpiScaleY = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        Height = _config.HeightPx / dpiScaleY;
    }
}