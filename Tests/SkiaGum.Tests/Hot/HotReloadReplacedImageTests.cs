using System;
using System.IO;
using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace SkiaGum.Tests.Hot;

/// <summary>
/// A changed image keeps its cached SKBitmap instance and gets the new pixels, and Sprites under the roots draw
/// them (#8160).
/// </summary>
public class HotReloadReplacedImageTests : IDisposable
{
    private readonly SKSurface _surface;
    private readonly string _directory;
    private readonly SKFilterMode _savedFilter = global::RenderingLibrary.Graphics.Renderer.TextureFilter;

    public HotReloadReplacedImageTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(8, 8));
        GumService.Default.Initialize(_surface.Canvas, 8, 8);
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
        _directory = Path.Combine(Path.GetTempPath(), "SkiaHotReloadReplacedImage8160_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = _savedFilter;
        try { Directory.Delete(_directory, recursive: true); } catch { /* best-effort */ }
    }

    private string WritePng(string name, int width, int height, SKColor color)
    {
        string path = Path.Combine(_directory, name);
        using SKBitmap bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.Erase(color);
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    private SpriteRuntime ShowSprite(string path)
    {
        SpriteRuntime sprite = new SpriteRuntime();
        sprite.SourceFileName = path;
        sprite.Width = 4;
        sprite.Height = 4;
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        GumService.Default.Root.Children.Add(sprite);
        return sprite;
    }

    private SKColor DrawnPixel()
    {
        _surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Draw();
        using SKImage snapshot = _surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(snapshot);
        return bitmap.GetPixel(1, 1);
    }

    [Fact]
    public void ReplaceCachedBitmap_SameSize_DrawsTheNewPixelsFromTheSameBitmap()
    {
        string path = WritePng("art.png", 4, 4, SKColors.Red);
        SpriteRuntime sprite = ShowSprite(path);
        SKBitmap before = sprite.Texture!;
        DrawnPixel().ShouldBe(SKColors.Red);

        WritePng("art.png", 4, 4, SKColors.Blue);
        CachedImageReplacement result = ContentLoader.ReplaceCachedBitmap(path);

        result.ShouldBe(CachedImageReplacement.Replaced);
        sprite.Texture.ShouldBeSameAs(before);
        LoaderManager.Self.LoadContent<SKBitmap>(path).ShouldBeSameAs(before);
        DrawnPixel().ShouldBe(SKColors.Blue);
    }

    // A CPU raster image shares the bitmap's memory, but a GPU surface caches an image's pixels by its id, so
    // the sprite needs an image made after the pixels changed.
    [Fact]
    public void ReplaceCachedBitmap_SameSize_GivesTheSpriteANewImage()
    {
        string path = WritePng("art.png", 4, 4, SKColors.Red);
        SpriteRuntime sprite = ShowSprite(path);
        SkiaGum.Renderables.Sprite renderable = (SkiaGum.Renderables.Sprite)sprite.RenderableComponent!;
        uint imageBefore = renderable.Image!.UniqueId;

        WritePng("art.png", 4, 4, SKColors.Blue);
        ContentLoader.ReplaceCachedBitmap(path).ShouldBe(CachedImageReplacement.Replaced);

        renderable.Image!.UniqueId.ShouldNotBe(imageBefore);
    }

    // Visuals hold the bitmap directly; one that no GumService root reaches (a NineSlice built by code,
    // another service's tree) still makes a new image on its next draw.
    [Fact]
    public void ReplaceCachedBitmap_SameSize_NineSliceUnderNoRootDrawsTheNewPixels()
    {
        string path = WritePng("art.png", 4, 4, SKColors.Red);
        NineSliceRuntime nineSlice = new NineSliceRuntime();
        nineSlice.SetProperty("SourceFile", path);
        SkiaGum.Renderables.NineSlice renderable = (SkiaGum.Renderables.NineSlice)nineSlice.RenderableComponent!;
        uint imageBefore = renderable.Image!.UniqueId;

        WritePng("art.png", 4, 4, SKColors.Blue);
        ContentLoader.ReplaceCachedBitmap(path).ShouldBe(CachedImageReplacement.Replaced);

        SKImage after = renderable.Image!;
        after.UniqueId.ShouldNotBe(imageBefore);
        using SKBitmap pixels = SKBitmap.FromImage(after);
        pixels.GetPixel(1, 1).ShouldBe(SKColors.Blue);
    }

    // A bitmap Gum did not rewrite keeps the image it has: code that edits a bitmap refreshes it itself (#7882).
    [Fact]
    public void ReplaceCachedBitmap_OtherBitmap_KeepsItsImage()
    {
        string path = WritePng("art.png", 4, 4, SKColors.Red);
        string otherPath = WritePng("other.png", 4, 4, SKColors.Red);
        ShowSprite(path);
        SpriteRuntime other = ShowSprite(otherPath);
        SkiaGum.Renderables.Sprite renderable = (SkiaGum.Renderables.Sprite)other.RenderableComponent!;
        SKImage imageBefore = renderable.Image!;

        WritePng("art.png", 4, 4, SKColors.Blue);
        ContentLoader.ReplaceCachedBitmap(path).ShouldBe(CachedImageReplacement.Replaced);

        renderable.Image.ShouldBeSameAs(imageBefore);
    }

    [Fact]
    public void ReplaceCachedBitmap_TranslucentPixels_MatchWhatAFreshLoadGives()
    {
        string path = WritePng("art.png", 4, 4, SKColors.Red);
        SpriteRuntime sprite = ShowSprite(path);

        SKColor translucent = new SKColor(0, 200, 0, 128);
        WritePng("art.png", 4, 4, translucent);
        ContentLoader.ReplaceCachedBitmap(path).ShouldBe(CachedImageReplacement.Replaced);

        using SKBitmap fresh = SKBitmap.Decode(path);
        sprite.Texture!.GetPixel(1, 1).ShouldBe(fresh.GetPixel(1, 1));
    }

    [Fact]
    public void ReplaceCachedBitmap_SizeChanged_KeepsTheOldPixels()
    {
        string path = WritePng("art.png", 4, 4, SKColors.Red);
        ShowSprite(path);

        WritePng("art.png", 8, 8, SKColors.Blue);

        ContentLoader.ReplaceCachedBitmap(path).ShouldBe(CachedImageReplacement.SizeChanged);
        DrawnPixel().ShouldBe(SKColors.Red);
    }

    [Fact]
    public void ReplaceCachedBitmap_FileNotCached_ReplacesNothing()
    {
        string path = WritePng("never-loaded.png", 4, 4, SKColors.Red);

        ContentLoader.ReplaceCachedBitmap(path).ShouldBe(CachedImageReplacement.NotReplaced);
    }

    // SVG and Lottie documents are cached apart from bitmaps and are not reloaded: the result says a restart is needed.
    [Theory]
    [InlineData("svg", "art.svg",
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"red\"/></svg>")]
    [InlineData("lottie", "art.json",
        "{\"v\":\"5.5.2\",\"fr\":30,\"ip\":0,\"op\":30,\"w\":10,\"h\":10,\"nm\":\"t\",\"ddd\":0,\"assets\":[],\"layers\":[]}")]
    public void PerformFileReload_SvgOrLottieReplaced_ReportsThatARestartIsNeeded(string kind, string fileName, string content)
    {
        string path = Path.Combine(_directory, fileName);
        File.WriteAllText(path, content);
        GraphicalUiElement runtime = kind == "svg" ? new SvgRuntime() : new LottieAnimationRuntime();
        runtime.SetProperty("SourceFile", path);
        GumService.Default.Root.Children.Add(runtime);
        GumHotReloadManager manager = new GumHotReloadManager(_ => { }, (_, _) => 0, LoaderManager.Self.Dispose)
        {
            ReplaceCachedImage = ContentLoader.ReplaceCachedBitmap
        };
        HotReloadResult? result = null;
        manager.ReloadFinished += finished => result = finished;

        File.WriteAllText(path, content.Replace("10", "12"));
        manager.PerformFileReload(new[] { GumService.Default.Root }, new[] { path });

        result.ShouldNotBeNull();
        result.MatchesRestart.ShouldBeFalse();
        result.Issues.ShouldHaveSingleItem().Kind.ShouldBe(HotReloadIssueKind.ReloadFailed);
        result.Issues[0].Message.ShouldContain("restarts");
    }
}
