using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gum.Bundle;
using Shouldly;
using ToolsUtilities;

namespace Gum.Bundle.Tests;

/// <summary>
/// Tests for <see cref="GumBundleLoader"/>. The hook seam (FileManager.CustomGetStreamFromFile)
/// is global mutable state, so each test stashes the previous value in the constructor and
/// restores it in Dispose to guarantee isolation regardless of pass/fail order.
/// </summary>
public class GumBundleLoaderTests : IDisposable
{
    private readonly Func<string, Stream>? _previousHook;
    private readonly string _tempDir;

    public GumBundleLoaderTests()
    {
        _previousHook = FileManager.CustomGetStreamFromFile;
        FileManager.CustomGetStreamFromFile = null;
        _tempDir = Path.Combine(Path.GetTempPath(), "GumBundleLoaderTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _previousHook;
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void Resolve_with_gumpkg_extension_installs_hook_that_serves_child_element_files_from_bundle()
    {
        const string gumxXml = "<GumProjectSave />";
        byte[] childBytes = Encoding.UTF8.GetBytes("<ScreenSave Name=\"MainScreen\" />");

        string gumpkgPath = Path.Combine(_tempDir, "Project.gumpkg");
        WriteBundle(gumpkgPath, new (string, byte[])[]
        {
            ("Project.gumx", Encoding.UTF8.GetBytes(gumxXml)),
            ("Screens/MainScreen.gusx", childBytes),
        });

        ProjectResolution resolution = GumBundleLoader.Resolve(gumpkgPath);

        resolution.UsedBundle.ShouldBeTrue();
        resolution.FileProvider.ShouldBeOfType<BundleGumFileProvider>();
        FileManager.CustomGetStreamFromFile.ShouldNotBeNull();

        // The runtime loader passes absolute paths (with platform-native separators) to the
        // hook for child .gusx/.gucx files; this mirrors the behavior exercised in
        // GumProjectSaveTests.Load_ShouldLoadEntireProjectThroughCustomGetStreamFromFile_*.
        string childAbsolutePath = Path.Combine(_tempDir, "Screens", "MainScreen.gusx");
        using Stream stream = FileManager.CustomGetStreamFromFile!(childAbsolutePath);
        stream.ShouldNotBeNull();

        using MemoryStream copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.ToArray().ShouldBe(childBytes);
    }

    [Fact]
    public void Resolve_with_gumpkg_extension_returns_sibling_gumx_path_for_GumProjectSave_Load()
    {
        // The bundle stores its .gumx by name; the resolved path handed to GumProjectSave.Load
        // is the sibling .gumx path so the installed hook can intercept the read.
        byte[] gumxBytes = Encoding.UTF8.GetBytes("<GumProjectSave />");
        string gumpkgPath = Path.Combine(_tempDir, "Project.gumpkg");
        string expectedGumxPath = Path.Combine(_tempDir, "Project.gumx");
        WriteBundle(gumpkgPath, new (string, byte[])[]
        {
            ("Project.gumx", gumxBytes),
        });

        ProjectResolution resolution = GumBundleLoader.Resolve(gumpkgPath);

        resolution.UsedBundle.ShouldBeTrue();
        resolution.ResolvedGumxPath.ShouldBe(expectedGumxPath);
        FileManager.CustomGetStreamFromFile.ShouldNotBeNull();

        using Stream stream = FileManager.CustomGetStreamFromFile!(expectedGumxPath);
        using MemoryStream copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.ToArray().ShouldBe(gumxBytes);
    }

    [Fact]
    public void Resolve_with_gumx_extension_leaves_hook_untouched()
    {
        string gumxPath = Path.Combine(_tempDir, "Project.gumx");
        File.WriteAllText(gumxPath, "<GumProjectSave />");

        ProjectResolution resolution = GumBundleLoader.Resolve(gumxPath);

        resolution.UsedBundle.ShouldBeFalse();
        resolution.ResolvedGumxPath.ShouldBe(gumxPath);
        resolution.FileProvider.ShouldBeOfType<LooseFileGumFileProvider>();
        FileManager.CustomGetStreamFromFile.ShouldBeNull();
    }

    [Fact]
    public void Resolve_with_gumx_extension_does_not_probe_for_sibling_gumpkg_even_when_present()
    {
        // Cross-platform consistency: a .gumx caller gets loose mode regardless of whether a
        // .gumpkg happens to sit next to it. This avoids the "works on desktop but 404s in
        // Blazor" surprise the previous sibling-probing behavior caused.
        string gumxPath = Path.Combine(_tempDir, "Project.gumx");
        string gumpkgPath = Path.Combine(_tempDir, "Project.gumpkg");
        File.WriteAllText(gumxPath, "<GumProjectSave />");
        WriteBundle(gumpkgPath, new (string, byte[])[]
        {
            ("Project.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
        });

        ProjectResolution resolution = GumBundleLoader.Resolve(gumxPath);

        resolution.UsedBundle.ShouldBeFalse();
        FileManager.CustomGetStreamFromFile.ShouldBeNull();
    }

    [Fact]
    public void Resolve_with_gumpkg_extension_preserves_existing_user_hook_as_fallback()
    {
        // User has their own hook installed (e.g. they're loading other content from a zip).
        // Bundle resolution must compose, not clobber: bundle entries come first, falls back
        // to user hook on miss.
        byte[] gumxBytes = Encoding.UTF8.GetBytes("<GumProjectSave />");
        byte[] userBytes = Encoding.UTF8.GetBytes("from-user");

        string gumpkgPath = Path.Combine(_tempDir, "Project.gumpkg");
        string expectedGumxPath = Path.Combine(_tempDir, "Project.gumx");
        WriteBundle(gumpkgPath, new (string, byte[])[]
        {
            ("Project.gumx", gumxBytes),
        });

        bool userHookCalled = false;
        Func<string, Stream> userHook = path =>
        {
            userHookCalled = true;
            return new MemoryStream(userBytes);
        };
        FileManager.CustomGetStreamFromFile = userHook;

        ProjectResolution resolution = GumBundleLoader.Resolve(gumpkgPath);

        resolution.UsedBundle.ShouldBeTrue();
        FileManager.CustomGetStreamFromFile.ShouldNotBeSameAs(userHook);

        // Bundle hit: served from bundle, user hook NOT called.
        using (Stream s = FileManager.CustomGetStreamFromFile!(expectedGumxPath))
        {
            using MemoryStream copy = new MemoryStream();
            s.CopyTo(copy);
            copy.ToArray().ShouldBe(gumxBytes);
        }
        userHookCalled.ShouldBeFalse();

        // Bundle miss: falls back to user hook.
        string somethingElse = Path.Combine(_tempDir, "Other", "thing.png");
        using (Stream s = FileManager.CustomGetStreamFromFile!(somethingElse))
        {
            using MemoryStream copy = new MemoryStream();
            s.CopyTo(copy);
            copy.ToArray().ShouldBe(userBytes);
        }
        userHookCalled.ShouldBeTrue();
    }

    [Fact]
    public void Resolve_with_gumpkg_extension_throws_FileNotFoundException_when_bundle_missing()
    {
        string gumpkgPath = Path.Combine(_tempDir, "Missing.gumpkg");

        Should.Throw<FileNotFoundException>(() => GumBundleLoader.Resolve(gumpkgPath));
    }

    [Fact]
    public void Resolve_throws_ArgumentException_for_unrecognized_extension()
    {
        string badPath = Path.Combine(_tempDir, "Project.gum");

        Should.Throw<ArgumentException>(() => GumBundleLoader.Resolve(badPath));
    }

    private static void WriteBundle(string path, IEnumerable<(string, byte[])> entries)
    {
        using FileStream fs = File.Create(path);
        GumBundleWriter.Write(fs, entries);
    }
}
