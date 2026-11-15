using Gum.Bundle;
using Raylib_cs;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

/// <summary>
/// #6514: gumcli pack stores PNGs as raw pixels under the PNG's path. Raylib draws straight alpha,
/// so a straight package uploads untouched and a premultiplied one is converted with one warning
/// per package, matching the MonoGame loader. Raylib never bleeds transparent edges of loose PNGs, so
/// an unpremultiplied package texture is not bled either.
/// </summary>
public class RawTextureLoadTests : BaseTestClass
{
    private readonly string _projectDirectory;
    private readonly string _previousRelativeDirectory;
    private readonly Func<string, Stream>? _previousHook;
    private readonly bool _previousCacheTextures;
    private readonly Action<string> _previousReportWarning;
    private readonly List<string> _warnings = new();

    public RawTextureLoadTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumRaylibRawTexture_" + Guid.NewGuid().ToString("N"));
        _previousRelativeDirectory = FileManager.RelativeDirectory;
        _previousHook = FileManager.CustomGetStreamFromFile;
        _previousCacheTextures = LoaderManager.Self.CacheTextures;
        LoaderManager.Self.CacheTextures = false;
        _previousReportWarning = GumRawTexture.ReportWarning;
        GumRawTexture.ReportWarning = _warnings.Add;
        GumRawTexture.ResetWarnings();
    }

    public override void Dispose()
    {
        GumRawTexture.ReportWarning = _previousReportWarning;
        GumRawTexture.ResetWarnings();
        LoaderManager.Self.CacheTextures = _previousCacheTextures;
        FileManager.CustomGetStreamFromFile = _previousHook;
        FileManager.RelativeDirectory = _previousRelativeDirectory;
        try { Directory.Delete(_projectDirectory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    [Theory]
    [InlineData(false, new byte[] { 255, 128, 0, 128, 255, 128, 0, 0 }, false)]
    [InlineData(true, new byte[] { 255, 128, 0, 128, 0, 0, 0, 0 }, true)]
    public void RawTextureEntry_ShouldLoadAsStraightAlpha_AndWarnOnceOnMismatch(
        bool packageIsPremultiplied, byte[] expectedPixels, bool expectWarning)
    {
        byte[] storedPixels = packageIsPremultiplied
            ? new byte[] { 128, 64, 0, 128, 0, 0, 0, 0 }
            : new byte[] { 255, 128, 0, 128, 255, 128, 0, 0 };
        InstallBundle(new List<(string, byte[])>
        {
            ("Bundled.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
            ("Sprites/a.png", GumRawTexture.Encode(width: 2, height: 1, storedPixels, packageIsPremultiplied)),
            ("Sprites/b.png", GumRawTexture.Encode(width: 2, height: 1, storedPixels, packageIsPremultiplied)),
        });

        Texture2D first = LoaderManager.Self.LoadContent<Texture2D>("Sprites/a.png");
        LoaderManager.Self.LoadContent<Texture2D>("Sprites/b.png");

        first.Width.ShouldBe(2);
        first.Height.ShouldBe(1);
        ReadPixels(first).ShouldBe(expectedPixels);
        _warnings.Count.ShouldBe(expectWarning ? 1 : 0);
        if (expectWarning)
        {
            _warnings[0].ShouldContain("Bundled.gumpkg");
            _warnings[0].ShouldContain("raylib draws straight alpha");
            _warnings[0].ShouldContain("Repack it with 'gumcli pack' without --premultiplied-alpha");
            _warnings[0].ShouldNotContain("Renderer.UsePremultipliedAlpha");
        }
    }

    // A one-page font loads its page as a texture (covered above); two pages go through the atlas merge.
    [Fact]
    public void RawFontPageEntries_ShouldMergeIntoTheFontAtlas()
    {
        string fnt =
            "info face=\"Bundled\" size=-14 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
            "common lineHeight=16 base=12 scaleW=4 scaleH=2 pages=2 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n" +
            "page id=0 file=\"FontPage_0.png\"\n" +
            "page id=1 file=\"FontPage_1.png\"\n" +
            "chars count=2\n" +
            "char id=65 x=0 y=0 width=2 height=2 xoffset=0 yoffset=0 xadvance=3 page=0 chnl=15\n" +
            "char id=66 x=0 y=0 width=2 height=2 xoffset=0 yoffset=0 xadvance=3 page=1 chnl=15\n";
        byte[] firstPagePixels = new byte[4 * 2 * 4];
        Array.Fill(firstPagePixels, (byte)255);
        byte[] secondPagePixels = new byte[4 * 2 * 4];
        Array.Fill(secondPagePixels, (byte)7);
        InstallBundle(new List<(string, byte[])>
        {
            ("Bundled.gumx", Encoding.UTF8.GetBytes("<GumProjectSave />")),
            ("Fonts/Bundled.fnt", Encoding.UTF8.GetBytes(fnt)),
            ("Fonts/FontPage_0.png", GumRawTexture.Encode(width: 4, height: 2, firstPagePixels, isPremultiplied: false)),
            ("Fonts/FontPage_1.png", GumRawTexture.Encode(width: 4, height: 2, secondPagePixels, isPremultiplied: false)),
        });

        Font font = LoaderManager.Self.LoadContent<Font>(Path.Combine(_projectDirectory, "Fonts", "Bundled.fnt"));

        font.GlyphCount.ShouldBe(2);
        font.Texture.Width.ShouldBe(4);
        font.Texture.Height.ShouldBe(4);
        ReadPixels(font.Texture).ShouldBe(firstPagePixels.Concat(secondPagePixels).ToArray());
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

        GumBundleFormatException exception =
            Should.Throw<GumBundleFormatException>(() => LoaderManager.Self.LoadContent<Texture2D>("Sprites/a.png"));

        exception.Message.ShouldContain("Raw texture pixel data is truncated");
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

    private static unsafe byte[] ReadPixels(Texture2D texture)
    {
        Image image = Raylib.LoadImageFromTexture(texture);
        try
        {
            Raylib.ImageFormat(ref image, PixelFormat.UncompressedR8G8B8A8);
            byte[] pixels = new byte[image.Width * image.Height * 4];
            System.Runtime.InteropServices.Marshal.Copy((IntPtr)image.Data, pixels, 0, pixels.Length);
            return pixels;
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }
}
