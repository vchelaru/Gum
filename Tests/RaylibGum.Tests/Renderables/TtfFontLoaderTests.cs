using Gum.GueDeriving;
using KernSmith.Gum;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using static Raylib_cs.Raylib;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// #6583: a TTF loaded natively on raylib sizes so its em equals the requested size, matching the font KernSmith
/// builds for MonoGame and Raylib from the same file and FontSize. The two fonts have different
/// (ascent - descent) / em ratios, so sizing by ascent-to-descent would draw each at a different scale.
/// </summary>
public class TtfFontLoaderTests : BaseTestClass
{
    [Theory]
    [InlineData("Orbitron-Black.ttf", 24)]
    [InlineData("Orbitron-Black.ttf", 48)]
    [InlineData("OpenSans-Regular.ttf", 24)]
    [InlineData("OpenSans-Regular.ttf", 48)]
    public void LoadFont_CapHeightAndBaseline_MatchKernSmithAtTheSameFontSize(string fileName, int fontSize)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", fileName);
        Font native = TtfFontLoader.LoadFont(path, fontSize);
        Font kernSmith = CreateKernSmithFont(path, fontSize);
        try
        {
            float nativeCapHeight = GetGlyphAtlasRec(native, 'H').Height;
            float kernSmithCapHeight = GetGlyphAtlasRec(kernSmith, 'H').Height;
            nativeCapHeight.ShouldBe(kernSmithCapHeight, 1.5f, "'H' must be as tall as KernSmith's at the same FontSize");

            native.BaseSize.ShouldBe(kernSmith.BaseSize, "BaseSize is the em, as for a KernSmith font");
            BaselineOf(native).ShouldBe(BaselineOf(kernSmith), 1.5f, "the baseline must sit where KernSmith's does");
            LineHeightOf(native).ShouldBe(LineHeightOf(kernSmith), 1.5f, "the line must be as tall as KernSmith's");
        }
        finally
        {
            TtfFontLoader.Unload(native);
            new ManagedFont(kernSmith).Dispose();
        }
    }

    [Fact]
    public void FontSizeRun_AtTheLoadedSize_DrawsAsWideAsPlainText_WithoutAFontCreator()
    {
        // With no font creator a [FontSize=N] run scales the base atlas to N, treating N as the font's
        // BaseSize unit, so BaseSize must be the em the font was requested at.
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "OpenSans-Regular.ttf");
        Font font = TtfFontLoader.LoadFont(path, 24);
        try
        {
            TextRuntime plain = new();
            ((Text)plain.RenderableComponent!).Font = font;
            plain.Text = "HHHHHHHH";
            float plainWidth = ((Text)plain.RenderableComponent!).WrappedTextWidth;

            TextRuntime tagged = new();
            ((Text)tagged.RenderableComponent!).Font = font;
            tagged.Text = "[FontSize=24]HHHHHHHH[/FontSize]";
            float taggedWidth = ((Text)tagged.RenderableComponent!).WrappedTextWidth;

            taggedWidth.ShouldBe(plainWidth, 1f);
        }
        finally
        {
            TtfFontLoader.Unload(font);
        }
    }

    [Fact]
    public void LineHeight_RawFontReusingTheTextureIdOfAFontFreedWithoutUnload_IsItsOwn()
    {
        // Freeing a TtfFontLoader font with raylib's UnloadFont leaves its registered metrics behind; a later
        // font that gets the same texture id must not pick them up.
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "OpenSans-Regular.ttf");
        Font registered = TtfFontLoader.LoadFont(path, 24);
        uint staleTextureId = registered.Texture.Id;
        UnloadFont(registered);

        Font raw = LoadFontEx(path, 40, null, 0);
        try
        {
            raw.Texture.Id.ShouldBe(staleTextureId, "sanity check: raylib must reuse the freed texture id");

            LineHeightOf(raw).ShouldBe(40);
        }
        finally
        {
            UnloadFont(raw);
            RaylibFontMetricsRegistry.Remove(staleTextureId);
        }
    }

    [Fact]
    public void LoadContent_TtfFile_LoadsAtTheSameSizeAsTtfFontLoader()
    {
        // ContentLoader's own TTF branch must size the font the same way as every other raylib TTF load.
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "OpenSans-Regular.ttf");
        Font expected = TtfFontLoader.LoadFont(path, 24);
        bool previousCache = LoaderManager.Self.CacheTextures;
        LoaderManager.Self.CacheTextures = false;
        try
        {
            Font loaded = LoaderManager.Self.LoadContent<Font>(path);

            loaded.BaseSize.ShouldBe(24);
            LineHeightOf(loaded).ShouldBe(LineHeightOf(expected));
            LineHeightOf(loaded).ShouldBeGreaterThan(24, "OpenSans' ascent-to-descent is larger than its em");
            new ManagedFont(loaded).Dispose();
        }
        finally
        {
            LoaderManager.Self.CacheTextures = previousCache;
            TtfFontLoader.Unload(expected);
        }
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 0, 1, 0, 0, 0, 0 })]
    [InlineData(new byte[] { 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 })]
    public void TryReadEmMetrics_FileWithoutReadableHeadAndHhea_ReturnsFalse(byte[] contents)
    {
        string path = Path.Combine(Path.GetTempPath(), "TtfFontLoaderTests_" + Path.GetRandomFileName() + ".ttf");
        File.WriteAllBytes(path, contents);
        try
        {
            TtfFontLoader.TryReadEmMetrics(path, out _, out _, out _).ShouldBeFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static Font CreateKernSmithFont(string path, int fontSize)
    {
        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = Path.GetFileNameWithoutExtension(path),
            FontFile = path,
            FontSize = fontSize,
            UseSmoothing = true,
            Ranges = "32-126",
        };
        Font? font = new KernSmithRaylibFontCreator().TryCreateFont(bmfcSave);
        font.ShouldNotBeNull("sanity check: KernSmith must rasterize the font");
        return font!.Value;
    }

    private static float LineHeightOf(Font font)
    {
        TextRuntime runtime = new();
        Text text = (Text)runtime.RenderableComponent!;
        text.Font = font;
        return text.LineHeightInPixels;
    }

    private static float BaselineOf(Font font)
    {
        TextRuntime runtime = new();
        Text text = (Text)runtime.RenderableComponent!;
        text.Font = font;
        return text.LineHeightInPixels - text.DescenderHeight;
    }
}
