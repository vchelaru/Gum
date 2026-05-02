using Gum.Bundle;
using Gum.DataTypes;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// End-to-end tests for the loose-vs-bundle resolution rule (plan §4) layered on top of
/// <see cref="GumProjectSave.Load"/>. These exercise the full child-element walk, not just
/// the .gumx, by reusing the GumProject.zip fixture that
/// <see cref="GumProjectSaveTests.Load_ShouldLoadEntireProjectThroughCustomGetStreamFromFile_WhenAllFilesComeFromZip"/>
/// already proves loads correctly through the CustomGetStreamFromFile seam.
/// </summary>
public class GumServiceBundleLoadTests : BaseTestClass, IDisposable
{
    private readonly string _tempRoot;
    private readonly string _projectDir;
    private readonly string _gumxPath;
    private readonly string _gumpkgPath;
    private readonly Dictionary<string, byte[]> _entriesByRelativePath;

    public GumServiceBundleLoadTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "GumServiceBundleLoadTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempRoot);
        _projectDir = Path.Combine(_tempRoot, "GumProject");
        _gumxPath = Path.Combine(_projectDir, "FromZipFileGumProject.gumx");
        _gumpkgPath = Path.Combine(_projectDir, "FromZipFileGumProject.gumpkg");

        _entriesByRelativePath = ExtractZipToBundleEntries();
    }

    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void LoadProject_prefers_loose_provider_when_both_exist()
    {
        WriteEntriesAsLooseFiles(_projectDir);
        WriteBundle(_gumpkgPath, _entriesByRelativePath);

        BundleResolution resolution = GumBundleLoader.Resolve(_gumxPath);

        resolution.UsedBundle.ShouldBeFalse();
        FileManager.CustomGetStreamFromFile.ShouldBeNull();

        GumProjectSave? project = GumProjectSave.Load(resolution.ResolvedGumxPath, out GumLoadResult result);

        result.ErrorMessage.ShouldBeNullOrEmpty();
        project.ShouldNotBeNull();
        project!.Screens.ShouldNotBeEmpty();
    }

    [Fact]
    public void LoadProject_uses_bundle_provider_when_only_gumpkg_exists()
    {
        WriteBundle(_gumpkgPath, _entriesByRelativePath);

        BundleResolution resolution = GumBundleLoader.Resolve(_gumxPath);

        resolution.UsedBundle.ShouldBeTrue();
        FileManager.CustomGetStreamFromFile.ShouldNotBeNull();

        GumProjectSave? project = GumProjectSave.Load(resolution.ResolvedGumxPath, out GumLoadResult result);

        result.ErrorMessage.ShouldBeNullOrEmpty();
        result.MissingFiles.ShouldBeEmpty();
        project.ShouldNotBeNull();
        project!.Screens.ShouldNotBeEmpty();
        project.StandardElements.ShouldNotBeEmpty();
    }

    [Fact]
    public void LoadProject_uses_loose_provider_when_only_gumx_exists()
    {
        WriteEntriesAsLooseFiles(_projectDir);

        BundleResolution resolution = GumBundleLoader.Resolve(_gumxPath);

        resolution.UsedBundle.ShouldBeFalse();
        FileManager.CustomGetStreamFromFile.ShouldBeNull();

        GumProjectSave? project = GumProjectSave.Load(resolution.ResolvedGumxPath, out GumLoadResult result);

        result.ErrorMessage.ShouldBeNullOrEmpty();
        project.ShouldNotBeNull();
        project!.Screens.ShouldNotBeEmpty();
    }

    [Fact]
    public void LoadProject_via_bundle_produces_GumProjectSave_equivalent_to_loose_load()
    {
        // Loose load first.
        WriteEntriesAsLooseFiles(_projectDir);
        GumProjectSave? loose = GumProjectSave.Load(_gumxPath, out GumLoadResult looseResult);
        looseResult.ErrorMessage.ShouldBeNullOrEmpty();
        loose.ShouldNotBeNull();

        // Tear down the loose project and reset the hook between loads so the second load
        // genuinely goes through the bundle path.
        FileManager.CustomGetStreamFromFile = null;
        DeleteLooseProjectFiles(_projectDir);
        File.Exists(_gumxPath).ShouldBeFalse();

        WriteBundle(_gumpkgPath, _entriesByRelativePath);
        BundleResolution resolution = GumBundleLoader.Resolve(_gumxPath);
        resolution.UsedBundle.ShouldBeTrue();

        GumProjectSave? bundled = GumProjectSave.Load(resolution.ResolvedGumxPath, out GumLoadResult bundledResult);
        bundledResult.ErrorMessage.ShouldBeNullOrEmpty();
        bundledResult.MissingFiles.ShouldBeEmpty();
        bundled.ShouldNotBeNull();

        // Structural equivalence: same number of screens/components/standards, and a sampled
        // variable on the first screen comes through identically. Catches regressions where
        // bundle loading silently truncates the element walk.
        bundled!.Screens.Count.ShouldBe(loose!.Screens.Count);
        bundled.Components.Count.ShouldBe(loose.Components.Count);
        bundled.StandardElements.Count.ShouldBe(loose.StandardElements.Count);
        bundled.Behaviors.Count.ShouldBe(loose.Behaviors.Count);

        ScreenSave looseFirstScreen = loose.Screens.First();
        ScreenSave bundledFirstScreen = bundled.Screens.First(s => s.Name == looseFirstScreen.Name);
        bundledFirstScreen.Instances.Count.ShouldBe(looseFirstScreen.Instances.Count);
        bundledFirstScreen.DefaultState.Variables.Count.ShouldBe(looseFirstScreen.DefaultState.Variables.Count);
    }

    private static Dictionary<string, byte[]> ExtractZipToBundleEntries()
    {
        // Bundle entries are keyed by paths relative to the .gumx directory. The zip stores
        // everything under "GumProject/" — strip that prefix so keys are like "Screens/FirstScreen.gusx".
        const string zipPrefix = "GumProject/";
        string zipPath = Path.Combine(AppContext.BaseDirectory, "TestContent", "GumProject.zip");
        File.Exists(zipPath).ShouldBeTrue($"Test zip missing at {zipPath}");

        Dictionary<string, byte[]> entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        using FileStream fs = File.OpenRead(zipPath);
        using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName.EndsWith("/"))
            {
                continue;
            }

            string key = entry.FullName.StartsWith(zipPrefix, StringComparison.Ordinal)
                ? entry.FullName.Substring(zipPrefix.Length)
                : entry.FullName;

            using Stream s = entry.Open();
            using MemoryStream ms = new MemoryStream();
            s.CopyTo(ms);
            entries[key] = ms.ToArray();
        }
        return entries;
    }

    private void WriteEntriesAsLooseFiles(string projectDir)
    {
        Directory.CreateDirectory(projectDir);
        foreach (KeyValuePair<string, byte[]> kvp in _entriesByRelativePath)
        {
            string fullPath = Path.Combine(projectDir, kvp.Key.Replace('/', Path.DirectorySeparatorChar));
            string? parent = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            File.WriteAllBytes(fullPath, kvp.Value);
        }
    }

    private static void DeleteLooseProjectFiles(string projectDir)
    {
        if (Directory.Exists(projectDir))
        {
            foreach (string path in Directory.EnumerateFiles(projectDir, "*", SearchOption.AllDirectories))
            {
                File.Delete(path);
            }
        }
    }

    private static void WriteBundle(string path, IReadOnlyDictionary<string, byte[]> entries)
    {
        string? parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        List<(string, byte[])> tuples = new List<(string, byte[])>();
        foreach (KeyValuePair<string, byte[]> kvp in entries)
        {
            tuples.Add((kvp.Key, kvp.Value));
        }
        using FileStream fs = File.Create(path);
        GumBundleWriter.Write(fs, tuples);
    }
}
