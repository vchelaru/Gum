using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6426: drawing a styled (BBCode) <see cref="Text"/> whose text, runs and wrap width did not change
/// must allocate nothing per frame on Raylib, like plain text does. Each guard draws a warmed-up scene
/// a fixed number of frames through the real draw pass (the hidden test window supplies the GL context)
/// and keeps the lowest of a few attempts, so a one-off blip cannot fail it.
/// </summary>
public class StyledTextAllocationTests : BaseTestClass
{
    private const string ColoredSingleLine = "ab[Color=Red]cdef[/Color]ghij";
    private const string ColoredWrapped =
        "lorem [Color=Red]ipsum dolor[/Color] sit amet consectetur [Color=Blue]adipiscing elit[/Color] " +
        "sed do eiusmod tempor [Color=Green]incididunt ut labore[/Color] et dolore magna aliqua";
    private const string ScaledWrapped =
        "lorem [FontScale=2]ipsum[/FontScale] dolor sit amet [Color=Red]consectetur[/Color] adipiscing " +
        "[FontScale=1.5]elit sed do[/FontScale] eiusmod tempor incididunt ut labore et dolore magna aliqua";

    [Fact]
    public void Draw_PlainText_AllocatesNothingPerFrame()
    {
        long bytes = MeasureDraw("abcdefghij", wrapWidth: 0, out TextRuntime text);

        ((Text)text.RenderableComponent!).InlineVariables.Count.ShouldBe(0);
        bytes.ShouldBe(0, "measured bytes over the measured frames");
    }

    [Fact]
    public void Draw_UnchangedBbCodeColoredText_AllocatesNothingPerFrame()
    {
        long bytes = MeasureDraw(ColoredSingleLine, wrapWidth: 0, out TextRuntime text);

        ((Text)text.RenderableComponent!).InlineVariables.Count.ShouldBeGreaterThan(0);
        bytes.ShouldBe(0, "measured bytes over the measured frames");
    }

    [Fact]
    public void Draw_UnchangedBbCodeColoredWrappedText_AllocatesNothingPerFrame()
    {
        long bytes = MeasureDraw(ColoredWrapped, wrapWidth: 150, out TextRuntime text);

        Text renderable = (Text)text.RenderableComponent!;
        renderable.InlineVariables.Count.ShouldBeGreaterThan(0);
        renderable.WrappedText.Count.ShouldBeGreaterThan(2);
        bytes.ShouldBe(0, "measured bytes over the measured frames");
    }

    [Fact]
    public void Draw_UnchangedBbCodeFontScaleWrappedText_AllocatesNothingPerFrame()
    {
        long bytes = MeasureDraw(ScaledWrapped, wrapWidth: 200, out TextRuntime text);

        Text renderable = (Text)text.RenderableComponent!;
        renderable.InlineVariables.Count.ShouldBeGreaterThan(0);
        renderable.WrappedText.Count.ShouldBeGreaterThan(2);
        bytes.ShouldBe(0, "measured bytes over the measured frames");
    }

    // #6440: a typewriter reveal. The count is held for several frames between changes.
    private const string RevealStyled =
        "lorem [Color=Red]ipsum dolor[/Color] sit\n[FontScale=1.5]amet consectetur[/FontScale] adipiscing\n" +
        "[Color=Blue]elit sed do eiusmod[/Color] tempor";
    private const string RevealPlain = "lorem ipsum dolor sit\namet consectetur adipiscing\nelit sed do eiusmod tempor";

    [Theory]
    [InlineData(RevealStyled)]
    [InlineData(RevealPlain)]
    public void Draw_MaxLettersToShowHeldConstantMidLine_AllocatesNothingPerFrame(string markup)
    {
        long bytes = MeasureDraw(markup, wrapWidth: 0, out TextRuntime text, maxLettersForFrame: _ => 30);

        text.MaxLettersToShow.ShouldBe(30);
        bytes.ShouldBe(0, "measured bytes over the measured frames");
    }

    [Theory]
    [InlineData(RevealStyled)]
    [InlineData(RevealPlain)]
    public void Draw_MaxLettersToShowChangedEveryFrame_AllocatesAtMostOneShortStringPerFrame(string markup)
    {
        // The count walks through the middle of the second line, so every frame cuts a line at a new place.
        long bytes = MeasureDraw(markup, wrapWidth: 0, out _, maxLettersForFrame: frame => 25 + frame % 20);

        const int measuredFrames = 20;
        (bytes / measuredFrames).ShouldBeLessThanOrEqualTo(96, "bytes per frame; one substring of a short line is the most a new count may cost");
    }

    private static long MeasureDraw(string markup, float wrapWidth, out TextRuntime text, Func<int, int>? maxLettersForFrame = null)
    {
        text = new TextRuntime();
        if (wrapWidth > 0)
        {
            text.WidthUnits = DimensionUnitType.Absolute;
            text.Width = wrapWidth;
            text.HeightUnits = DimensionUnitType.RelativeToChildren;
        }
        text.X = 10;
        text.Y = 10;
        text.Text = markup;
        Gum.GumService.Default.Root.Children.Add(text);
        Gum.GumService.Default.Root.UpdateLayout();

        long best = long.MaxValue;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            for (int frame = 0; frame < 30; frame++)
            {
                SetCount(text, maxLettersForFrame, frame);
                DrawFrame();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < 20; frame++)
            {
                SetCount(text, maxLettersForFrame, frame);
                DrawFrame();
            }
            best = Math.Min(best, GC.GetAllocatedBytesForCurrentThread() - before);
        }
        return best;
    }

    private static void SetCount(TextRuntime text, Func<int, int>? maxLettersForFrame, int frame)
    {
        if (maxLettersForFrame != null)
        {
            text.MaxLettersToShow = maxLettersForFrame(frame);
        }
    }

    private static void DrawFrame()
    {
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();
    }
}
