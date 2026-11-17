using Gum;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.IO;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Hot reload writes a changed image's pixels into the cached texture with a real GraphicsDevice, after the
/// same load-time fix-up a first load applies (#8160).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class HotReloadReplacedImageTests : BaseTestClass
{
    private readonly string _directory;
    private readonly bool _previousPremultiplied = Renderer.UsePremultipliedAlpha;

    public HotReloadReplacedImageTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GumHotReloadReplacedImage8160_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public override void Dispose()
    {
        Renderer.UsePremultipliedAlpha = _previousPremultiplied;
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    private string WritePng(GraphicsDevice device, string name, int width, int height, XnaColor color)
    {
        string path = Path.Combine(_directory, name);
        using Texture2D source = new(device, width, height, false, SurfaceFormat.Color);
        XnaColor[] pixels = new XnaColor[width * height];
        Array.Fill(pixels, color);
        source.SetData(pixels);
        using FileStream stream = File.Create(path);
        source.SaveAsPng(stream, width, height);
        return path;
    }

    private static XnaColor PixelOf(Texture2D texture)
    {
        XnaColor[] pixels = new XnaColor[texture.Width * texture.Height];
        texture.GetData(pixels);
        return pixels[0];
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReplaceCachedTexture_SameSize_WritesTheFixedUpPixelsIntoTheSameTexture(bool premultiplied)
    {
        using DeviceGame game = new();
        game.RunOneFrame();
        Renderer.UsePremultipliedAlpha = premultiplied;
        LoaderManager.Self.CacheTextures = true;
        string path = WritePng(game.GraphicsDevice, "art.png", 4, 4, XnaColor.Red);
        Texture2D before = LoaderManager.Self.LoadContent<Texture2D>(path)!;

        WritePng(game.GraphicsDevice, "art.png", 4, 4, new XnaColor(0, 200, 0, 128));

        GumService.ReplaceCachedTexture(path).ShouldBe(CachedImageReplacement.Replaced);
        LoaderManager.Self.LoadContent<Texture2D>(path).ShouldBeSameAs(before);
        before.IsDisposed.ShouldBeFalse();
        XnaColor pixel = PixelOf(before);
        pixel.A.ShouldBe((byte)128);
        if (premultiplied)
        {
            pixel.G.ShouldBeInRange((byte)99, (byte)101);
        }
        else
        {
            pixel.G.ShouldBe((byte)200);
        }
    }

    [Fact]
    public void ReplaceCachedTexture_SizeChanged_KeepsTheOldPixels()
    {
        using DeviceGame game = new();
        game.RunOneFrame();
        LoaderManager.Self.CacheTextures = true;
        string path = WritePng(game.GraphicsDevice, "art.png", 4, 4, XnaColor.Red);
        Texture2D before = LoaderManager.Self.LoadContent<Texture2D>(path)!;

        WritePng(game.GraphicsDevice, "art.png", 8, 8, XnaColor.Blue);

        GumService.ReplaceCachedTexture(path).ShouldBe(CachedImageReplacement.SizeChanged);
        PixelOf(before).ShouldBe(XnaColor.Red);
        LoaderManager.Self.LoadContent<Texture2D>(path).ShouldBeSameAs(before);
    }

    private sealed class DeviceGame : SharedGameLease
    {
        protected override void OnInitialize() => GumService.Default.Initialize(GraphicsDevice);

        protected override void OnDispose()
        {
            if (GumService.Default.IsInitialized)
            {
                GumService.Default.Uninitialize();
            }
        }
    }
}
