using Gum.Managers;
using RenderingLibrary.Content;
using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// What a Sprite or NineSlice draws on a CPU raster surface for every way a texture reaches it, and that
/// renderables sharing one bitmap keep drawing it correctly while others are disposed, reassigned or
/// refreshed. Pins the behavior any pixel-sharing change to the Texture setters must keep.
/// </summary>
public class TextureAssignmentDrawTests : BaseTestClass
{
    private const int Size = 3;

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "GumSkiaTextureDraw_" + Guid.NewGuid().ToString("N")) + "/";
    private readonly SKFilterMode _savedFilter = global::RenderingLibrary.Graphics.Renderer.TextureFilter;
    private readonly SKSurface _surface;

    public TextureAssignmentDrawTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        LoaderManager.Self.ContentLoader = new ContentLoader();
        LoaderManager.Self.CacheTextures = true;
        Directory.CreateDirectory(_directory);
        _surface = SKSurface.Create(new SKImageInfo(64, 16));
        GumService.Default.Initialize(_surface.Canvas, 64, 16);
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
    }

    public override void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = _savedFilter;
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    public static TheoryData<string> RuntimeNames => new() { nameof(SpriteRuntime), nameof(NineSliceRuntime) };

    // Nine distinct opaque colors, so a wrong source rectangle, flip or channel order is visible.
    private static SKColor Expected(int x, int y) => new((byte)(40 + x * 80), (byte)(40 + y * 80), (byte)(200 - x * 30 - y * 30));

    private static SKBitmap Pattern(int shift = 0)
    {
        SKBitmap bitmap = new(Size, Size);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                SKColor color = Expected(x, y);
                bitmap.SetPixel(x, y, new SKColor((byte)(color.Red ^ shift), color.Green, color.Blue));
            }
        }
        return bitmap;
    }

    private static GraphicalUiElement Create(string name, float x)
    {
        GraphicalUiElement runtime = name == nameof(SpriteRuntime) ? new SpriteRuntime() : new NineSliceRuntime();
        runtime.X = x;
        runtime.Y = 0;
        runtime.Width = Size;
        runtime.Height = Size;
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.HeightUnits = DimensionUnitType.Absolute;
        return runtime;
    }

    private static void SetTexture(GraphicalUiElement runtime, SKBitmap? texture)
    {
        if (runtime is SpriteRuntime sprite) sprite.Texture = texture;
        else ((NineSliceRuntime)runtime).Texture = texture;
    }

    private static void SetImage(GraphicalUiElement runtime, SKImage? image)
    {
        if (runtime.RenderableComponent is Sprite sprite) sprite.Image = image;
        else ((NineSlice)runtime.RenderableComponent).Image = image;
    }

    private static SKImage? ImageOf(GraphicalUiElement runtime) => runtime.RenderableComponent switch
    {
        Sprite sprite => sprite.Image,
        NineSlice nineSlice => nineSlice.Image,
        _ => null,
    };

    private SKBitmap DrawAll(params GraphicalUiElement[] runtimes)
    {
        _surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Root.Children.Clear();
        foreach (GraphicalUiElement runtime in runtimes)
        {
            GumService.Default.Root.Children.Add(runtime);
        }
        GumService.Default.Draw();
        using SKImage snapshot = _surface.Snapshot();
        return SKBitmap.FromImage(snapshot);
    }

    // The pattern's nine pixels, drawn 1:1 with its top left at (left, 0), must come out unchanged.
    private static void ShouldShowPattern(SKBitmap drawn, float left, int shift = 0)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                SKColor color = Expected(x, y);
                SKColor expected = new((byte)(color.Red ^ shift), color.Green, color.Blue);
                drawn.GetPixel((int)left + x, y).ShouldBe(expected, $"pixel ({x},{y}) of the sprite at x={left}");
            }
        }
    }

    private string WritePng(string fileName, SKBitmap bitmap)
    {
        string path = _directory + fileName;
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    #region Every path draws the reference image

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_TextureAssignedInCode_ShowsTheBitmap(string name)
    {
        using SKBitmap pattern = Pattern();
        GraphicalUiElement runtime = Create(name, 4);
        SetTexture(runtime, pattern);

        using SKBitmap drawn = DrawAll(runtime);

        ShouldShowPattern(drawn, 4);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_ImageAssignedDirectly_ShowsTheImage(string name)
    {
        using SKBitmap pattern = Pattern();
        using SKImage image = SKImage.FromBitmap(pattern);
        GraphicalUiElement runtime = Create(name, 4);
        SetImage(runtime, image);

        using SKBitmap drawn = DrawAll(runtime);

        ShouldShowPattern(drawn, 4);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_SourceFileName_ShowsTheFile(string name)
    {
        using SKBitmap pattern = Pattern();
        string path = WritePng("pattern.png", pattern);
        GraphicalUiElement runtime = Create(name, 4);

        if (runtime is SpriteRuntime sprite) sprite.SourceFileName = path;
        else ((NineSliceRuntime)runtime).SourceFileName = path;
        using SKBitmap drawn = DrawAll(runtime);

        ShouldShowPattern(drawn, 4);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_SourceFileProperty_ChangedToAnotherFile_ShowsTheNewFile(string name)
    {
        using SKBitmap first = Pattern();
        using SKBitmap second = Pattern(shift: 0x55);
        string firstPath = WritePng("first.png", first);
        string secondPath = WritePng("second.png", second);
        GraphicalUiElement runtime = Create(name, 4);
        runtime.SetProperty("SourceFile", firstPath);
        DrawAll(runtime).Dispose();

        runtime.SetProperty("SourceFile", secondPath);
        using SKBitmap drawn = DrawAll(runtime);

        ShouldShowPattern(drawn, 4, shift: 0x55);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_Clone_ShowsTheBitmapOfTheOriginal(string name)
    {
        using SKBitmap pattern = Pattern();
        GraphicalUiElement original = Create(name, 4);
        SetTexture(original, pattern);
        GraphicalUiElement clone = original.Clone();
        clone.X = 20;

        using SKBitmap drawn = DrawAll(original, clone);

        ShouldShowPattern(drawn, 4);
        ShouldShowPattern(drawn, 20);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_CloneOfRenderableWithAssignedImage_ShowsTheImage(string name)
    {
        using SKBitmap pattern = Pattern();
        using SKImage image = SKImage.FromBitmap(pattern);
        GraphicalUiElement original = Create(name, 4);
        SetImage(original, image);
        GraphicalUiElement clone = original.Clone();
        clone.X = 20;

        using SKBitmap drawn = DrawAll(original, clone);

        ShouldShowPattern(drawn, 4);
        ShouldShowPattern(drawn, 20);
    }

    #endregion

    #region Sharing one bitmap

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_ManyRenderablesOnOneBitmap_AllShowIt(string name)
    {
        using SKBitmap pattern = Pattern();
        GraphicalUiElement[] runtimes = Enumerable.Range(0, 8).Select(i => Create(name, 4 + i * 6)).ToArray();
        foreach (GraphicalUiElement runtime in runtimes)
        {
            SetTexture(runtime, pattern);
        }

        using SKBitmap drawn = DrawAll(runtimes);

        for (int i = 0; i < runtimes.Length; i++)
        {
            ShouldShowPattern(drawn, 4 + i * 6);
        }
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Dispose_OneOfManyRenderablesOnOneBitmap_LeavesTheOthersDrawing(string name)
    {
        using SKBitmap pattern = Pattern();
        GraphicalUiElement[] runtimes = Enumerable.Range(0, 4).Select(i => Create(name, 4 + i * 6)).ToArray();
        foreach (GraphicalUiElement runtime in runtimes)
        {
            SetTexture(runtime, pattern);
        }
        DrawAll(runtimes).Dispose();

        ((IDisposable)runtimes[1].RenderableComponent).Dispose();
        using SKBitmap drawn = DrawAll(runtimes[0], runtimes[2], runtimes[3]);

        ShouldShowPattern(drawn, 4);
        ShouldShowPattern(drawn, 16);
        ShouldShowPattern(drawn, 22);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Dispose_FirstCreatedOfManyRenderablesOnOneBitmap_LeavesTheOthersDrawing(string name)
    {
        using SKBitmap pattern = Pattern();
        GraphicalUiElement[] runtimes = Enumerable.Range(0, 3).Select(i => Create(name, 4 + i * 6)).ToArray();
        foreach (GraphicalUiElement runtime in runtimes)
        {
            SetTexture(runtime, pattern);
        }

        ((IDisposable)runtimes[0].RenderableComponent).Dispose();
        using SKBitmap drawn = DrawAll(runtimes[1], runtimes[2]);

        ShouldShowPattern(drawn, 10);
        ShouldShowPattern(drawn, 16);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Dispose_Twice_DoesNotThrow(string name)
    {
        using SKBitmap pattern = Pattern();
        GraphicalUiElement runtime = Create(name, 4);
        SetTexture(runtime, pattern);
        IDisposable renderable = (IDisposable)runtime.RenderableComponent;

        renderable.Dispose();

        Should.NotThrow(renderable.Dispose);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Dispose_AllRenderablesOnOneBitmap_DisposesEveryOwnedImageAndKeepsTheBitmap(string name)
    {
        using SKBitmap pattern = Pattern();
        GraphicalUiElement[] runtimes = Enumerable.Range(0, 6).Select(i => Create(name, 4 + i * 6)).ToArray();
        foreach (GraphicalUiElement runtime in runtimes)
        {
            SetTexture(runtime, pattern);
        }
        SKImage[] images = runtimes.Select(runtime => ImageOf(runtime)!).ToArray();

        foreach (GraphicalUiElement runtime in runtimes)
        {
            ((IDisposable)runtime.RenderableComponent).Dispose();
        }

        images.ShouldAllBe(image => image.Handle == IntPtr.Zero, "no image is left alive once every owner is disposed");
        pattern.Handle.ShouldNotBe(IntPtr.Zero);
        GraphicalUiElement later = Create(name, 4);
        SetTexture(later, pattern);
        using SKBitmap drawn = DrawAll(later);
        ShouldShowPattern(drawn, 4);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Texture_ReplacedOnOneOfManyRenderables_LeavesTheOthersOnTheOldBitmap(string name)
    {
        using SKBitmap first = Pattern();
        using SKBitmap second = Pattern(shift: 0x55);
        GraphicalUiElement a = Create(name, 4);
        GraphicalUiElement b = Create(name, 10);
        SetTexture(a, first);
        SetTexture(b, first);
        DrawAll(a, b).Dispose();

        SetTexture(a, second);
        using SKBitmap drawn = DrawAll(a, b);

        ShouldShowPattern(drawn, 4, shift: 0x55);
        ShouldShowPattern(drawn, 10);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Texture_ReplacedAndRestored_ShowsTheRestoredBitmap(string name)
    {
        using SKBitmap first = Pattern();
        using SKBitmap second = Pattern(shift: 0x55);
        GraphicalUiElement runtime = Create(name, 4);
        SetTexture(runtime, first);
        SetTexture(runtime, second);
        SetTexture(runtime, first);

        using SKBitmap drawn = DrawAll(runtime);

        ShouldShowPattern(drawn, 4);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Texture_AssignedNull_DrawsNothing_AndAssignedAgain_Draws(string name)
    {
        using SKBitmap pattern = Pattern();
        GraphicalUiElement runtime = Create(name, 4);
        SetTexture(runtime, pattern);
        SetTexture(runtime, null);

        using SKBitmap blank = DrawAll(runtime);
        SetTexture(runtime, pattern);
        using SKBitmap drawn = DrawAll(runtime);

        blank.GetPixel(5, 1).Alpha.ShouldBe((byte)0);
        ShouldShowPattern(drawn, 4);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Texture_NeverAssigned_DrawsNothing_AndDoesNotThrow(string name)
    {
        GraphicalUiElement runtime = Create(name, 4);

        using SKBitmap drawn = Should.NotThrow(() => DrawAll(runtime));

        drawn.GetPixel(5, 1).Alpha.ShouldBe((byte)0);
    }

    #endregion

    #region Pixels edited after assignment

    // Pixel edits are picked up by assigning the bitmap again, which is how PixelDataTextureApplier
    // publishes a re-uploaded texture (it calls NotifyPixelsChanged and then sets Texture).
    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Texture_BitmapEditedThenReassigned_EveryRenderableShowsTheNewPixels(string name)
    {
        using SKBitmap pattern = Pattern();
        GraphicalUiElement[] runtimes = Enumerable.Range(0, 5).Select(i => Create(name, 4 + i * 6)).ToArray();
        foreach (GraphicalUiElement runtime in runtimes)
        {
            SetTexture(runtime, pattern);
        }
        DrawAll(runtimes).Dispose();

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                SKColor color = Expected(x, y);
                pattern.SetPixel(x, y, new SKColor((byte)(color.Red ^ 0x55), color.Green, color.Blue));
            }
        }
        pattern.NotifyPixelsChanged();
        foreach (GraphicalUiElement runtime in runtimes)
        {
            SetTexture(runtime, pattern);
        }
        using SKBitmap drawn = DrawAll(runtimes);

        for (int i = 0; i < runtimes.Length; i++)
        {
            ShouldShowPattern(drawn, 4 + i * 6, shift: 0x55);
        }
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Texture_BitmapErasedThenReassigned_ShowsTheNewColor(string name)
    {
        using SKBitmap bitmap = new(Size, Size);
        bitmap.Erase(SKColors.Red);
        GraphicalUiElement runtime = Create(name, 4);
        SetTexture(runtime, bitmap);
        DrawAll(runtime).Dispose();

        bitmap.Erase(SKColors.Blue);
        SetTexture(runtime, bitmap);
        using SKBitmap drawn = DrawAll(runtime);

        drawn.GetPixel(5, 1).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void PooledPixelData_AppliedAgainForTheSameOwner_ShowsTheNewPixels()
    {
        GraphicalUiElement owner = AttachedOwner();
        SpriteRuntime sprite = (SpriteRuntime)Create(nameof(SpriteRuntime), 4);
        byte[] red = Solid(255, 0, 0);
        byte[] blue = Solid(0, 0, 255);

        GraphicalUiElement.ApplyPooledTextureFromPixelData!(sprite, owner, red, PooledSize, PooledSize);
        DrawAll(sprite).Dispose();
        GraphicalUiElement.ApplyPooledTextureFromPixelData!(sprite, owner, blue, PooledSize, PooledSize);
        using SKBitmap drawn = DrawAll(sprite);

        drawn.GetPixel(5, 1).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void PooledPixelData_TwoOwners_DoNotShowEachOthersPixels()
    {
        GraphicalUiElement firstOwner = AttachedOwner();
        GraphicalUiElement secondOwner = AttachedOwner();
        SpriteRuntime first = (SpriteRuntime)Create(nameof(SpriteRuntime), 4);
        SpriteRuntime second = (SpriteRuntime)Create(nameof(SpriteRuntime), 10);

        GraphicalUiElement.ApplyPooledTextureFromPixelData!(first, firstOwner, Solid(255, 0, 0), PooledSize, PooledSize);
        GraphicalUiElement.ApplyPooledTextureFromPixelData!(second, secondOwner, Solid(0, 0, 255), PooledSize, PooledSize);
        GraphicalUiElement.ApplyPooledTextureFromPixelData!(first, firstOwner, Solid(0, 255, 0), PooledSize, PooledSize);
        using SKBitmap drawn = DrawAll(first, second);

        drawn.GetPixel(5, 1).ShouldBe(SKColors.Lime);
        drawn.GetPixel(11, 1).ShouldBe(SKColors.Blue);
    }

    // The pooled-texture list is process-wide, and a detached owner's bitmap is handed to the next caller
    // whatever its size. ColorPickerTests (same process) leave 256x256 entries in it, so pooled pixel data
    // here must also be 256x256: any other size would reclaim one of those bitmaps and draw it scaled, or
    // leave a small bitmap that ColorPicker's 256x256 upload then overruns.
    private const int PooledSize = 256;

    // A pooled texture is reserved for an owner only while the owner is in the visual tree.
    private static GraphicalUiElement AttachedOwner()
    {
        ContainerRuntime owner = new();
        GumService.Default.Root.Children.Add(owner);
        return owner;
    }

    private static byte[] Solid(byte red, byte green, byte blue)
    {
        byte[] rgba = new byte[PooledSize * PooledSize * 4];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = red;
            rgba[i + 1] = green;
            rgba[i + 2] = blue;
            rgba[i + 3] = 255;
        }
        return rgba;
    }

    #endregion

    #region Linear sampling

    // NineSlice cuts a section image per source rectangle under Linear sampling; those must follow the texture.
    [Fact]
    public void Draw_NineSliceUnderLinearSampling_AfterTextureSwap_ShowsTheNewTexture()
    {
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Linear;
        using SKBitmap red = new(9, 9);
        red.Erase(SKColors.Red);
        using SKBitmap green = new(9, 9);
        green.Erase(SKColors.Lime);
        NineSliceRuntime nineSlice = (NineSliceRuntime)Create(nameof(NineSliceRuntime), 4);
        nineSlice.Width = 9;
        nineSlice.Height = 9;
        nineSlice.Texture = red;
        DrawAll(nineSlice).Dispose();

        nineSlice.Texture = green;
        using SKBitmap drawn = DrawAll(nineSlice);

        drawn.GetPixel(8, 4).ShouldBe(SKColors.Lime);
        drawn.GetPixel(5, 1).ShouldBe(SKColors.Lime);
    }

    #endregion

    #region Renderables cut different parts of one sheet

    public static TheoryData<string, SKFilterMode> RuntimesAndFilters => new()
    {
        { nameof(SpriteRuntime), SKFilterMode.Nearest },
        { nameof(SpriteRuntime), SKFilterMode.Linear },
        { nameof(NineSliceRuntime), SKFilterMode.Nearest },
        { nameof(NineSliceRuntime), SKFilterMode.Linear },
    };

    private static void ShowRegion(GraphicalUiElement runtime, int left)
    {
        runtime.SetProperty("TextureAddress", TextureAddress.Custom);
        runtime.SetProperty("TextureLeft", left);
        runtime.SetProperty("TextureTop", 0);
        runtime.SetProperty("TextureWidth", Size);
        runtime.SetProperty("TextureHeight", Size);
    }

    // The case behind #7876: many nine-slices and sprites each show their own region of one sprite sheet.
    [Theory]
    [MemberData(nameof(RuntimesAndFilters))]
    public void Draw_RenderablesOnOneSheet_EachShowsItsOwnSourceRegion(string name, SKFilterMode filter)
    {
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = filter;
        using SKBitmap sheet = new(Size * 2, Size);
        using (SKBitmap left = Pattern())
        using (SKBitmap right = Pattern(shift: 0x55))
        using (SKCanvas canvas = new(sheet))
        {
            canvas.DrawBitmap(left, 0, 0);
            canvas.DrawBitmap(right, Size, 0);
        }
        GraphicalUiElement fromLeft = Create(name, 4);
        GraphicalUiElement fromRight = Create(name, 10);
        GraphicalUiElement fromLeftAgain = Create(name, 16);
        SetTexture(fromLeft, sheet);
        SetTexture(fromRight, sheet);
        SetTexture(fromLeftAgain, sheet);
        ShowRegion(fromLeft, 0);
        ShowRegion(fromRight, Size);
        ShowRegion(fromLeftAgain, 0);

        using SKBitmap drawn = DrawAll(fromLeft, fromRight, fromLeftAgain);

        ShouldShowPattern(drawn, 4);
        ShouldShowPattern(drawn, 10, shift: 0x55);
        ShouldShowPattern(drawn, 16);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_RenderablesOnOneBitmap_EachKeepsItsOwnTint(string name)
    {
        using SKBitmap white = new(Size, Size);
        white.Erase(SKColors.White);
        GraphicalUiElement tinted = Create(name, 4);
        GraphicalUiElement plain = Create(name, 10);
        GraphicalUiElement faded = Create(name, 16);
        SetTexture(tinted, white);
        SetTexture(plain, white);
        SetTexture(faded, white);
        tinted.SetProperty("Red", 255);
        tinted.SetProperty("Green", 0);
        tinted.SetProperty("Blue", 0);
        faded.SetProperty("Alpha", 0);

        using SKBitmap drawn = DrawAll(tinted, plain, faded);

        drawn.GetPixel(5, 1).ShouldBe(SKColors.Red);
        drawn.GetPixel(11, 1).ShouldBe(SKColors.White);
        drawn.GetPixel(17, 1).Alpha.ShouldBe((byte)0);
    }

    [Fact]
    public void Draw_SpriteWithOnlyAnImage_WrapRepeatsTheImage()
    {
        using SKBitmap strip = new(2, 1);
        strip.SetPixel(0, 0, SKColors.Red);
        strip.SetPixel(1, 0, SKColors.Blue);
        using SKImage image = SKImage.FromBitmap(strip);
        SpriteRuntime sprite = (SpriteRuntime)Create(nameof(SpriteRuntime), 4);
        sprite.Width = 4;
        sprite.Height = 1;
        sprite.Image = image;
        sprite.TextureAddress = TextureAddress.Custom;
        sprite.TextureLeft = 0;
        sprite.TextureTop = 0;
        sprite.TextureWidth = 4;
        sprite.TextureHeight = 1;
        sprite.SetProperty("Wrap", true);

        using SKBitmap drawn = DrawAll(sprite);

        drawn.GetPixel(4, 0).ShouldBe(SKColors.Red);
        drawn.GetPixel(5, 0).ShouldBe(SKColors.Blue);
        drawn.GetPixel(6, 0).ShouldBe(SKColors.Red);
        drawn.GetPixel(7, 0).ShouldBe(SKColors.Blue);
    }

    #endregion
}
