using Gum.Wireframe;
using RaylibGum.Renderables;
using RenderingLibrary.Content;
using Shouldly;
using ToolsUtilities;
using LetterCustomization = Gum.Renderables.LetterCustomization;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6639: a [Custom] letter that is scaled or turned draws its native-TTF OutlineThickness inside the letter's
/// transform, so the outline scales and turns with the letter instead of staying a screen-space ring.
/// </summary>
public class TextNativeTtfFallbackTransformedOutlineDrawTests : BaseTestClass
{
    private readonly IRaylibFontCreator? _previousCreator;
    private readonly bool _previousCacheTextures;
    private readonly List<string> _cacheKeys = new();

    public TextNativeTtfFallbackTransformedOutlineDrawTests()
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
    public void Draw_LetterScaledOnX_StretchesTheOutlineOnXOnly()
    {
        Text.Customizations["OutlineStretchX"] = (index, block) => new LetterCustomization { ScaleX = 2 };

        // FontScale is 2, so a thickness of 3 is 6 drawn pixels per side before the letter's own ScaleX of 2.
        TextInkRender.Bounds plain = Render("OutlineStretchX", outlineThickness: 0, rotation: 0);
        TextInkRender.Bounds outlined = Render("OutlineStretchX", outlineThickness: 3, rotation: 0);

        (plain.Left - outlined.Left).ShouldBeInRange(12f, 19f);
        (outlined.Right - plain.Right).ShouldBeInRange(12f, 19f);
        (plain.Top - outlined.Top).ShouldBeInRange(5f, 9f);
        (outlined.Bottom - plain.Bottom).ShouldBeInRange(5f, 9f);
    }

    [Fact]
    public void Draw_LetterScaledOnXAndTurnedQuarter_TurnsTheStretchedOutlineWithIt()
    {
        Text.Customizations["OutlineStretchTurn"] = (index, block) => new LetterCustomization { ScaleX = 2, RotationDegrees = 90 };

        TextInkRender.Bounds plain = Render("OutlineStretchTurn", outlineThickness: 0, rotation: 0);
        TextInkRender.Bounds outlined = Render("OutlineStretchTurn", outlineThickness: 3, rotation: 0);

        // The stretch now runs along y.
        (plain.Top - outlined.Top).ShouldBeInRange(12f, 19f);
        (outlined.Bottom - plain.Bottom).ShouldBeInRange(12f, 19f);
        (plain.Left - outlined.Left).ShouldBeInRange(5f, 9f);
        (outlined.Right - plain.Right).ShouldBeInRange(5f, 9f);
    }

    private TextInkRender.Bounds Render(string customName, int outlineThickness, float rotation)
    {
        string fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Nunito", "Nunito-Bold.ttf");
        return TextInkRender.InkBounds(TextInkRender.Render($"[Custom={customName}]H[/Custom]", 90, 160, text =>
        {
            text.Font = fontPath;
            text.FontSize = 32;
            text.OutlineThickness = outlineThickness;
            _cacheKeys.Add(FileManager.Standardize(text.GetFontCacheFileName(fontPath), preserveCase: true, makeAbsolute: true));
        }));
    }
}
