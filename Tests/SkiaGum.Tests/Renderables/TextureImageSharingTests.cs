using SkiaGum.Tests.Performance;
using RenderingLibrary.Content;
using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Gum.Graphics.Animation;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Renderables that show the same loaded <see cref="SKBitmap"/> share one pixel buffer instead of each
/// copying it (#7876: <c>SKImage.FromBitmap</c> of a mutable bitmap copies the pixels, so 517 nine-slices
/// cut from one 1024x1024 sprite sheet held 517 copies). A buffer is compared through
/// <see cref="SKImage.PeekPixels"/> so the check does not depend on how sharing is implemented.
/// </summary>
public class TextureImageSharingTests : BaseTestClass
{
    private const int Count = 12;

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "GumSkiaImageSharing_" + Guid.NewGuid().ToString("N")) + "/";

    public TextureImageSharingTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        LoaderManager.Self.ContentLoader = new ContentLoader();
        LoaderManager.Self.CacheTextures = true;
        Directory.CreateDirectory(_directory);
    }

    public override void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    public static TheoryData<string> RenderableNames => new()
    {
        nameof(Sprite), nameof(NineSlice), nameof(SpriteRuntime), nameof(NineSliceRuntime),
    };

    public static TheoryData<string> RuntimeNames => new() { nameof(SpriteRuntime), nameof(NineSliceRuntime) };

    private static SKBitmap Sheet(SKColor color, int size = 32)
    {
        SKBitmap bitmap = new(size, size);
        bitmap.Erase(color);
        return bitmap;
    }

    private static object Create(string name) => name switch
    {
        nameof(Sprite) => new Sprite(),
        nameof(NineSlice) => new NineSlice(),
        nameof(SpriteRuntime) => new SpriteRuntime(),
        nameof(NineSliceRuntime) => new NineSliceRuntime(),
        _ => throw new ArgumentException(name),
    };

    private static object Renderable(object created) =>
        created is GraphicalUiElement runtime ? runtime.RenderableComponent : created;

    private static void SetTexture(object created, SKBitmap? texture)
    {
        switch (created)
        {
            case Sprite sprite: sprite.Texture = texture; break;
            case NineSlice nineSlice: nineSlice.Texture = texture; break;
            case SpriteRuntime spriteRuntime: spriteRuntime.Texture = texture; break;
            case NineSliceRuntime nineSliceRuntime: nineSliceRuntime.Texture = texture; break;
            default: throw new ArgumentException(created.GetType().Name);
        }
    }

    private static SKImage? ImageOf(object created) => Renderable(created) switch
    {
        Sprite sprite => sprite.Image,
        NineSlice nineSlice => nineSlice.Image,
        _ => throw new ArgumentException(created.GetType().Name),
    };

    // The address of the pixels an image reads from. Two images with the same address copied nothing.
    private static nint BufferOf(SKImage image)
    {
        using SKPixmap? pixmap = image.PeekPixels();
        return pixmap != null ? pixmap.GetPixels() : (nint)image.UniqueId;
    }

    private static int DistinctBuffers(IEnumerable<object> renderables) =>
        renderables.Select(renderable => BufferOf(ImageOf(renderable)!)).Distinct().Count();

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Texture_AssignedToManyRenderables_ShouldShareOnePixelBuffer(string name)
    {
        using SKBitmap sheet = Sheet(SKColors.Red);
        List<object> renderables = Enumerable.Range(0, Count).Select(_ => Create(name)).ToList();

        foreach (object renderable in renderables)
        {
            SetTexture(renderable, sheet);
        }

        DistinctBuffers(renderables).ShouldBe(1);
        foreach (object renderable in renderables)
        {
            (renderable as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public void Texture_AssignedToSpritesAndNineSlices_ShouldShareOnePixelBuffer()
    {
        using SKBitmap sheet = Sheet(SKColors.Red);
        List<object> renderables = new();
        for (int i = 0; i < Count; i++)
        {
            object renderable = i % 2 == 0 ? new Sprite() : new NineSlice();
            SetTexture(renderable, sheet);
            renderables.Add(renderable);
        }

        DistinctBuffers(renderables).ShouldBe(1);
        renderables.ForEach(renderable => ((IDisposable)renderable).Dispose());
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void SourceFileName_SameFileOnManyRuntimes_ShouldShareOnePixelBuffer(string name)
    {
        string path = WritePng("sheet.png", SKColors.Red);
        List<GraphicalUiElement> runtimes = new();

        for (int i = 0; i < Count; i++)
        {
            GraphicalUiElement runtime = (GraphicalUiElement)Create(name);
            if (runtime is SpriteRuntime sprite) sprite.SourceFileName = path;
            else ((NineSliceRuntime)runtime).SourceFileName = path;
            runtimes.Add(runtime);
        }

        DistinctBuffers(runtimes).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Clone_ShouldShareThePixelBufferOfTheOriginal(string name)
    {
        using SKBitmap sheet = Sheet(SKColors.Red);
        object original = Create(name);
        SetTexture(original, sheet);

        List<object> all = new() { original };
        for (int i = 0; i < Count; i++)
        {
            all.Add(original is GraphicalUiElement runtime
                ? runtime.Clone()
                : ((ICloneable)original).Clone());
        }

        DistinctBuffers(all).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Texture_ReassignedToTheSameBitmap_ShouldStillShareThePixelBufferWithTheOthers(string name)
    {
        using SKBitmap sheet = Sheet(SKColors.Red);
        object renderable = Create(name);
        SetTexture(renderable, sheet);
        object witness = Create(name);
        SetTexture(witness, sheet);

        // A layout pass or an animation frame that lands on the same sheet assigns it again.
        SetTexture(renderable, sheet);
        SetTexture(renderable, sheet);

        BufferOf(ImageOf(renderable)!).ShouldBe(BufferOf(ImageOf(witness)!));
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void AnimationFrames_OnOneSheet_ShouldKeepSharingThePixelBuffer(string name)
    {
        using SKBitmap sheet = Sheet(SKColors.Red);
        AnimationChain chain = new() { Name = "c" };
        for (int i = 0; i < 4; i++)
        {
            chain.Add(new AnimationFrame
            {
                FrameLength = 1f,
                Texture = sheet,
                LeftCoordinate = i / 4f,
                RightCoordinate = (i + 1) / 4f,
                BottomCoordinate = 1f,
            });
        }

        GraphicalUiElement runtime = (GraphicalUiElement)Create(name);
        AnimationChainList chains = new() { chain };
        if (runtime is SpriteRuntime sprite)
        {
            sprite.AnimationChains = chains;
            sprite.CurrentChainName = "c";
            sprite.Animate = true;
        }
        else
        {
            NineSliceRuntime nineSlice = (NineSliceRuntime)runtime;
            nineSlice.AnimationChains = chains;
            nineSlice.CurrentChainName = "c";
            nineSlice.Animate = true;
        }

        runtime.AnimateSelf(0.0);
        object witness = Create(name);
        SetTexture(witness, sheet);
        nint shared = BufferOf(ImageOf(witness)!);
        List<nint> buffers = new() { BufferOf(ImageOf(runtime)!) };
        for (int i = 0; i < 4; i++)
        {
            runtime.AnimateSelf(1.0);
            buffers.Add(BufferOf(ImageOf(runtime)!));
        }

        buffers.ShouldAllBe(buffer => buffer == shared);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Texture_DifferentBitmaps_ShouldNotShareAPixelBuffer(string name)
    {
        using SKBitmap first = Sheet(SKColors.Red);
        using SKBitmap second = Sheet(SKColors.Lime);
        object a = Create(name);
        object b = Create(name);

        SetTexture(a, first);
        SetTexture(b, second);

        DistinctBuffers(new[] { a, b }).ShouldBe(2);
    }

    [Fact]
    public void FormsDefaultVisuals_BuiltInCode_ShouldShareOnePixelBufferOfTheSpriteSheet()
    {
        using SkiaSceneHost host = new();
        SkiaSceneHost.BuildRealisticFormsScene();
        List<object> nineSlices = new();
        Collect(GumService.Default.Root, nineSlices);

        nineSlices.Count.ShouldBeGreaterThan(Count);
        DistinctBuffers(nineSlices).ShouldBe(1);
    }

    private static void Collect(GraphicalUiElement parent, List<object> nineSlices)
    {
        foreach (GraphicalUiElement child in parent.Children)
        {
            if (child.RenderableComponent is NineSlice { Image: not null })
            {
                nineSlices.Add(child.RenderableComponent);
            }
            Collect(child, nineSlices);
        }
    }

    private string WritePng(string fileName, SKColor color)
    {
        string path = _directory + fileName;
        using SKBitmap bitmap = Sheet(color);
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }
}
