using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace OpenClassBanner;

public partial class App
{
    private ConfigManager? _configManager;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private System.Windows.Forms.ToolStripMenuItem? _profilesMenu;
    private readonly List<MainWindow> _windows = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Logger.Initialize();
        Logger.Info("Open Class Banner starting.");
#if DEBUG
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
#endif

        var installPaths = InstallPathResolver.Resolve(AppContext.BaseDirectory);
        var configDirectory = installPaths.ConfigurationDirectory;
        _configManager = new ConfigManager(configDirectory, AppContext.BaseDirectory);
        Logger.Debug($"Environment: appVersion={typeof(App).Assembly.GetName().Version}; runtime={RuntimeInformation.FrameworkDescription}; OS={Environment.OSVersion.VersionString}; architecture={RuntimeInformation.ProcessArchitecture}; installScope={installPaths.Scope}; appDirectory='{AppContext.BaseDirectory}'; configDirectory='{configDirectory}'; logFile='{Logger.LogPath}'.");
        Logger.Debug($"Configuration initialized with {_configManager.Profiles.Count} profile(s).");
        _configManager.ConfigurationChanged += OnConfigurationChanged;
        _configManager.ProfilesChanged += OnProfilesChanged;
        CreateTrayIcon();

        var screens = System.Windows.Forms.Screen.AllScreens;
        Logger.Debug($"Detected {screens.Length} display(s).");
        foreach (var screen in screens)
        {
            var bounds = screen.Bounds;
            Logger.Debug($"Display '{screen.DeviceName}': bounds={bounds}; primary={screen.Primary}.");
            var window = new MainWindow(bounds, _configManager.Current);
            _windows.Add(window);
            window.Show();
        }

        Logger.Info($"Started {_windows.Count} banner window(s).");
    }

#if DEBUG
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("Unhandled WPF dispatcher exception", e.Exception);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            Logger.Error($"Unhandled process exception; terminating={e.IsTerminating}", exception);
        else
            Logger.Error($"Unhandled process exception; terminating={e.IsTerminating}; exceptionObject={e.ExceptionObject}");
    }
#endif

    private void OnConfigurationChanged(BannerConfig config)
    {
        Dispatcher.InvokeAsync(() =>
        {
            foreach (var window in _windows)
                window.ApplyConfig(config);
        });
    }

    private void CreateTrayIcon()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        _profilesMenu = new System.Windows.Forms.ToolStripMenuItem("Profiles");
        menu.Items.Add(_profilesMenu);
        UpdateProfileMenu();
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Reload Config", null, (_, _) => _configManager?.ForceReload());
        menu.Items.Add("Edit Config", null, (_, _) => OpenConfigFile());
        menu.Items.Add("Exit", null, (_, _) => Shutdown());

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Text = "Open Class Banner",
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    private void OnProfilesChanged()
    {
        Dispatcher.InvokeAsync(UpdateProfileMenu);
    }

    private void UpdateProfileMenu()
    {
        if (_profilesMenu is null || _configManager is null)
            return;

        _profilesMenu.DropDownItems.Clear();
        foreach (var profile in _configManager.Profiles)
        {
            var item = new System.Windows.Forms.ToolStripMenuItem(profile.DisplayName)
            {
                Tag = profile.FileName,
                Checked = string.Equals(profile.FileName, _configManager.ActiveProfileFileName, StringComparison.OrdinalIgnoreCase),
                CheckOnClick = true
            };
            item.Click += (_, _) => _configManager.SelectProfile((string)item.Tag!);
            _profilesMenu.DropDownItems.Add(item);
        }

        _profilesMenu.Enabled = _profilesMenu.DropDownItems.Count > 0;
    }

    private void OpenConfigFile()
    {
        if (_configManager is null)
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _configManager.ActiveConfigPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Error("Unable to open the active configuration profile in the default editor", ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Open Class Banner shutting down.");
        _configManager?.Dispose();
        _trayIcon?.Dispose();
        foreach (var window in _windows)
        {
            window.Close();
        }

        base.OnExit(e);
    }
}

