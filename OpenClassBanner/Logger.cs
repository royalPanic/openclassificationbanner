using System.IO;
using System.Globalization;

namespace OpenClassBanner;

public static class Logger
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private const int RetainedFiles = 3;
    private static readonly object Sync = new();
    private static string? _logPath;

    public static void Initialize()
    {
        lock (Sync)
        {
            _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenClassBanner", "logs", "agent.log");
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        }
    }

    public static void Debug(string message) => Write("DEBUG", message);
    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? exception = null) => Write("ERROR", exception is null ? message : $"{message}: {exception}");

    private static void Write(string level, string message)
    {
        lock (Sync)
        {
            if (_logPath is null)
                return;

            try
            {
                RotateIfNeeded();
                File.AppendAllText(_logPath, $"{DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture)} [{level}] {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }

    private static void RotateIfNeeded()
    {
        if (_logPath is null || !File.Exists(_logPath) || new FileInfo(_logPath).Length < MaxFileSize)
            return;

        for (var index = RetainedFiles - 1; index >= 1; index--)
        {
            var source = $"{_logPath}.{index}";
            var destination = $"{_logPath}.{index + 1}";
            if (File.Exists(source))
                File.Move(source, destination, true);
        }

        File.Move(_logPath, $"{_logPath}.1", true);
    }
}
