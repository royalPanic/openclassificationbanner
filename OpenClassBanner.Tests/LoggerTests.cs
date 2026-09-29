using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenClassBanner;

namespace OpenClassBanner.Tests;

[TestClass]
public sealed class LoggerTests
{
    [TestMethod]
    public void InitializeAndLogWritesStructuredEntry()
    {
        Logger.Initialize();
        var marker = $"unit-test-{Guid.NewGuid():N}";
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenClassBanner", "logs", "agent.log");

        Logger.Info(marker);

        Assert.IsTrue(File.Exists(logPath));
        StringAssert.Contains(File.ReadAllText(logPath), $"[INFO] {marker}");
    }

    [TestMethod]
    public void ErrorLogIncludesExceptionDetails()
    {
        Logger.Initialize();
        var marker = $"error-test-{Guid.NewGuid():N}";

        Logger.Error(marker, new InvalidOperationException("expected failure"));

        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenClassBanner", "logs", "agent.log");
        var contents = File.ReadAllText(logPath);
        StringAssert.Contains(contents, $"[ERROR] {marker}");
        StringAssert.Contains(contents, "expected failure");
    }
}
