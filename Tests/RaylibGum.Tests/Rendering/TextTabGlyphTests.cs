using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

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

    private static string FntText(bool withTabRow)
    {
        string fntText = File.ReadAllText(Path.Combine(FontDirectory, "Font18Arial.fnt"));
        if (!withTabRow)
        {
            return fntText;
        }
        return fntText
            .Replace("chars count=191", "chars count=192")
            .Replace("char id=32 ",
                $"char id=9    x=0     y=0     width=0     height=0     xoffset=0     yoffset=0     xadvance={TabAdvance}    page=0  chnl=15\r\nchar id=32 ");
    }

    private sealed class LoadedFont : IDisposable
    {
        private readonly Texture2D _page;
        public Font Font { get; }

        public LoadedFont(bool withTabRow)
        {
            _page = LoadTexture(Path.Combine(FontDirectory, "Font18Arial_0.png"));
            Font = ContentLoader.BuildFontFromFntText(FntText(withTabRow), _page);
        }

        public void Dispose()
        {
            RaylibFontMetricsRegistry.Remove(_page.Id);
            UnloadTexture(_page);
        }
    }

    private static (TextRuntime Runtime, Text Text) Make(Font font, string content)
    {
        TextRuntime runtime = new();
        Text text = (Text)runtime.RenderableComponent!;
        text.Font = font;
        runtime.Text = content;
        Gum.GumService.Default.Root.Children!.Add(runtime);
        Gum.GumService.Default.Root.UpdateLayout();
        return (runtime, text);
    }

    private static void Remove(TextRuntime runtime) => Gum.GumService.Default.Root.Children!.Remove(runtime);

    [Fact]
    public void Measure_FontWithoutTabRow_IsFourSpaces()
    {
        using LoadedFont loaded = new(withTabRow: false);
        (TextRuntime r, Text t) = Make(loaded.Font, "a\tb");
        float tab = t.MeasureString("a\tb");
        float spaces = t.MeasureString("a    b");
        Remove(r);

        tab.ShouldBe(spaces, 0.01f);
    }

    [Fact]
    public void Measure_FontWithTabRow_UsesTheRowsAdvance()
    {
        using LoadedFont loaded = new(withTabRow: true);
        (TextRuntime r, Text t) = Make(loaded.Font, "a\tb");
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
        (TextRuntime r1, Text t1) = Make(loaded.Font, "[Color=Red]a\tb[/Color]");
        float styled = r1.AbsoluteWidth;
        (TextRuntime r2, Text t2) = Make(loaded.Font, "ab");
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
        (TextRuntime r, Text t) = Make(loaded.Font, "a\tb");
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

    private static int RightmostInkColumn(Font font, string content)
    {
        ContainerRuntime container = new();
        container.Width = CanvasWidth;
        container.Height = CanvasHeight;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        ((Text)text.RenderableComponent!).Font = font;
        text.X = 2;
        text.Y = 2;
        text.Text = content;
        container.Children.Add(text);

        Gum.GumService.Default.Root.Children!.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D rt = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
        Image image = LoadImageFromTexture(rt.Texture);
        int rightmost = -1;
        for (int y = 0; y < CanvasHeight; y++)
        {
            for (int x = 0; x < CanvasWidth; x++)
            {
                if (GetImageColor(image, x, rt.Texture.Height - 1 - y).A > 0 && x > rightmost)
                {
                    rightmost = x;
                }
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Remove(container);
        rightmost.ShouldBeGreaterThan(0);
        return rightmost;
    }
}
