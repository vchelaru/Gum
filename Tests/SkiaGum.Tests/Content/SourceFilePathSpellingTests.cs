using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using SkiaSharp;
using ToolsUtilities;

namespace SkiaGum.Tests.Content;

/// <summary>
/// A SourceFile that names the same image file loads one shared bitmap however the path is spelled
/// (relative, with <c>./</c> or <c>..</c>, backslashes, absolute), as MonoGame does by keying its texture cache
/// on the standardized absolute path.
/// </summary>
public class SourceFilePathSpellingTests : BaseTestClass
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "GumSkiaPathSpelling_" + Guid.NewGuid().ToString("N")) + "/";

    public SourceFilePathSpellingTests()
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

    [Theory]
    [InlineData("sheet.png")]
    [InlineData("./sheet.png")]
    [InlineData("sub/../sheet.png")]
    [InlineData("sub\\..\\sheet.png")]
    [InlineData("ABSOLUTE")]
    [InlineData("ABSOLUTE_DOTDOT")]
    public void SourceFile_SameFileSpelledDifferently_LoadsOneBitmap(string spelling)
    {
        string path = _directory + "sheet.png";
        using (SKBitmap bitmap = new(4, 4))
        {
            bitmap.Erase(SKColors.Red);
            using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, data.ToArray());
        }
        SpriteRuntime reference = new() { SourceFileName = "sheet.png" };
        string spelled = spelling switch
        {
            "ABSOLUTE" => path,
            "ABSOLUTE_DOTDOT" => _directory + "sub/../sheet.png",
            _ => spelling,
        };

        SpriteRuntime other = new() { SourceFileName = spelled };

        other.Texture.ShouldNotBeNull();
        other.Texture.ShouldBeSameAs(reference.Texture);
    }
}
