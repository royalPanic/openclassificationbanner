using System.IO;
using Microsoft.Win32;

namespace OpenClassBanner;

internal sealed record ResolvedInstallPaths(string ConfigurationDirectory, string Scope);

internal static class InstallPathResolver
{
    private const string RegistryKeyPath = @"Software\OpenClassBanner";

    public static ResolvedInstallPaths Resolve(string applicationDirectory)
    {
        var normalizedApplicationDirectory = NormalizePath(applicationDirectory);
        return TryResolveFromRegistry(Registry.CurrentUser, "per-user", normalizedApplicationDirectory)
            ?? TryResolveFromRegistry(Registry.LocalMachine, "machine-wide", normalizedApplicationDirectory)
            ?? new ResolvedInstallPaths(applicationDirectory, "portable/development");
    }

    private static ResolvedInstallPaths? TryResolveFromRegistry(RegistryKey hive, string scope, string normalizedApplicationDirectory)
    {
        try
        {
            using var key = hive.OpenSubKey(RegistryKeyPath);
            var installDirectory = key?.GetValue("InstallDir") as string;
            var configDirectory = key?.GetValue("ConfigDir") as string;
            if (installDirectory is null || configDirectory is null)
                return null;

            if (string.Equals(NormalizePath(installDirectory), normalizedApplicationDirectory, StringComparison.OrdinalIgnoreCase))
                return new ResolvedInstallPaths(configDirectory, scope);
        }
        catch (Exception ex)
        {
            Logger.Debug($"Unable to read installation configuration path: {ex.Message}");
        }

        return null;
    }

    private static string NormalizePath(string path) => global::System.IO.Path.GetFullPath(path).TrimEnd(global::System.IO.Path.DirectorySeparatorChar, global::System.IO.Path.AltDirectorySeparatorChar);
}
