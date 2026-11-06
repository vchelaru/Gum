using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Forms.Input;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Caret, click, selection, Up/Down and vertical scroll of a multi-line TextBox on every line, with font
/// oversampling off and on at zoom 1.8, 3 and 5, at every horizontal alignment. Lines come from explicit
/// newlines, from wrapping, and from wrapping where the drawn line is 1% wider than the measured one
/// (so the line is drawn past the box edge). Each line's x follows the drawn glyphs: the display font's
/// advances times the compensation scale.
/// </summary>
public class TextBoxMultiLineGlyphPositionTests : BaseTestClass
{
    public const string Newlines = "newlines";
    public const string Wrapped = "wrapped";
    public const string WrappedDrawnWider = "wrappedDrawnWider";

    static readonly string[] NewlineLines = { "iWm mWi", "Wi imW Wim", "m Wi", "WWWiii mm" };
    const string WrappedText = "iWm mWi imW Wi mmm iii WWW im Wm mi iW Wim mWm iiW";
    const string WideText = "WWWWWWWWWW WWWWWWWWWW WWWWWWWWWW WWWWWWWWWW";

    // Where the pen lands on a line: every zoom, alignment and way of getting lines.
    public static IEnumerable<object[]> HorizontalCases() => BuildCases(new[] { 1f, 1.8f, 3f, 5f });

    // Which row a line is drawn on.
    public static IEnumerable<object[]> RowCases() => BuildCases(new[] { 1f });

    static IEnumerable<object[]> BuildCases(float[] zooms)
    {
        foreach (float zoom in zooms)
        {
            foreach (HorizontalAlignment alignment in new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
            {
                yield return new object[] { zoom, alignment, Newlines };
                // A wrapped line keeps its trailing space, whose alignment is not pinned here, so
                // wrapped text is checked left aligned.
                if (alignment == HorizontalAlignment.Left)
                {
                    yield return new object[] { zoom, alignment, Wrapped };
                    yield return new object[] { zoom, alignment, WrappedDrawnWider };
                }
            }
        }
    }

    public static IEnumerable<object[]> ScrollCases() => new[] { 1f }.SelectMany(zoom => new[]
    {
        new object[] { zoom, HorizontalAlignment.Left },
        new object[] { zoom, HorizontalAlignment.Center },
        new object[] { zoom, HorizontalAlignment.Right },
    });

    public static IEnumerable<object[]> ScrollInViewCases() => new[] { 1f, 1.8f, 3f, 5f }.SelectMany(zoom => new[]
    {
        new object[] { zoom, HorizontalAlignment.Left },
        new object[] { zoom, HorizontalAlignment.Center },
        new object[] { zoom, HorizontalAlignment.Right },
    });

    [Theory]
    [MemberData(nameof(HorizontalCases))]
    public void Caret_OnEveryLine_SitsAtTheDrawnPenPositionOfThatLine(float zoom, HorizontalAlignment alignment, string kind)
    {
        Harness h = new(zoom, alignment, kind);

        for (int row = 0; row < h.Lines.Count; row++)
        {
            for (int i = 0; i <= h.Lines[row].Length; i++)
            {
                if (h.IsAmbiguousBoundary(row, i))
                {
                    continue;
                }

                h.TextBox.CaretIndex = h.Starts[row] + i;

                h.CaretLeft.ShouldBe(h.X(row, i), tolerance: 0.01f, $"row {row} index {i}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(RowCases))]
    public void Caret_OnEveryLine_IsDrawnOnTheRowOfThatLine(float zoom, HorizontalAlignment alignment, string kind)
    {
        Harness h = new(zoom, alignment, kind);

        for (int row = 0; row < h.Lines.Count; row++)
        {
            h.TextBox.CaretIndex = h.Starts[row] + 1;

            h.CaretIsOnRow(row).ShouldBeTrue($"row {row}: caret center {h.CaretCenterY}, row center {h.RowCenterY(row)}");
        }
    }

    [Theory]
    [MemberData(nameof(HorizontalCases))]
    public void Click_OnTheFirstLine_ReturnsTheCharacterDrawnAtThePoint(float zoom, HorizontalAlignment alignment, string kind)
    {
        AssertClicksHitTheDrawnCharacters(new Harness(zoom, alignment, kind), rowCount: 1);
    }

    [Theory]
    [MemberData(nameof(RowCases))]
    public void Click_OnEveryLine_ReturnsTheCharacterDrawnAtThePoint(float zoom, HorizontalAlignment alignment, string kind)
    {
        Harness h = new(zoom, alignment, kind);

        AssertClicksHitTheDrawnCharacters(h, h.Lines.Count);
    }

    static void AssertClicksHitTheDrawnCharacters(Harness h, int rowCount)
    {
        for (int row = 0; row < rowCount; row++)
        {
            float y = h.RowCenterY(row);
            for (int character = 0; character < h.Lines[row].Length; character++)
            {
                float start = h.X(row, character);
                float advance = h.X(row, character + 1) - start;
                h.TextBox.GetCaretIndexAtPosition(start + advance * 0.25f, y)
                    .ShouldBe(h.Starts[row] + character, $"row {row} left quarter of character {character}");
                // The right half of a wrapped line's trailing space keeps the caret before it.
                if (h.IsAmbiguousBoundary(row, character + 1))
                {
                    continue;
                }
                h.TextBox.GetCaretIndexAtPosition(start + advance * 0.75f, y)
                    .ShouldBe(h.Starts[row] + character + 1, $"row {row} right quarter of character {character}");
            }
        }
    }

    static List<GraphicalUiElement> VisibleHighlights(Harness h) =>
        ((TextBoxBaseVisual)h.TextBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Where(child => child.Name == "SelectionInstance" && child.Visible)
            .OrderBy(child => child.GetAbsoluteTop())
            .ToList();

    static void SelectFromTheThirdCharacterOfTheFirstLineToTheFourthOfTheThird(Harness h)
    {
        h.TextBox.SelectionStart = h.Starts[0] + 2;
        h.TextBox.SelectionLength = h.Starts[2] + 3 - h.TextBox.SelectionStart;
    }

    [Theory]
    [MemberData(nameof(HorizontalCases))]
    public void Selection_SpanningSeveralLines_HighlightsTheDrawnGlyphsOfEachLine(float zoom, HorizontalAlignment alignment, string kind)
    {
        Harness h = new(zoom, alignment, kind);
        SelectFromTheThirdCharacterOfTheFirstLineToTheFourthOfTheThird(h);

        List<GraphicalUiElement> highlights = VisibleHighlights(h);

        highlights.Count.ShouldBe(3, "one highlight per selected line");
        for (int row = 0; row < 3; row++)
        {
            int from = row == 0 ? 2 : 0;
            int to = row == 2 ? 3 : h.Lines[row].Length;
            highlights[row].GetAbsoluteLeft().ShouldBe(h.X(row, from), tolerance: 0.01f, $"row {row} left");
            highlights[row].AbsoluteWidth.ShouldBe(h.X(row, to) - h.X(row, from), tolerance: 0.01f, $"row {row} width");
        }
    }

    [Theory]
    [MemberData(nameof(RowCases))]
    public void Selection_SpanningSeveralLines_HighlightsEachLineOnItsRow(float zoom, HorizontalAlignment alignment, string kind)
    {
        Harness h = new(zoom, alignment, kind);
        SelectFromTheThirdCharacterOfTheFirstLineToTheFourthOfTheThird(h);

        List<GraphicalUiElement> highlights = VisibleHighlights(h);

        highlights.Count.ShouldBe(3, "one highlight per selected line");
        for (int row = 0; row < 3; row++)
        {
            float center = highlights[row].GetAbsoluteTop() + highlights[row].AbsoluteHeight / 2f;
            Math.Abs(center - h.RowCenterY(row)).ShouldBeLessThan(h.Step / 2f, $"row {row}: highlight center {center}, row center {h.RowCenterY(row)}");
        }
    }

    static IEnumerable<(int From, Keys Key, int To)> Moves() =>
        new[] { (0, Keys.Down, 1), (1, Keys.Down, 2), (2, Keys.Up, 1), (1, Keys.Up, 0) };

    [Theory]
    [MemberData(nameof(HorizontalCases))]
    public void UpAndDown_BetweenLinesOfDifferentDrawnWidth_LandOnTheCharacterNearestTheSameX(float zoom, HorizontalAlignment alignment, string kind)
    {
        Harness h = new(zoom, alignment, kind);

        foreach ((int from, Keys key, int to) in Moves())
        {
            for (int i = 1; i < h.Lines[from].Length; i++)
            {
                h.TextBox.CaretIndex = h.Starts[from] + i;
                float x = h.X(from, i);

                h.TextBox.HandleKeyDown(key, false, false, false);

                int landed = h.TextBox.CaretIndex - h.Starts[to];
                landed.ShouldBeInRange(0, h.Lines[to].Length, $"line {from} index {i} {key}");
                // The end of a wrapped line is not a place the caret lands on that line.
                int candidates = h.IsAmbiguousBoundary(to, h.Lines[to].Length) ? h.Lines[to].Length : h.Lines[to].Length + 1;
                float nearest = Enumerable.Range(0, candidates).Min(j => Math.Abs(h.X(to, j) - x));
                Math.Abs(h.X(to, landed) - x).ShouldBe(nearest, tolerance: 0.01f, $"line {from} index {i} {key} landed at index {landed}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(RowCases))]
    public void UpAndDown_BetweenLines_DrawTheCaretOnTheTargetLinesRow(float zoom, HorizontalAlignment alignment, string kind)
    {
        Harness h = new(zoom, alignment, kind);

        foreach ((int from, Keys key, int to) in Moves())
        {
            h.TextBox.CaretIndex = h.Starts[from] + 1;

            h.TextBox.HandleKeyDown(key, false, false, false);

            h.CaretIsOnRow(to).ShouldBeTrue($"line {from} {key}: caret center {h.CaretCenterY}, row center {h.RowCenterY(to)}");
        }
    }

    [Theory]
    [MemberData(nameof(ScrollInViewCases))]
    public void Down_ThroughManyLines_KeepsTheCaretInsideTheBox(float zoom, HorizontalAlignment alignment)
    {
        Harness h = ManyLines(zoom, alignment);
        GraphicalUiElement clip = ((TextBoxBaseVisual)h.TextBox.Visual).ClipContainer;
        h.TextBox.CaretIndex = 0;

        for (int row = 1; row < 12; row++)
        {
            h.TextBox.HandleKeyDown(Keys.Down, false, false, false);

            GraphicalUiElement caret = ((TextBoxBaseVisual)h.TextBox.Visual).CaretInstance;
            caret.GetAbsoluteTop().ShouldBeGreaterThanOrEqualTo(clip.GetAbsoluteTop() - 1f, $"row {row}");
            (caret.GetAbsoluteTop() + caret.AbsoluteHeight).ShouldBeLessThanOrEqualTo(clip.GetAbsoluteTop() + clip.AbsoluteHeight + 1f, $"row {row}");
        }
    }

    [Theory]
    [MemberData(nameof(ScrollCases))]
    public void Down_ThroughManyLines_DrawsTheCaretOnTheRowOfItsLineWhileScrolling(float zoom, HorizontalAlignment alignment)
    {
        Harness h = ManyLines(zoom, alignment);
        h.TextBox.CaretIndex = 0;

        for (int row = 1; row < 12; row++)
        {
            h.TextBox.HandleKeyDown(Keys.Down, false, false, false);

            h.CaretIsOnRow(row).ShouldBeTrue($"row {row}: caret center {h.CaretCenterY}, row center {h.RowCenterY(row)}");
        }
    }

    static Harness ManyLines(float zoom, HorizontalAlignment alignment) =>
        new(zoom, alignment, Newlines, text: string.Join("\n", Enumerable.Range(1, 12).Select(i => i % 2 == 0 ? "iWm mWi" : "Wi imW Wim")), height: 70);

    // Everything below builds a TextBox whose pinned (measurement) font and drawn (display) font advance
    // each glyph differently, and computes where the drawn glyphs are.
    sealed class Harness
    {
        readonly Dictionary<char, int> _drawnAdvances;
        readonly float _compensation;
        readonly HorizontalAlignment _alignment;
        readonly TextRuntime _textInstance;
        readonly Text _contained;
        readonly float _step;

        public float Step => _step;
        public TextBox TextBox { get; }
        public List<string> Lines { get; } = new();
        public List<int> Starts { get; } = new();

        public Harness(float zoom, HorizontalAlignment alignment, string kind, string? text = null, float height = 600)
        {
            _alignment = alignment;
            Dictionary<char, int> measured = TextBoxMixedGlyphPositionTests.MeasuredAdvances;
            _drawnAdvances = zoom == 1f
                ? measured
                : measured.ToDictionary(pair => pair.Key, pair => (int)MathF.Round(pair.Value * zoom));
            _compensation = zoom == 1f ? 1f : 1f / zoom;
            if (kind == WrappedDrawnWider && zoom != 1f)
            {
                _compensation *= 1.01f;
            }

            TextBox = new TextBox();
            TextBox.TextWrapping = kind == Newlines ? Gum.Forms.TextWrapping.NoWrap : Gum.Forms.TextWrapping.Wrap;
            TextBox.AcceptsReturn = true;
            TextBox.Width = 1000;
            TextBox.Height = height;
            TextBox.Visual.X = 20;
            TextBox.Visual.Y = 30;
            _textInstance = ((TextBoxBaseVisual)TextBox.Visual).TextInstance;
            _textInstance.HorizontalAlignment = alignment;
            _contained = (Text)_textInstance.RenderableComponent!;
            _contained.BitmapFont = TextBoxMixedGlyphPositionTests.CreateFont(measured);
            if (zoom != 1f)
            {
                _contained.SetOversampledDisplayFont(TextBoxMixedGlyphPositionTests.CreateFont(_drawnAdvances));
                _contained.OversampleCompensationScale = _compensation;
            }

            if (kind != Newlines)
            {
                // Size the text area so the wrap lands where the test wants it: 170 holds ten W's.
                float inset = TextBox.Width - _textInstance.AbsoluteWidth;
                TextBox.Width = (kind == Wrapped ? 150f : 170f) + inset;
            }
            else
            {
                TextBox.Width = 700;
            }

            TextBox.Text = text ?? kind switch { Newlines => string.Join("\n", NewlineLines), Wrapped => WrappedText, _ => WideText };
            TextBox.IsFocused = true;

            _step = _contained.WrappedTextHeight / _contained.WrappedText.Count;
            int start = 0;
            if (text != null || kind == Newlines)
            {
                foreach (string line in TextBox.Text!.Split('\n'))
                {
                    Lines.Add(line);
                    Starts.Add(start);
                    start += line.Length + 1;
                }
            }
            else
            {
                foreach (string line in _contained.WrappedText)
                {
                    Lines.Add(line);
                    Starts.Add(start);
                    start += line.Length;
                }
                string.Concat(Lines).ShouldBe(TextBox.Text, "the wrapped lines are the text");
                Lines.Count.ShouldBeGreaterThanOrEqualTo(3, "the text wraps onto at least three lines");
            }
            if (kind == WrappedDrawnWider && zoom != 1f)
            {
                Pen(Lines[0].TrimEnd(), Lines[0].TrimEnd().Length)
                    .ShouldBeGreaterThan(_textInstance.AbsoluteWidth, "the first line is drawn wider than the box");
            }
        }

        // The end of a wrapped line is also the start of the next, so the caret may be drawn at either.
        public bool IsAmbiguousBoundary(int row, int index) =>
            Starts[row] + index != (TextBox.Text?.Length ?? 0)
            && index == Lines[row].Length
            && row + 1 < Lines.Count
            && Starts[row + 1] == Starts[row] + index;

        float Pen(string line, int count) => line.Take(count).Sum(character => _drawnAdvances[character]) * _compensation;

        float LineLeft(string line)
        {
            float width = Pen(line.TrimEnd(), line.TrimEnd().Length);
            float boxWidth = _textInstance.AbsoluteWidth;
            return _alignment switch
            {
                HorizontalAlignment.Center => (boxWidth - width) / 2f,
                HorizontalAlignment.Right => boxWidth - width,
                _ => 0f,
            };
        }

        // The screen x of the pen before character index of the line.
        public float X(int row, int index) =>
            _textInstance.GetAbsoluteLeft() + LineLeft(Lines[row]) + Pen(Lines[row], index);

        public float RowCenterY(int row) => _textInstance.GetAbsoluteTop() + (row + 0.5f) * _step;

        GraphicalUiElement Caret => ((TextBoxBaseVisual)TextBox.Visual).CaretInstance;

        public float CaretLeft => Caret.GetAbsoluteLeft();

        public float CaretCenterY => Caret.GetAbsoluteTop() + Caret.AbsoluteHeight / 2f;

        public bool CaretIsOnRow(int row) => Math.Abs(CaretCenterY - RowCenterY(row)) < _step / 2f;
    }
}
