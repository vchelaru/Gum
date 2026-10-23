using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Content;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// #6558: Raylib anchors letters at <c>LineHeightInPixels - DescenderHeight</c> below the line top, where
/// MonoGame uses <c>BitmapFont.BaselineY</c>, which is the .fnt file's <c>common base</c> as parsed by
/// <see cref="ParsedFontFile"/>. Both read the same .fnt, so the anchors must match for every font and size.
/// </summary>
public class TextBaselineTests : BaseTestClass
{
    [Theory]
    [InlineData(21, 17)]
    [InlineData(11, 9)]
    [InlineData(48, 38)]
    [InlineData(30, 30)]
    public void Baseline_FontFromFntText_EqualsTheFntBaseMonoGameUses(int lineHeight, int baseY)
    {
        string fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");
        string fntText = File.ReadAllText(Path.Combine(fixtureDirectory, "Font18Arial.fnt"))
            .Replace("common lineHeight=21 base=17", $"common lineHeight={lineHeight} base={baseY}");
        int monoGameBaselineY = new ParsedFontFile(fntText).Common!.Base;
        monoGameBaselineY.ShouldBe(baseY);

        Texture2D page = LoadTexture(Path.Combine(fixtureDirectory, "Font18Arial_0.png"));
        try
        {
            Font font = ContentLoader.BuildFontFromFntText(fntText, page);
            TextRuntime runtime = new();
            Text text = (Text)runtime.RenderableComponent!;
            text.Font = font;

            (text.LineHeightInPixels - text.DescenderHeight).ShouldBe(monoGameBaselineY);
        }
        finally
        {
            RaylibFontMetricsRegistry.Remove(page.Id);
            UnloadTexture(page);
        }
    }
}
