using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using SkiaSharp;
using ToolsUtilities;

namespace SkiaGum.Tests.Content;

/// <summary>
/// A Sprite or NineSlice SourceFile that names the same image file loads it once, so the renderables built from
/// it share one bitmap (and with it one pixel buffer).
/// </summary>
public class SourceFileTextureCacheTests : BaseTestClass
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "GumSkiaTextureCache_" + Guid.NewGuid().ToString("N")) + "/";

    public SourceFileTextureCacheTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        LoaderManager.Self.ContentLoader = new ContentLoader();
        LoaderManager.Self.CacheTextures = true;
        Directory.CreateDirectory(_directory + "sub");
        FileManager.RelativeDirectory = _directory;
    }

    public override void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
        base.Dispose();
    }

    public static TheoryData<string> RuntimeNames => new() { nameof(SpriteRuntime), nameof(NineSliceRuntime) };

    private string WritePng(string relativeName, SKColor color)
    {
        string path = _directory + relativeName;
        using SKBitmap bitmap = new(4, 4);
        bitmap.Erase(color);
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    private static GraphicalUiElement Create(string name) =>
        name == nameof(SpriteRuntime) ? new SpriteRuntime() : new NineSliceRuntime();

    private static void SetSource(GraphicalUiElement runtime, string path)
    {
        if (runtime is SpriteRuntime sprite) sprite.SourceFileName = path;
        else ((NineSliceRuntime)runtime).SourceFileName = path;
    }

    private static SKBitmap? TextureOf(GraphicalUiElement runtime) =>
        runtime is SpriteRuntime sprite ? sprite.Texture : ((NineSliceRuntime)runtime).Texture;

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void SourceFile_SamePathOnTwoRuntimes_LoadsOneBitmap(string name)
    {
        string path = WritePng("sheet.png", SKColors.Red);
        GraphicalUiElement first = Create(name);
        GraphicalUiElement second = Create(name);

        SetSource(first, path);
        SetSource(second, path);

        TextureOf(first).ShouldNotBeNull();
        TextureOf(second).ShouldBeSameAs(TextureOf(first));
    }

    [Fact]
    public void SourceFile_SameFileOnASpriteAndANineSlice_LoadsOneBitmap()
    {
        string path = WritePng("sheet.png", SKColors.Red);
        SpriteRuntime sprite = new();
        NineSliceRuntime nineSlice = new();

        sprite.SourceFileName = path;
        nineSlice.SourceFileName = path;

        nineSlice.Texture.ShouldBeSameAs(sprite.Texture);
    }
}
