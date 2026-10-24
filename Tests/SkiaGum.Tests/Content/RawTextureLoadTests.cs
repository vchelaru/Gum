using Gum.Bundle;
using SkiaGum.Content;
using SkiaSharp;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ToolsUtilities;
using Xunit;

// SkiaResourceManager is the loader every Skia host uses until it is removed.
#pragma warning disable CS0618

namespace SkiaGum.Tests.Content;

/// <summary>
/// #6514: gumcli pack stores PNGs as raw pixels under the PNG's path. Skia draws premultiplied
/// alpha, so a straight package is premultiplied at load and a premultiplied one loads untouched.
/// Neither warns: gumcli always packs straight alpha and Skia is always premultiplied, so repacking
/// cannot remove the conversion.
/// </summary>
public class RawTextureLoadTests : BaseTestClass
{
    private readonly string _projectDirectory;
    private readonly string _previousRelativeDirectory;
    private readonly Func<string, Stream>? _previousHook;
    private readonly Action<string> _previousReportWarning;
    private readonly List<string> _warnings = new();

    public RawTextureLoadTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumSkiaRawTexture_" + Guid.NewGuid().ToString("N"));
        _previousRelativeDirectory = FileManager.RelativeDirectory;
        _previousHook = FileManager.CustomGetStreamFromFile;
        _previousReportWarning = GumRawTexture.ReportWarning;
        GumRawTexture.ReportWarning = _warnings.Add;
        GumRawTexture.ResetWarnings();
    }

    public override void Dispose()
    {
        GumRawTexture.ReportWarning = _previousReportWarning;
        GumRawTexture.ResetWarnings();
        FileManager.CustomGetStreamFromFile = _previousHook;
        FileManager.RelativeDirectory = _previousRelativeDirectory;
        try { Directory.Delete(_projectDirectory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RawTextureEntry_ShouldLoadAsPremultipliedAlpha_WithoutWarning(bool packageIsPremultiplied)
    {
        byte[] storedPixels = packageIsPremultiplied
            ? new byte[] { 128, 64, 0, 128, 0, 0, 0, 0 }
            : new byte[] { 255, 128, 0, 128, 255, 128, 0, 0 };
        byte[] expectedPixels = { 128, 64, 0, 128, 0, 0, 0, 0 };
        InstallBundle(new List<(string, byte[])>
        {
            ("Bundled.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
            ("Sprites/a.png", GumRawTexture.Encode(width: 2, height: 1, storedPixels, packageIsPremultiplied)),
        });

        SKBitmap loaded = SkiaResourceManager.GetSKBitmap(Path.Combine(_projectDirectory, "Sprites", "a.png"));

        loaded.Width.ShouldBe(2);
        loaded.Height.ShouldBe(1);
        loaded.ColorType.ShouldBe(SKColorType.Rgba8888);
        loaded.AlphaType.ShouldBe(SKAlphaType.Premul);
        loaded.Bytes.ShouldBe(expectedPixels);
        _warnings.ShouldBeEmpty();
    }

    [Fact]
    public void RawTextureEntry_WithTruncatedPixels_ShouldThrowGumBundleFormatException()
    {
        byte[] entry = GumRawTexture.Encode(width: 2, height: 2, new byte[16], isPremultiplied: false);
        InstallBundle(new List<(string, byte[])>
        {
            ("Bundled.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
            ("Sprites/a.png", entry.AsSpan(0, entry.Length - 4).ToArray()),
        });
        string path = Path.Combine(_projectDirectory, "Sprites", "a.png");

        GumBundleFormatException exception =
            Should.Throw<GumBundleFormatException>(() => SkiaResourceManager.GetSKBitmap(path));

        exception.Message.ShouldContain("Raw texture pixel data is truncated");
        SkiaResourceManager.IsCached(path).ShouldBeFalse();
    }

    private void InstallBundle(List<(string path, byte[] content)> entries)
    {
        Directory.CreateDirectory(_projectDirectory);
        string bundlePath = Path.Combine(_projectDirectory, "Bundled.gumpkg");
        using (FileStream stream = File.Create(bundlePath))
        {
            GumBundleWriter.Write(stream, entries);
        }

        GumBundleLoader.Resolve(bundlePath).UsedBundle.ShouldBeTrue();
        FileManager.RelativeDirectory = _projectDirectory + Path.DirectorySeparatorChar;
    }
}
