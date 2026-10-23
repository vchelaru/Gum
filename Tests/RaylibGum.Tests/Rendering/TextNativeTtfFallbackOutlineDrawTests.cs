using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary.Content;
using Shouldly;
using ToolsUtilities;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6639: with no font creator, a Text whose font is a TTF file draws its OutlineThickness (black, extending
/// that many pixels outward from the glyphs), as Skia's halo does, instead of ignoring it.
/// </summary>
public class TextNativeTtfFallbackOutlineDrawTests : BaseTestClass
{
    private const string FontFileName = "Nunito-Bold.ttf";

    private readonly IRaylibFontCreator? _previousCreator;
    private readonly bool _previousCacheTextures;
    private readonly List<string> _cacheKeys = new();

    public TextNativeTtfFallbackOutlineDrawTests()
    {
        _previousCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
        _previousCacheTextures = LoaderManager.Self.CacheTextures;
        LoaderManager.Self.CacheTextures = true;
    }

    public override void Dispose()
    {
        foreach (string key in _cacheKeys)
        {
            LoaderManager.Self.Dispose(key);
        }
        LoaderManager.Self.CacheTextures = _previousCacheTextures;
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _previousCreator;
        base.Dispose();
    }

    [Fact]
    public void Draw_OutlineThickness_GrowsTheInkByThicknessOnEverySide()
    {
        // The tolerance covers the plain glyph's faint antialiased edge, which the overlapping outline passes push
        // over the ink threshold.
        TextInkRender.Bounds plain = Render(outlineThickness: 0);
        TextInkRender.Bounds outlined = Render(outlineThickness: 3);

        (plain.Left - outlined.Left).ShouldBeInRange(2f, 6f);
        (outlined.Right - plain.Right).ShouldBeInRange(2f, 6f);
        (plain.Top - outlined.Top).ShouldBeInRange(2f, 6f);
        (outlined.Bottom - plain.Bottom).ShouldBeInRange(2f, 6f);
    }

    [Fact]
    public void Draw_OutlineThickness_GrowsWithFontScale()
    {
        // FontScale stretches the outline with the glyphs, as it does for a font a creator baked an outline into (#6669).
        float GrowthLeft(float fontScale) =>
            Render(0, fontScale).Left - Render(2, fontScale).Left;

        float atScale1 = GrowthLeft(1);
        float atScale2 = GrowthLeft(2);

        atScale1.ShouldBeInRange(1f, 3f);
        atScale2.ShouldBe(atScale1 * 2, 1.5f);
    }

    [Fact]
    public void Draw_OutlineThickness_IsTheSameNumberOfPixelsAtEveryFontSize()
    {
        float GrowthLeft(float fontSize) =>
            Render(0, 1, fontSize).Left - Render(3, 1, fontSize).Left;

        GrowthLeft(24).ShouldBe(GrowthLeft(36), 1f);
        GrowthLeft(24).ShouldBeInRange(2f, 4f);
    }

    [Fact]
    public void Draw_OutlineThickness_DrawsTheOutlineBlackBehindTheGlyphs()
    {
        Color[] pixels = RenderPixels(outlineThickness: 3);
        TextInkRender.Bounds bounds = TextInkRender.InkBounds(pixels);

        // The outermost ink column is outline: opaque and black. Somewhere inside is the white glyph.
        bool leftEdgeIsBlack = false;
        bool hasWhiteGlyph = false;
        for (int y = 0; y < TextInkRender.CanvasHeight; y++)
        {
            Color edge = pixels[y * TextInkRender.CanvasWidth + (int)bounds.Left];
            leftEdgeIsBlack |= edge.A > 128 && edge.R < 64 && edge.G < 64 && edge.B < 64;
            for (int x = (int)bounds.Left; x < bounds.Right; x++)
            {
                Color pixel = pixels[y * TextInkRender.CanvasWidth + x];
                hasWhiteGlyph |= pixel.A > 250 && pixel.R > 200 && pixel.G > 200 && pixel.B > 200;
            }
        }
        leftEdgeIsBlack.ShouldBeTrue();
        hasWhiteGlyph.ShouldBeTrue();
    }

    [Fact]
    public void Draw_OutlineThicknessZero_DrawsNoBlackInk()
    {
        Color[] pixels = RenderPixels(outlineThickness: 0);

        pixels.Any(pixel => pixel.A > 128 && pixel.R < 64 && pixel.G < 64 && pixel.B < 64).ShouldBeFalse();
    }

    private TextInkRender.Bounds Render(int outlineThickness, float fontScale = 2, float fontSize = 32) =>
        TextInkRender.InkBounds(RenderPixels(outlineThickness, fontScale, fontSize));

    private Color[] RenderPixels(int outlineThickness, float fontScale = 2, float fontSize = 32)
    {
        string fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Nunito", FontFileName);
        return TextInkRender.Render("H", 60, 60, text =>
        {
            text.Font = fontPath;
            text.FontSize = fontSize;
            text.OutlineThickness = outlineThickness;
            text.FontScale = fontScale;
            _cacheKeys.Add(FileManager.Standardize(text.GetFontCacheFileName(fontPath), preserveCase: true, makeAbsolute: true));
        });
    }
}
