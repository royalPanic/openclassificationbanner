using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenClassBanner;

public sealed record ConfigProfile(string FileName, string DisplayName);

public sealed class ConfigManager : IDisposable
{
    private const string ExampleProfileFileName = "example-config.json";
    private const string LastProfileFileName = "last-profile.txt";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private static readonly Regex HexColor = new("^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly object _sync = new();
    private readonly string _directoryPath;
    private readonly string _lastProfilePath;
    private readonly FileSystemWatcher _watcher;
    private readonly System.Threading.Timer _debounceTimer;
    private BannerConfig _current;
    private List<ConfigProfile> _profiles;
    private string _activeFileName;

    public ConfigManager(string directoryPath, string? legacyDirectoryPath = null)
    {
        _directoryPath = directoryPath;
        _lastProfilePath = Path.Combine(directoryPath, LastProfileFileName);
        Directory.CreateDirectory(_directoryPath);
        if (legacyDirectoryPath is not null)
            MigrateLegacyConfiguration(legacyDirectoryPath);
        EnsureExampleProfile();
        _profiles = DiscoverProfiles();
        _activeFileName = ChooseInitialProfile(_profiles);
        _current = ReadConfigFile(_activeFileName);
        _debounceTimer = new System.Threading.Timer(_ => RefreshProfilesAndReload(), null, Timeout.Infinite, Timeout.Infinite);
        _watcher = new FileSystemWatcher(_directoryPath, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
    }

    public event Action<BannerConfig>? ConfigurationChanged;
    public event Action? ProfilesChanged;

    public BannerConfig Current
    {
        get { lock (_sync) return _current; }
    }

    public IReadOnlyList<ConfigProfile> Profiles
    {
        get { lock (_sync) return _profiles.ToArray(); }
    }

    public string ActiveProfileFileName
    {
        get { lock (_sync) return _activeFileName; }
    }

    public string ActiveConfigPath => Path.Combine(_directoryPath, ActiveProfileFileName);

    private void MigrateLegacyConfiguration(string legacyDirectoryPath)
    {
        if (string.Equals(
                Path.GetFullPath(legacyDirectoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(_directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase) || !Directory.Exists(legacyDirectoryPath))
        {
            return;
        }

        var migratedProfiles = 0;
        var migratedSelection = false;
        try
        {
            foreach (var sourcePath in Directory.EnumerateFiles(legacyDirectoryPath, "*.json"))
            {
                if (!IsProfileCandidate(sourcePath))
                    continue;

                var destinationPath = Path.Combine(_directoryPath, Path.GetFileName(sourcePath));
                if (File.Exists(destinationPath))
                    continue;

                try
                {
                    File.Copy(sourcePath, destinationPath);
                    migratedProfiles++;
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Unable to migrate a legacy configuration profile: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Unable to enumerate legacy configuration profiles: {ex.Message}");
        }

        var legacyLastProfilePath = Path.Combine(legacyDirectoryPath, LastProfileFileName);
        if (!File.Exists(_lastProfilePath) && File.Exists(legacyLastProfilePath))
        {
            try
            {
                File.Copy(legacyLastProfilePath, _lastProfilePath);
                migratedSelection = true;
            }
            catch (Exception ex) { Logger.Debug($"Unable to migrate legacy profile selection: {ex.Message}"); }
        }

        Logger.Debug($"Legacy config migration completed: {migratedProfiles} profile(s) and remembered selection migrated; selectionMigrated={migratedSelection}.");
    }

    public void SelectProfile(string fileName)
    {
        try
        {
            var profile = Profiles.FirstOrDefault(item => string.Equals(item.FileName, fileName, StringComparison.OrdinalIgnoreCase));
            if (profile is null)
                return;

            var updated = ReadConfigFile(profile.FileName);
            lock (_sync)
            {
                _activeFileName = profile.FileName;
                _current = updated;
            }

            PersistSelectedProfile(profile.FileName);
            ConfigurationChanged?.Invoke(updated);
            ProfilesChanged?.Invoke();
            Logger.Info($"Selected configuration profile '{profile.DisplayName}' ({profile.FileName}).");
        }
        catch (Exception ex)
        {
            Logger.Error($"Unable to select configuration profile '{fileName}'", ex);
        }
    }

    public void ForceReload() => RefreshProfilesAndReload();

    private void EnsureExampleProfile()
    {
        var examplePath = Path.Combine(_directoryPath, ExampleProfileFileName);
        if (File.Exists(examplePath) && IsUsableProfileFile(examplePath))
            return;

        var example = new BannerConfig();
        File.WriteAllText(examplePath, JsonSerializer.Serialize(example, JsonOptions));
        Logger.Info("Created example-config.json because no usable configuration profiles were found.");
    }

    private List<ConfigProfile> DiscoverProfiles()
    {
        var profiles = new List<ConfigProfile>();
        foreach (var path in Directory.EnumerateFiles(_directoryPath, "*.json"))
        {
            var fileName = Path.GetFileName(path);
            if (!IsProfileCandidate(path))
                continue;

            try
            {
                var config = ReadConfigFile(fileName);
                profiles.Add(new ConfigProfile(fileName, config.Name));
            }
            catch (Exception ex)
            {
                Logger.Warn($"Ignoring invalid configuration profile '{fileName}': {ex.Message}");
            }
        }

        return profiles.OrderBy(profile => profile.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private bool IsUsableProfileFile(string path)
    {
        if (!IsProfileCandidate(path))
            return false;

        try
        {
            _ = ReadConfigFile(Path.GetFileName(path));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsProfileCandidate(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".assets.json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.EnumerateObject().Any(property =>
                    property.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("BannerText", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("LeftText", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("CenterText", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("RightText", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("BackgroundColor", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("ForegroundColor", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private string ChooseInitialProfile(IReadOnlyList<ConfigProfile> profiles)
    {
        var remembered = File.Exists(_lastProfilePath) ? File.ReadAllText(_lastProfilePath).Trim() : string.Empty;
        if (profiles.Any(profile => string.Equals(profile.FileName, remembered, StringComparison.OrdinalIgnoreCase)))
            return remembered;

        var example = profiles.FirstOrDefault(profile => string.Equals(profile.FileName, ExampleProfileFileName, StringComparison.OrdinalIgnoreCase));
        return (example ?? profiles[0]).FileName;
    }

    private BannerConfig ReadConfigFile(string fileName)
    {
        var path = Path.Combine(_directoryPath, fileName);
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<BannerConfig>(json, JsonOptions) ?? throw new InvalidDataException("Configuration is empty.");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("Name", out _) || string.IsNullOrWhiteSpace(config.Name))
            config.Name = Path.GetFileNameWithoutExtension(fileName);
        if (!root.TryGetProperty("LeftText", out _) && !root.TryGetProperty("CenterText", out _) && !root.TryGetProperty("RightText", out _))
            config.CenterText = config.BannerText;

        config.LeftText ??= string.Empty;
        config.CenterText ??= string.Empty;
        config.RightText ??= string.Empty;
        Validate(config);
        return config;
    }

    private static void Validate(BannerConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
            throw new InvalidDataException("Name must not be empty.");
        if (!HexColor.IsMatch(config.BackgroundColor) || !HexColor.IsMatch(config.ForegroundColor))
            throw new InvalidDataException("Colors must use #RRGGBB or #AARRGGBB notation.");
        if (config.HeightPx is < 12 or > 200)
            throw new InvalidDataException("HeightPx must be between 12 and 200.");
        if (!double.IsFinite(config.FontSize) || config.FontSize is < 8 or > 96)
            throw new InvalidDataException("FontSize must be between 8 and 96.");
        if (!double.IsFinite(config.VerticalTextOffsetPx) || config.VerticalTextOffsetPx is < 0 or > 50)
            throw new InvalidDataException("VerticalTextOffsetPx must be between 0 and 50.");
        if (string.IsNullOrWhiteSpace(config.FontFamily))
            throw new InvalidDataException("FontFamily must not be empty.");
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e) => _debounceTimer.Change(300, Timeout.Infinite);
    private void OnFileChanged(object sender, RenamedEventArgs e) => _debounceTimer.Change(300, Timeout.Infinite);

    private void RefreshProfilesAndReload()
    {
        try
        {
            var profiles = DiscoverProfiles();
            var activeFileName = ActiveProfileFileName;
            if (!profiles.Any(profile => string.Equals(profile.FileName, activeFileName, StringComparison.OrdinalIgnoreCase)))
                activeFileName = ChooseInitialProfile(profiles);

            var updated = ReadConfigFile(activeFileName);
            lock (_sync)
            {
                _profiles = profiles;
                _activeFileName = activeFileName;
                _current = updated;
            }

            ConfigurationChanged?.Invoke(updated);
            ProfilesChanged?.Invoke();
            Logger.Info($"Configuration profile '{activeFileName}' reloaded.");
        }
        catch (Exception ex)
        {
            Logger.Error("Configuration profile reload failed; keeping the last valid configuration", ex);
        }
    }

    private void PersistSelectedProfile(string fileName)
    {
        try { File.WriteAllText(_lastProfilePath, fileName); }
        catch (Exception ex) { Logger.Warn($"Unable to remember selected profile '{fileName}': {ex.Message}"); }
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _debounceTimer.Dispose();
    }
}