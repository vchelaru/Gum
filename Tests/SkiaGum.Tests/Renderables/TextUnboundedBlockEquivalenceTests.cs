using MonoGameGum.Tests;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;
using Topten.RichTextKit;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A Text keeps the block it laid out with no width limit when its Width becomes the width it measured (#7865).
/// That is only allowed where the kept block draws and lays out exactly like one built at that width, so these
/// compare the two directly: line offsets, measured size and pixels, with and without a MaxLettersToShow reveal.
/// </summary>
public class TextUnboundedBlockEquivalenceTests
{
    private readonly ITestOutputHelper _output;

    public TextUnboundedBlockEquivalenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static byte[] Render(Text text)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(900, 400));
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        surface.Canvas.Clear(SKColors.Transparent);
        text.X = 20;
        text.Y = 20;
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    private static string Describe(Text text)
    {
        TextBlock block = text.GetCachedTextBlock();
        string lines = string.Join("|", block.Lines.Select(line => $"{line.Start}+{line.Length}y{line.YCoord}"));
        // Flattened across runs: where RichTextKit splits a line into runs is internal and not drawn.
        System.Text.StringBuilder glyphs = new();
        foreach (FontRun run in block.FontRuns)
        {
            foreach (SKPoint position in run.GlyphPositions)
            {
                glyphs.Append(position.X).Append(',');
            }
        }

        return $"{lines} {glyphs} w={block.MeasuredWidth} h={block.MeasuredHeight}";
    }

    /// <summary>Whether the Text laid out and drawn after its Width moved matches a fresh Text at that width.</summary>
    private static bool MatchesFresh(Text text, string content, HorizontalAlignment alignment, float? width, int? maxLetters, out string detail)
    {
        Text fresh = new Text { RawText = content, HorizontalAlignment = alignment, FontSize = text.FontSize, Width = width, MaxLettersToShow = maxLetters };
        string kept = Describe(text);
        string expected = Describe(fresh);
        bool pixelsEqual = Render(text).AsSpan().SequenceEqual(Render(fresh));
        detail = $"kept={kept} fresh={expected} pixelsEqual={pixelsEqual}";
        return kept == expected && pixelsEqual;
    }

    [Theory]
    [InlineData("ab  \nabcdef", HorizontalAlignment.Left)]
    [InlineData("ab  \nabcdef", HorizontalAlignment.Center)]
    [InlineData("ab  \nabcdef", HorizontalAlignment.Right)]
    [InlineData("world \nHello", HorizontalAlignment.Center)]
    [InlineData("world \nHello", HorizontalAlignment.Right)]
    [InlineData("a\t\nabcdef", HorizontalAlignment.Right)]
    [InlineData("[Color=Red]ab  [/Color]\nabcdef", HorizontalAlignment.Center)]
    [InlineData("[Color=Red]ab  [/Color]\nabcdef", HorizontalAlignment.Right)]
    public void WidthSetToMeasuredWidth_LineEndsInSpaces_MatchesBlockBuiltAtThatWidth(string content, HorizontalAlignment alignment)
    {
        Text text = new Text { RawText = content, HorizontalAlignment = alignment, Width = null };
        float measuredWidth = text.GetCachedTextBlock().MeasuredWidth;

        text.Width = measuredWidth;

        MatchesFresh(text, content, alignment, measuredWidth, null, out string detail).ShouldBeTrue(detail);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void WidthSetToMeasuredWidth_RevealBuiltWhileUnbounded_MatchesBlockBuiltAtThatWidth(HorizontalAlignment alignment)
    {
        const string content = "Short\nA much longer second line here\nMid line";
        Text text = new Text { RawText = content, HorizontalAlignment = alignment, Width = null, MaxLettersToShow = 12 };
        Render(text);
        float measuredWidth = text.GetCachedTextBlock().MeasuredWidth;

        text.Width = measuredWidth;

        MatchesFresh(text, content, alignment, measuredWidth, 12, out string detail).ShouldBeTrue(detail);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void WidthSetToMeasuredWidth_LinesWithoutTrailingSpace_KeepsTheBlock(HorizontalAlignment alignment)
    {
        Text text = new Text { RawText = "Short\nA much longer line\nMid", HorizontalAlignment = alignment, Width = null };
        TextBlock unbounded = text.GetCachedTextBlock();

        text.Width = unbounded.MeasuredWidth;

        ReferenceEquals(text.GetCachedTextBlock(), unbounded).ShouldBeTrue();
    }

    private static string Generate(Random random)
    {
        string[] words =
        {
            "Hello", "world", "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", "iiii", "WWWW",
            "\U0001F600", "\U0001F44D", "x", "a\u200Bb", "\u05E9\u05DC\u05D5\u05DD", "\u0645\u0631\u062D\u0628\u0627",
            "[Color=Red]red[/Color]", "[FontSize=40]big[/FontSize]", "[IsBold=true]b[/IsBold]", "[Color=Red]pad  [/Color]",
        };
        string[] separators = { " ", " ", " ", "\n", "  ", " \n", "  \n", "\t\n", "​\n", " \n", "\n\n", "\t", "" };
        int count = random.Next(0, 14);
        System.Text.StringBuilder builder = new();
        for (int i = 0; i < count; i++)
        {
            builder.Append(words[random.Next(words.Length)]);
            builder.Append(separators[random.Next(separators.Length)]);
        }

        if (random.Next(4) == 0)
        {
            builder.Append("   ");
        }

        return builder.ToString();
    }

    // The kept block stands in for the block at the width layout sets; every width that keeps it must produce the same output.
    [Fact]
    public void UnboundedBlockKeptAcrossWidthChanges_AlwaysMatchesBlockBuiltAtThatWidth()
    {
        Random random = new Random(7866);
        List<string> contents = new List<string> { "", " ", "\u200B", "x", "\n", "a \n", "a\n " };
        int generated = SweepScale.Cases(300);
        for (int i = 0; i < generated; i++)
        {
            contents.Add(Generate(random));
        }

        int checks = 0;
        int differences = 0;
        int kept = 0;
        System.Text.StringBuilder report = new();
        foreach (string content in contents)
        {
            foreach (HorizontalAlignment alignment in new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
            {
                Text text = new Text { RawText = content, HorizontalAlignment = alignment, FontSize = random.Next(10, 40), Width = null };
                TextBlock unbounded = text.GetCachedTextBlock();
                float measuredWidth = unbounded.MeasuredWidth;
                float?[] widths = { null, measuredWidth, measuredWidth, measuredWidth + 0.5f, measuredWidth - 0.5f, measuredWidth + 40, measuredWidth * 0.7f };
                for (int step = 0; step < 6; step++)
                {
                    float? width = widths[random.Next(widths.Length)];
                    if (width <= 0)
                    {
                        width = 1;
                    }

                    text.Width = width;
                    if (ReferenceEquals(text.GetCachedTextBlock(), unbounded))
                    {
                        kept++;
                    }

                    checks++;
                    if (!MatchesFresh(text, content, alignment, width, null, out string detail))
                    {
                        differences++;
                        if (differences <= 5)
                        {
                            report.AppendLine($"DIFF '{content.Replace("\n", "\\n").Replace("\t", "\\t")}' {alignment} width={width}: {detail}");
                        }
                    }
                }
            }
        }

        _output.WriteLine($"checks={checks} kept={kept} differences={differences}");
        differences.ShouldBe(0, report.ToString());
        kept.ShouldBeGreaterThan(checks / 10);
    }
}
