using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Bitmaps Gum did not load (#7876): one the caller made and assigned, one a host's own IContentLoader
/// returns, and the pooled bitmap the color picker re-uploads. Gum leaves them mutable and each renderable
/// keeps its own snapshot, which is the pre-sharing behavior and the documented limit.
/// </summary>
public class SharedTextureCallerBitmapTests : SharedTextureTestBase
{
    private static readonly SKColor Edited = new(250, 250, 250);

    // A host's loader (Unity, SkiaGameRendering) that hands back a bitmap it decoded itself, mutable.
    private sealed class HostContentLoader : IContentLoader
    {
        public SKBitmap? Bitmap { get; set; }

        public T? LoadContent<T>(string contentName) =>
            typeof(T) == typeof(SKBitmap) ? (T?)(object?)Bitmap : default;

        public T? TryLoadContent<T>(string contentName) => LoadContent<T>(contentName);
    }

    public SharedTextureCallerBitmapTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static SKColor Center(GraphicalUiElement runtime) => ColorAt(4 + 15, 4 + 15, runtime);

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void CallerMadeBitmap_Assigned_StaysMutableAndDrawsTheSameAsALoadedOne(string name)
    {
        using SKBitmap callerMade = CreateGrid(GridSize, seed: 0);
        SKBitmap loaded = Load(GridSize, seed: 0);

        GraphicalUiElement fromCaller = CreateRuntime(name, callerMade, x: 4);
        GraphicalUiElement fromLoader = CreateRuntime(name, loaded, x: 4);

        callerMade.IsImmutable.ShouldBeFalse();
        Render(fromCaller).ShouldBe(Render(fromLoader));
        Render(fromCaller).Any(value => value != 0).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void CallerMadeBitmap_Assigned_IsCopiedPerRenderable(string name)
    {
        // The documented limit: only a bitmap Gum loaded is shared. A caller's mutable bitmap is
        // snapshotted by Skia, so each renderable has a private buffer.
        using SKBitmap callerMade = CreateGrid(GridSize, seed: 0);
        GraphicalUiElement first = CreateRuntime(name, callerMade, x: 4);
        GraphicalUiElement second = CreateRuntime(name, callerMade, x: 44);

        PixelAddress(GetImage(first)).ShouldNotBe(callerMade.GetPixels());
        PixelAddress(GetImage(first)).ShouldNotBe(PixelAddress(GetImage(second)));
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void CallerMadeBitmap_EditedAfterAssigning_StillTakesEditsAndShowsThemWhenAssignedAgain(string name)
    {
        using SKBitmap callerMade = CreateGrid(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(name, callerMade, x: 4);
        SKColor assigned = Center(runtime);

        callerMade.Erase(Edited);

        callerMade.IsImmutable.ShouldBeFalse();
        callerMade.GetPixel(6, 6).ShouldBe(Edited);
        Center(runtime).ShouldBe(assigned);
        SetTexture(runtime, callerMade);
        Center(runtime).ShouldBe(Edited);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void CallerMadeBitmap_Disposed_LeavesTheRenderableDrawing(string name)
    {
        SKBitmap callerMade = CreateGrid(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(name, callerMade, x: 4);
        byte[] before = Render(runtime);

        callerMade.Dispose();

        Render(runtime).ShouldBe(before);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void HostContentLoader_ReturningAMutableBitmap_DrawsItAndLeavesItMutable(string name)
    {
        using SKBitmap hostBitmap = CreateGrid(GridSize, seed: 0);
        LoaderManager.Self.ContentLoader = new HostContentLoader { Bitmap = hostBitmap };
        LoaderManager.Self.CacheTextures = false;
        GraphicalUiElement runtime = name == nameof(Sprite) ? new SpriteRuntime() : new NineSliceRuntime();
        runtime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        runtime.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        runtime.Width = 30;
        runtime.Height = 30;
        runtime.X = 4;
        runtime.Y = 4;
        if (runtime is NineSliceRuntime nineSlice) nineSlice.BorderScale = 3;

        if (runtime is SpriteRuntime sprite) sprite.SourceFileName = "host_texture.png";
        else ((NineSliceRuntime)runtime).SourceFileName = "host_texture.png";

        SKBitmap? texture = runtime.RenderableComponent switch
        {
            Sprite s => s.Texture,
            NineSlice n => n.Texture,
            _ => null,
        };
        texture.ShouldBeSameAs(hostBitmap);
        hostBitmap.IsImmutable.ShouldBeFalse();
        using SKBitmap expectedSource = CreateGrid(GridSize, seed: 0);
        Render(runtime).ShouldBe(Render(CreateRuntime(name, expectedSource, x: 4)));
        Render(runtime).Any(value => value != 0).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void HostContentLoader_ReturningAMutableBitmap_TakesEditsAndShowsThemWhenAssignedAgain(string name)
    {
        using SKBitmap hostBitmap = CreateGrid(GridSize, seed: 0);
        LoaderManager.Self.ContentLoader = new HostContentLoader { Bitmap = hostBitmap };
        LoaderManager.Self.CacheTextures = false;
        GraphicalUiElement runtime = CreateRuntime(name, null, x: 4);
        if (runtime is SpriteRuntime sprite) sprite.SourceFileName = "host_texture.png";
        else ((NineSliceRuntime)runtime).SourceFileName = "host_texture.png";

        hostBitmap.Erase(Edited);
        SetTexture(runtime, hostBitmap);

        hostBitmap.IsImmutable.ShouldBeFalse();
        Center(runtime).ShouldBe(Edited);
    }

    #region The pooled color-picker bitmap

    private static byte[] Solid(byte red, byte green, byte blue)
    {
        byte[] rgba = new byte[4 * 4 * 4];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = red;
            rgba[i + 1] = green;
            rgba[i + 2] = blue;
            rgba[i + 3] = 255;
        }
        return rgba;
    }

    [Fact]
    public void PooledPixelData_UploadedManyTimes_StaysOneMutableBitmapThatShowsTheLatest()
    {
        SpriteRuntime owner = new();
        SpriteRuntime target = new();
        SKBitmap? first = null;

        for (int step = 0; step < 50; step++)
        {
            PixelDataTextureApplier.ApplyPooled(target, owner, Solid((byte)step, (byte)(step * 2), 0), 4, 4);
            SKBitmap texture = ((Sprite)target.RenderableComponent).Texture!;
            first ??= texture;
            texture.ShouldBeSameAs(first);
            texture.IsImmutable.ShouldBeFalse();
            ReadFirstPixel(GetImage(target)).ShouldBe(new SKColor((byte)step, (byte)(step * 2), 0));
        }
    }

    [Fact]
    public void PooledPixelData_AfterCachedPixelDataOnTheSameSprite_IsMutableAndNotTheCachedBitmap()
    {
        string cacheKey = "SharedTextureCallerBitmapTests_" + Guid.NewGuid().ToString("N");
        SpriteRuntime owner = new();
        SpriteRuntime target = new();
        PixelDataTextureApplier.ApplyCached(target, cacheKey, Solid(255, 0, 0), 4, 4);
        SKBitmap cached = ((Sprite)target.RenderableComponent).Texture!;

        PixelDataTextureApplier.ApplyPooled(target, owner, Solid(0, 0, 255), 4, 4);
        SKBitmap pooled = ((Sprite)target.RenderableComponent).Texture!;
        PixelDataTextureApplier.ApplyPooled(target, owner, Solid(0, 255, 0), 4, 4);

        cached.IsImmutable.ShouldBeTrue();
        pooled.ShouldNotBeSameAs(cached);
        pooled.IsImmutable.ShouldBeFalse();
        cached.GetPixel(0, 0).ShouldBe(SKColors.Red);
        ReadFirstPixel(GetImage(target)).ShouldBe(SKColors.Lime);
        LoaderManager.Self.Dispose(cacheKey);
    }

    [Fact]
    public void CachedPixelData_AfterPooledPixelDataOnTheSameSprite_LeavesThePooledBitmapMutable()
    {
        string cacheKey = "SharedTextureCallerBitmapTests_" + Guid.NewGuid().ToString("N");
        SpriteRuntime owner = new();
        SpriteRuntime target = new();
        PixelDataTextureApplier.ApplyPooled(target, owner, Solid(0, 0, 255), 4, 4);
        SKBitmap pooled = ((Sprite)target.RenderableComponent).Texture!;

        PixelDataTextureApplier.ApplyCached(target, cacheKey, Solid(255, 0, 0), 4, 4);
        PixelDataTextureApplier.ApplyPooled(target, owner, Solid(0, 255, 0), 4, 4);

        ((Sprite)target.RenderableComponent).Texture.ShouldBeSameAs(pooled);
        pooled.IsImmutable.ShouldBeFalse();
        ReadFirstPixel(GetImage(target)).ShouldBe(SKColors.Lime);
        LoaderManager.Self.Dispose(cacheKey);
    }

    private static SKColor ReadFirstPixel(SKImage image)
    {
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(0, 0);
    }

    #endregion
}
