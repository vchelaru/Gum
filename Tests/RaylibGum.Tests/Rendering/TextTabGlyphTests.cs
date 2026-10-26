using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;
using static RaylibGum.Tests.Rendering.TextMeasureHelpers;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6848: MonoGame/KNI/FNA use a <c>char id=9</c> row from the .fnt for a tab and fall back to four spaces
/// when the font has none. raylib does the same for measuring, drawing and caret hit testing.
/// </summary>
public class TextTabGlyphTests : BaseTestClass
{
    private const int CanvasWidth = 200;
    private const int CanvasHeight = 60;
    private const int TabAdvance = 40;
    // Font18Arial.fnt: a space advances 5 pixels, so four spaces are 20.
    private const int FourSpaces = 20;

    private static string FontDirectory => Path.Combine(AppContext.BaseDirectory, "Content", "FontCache");

    private static string FntText(bool withTabRow, int tabAdvance = TabAdvance)
    {
        string fntText = File.ReadAllText(Path.Combine(FontDirectory, "Font18Arial.fnt"));
        if (!withTabRow)
        {
            return fntText;
        }
        return fntText
            .Replace("chars count=191", "chars count=192")
            .Replace("char id=32 ",
                $"char id=9    x=0     y=0     width=0     height=0     xoffset=0     yoffset=0     xadvance={tabAdvance}    page=0  chnl=15\r\nchar id=32 ");
    }

    private sealed class LoadedFont : IDisposable
    {
        private readonly Texture2D _page;
        public Font Font { get; }
        public Texture2D Page => _page;

        public LoadedFont(bool withTabRow, int tabAdvance = TabAdvance)
        {
            _page = LoadTexture(Path.Combine(FontDirectory, "Font18Arial_0.png"));
            Font = ContentLoader.BuildFontFromFntText(FntText(withTabRow, tabAdvance), _page);
        }

        public void Dispose()
        {
            RaylibFontMetricsRegistry.Remove(_page.Id);
            UnloadTexture(_page);
        }
    }

    [Fact]
    public void Measure_FontWithoutTabRow_IsFourSpaces()
    {
        using LoadedFont loaded = new(withTabRow: false);
        (TextRuntime r, Text t) = Make("a\tb", font: loaded.Font);
        float tab = t.MeasureString("a\tb");
        float spaces = t.MeasureString("a    b");
        Remove(r);

        tab.ShouldBe(spaces, 0.01f);
    }

    [Fact]
    public void Measure_FontWithTabRow_UsesTheRowsAdvance()
    {
        using LoadedFont loaded = new(withTabRow: true);
        (TextRuntime r, Text t) = Make("a\tb", font: loaded.Font);
        float tab = t.MeasureString("a\tb");
        float noTab = t.MeasureString("ab");
        float layoutWidth = r.AbsoluteWidth;
        Remove(r);

        tab.ShouldBe(noTab + TabAdvance, 0.01f);
        layoutWidth.ShouldBe(tab, 0.01f);
    }

    [Fact]
    public void Measure_StyledRunWithTabRow_UsesTheRowsAdvance()
    {
        using LoadedFont loaded = new(withTabRow: true);
        (TextRuntime r1, Text t1) = Make("[Color=Red]a\tb[/Color]", font: loaded.Font);
        float styled = r1.AbsoluteWidth;
        (TextRuntime r2, Text t2) = Make("ab", font: loaded.Font);
        float noTab = r2.AbsoluteWidth;
        Remove(r1); Remove(r2);

        styled.ShouldBe(noTab + TabAdvance, 0.01f);
    }

    [Fact]
    public void Draw_FontWithTabRow_AdvancesByTheRowsWidth()
    {
        int withoutRow;
        int withRow;
        using (LoadedFont plain = new(withTabRow: false))
        {
            withoutRow = RightmostInkColumn(plain.Font, "a\tb");
        }
        using (LoadedFont tabbed = new(withTabRow: true))
        {
            withRow = RightmostInkColumn(tabbed.Font, "a\tb");
        }

        (withRow - withoutRow).ShouldBe(TabAdvance - FourSpaces);
    }

    [Fact]
    public void HitTest_FontWithTabRow_CountsTheTabAsOneIndex()
    {
        using LoadedFont loaded = new(withTabRow: true);
        (TextRuntime r, Text t) = Make("a\tb", font: loaded.Font);
        float afterA = t.MeasureString("a");
        // 30 pixels into a 40 pixel tab is past its midpoint, so the caret goes after the tab.
        int pastTabMidpoint = t.GetCharacterIndexAtPosition(afterA + 30, 2f);
        // 10 pixels in is before the midpoint, so the caret stays before the tab.
        int beforeTabMidpoint = t.GetCharacterIndexAtPosition(afterA + 10, 2f);
        int atEnd = t.GetCharacterIndexAtPosition(r.AbsoluteWidth + 5, 2f);
        Remove(r);

        beforeTabMidpoint.ShouldBe(1);
        pastTabMidpoint.ShouldBe(2);
        atEnd.ShouldBe(3);
    }

    [Fact]
    public void Measure_RaylibDefaultFont_IsFourSpaces()
    {
        Font font = GetFontDefault();

        RaylibFontMetricsRegistry.HasTabGlyph(font).ShouldBeFalse();
        AssertTabIsFourSpaces(font);
    }

    [Fact]
    public void Measure_TtfFontLoaderFont_IsFourSpaces()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "OpenSans-Regular.ttf");
        Font font = TtfFontLoader.LoadFont(path, 24);
        try
        {
            RaylibFontMetricsRegistry.HasTabGlyph(font).ShouldBeFalse();
            AssertTabIsFourSpaces(font);
        }
        finally
        {
            TtfFontLoader.Unload(font);
        }
    }

    private static void AssertTabIsFourSpaces(Font font)
    {
        (TextRuntime r, Text t) = Make("a\tb", font: font);
        float tab = t.MeasureString("a\tb");
        float spaces = t.MeasureString("a    b");
        Remove(r);

        tab.ShouldBe(spaces, 0.01f);
    }

    [Fact]
    public void HasTabGlyph_FontRebuiltWithoutTheRowOnTheSameTexture_IsScannedAgain()
    {
        using LoadedFont loaded = new(withTabRow: true);
        RaylibFontMetricsRegistry.HasTabGlyph(loaded.Font).ShouldBeTrue();

        // Same texture id, BaseSize and atlas size, so only Register dropping the cached answer can change it.
        Font rebuilt = ContentLoader.BuildFontFromFntText(FntText(withTabRow: false), loaded.Page);

        RaylibFontMetricsRegistry.HasTabGlyph(rebuilt).ShouldBeFalse();
    }

    [Fact]
    public void HasTabGlyph_AfterTheFontIsRemoved_IsScannedAgain()
    {
        using LoadedFont tabbed = new(withTabRow: true);
        using LoadedFont plain = new(withTabRow: false);
        RaylibFontMetricsRegistry.HasTabGlyph(tabbed.Font).ShouldBeTrue();

        // A font with no tab row that now holds the texture id of the removed one, as a reused id would.
        Font reusesTextureId = plain.Font;
        reusesTextureId.Texture = tabbed.Font.Texture;
        RaylibFontMetricsRegistry.Remove(tabbed.Page.Id);

        RaylibFontMetricsRegistry.HasTabGlyph(reusesTextureId).ShouldBeFalse();
    }

    [Fact]
    public void Wrap_FontWithTabRow_BreaksWhereTheRowsWidthNoLongerFits()
    {
        // Widths at FontScale 1: a and b are 10, c is 9, a space is 5. "aa<tab>bb cc" is 20 + tab + 20 + 5 + 18,
        // which fits in 95 as four spaces (83) but not with a 40 pixel tab (103).
        using LoadedFont tabbed = new(withTabRow: true);
        using LoadedFont plain = new(withTabRow: false);
        (TextRuntime r1, Text t1) = Make("aa\tbb cc", width: 95, font: tabbed.Font);
        List<string> withRow = t1.WrappedText.ToList();
        (TextRuntime r2, Text t2) = Make("aa\tbb cc", width: 95, font: plain.Font);
        List<string> withoutRow = t2.WrappedText.ToList();
        Remove(r1); Remove(r2);

        withRow.ShouldBe(new List<string> { "aa\tbb ", "cc" });
        withoutRow.ShouldBe(new List<string> { "aa\tbb cc" });
    }

    // A tab row with xadvance=0 (and width=0) is a tab of no width, as on MonoGame. It must measure and wrap
    // exactly like the same text without the tab characters.
    [Fact]
    public void ZeroAdvanceTabRow_HasNoWidthAndWrapsLikeTextWithoutTheTab()
    {
        using LoadedFont loaded = new(withTabRow: true, tabAdvance: 0);
        (TextRuntime r1, Text t1) = Make("a\tb", font: loaded.Font);
        float tab = t1.MeasureString("a\tb");
        float noTab = t1.MeasureString("ab");
        (TextRuntime r2, Text t2) = Make("aaa\tbbb\tccc ddd eee", width: 60, font: loaded.Font);
        List<string> tabbedLines = t2.WrappedText.ToList();
        (TextRuntime r3, Text t3) = Make("aaabbbccc ddd eee", width: 60, font: loaded.Font);
        List<string> plainLines = t3.WrappedText.ToList();
        Remove(r1); Remove(r2); Remove(r3);

        tab.ShouldBe(noTab, 0.01f);
        tabbedLines.Select(line => line.Replace("\t", "")).ToList().ShouldBe(plainLines);
    }

    private static int RightmostInkColumn(Font font, string content)
    {
        Color[] pixels = TextInkRender.Render(content, 2, 2, text =>
        {
            text.FontScale = 1;
            ((Text)text.RenderableComponent!).Font = font;
        });
        return (int)TextInkRender.InkBounds(pixels).Right;
    }
}
