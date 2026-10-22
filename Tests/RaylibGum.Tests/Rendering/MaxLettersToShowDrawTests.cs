using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6440: pins what a <see cref="Text"/> with MaxLettersToShow draws on Raylib. Each case renders the
/// text into a render target and compares every pixel with a control text whose markup holds only the
/// letters that should be visible (letters are counted across wrapped lines, an explicit newline counts
/// as one), so the expectation is the spec rather than a recorded image.
/// </summary>
public class MaxLettersToShowDrawTests : BaseTestClass
{
    private const int CanvasWidth = 320;
    private const int CanvasHeight = 220;

    private const string TwoLineStyled = "a[FontScale=2]bc[/FontScale]d\n[Color=Blue]efg[/Color]h\nij";
    private const string NestedStyled = "[Color=Green][FontScale=1.5]abcd[/FontScale]efg[/Color]";

    [Theory]
    [InlineData(0, "")]
    [InlineData(2, "ab")]
    [InlineData(4, "ab[Color=Red]cd[/Color]")]
    [InlineData(6, "ab[Color=Red]cdef[/Color]")]
    [InlineData(8, "ab[Color=Red]cdef[/Color]gh")]
    [InlineData(10, "ab[Color=Red]cdef[/Color]ghij")]
    [InlineData(99, "ab[Color=Red]cdef[/Color]ghij")]
    public void Draw_StyledSingleLine_ShowsOnlyTheFirstLetters(int count, string visibleMarkup)
    {
        AssertDrawsLikeControl("ab[Color=Red]cdef[/Color]ghij", count, visibleMarkup, wrapWidth: 0);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(3, "a[FontScale=2]bc[/FontScale]")]
    [InlineData(4, "a[FontScale=2]bc[/FontScale]d")]
    [InlineData(5, "a[FontScale=2]bc[/FontScale]d\n")]
    [InlineData(7, "a[FontScale=2]bc[/FontScale]d\n[Color=Blue]ef[/Color]")]
    [InlineData(10, "a[FontScale=2]bc[/FontScale]d\n[Color=Blue]efg[/Color]h\n")]
    [InlineData(11, "a[FontScale=2]bc[/FontScale]d\n[Color=Blue]efg[/Color]h\ni")]
    [InlineData(99, TwoLineStyled)]
    public void Draw_StyledMultiLine_ShowsOnlyTheFirstLetters(int count, string visibleMarkup)
    {
        AssertDrawsLikeControl(TwoLineStyled, count, visibleMarkup, wrapWidth: 0);
    }

    [Theory]
    [InlineData(2, "[Color=Green][FontScale=1.5]ab[/FontScale][/Color]")]
    [InlineData(4, "[Color=Green][FontScale=1.5]abcd[/FontScale][/Color]")]
    [InlineData(5, "[Color=Green][FontScale=1.5]abcd[/FontScale]e[/Color]")]
    [InlineData(99, NestedStyled)]
    public void Draw_StyledNestedRuns_ShowsOnlyTheFirstLetters(int count, string visibleMarkup)
    {
        AssertDrawsLikeControl(NestedStyled, count, visibleMarkup, wrapWidth: 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(99)]
    public void Draw_PlainMultiLine_ShowsOnlyTheFirstLetters(int count)
    {
        AssertWrappedDrawsLikeControl("abcdef\nghi\njklmn", count, wrapWidth: 0, styled: false);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(23)]
    [InlineData(40)]
    [InlineData(500)]
    public void Draw_PlainWrappedText_ShowsOnlyTheFirstLetters(int count)
    {
        AssertWrappedDrawsLikeControl(
            "lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor",
            count,
            wrapWidth: 110,
            styled: false);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(27)]
    [InlineData(40)]
    [InlineData(500)]
    public void Draw_StyledWrappedText_ShowsOnlyTheFirstLetters(int count)
    {
        AssertWrappedDrawsLikeControl(
            "lorem [Color=Red]ipsum dolor[/Color] sit amet consectetur [Color=Blue]adipiscing elit[/Color] sed do",
            count,
            wrapWidth: 110,
            styled: true);
    }

    [Fact]
    public void Draw_NullCount_ShowsEverything()
    {
        Color[] withNull = Render("ab[Color=Red]cdef[/Color]ghij", maxLetters: null, wrapWidth: 0);
        Color[] control = Render("ab[Color=Red]cdef[/Color]ghij", maxLetters: 99, wrapWidth: 0);

        InkCount(withNull).ShouldBeGreaterThan(0);
        withNull.SequenceEqual(control).ShouldBeTrue();
    }

    private static void AssertDrawsLikeControl(string markup, int count, string visibleMarkup, float wrapWidth)
    {
        Color[] actual = Render(markup, count, wrapWidth);
        Color[] control = Render(visibleMarkup, maxLetters: null, wrapWidth);

        if (visibleMarkup.Length == 0)
        {
            InkCount(actual).ShouldBe(0, "because zero letters are shown");
        }
        else
        {
            InkCount(actual).ShouldBeGreaterThan(0, "because the control draws something");
        }
        AssertSamePixels(actual, control, $"MaxLettersToShow={count} should draw like '{visibleMarkup}'");
    }

    /// <summary>
    /// Builds the control from the wrapped lines the real text produced: the letters that fit within
    /// <paramref name="count"/> across lines, one line per drawn line. Styled controls reuse the markup
    /// untouched, so only the plain case needs the lines re-joined; the styled wrapped case checks the
    /// pixel difference against a control with the same wrap and the letters past the cut blanked.
    /// </summary>
    private static void AssertWrappedDrawsLikeControl(string markup, int count, float wrapWidth, bool styled)
    {
        Color[] actual = Render(markup, count, wrapWidth);

        Text source = BuildText(markup, maxLetters: null, wrapWidth, out TextRuntime runtime);
        List<string> lines = source.WrappedText.ToList();
        Gum.GumService.Default.Root.Children!.Remove(runtime);

        string control;
        if (styled)
        {
            control = BlankPastTheCut(markup, count);
        }
        else
        {
            System.Text.StringBuilder builder = new();
            int remaining = count;
            for (int i = 0; i < lines.Count && remaining > 0; i++)
            {
                string line = lines[i];
                string shown = remaining < line.Length ? line.Substring(0, remaining) : line;
                remaining -= shown.Length;
                builder.Append(shown);
                if (i < lines.Count - 1 && !shown.EndsWith('\n') && remaining > 0)
                {
                    builder.Append('\n');
                }
            }
            control = builder.ToString();
        }

        Color[] expected = Render(control, maxLetters: null, styled ? wrapWidth : 0);
        if (count == 0)
        {
            InkCount(actual).ShouldBe(0, "because zero letters are shown");
        }
        else
        {
            InkCount(actual).ShouldBeGreaterThan(0);
        }
        AssertSamePixels(actual, expected, $"MaxLettersToShow={count} should draw like the first {count} letters");
    }

    /// <summary>
    /// The markup with every visible letter past <paramref name="count"/> replaced by a space (the tags
    /// and every letter index stay put, so the wrap and the runs are the same and a space draws nothing).
    /// </summary>
    private static string BlankPastTheCut(string markup, int count)
    {
        // Walk the markup; a character outside a [tag] is a letter of the stripped text.
        System.Text.StringBuilder builder = new();
        int letter = 0;
        bool inTag = false;
        foreach (char c in markup)
        {
            if (c == '[')
            {
                inTag = true;
            }

            if (inTag || c == '\n')
            {
                builder.Append(c);
            }
            else
            {
                builder.Append(letter < count ? c : ' ');
                letter++;
            }

            if (c == ']')
            {
                inTag = false;
            }
        }
        return builder.ToString();
    }

    private static Text BuildText(string markup, int? maxLetters, float wrapWidth, out TextRuntime runtime)
    {
        runtime = new TextRuntime();
        if (wrapWidth > 0)
        {
            runtime.WidthUnits = DimensionUnitType.Absolute;
            runtime.Width = wrapWidth;
            runtime.HeightUnits = DimensionUnitType.RelativeToChildren;
        }
        runtime.X = 2;
        runtime.Y = 2;
        runtime.Text = markup;
        runtime.MaxLettersToShow = maxLetters;
        Gum.GumService.Default.Root.Children!.Add(runtime);
        Gum.GumService.Default.Root.UpdateLayout();
        return (Text)runtime.RenderableComponent!;
    }

    private static Color[] Render(string markup, int? maxLetters, float wrapWidth)
    {
        ContainerRuntime container = new();
        container.Width = CanvasWidth;
        container.Height = CanvasHeight;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        if (wrapWidth > 0)
        {
            text.WidthUnits = DimensionUnitType.Absolute;
            text.Width = wrapWidth;
            text.HeightUnits = DimensionUnitType.RelativeToChildren;
        }
        text.X = 2;
        text.Y = 2;
        text.Text = markup;
        text.MaxLettersToShow = maxLetters;
        container.Children.Add(text);

        Gum.GumService.Default.Root.Children!.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D rt = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
        Image image = LoadImageFromTexture(rt.Texture);
        Color[] result = new Color[CanvasWidth * CanvasHeight];
        for (int y = 0; y < CanvasHeight; y++)
        {
            for (int x = 0; x < CanvasWidth; x++)
            {
                result[y * CanvasWidth + x] = GetImageColor(image, x, rt.Texture.Height - 1 - y);
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Remove(container);
        return result;
    }

    /// <summary>
    /// Fails with where the two images differ (count, bounding box, first few pixels), because a
    /// bare "SequenceEqual was false" gave nothing to go on when this failed on CI only (#6453).
    /// </summary>
    private static void AssertSamePixels(Color[] actual, Color[] expected, string because)
    {
        int differing = 0;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        System.Text.StringBuilder first = new();
        for (int i = 0; i < actual.Length; i++)
        {
            if (actual[i].R == expected[i].R && actual[i].G == expected[i].G &&
                actual[i].B == expected[i].B && actual[i].A == expected[i].A)
            {
                continue;
            }

            int x = i % CanvasWidth;
            int y = i / CanvasWidth;
            differing++;
            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            if (differing <= 5)
            {
                first.Append($" ({x},{y}) actual=({actual[i].R},{actual[i].G},{actual[i].B},{actual[i].A}) expected=({expected[i].R},{expected[i].G},{expected[i].B},{expected[i].A})");
            }
        }

        differing.ShouldBe(0,
            $"{because}; inkActual={InkCount(actual)} inkExpected={InkCount(expected)} bounds=({minX},{minY})-({maxX},{maxY}) first:{first}");
    }

    private static int InkCount(Color[] pixels) => pixels.Count(p => p.A > 0);
}
