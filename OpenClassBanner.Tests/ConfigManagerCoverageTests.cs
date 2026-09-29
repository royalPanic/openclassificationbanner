using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenClassBanner;

namespace OpenClassBanner.Tests;

[TestClass]
public sealed class ConfigManagerCoverageTests
{
    [TestMethod]
    public void ConstructorCreatesDefaultProfileWhenDirectoryIsEmpty()
    {
        using var directory = new TestDirectory();
        using var manager = new ConfigManager(directory.Path);

        Assert.AreEqual("example-config.json", manager.ActiveProfileFileName);
        Assert.AreEqual("UNCLASSIFIED", manager.Current.BannerText);
        Assert.AreEqual("#007A33", manager.Current.BackgroundColor);
        Assert.IsTrue(File.Exists(Path.Combine(directory.Path, "example-config.json")));
        Assert.AreEqual(1, manager.Profiles.Count);
    }

    [TestMethod]
    public void ConstructorUsesProfileNameAndMapsLegacyBannerText()
    {
        using var directory = new TestDirectory();
        directory.WriteProfile("legacy.json", new { BannerText = "CONFIDENTIAL", BackgroundColor = "#112233", ForegroundColor = "#FFFFFF", HeightPx = 48, FontSize = 20, FontFamily = "Arial" });

        using var manager = new ConfigManager(directory.Path);
        manager.SelectProfile("legacy.json");

        Assert.AreEqual("legacy", manager.Current.Name);
        Assert.AreEqual("CONFIDENTIAL", manager.Current.CenterText);
        Assert.AreEqual("legacy.json", manager.ActiveProfileFileName);
    }

    [TestMethod]
    public void ProfilesAreSortedByDisplayNameAndExcludeInvalidJson()
    {
        using var directory = new TestDirectory();
        directory.WriteProfile("zulu.json", new { Name = "Zulu", BannerText = "Z" });
        directory.WriteProfile("alpha.json", new { Name = "Alpha", BannerText = "A" });
        File.WriteAllText(Path.Combine(directory.Path, "broken.json"), "not json");

        using var manager = new ConfigManager(directory.Path);

        CollectionAssert.AreEqual(new[] { "Alpha", "Example Configuration", "Zulu" }, manager.Profiles.Select(profile => profile.DisplayName).ToArray());
    }

    [TestMethod]
    public void ConstructorRestoresRememberedProfile()
    {
        using var directory = new TestDirectory();
        directory.WriteProfile("first.json", new { Name = "First", BannerText = "FIRST" });
        directory.WriteProfile("second.json", new { Name = "Second", BannerText = "SECOND" });
        File.WriteAllText(Path.Combine(directory.Path, "last-profile.txt"), "SECOND.JSON\n");

        using var manager = new ConfigManager(directory.Path);

        Assert.AreEqual("second.json", manager.ActiveProfileFileName);
        Assert.AreEqual("SECOND", manager.Current.CenterText);
    }

    [TestMethod]
    public void SelectProfileRaisesEventsAndPersistsSelection()
    {
        using var directory = new TestDirectory();
        directory.WriteProfile("alpha.json", new { Name = "Alpha", BannerText = "ALPHA" });
        directory.WriteProfile("beta.json", new { Name = "Beta", BannerText = "BETA" });
        using var manager = new ConfigManager(directory.Path);
        BannerConfig? changed = null;
        var profilesChanged = 0;
        manager.ConfigurationChanged += config => changed = config;
        manager.ProfilesChanged += () => profilesChanged++;

        manager.SelectProfile("BETA.JSON");

        Assert.IsNotNull(changed);
        Assert.AreEqual("BETA", changed!.CenterText);
        Assert.AreEqual("beta.json", manager.ActiveProfileFileName);
        Assert.AreEqual("beta.json", File.ReadAllText(Path.Combine(directory.Path, "last-profile.txt")));
        Assert.AreEqual(1, profilesChanged);
    }

    [TestMethod]
    public void SelectProfileIgnoresUnknownProfile()
    {
        using var directory = new TestDirectory();
        using var manager = new ConfigManager(directory.Path);
        var changed = 0;
        manager.ConfigurationChanged += _ => changed++;
        var original = manager.ActiveProfileFileName;

        manager.SelectProfile("missing.json");

        Assert.AreEqual(original, manager.ActiveProfileFileName);
        Assert.AreEqual(0, changed);
    }

    [TestMethod]
    public void ForceReloadReadsUpdatedConfigAndRaisesEvent()
    {
        using var directory = new TestDirectory();
        directory.WriteProfile("config.json", new { Name = "Initial", BannerText = "INITIAL", BackgroundColor = "#007A33", ForegroundColor = "#FFFFFF", HeightPx = 36, FontSize = 16, FontFamily = "Segoe UI" });
        File.WriteAllText(Path.Combine(directory.Path, "last-profile.txt"), "config.json");
        using var manager = new ConfigManager(directory.Path);
        BannerConfig? observed = null;
        manager.ConfigurationChanged += config => observed = config;

        directory.WriteProfile("config.json", new { Name = "Updated", BannerText = "UPDATED", BackgroundColor = "#0000FF", ForegroundColor = "#FFFFFF", HeightPx = 48, FontSize = 20, FontFamily = "Arial" });
        manager.ForceReload();

        Assert.IsNotNull(observed);
        Assert.AreEqual("UPDATED", observed!.BannerText);
        Assert.AreEqual("#0000FF", manager.Current.BackgroundColor);
        Assert.AreEqual(48, manager.Current.HeightPx);
    }

    [TestMethod]
    public void ForceReloadKeepsLastValidConfigWhenNewConfigIsInvalid()
    {
        using var directory = new TestDirectory();
        directory.WriteProfile("config.json", new { Name = "Valid", BannerText = "VALID" });
        File.WriteAllText(Path.Combine(directory.Path, "last-profile.txt"), "config.json");
        using var manager = new ConfigManager(directory.Path);
        var original = manager.Current;
        var changed = 0;
        manager.ConfigurationChanged += _ => changed++;

        File.WriteAllText(Path.Combine(directory.Path, "config.json"), "{\"Name\":\"Invalid\",\"HeightPx\":201}");
        manager.ForceReload();

        Assert.AreEqual("VALID", manager.Current.CenterText);
        Assert.AreEqual(original.HeightPx, manager.Current.HeightPx);
        Assert.AreEqual(0, changed);
    }

    [TestMethod]
    [DataRow("#12345")]
    [DataRow("red")]
    public void InvalidColorIsRejected(string color)
    {
        using var directory = new TestDirectory();
        directory.WriteProfile("invalid.json", new { Name = "Invalid", BannerText = "X", BackgroundColor = color });

        using var manager = new ConfigManager(directory.Path);

        Assert.IsFalse(manager.Profiles.Any(profile => profile.FileName == "invalid.json"));
    }

    [TestMethod]
    public void InvalidNumericRangesAreRejected()
    {
        using var directory = new TestDirectory();
        directory.WriteProfile("too-short.json", new { Name = "Short", BannerText = "X", HeightPx = 11 });
        directory.WriteProfile("too-large-font.json", new { Name = "Large Font", BannerText = "X", FontSize = 97 });
        directory.WriteProfile("negative-offset.json", new { Name = "Negative Offset", BannerText = "X", VerticalTextOffsetPx = -1 });

        using var manager = new ConfigManager(directory.Path);

        Assert.IsFalse(manager.Profiles.Any(profile => profile.FileName == "too-short.json"));
        Assert.IsFalse(manager.Profiles.Any(profile => profile.FileName == "too-large-font.json"));
        Assert.IsFalse(manager.Profiles.Any(profile => profile.FileName == "negative-offset.json"));
    }

    [TestMethod]
    public void ProfilesReturnsSnapshotUntilReload()
    {
        using var directory = new TestDirectory();
        using var manager = new ConfigManager(directory.Path);
        var initialProfiles = manager.Profiles;
        directory.WriteProfile("new.json", new { Name = "New", BannerText = "NEW" });

        Assert.AreEqual(1, initialProfiles.Count);
        Assert.AreEqual(1, manager.Profiles.Count);

        manager.ForceReload();

        Assert.IsTrue(manager.Profiles.Any(profile => profile.FileName == "new.json"));
    }

    [TestMethod]
    public void GeneratedRuntimeJsonFilesAreNotDiscoveredAsProfiles()
    {
        using var directory = new TestDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "app.deps.json"), "{\"Name\":\"Runtime\"}");
        File.WriteAllText(Path.Combine(directory.Path, "app.runtimeconfig.json"), "{\"Name\":\"Runtime\"}");
        File.WriteAllText(Path.Combine(directory.Path, "app.assets.json"), "{\"Name\":\"Runtime\"}");

        using var manager = new ConfigManager(directory.Path);

        Assert.IsFalse(manager.Profiles.Any(profile => profile.FileName.Contains("deps", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(manager.Profiles.Any(profile => profile.FileName.Contains("runtimeconfig", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(manager.Profiles.Any(profile => profile.FileName.Contains("assets", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void DisposeCanBeCalledMoreThanOnce()
    {
        using var directory = new TestDirectory();
        var manager = new ConfigManager(directory.Path);

        manager.Dispose();
        manager.Dispose();
    }
}
