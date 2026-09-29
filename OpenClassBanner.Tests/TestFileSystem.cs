using System.IO;
using System.Text.Json;

namespace OpenClassBanner.Tests;

internal sealed class TestDirectory : IDisposable
{
    public TestDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "OpenClassBannerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string WriteProfile(string fileName, object profile)
    {
        var path = System.IO.Path.Combine(Path, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(profile));
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, true);
    }
}
