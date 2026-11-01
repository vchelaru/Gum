using Gum.Clipboard;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// TextBox and PasswordBox of every packaged theme, loaded from the theme's component files by the
/// real loader and driven by a scripted cursor and keyboard: typing, caret, scrolling, selection
/// highlight placement, placeholder, MaxLength, read-only, disabled, AcceptsTab, masking and runtime
/// toggles of AcceptsReturn and TextWrapping.
/// </summary>
public class ThemedTextBoxInputTests : FileThemeInputTestBase
{
    private const string LongLine = "0123456789 0123456789 0123456789 0123456789 0123456789 0123456789 0123456789";

    private static readonly string[] TextThemeNames = { "", "Retro95" };

    public static IEnumerable<object[]> Themes() => TextThemeNames.Select(theme => new object[] { theme });

    public static IEnumerable<object[]> TextBoxThemes() => TextThemeNames.Select(theme => new object[] { theme, "TextBox" });

    public static IEnumerable<object[]> ThemesAndControls() =>
        TextThemeNames.SelectMany(theme => new[] { "TextBox", "PasswordBox" }.Select(control => new object[] { theme, control }));

    #region Helpers

    private sealed record Placed(GumProjectSave Project, InteractiveGue Visual, TextBoxBase Box, string Theme);

    private Placed Place(string theme, string control, Action<TextBoxBase>? configure = null, GumProjectSave? project = null)
    {
        project ??= LoadTheme(theme);
        string component = project.Components.Single(item => item.Name.EndsWith("Controls/" + control)).Name;
        ScreenSave screen = new ScreenSave { Name = "Screen" + project.Screens.Count };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = component, ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        InteractiveGue visual = (InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!;
        TextBoxBase box = (TextBoxBase)visual.FormsControlAsObject!;
        box.Width = 200;
        configure?.Invoke(box);
        Driver.Step(2);
        return new Placed(project, visual, box, theme);
    }

    private static string Content(TextBoxBase box) => box is TextBox textBox ? textBox.Text ?? "" : ((PasswordBox)box).Password ?? "";

    private static void SetContent(TextBoxBase box, string value)
    {
        if (box is TextBox textBox) textBox.Text = value;
        else ((PasswordBox)box).Password = value;
    }

    private static GraphicalUiElement? Child(Placed placed, string name) => placed.Visual.GetGraphicalUiElementByName(name);

    private static GraphicalUiElement TextGue(Placed placed) => Child(placed, "TextInstance")!;

    private static IEnumerable<GraphicalUiElement> Descendants(GraphicalUiElement root)
    {
        foreach (GraphicalUiElement child in root.Children.OfType<GraphicalUiElement>())
        {
            yield return child;
            foreach (GraphicalUiElement nested in Descendants(child))
            {
                yield return nested;
            }
        }
    }

    private static List<GraphicalUiElement> Highlights(Placed placed) =>
        Descendants(placed.Visual)
            .Where(item => item.Name != null && item.Name.StartsWith("SelectionInstance") && item.AbsoluteVisible && item.AbsoluteWidth > 0.5f)
            .ToList();

    private static float LineHeight(Placed placed)
    {
        Text text = (Text)TextGue(placed).RenderableComponent!;
        return text.WrappedTextHeight / Math.Max(1, text.WrappedText.Count);
    }

    /// <summary>Where the drawn line <paramref name="line"/> of the text sits on screen (top, height).</summary>
    private static (float Top, float Height) LineBand(Placed placed, int line)
    {
        GraphicalUiElement textGue = TextGue(placed);
        Text text = (Text)textGue.RenderableComponent!;
        float lineHeight = LineHeight(placed);
        float offset = text.VerticalAlignment switch
        {
            VerticalAlignment.Center => (textGue.AbsoluteHeight - text.WrappedTextHeight) / 2f,
            VerticalAlignment.Bottom => textGue.AbsoluteHeight - text.WrappedTextHeight,
            _ => 0f,
        };
        return (textGue.AbsoluteTop + offset + line * lineHeight, lineHeight);
    }

    private static float VerticalOverlap(float topA, float heightA, float topB, float heightB) =>
        Math.Max(0f, Math.Min(topA + heightA, topB + heightB) - Math.Max(topA, topB));

    private static bool HighlightCoversLine(Placed placed, List<GraphicalUiElement> highlights, int line)
    {
        (float top, float height) = LineBand(placed, line);
        return highlights.Any(item => VerticalOverlap(item.AbsoluteTop, item.AbsoluteHeight, top, height) >= 0.5f * height);
    }

    private void Chord(GumKeys key, bool shift = false, bool ctrl = false)
    {
        if (shift) Driver.SetKey(GumKeys.LeftShift, true);
        if (ctrl) Driver.SetKey(GumKeys.LeftControl, true);
        Driver.PressKey(key);
        if (shift) Driver.SetKey(GumKeys.LeftShift, false);
        if (ctrl) Driver.SetKey(GumKeys.LeftControl, false);
    }

    private void FocusByClick(Placed placed)
    {
        int x = (int)(placed.Visual.AbsoluteLeft + placed.Visual.AbsoluteWidth / 2);
        int y = (int)(placed.Visual.AbsoluteTop + placed.Visual.AbsoluteHeight / 2);
        Driver.Click(x, y);
    }

    private static string Describe(Placed placed) => $"{(placed.Theme.Length == 0 ? "FormsTemplate" : placed.Theme)} {placed.Box.GetType().Name}";


    private static void MakeMultiLine(TextBoxBase box)
    {
        TextBox textBox = (TextBox)box;
        textBox.AcceptsReturn = true;
        textBox.Height = 120;
    }

    #endregion

    #region Typing, caret and horizontal scroll

    [Theory]
    [MemberData(nameof(ThemesAndControls))]
    public void Click_ThenType_FocusesAndPutsCaretInsideTheBox(string theme, string control)
    {
        Placed placed = Place(theme, control);

        FocusByClick(placed);
        Driver.Type("abc");

        placed.Box.IsFocused.ShouldBeTrue(Describe(placed));
        Content(placed.Box).ShouldBe("abc", Describe(placed));
        placed.Box.CaretIndex.ShouldBe(3, Describe(placed));
        GraphicalUiElement caret = Child(placed, "CaretInstance")!;
        caret.AbsoluteVisible.ShouldBeTrue(Describe(placed));
        caret.AbsoluteLeft.ShouldBeInRange(placed.Visual.AbsoluteLeft, placed.Visual.AbsoluteLeft + placed.Visual.AbsoluteWidth, Describe(placed));
        caret.AbsoluteTop.ShouldBeInRange(placed.Visual.AbsoluteTop - 1f, placed.Visual.AbsoluteTop + placed.Visual.AbsoluteHeight, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(ThemesAndControls))]
    public void LongText_CaretAtEnd_StaysInsideTheBox(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);

        Driver.Type(LongLine);

        GraphicalUiElement caret = Child(placed, "CaretInstance")!;
        float left = placed.Visual.AbsoluteLeft;
        float right = left + placed.Visual.AbsoluteWidth;
        caret.AbsoluteLeft.ShouldBeInRange(left, right, Describe(placed));
        TextGue(placed).AbsoluteLeft.ShouldBeLessThan(left, $"{Describe(placed)}: long text did not scroll left");
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void LongText_HomeAfterEnd_ScrollsTextBackToTheStart(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);
        Driver.Type(LongLine);

        Chord(GumKeys.Home);

        GraphicalUiElement text = TextGue(placed);
        GraphicalUiElement caret = Child(placed, "CaretInstance")!;
        float left = placed.Visual.AbsoluteLeft;
        caret.AbsoluteLeft.ShouldBeInRange(left, left + 40f, Describe(placed));
        text.AbsoluteLeft.ShouldBeInRange(left, left + 40f, $"{Describe(placed)}: text still scrolled");
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void LongText_CaretMovedToMiddle_CaretMatchesTheCharacterItIsBefore(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);
        Driver.Type(LongLine);

        for (int i = 0; i < 30; i++)
        {
            Chord(GumKeys.Left);
        }

        placed.Box.CaretIndex.ShouldBe(LongLine.Length - 30, Describe(placed));
        GraphicalUiElement caret = Child(placed, "CaretInstance")!;
        float left = placed.Visual.AbsoluteLeft;
        caret.AbsoluteLeft.ShouldBeInRange(left, left + placed.Visual.AbsoluteWidth, Describe(placed));
    }

    #endregion

    #region Single-line selection

    [Theory]
    [MemberData(nameof(ThemesAndControls))]
    public void ShiftLeft_SelectsFromTheEnd_HighlightEndsAtTheTextEndAndOverlapsTheLine(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);
        Driver.Type("hello world");

        for (int i = 0; i < 5; i++)
        {
            Chord(GumKeys.Left, shift: true);
        }

        placed.Box.SelectionStart.ShouldBe(6, Describe(placed));
        placed.Box.SelectionLength.ShouldBe(5, Describe(placed));
        List<GraphicalUiElement> highlights = Highlights(placed);
        highlights.Count.ShouldBe(1, Describe(placed));
        GraphicalUiElement highlight = highlights[0];
        GraphicalUiElement caret = Child(placed, "CaretInstance")!;
        (float top, float height) = LineBand(placed, 0);
        VerticalOverlap(highlight.AbsoluteTop, highlight.AbsoluteHeight, top, height)
            .ShouldBeGreaterThanOrEqualTo(0.5f * height, $"{Describe(placed)}: highlight y {highlight.AbsoluteTop}+{highlight.AbsoluteHeight} vs text line {top}+{height}");
        highlight.AbsoluteHeight.ShouldBeLessThanOrEqualTo(placed.Visual.AbsoluteHeight, Describe(placed));
        highlight.AbsoluteLeft.ShouldBeGreaterThanOrEqualTo(placed.Visual.AbsoluteLeft, Describe(placed));
        (highlight.AbsoluteLeft + highlight.AbsoluteWidth).ShouldBeLessThanOrEqualTo(placed.Visual.AbsoluteLeft + placed.Visual.AbsoluteWidth + 1f, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(ThemesAndControls))]
    public void CtrlA_SelectsEverything_HighlightWidthMatchesTheText(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);
        Driver.Type("hello world");

        Chord(GumKeys.A, ctrl: true);

        placed.Box.SelectionLength.ShouldBe(11, Describe(placed));
        List<GraphicalUiElement> highlights = Highlights(placed);
        highlights.Count.ShouldBe(1, Describe(placed));
        Text text = (Text)TextGue(placed).RenderableComponent!;
        highlights[0].AbsoluteWidth.ShouldBe(text.WrappedTextWidth, 4f, $"{Describe(placed)}: highlight width vs text width");
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void ShiftHome_SelectsToTheStart(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);
        Driver.Type("hello world");

        Chord(GumKeys.Home, shift: true);

        placed.Box.SelectionStart.ShouldBe(0, Describe(placed));
        placed.Box.SelectionLength.ShouldBe(11, Describe(placed));
        Highlights(placed).Count.ShouldBe(1, Describe(placed));
        Chord(GumKeys.End, shift: true);
        placed.Box.SelectionLength.ShouldBe(0, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void MouseDrag_AcrossText_SelectsAndShowsHighlight(string theme, string control)
    {
        Placed placed = Place(theme, control);
        SetContent(placed.Box, "hello world");
        Driver.Step(2);
        GraphicalUiElement text = TextGue(placed);
        int y = (int)(placed.Visual.AbsoluteTop + placed.Visual.AbsoluteHeight / 2);

        Driver.MoveTo((int)text.AbsoluteLeft + 1, y);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: true);
        Driver.MoveTo((int)(text.AbsoluteLeft + text.AbsoluteWidth), y);
        Driver.MoveTo((int)(text.AbsoluteLeft + text.AbsoluteWidth), y);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: false);

        placed.Box.SelectionLength.ShouldBeGreaterThan(8, Describe(placed));
        Highlights(placed).Count.ShouldBe(1, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void DoubleClick_SelectsText(string theme, string control)
    {
        Placed placed = Place(theme, control);
        SetContent(placed.Box, "hello world");
        Driver.Step(2);
        int x = (int)(placed.Visual.AbsoluteLeft + 10);
        int y = (int)(placed.Visual.AbsoluteTop + placed.Visual.AbsoluteHeight / 2);

        Driver.Click(x, y);
        Driver.Click(x, y);

        placed.Box.SelectionLength.ShouldBeGreaterThan(0, Describe(placed));
        Highlights(placed).Count.ShouldBe(1, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void EmojiSelectedWithShiftLeft_HighlightHasWidth(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);
        Driver.Type("a\U0001F600b");

        Chord(GumKeys.Left);
        Chord(GumKeys.Left, shift: true);

        placed.Box.SelectionLength.ShouldBe(2, Describe(placed));
        Highlights(placed).Count.ShouldBe(1, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void SelectionScrolledWithLongText_HighlightStaysInsideTheBox(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);
        Driver.Type(LongLine);

        for (int i = 0; i < 6; i++)
        {
            Chord(GumKeys.Left, shift: true);
        }

        List<GraphicalUiElement> highlights = Highlights(placed);
        highlights.Count.ShouldBe(1, Describe(placed));
        float left = placed.Visual.AbsoluteLeft;
        highlights[0].AbsoluteLeft.ShouldBeGreaterThanOrEqualTo(left - 1f, Describe(placed));
        (highlights[0].AbsoluteLeft + highlights[0].AbsoluteWidth).ShouldBeLessThanOrEqualTo(left + placed.Visual.AbsoluteWidth + 1f, Describe(placed));
    }

    #endregion

    #region Multi-line selection placement

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_SelectAll_EachLineHasAHighlightOnItsOwnLine(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        FocusByClick(placed);
        SetContent(placed.Box, "alpha\nbeta\n\ngamma delta");
        Driver.Step(2);

        Chord(GumKeys.A, ctrl: true);

        List<GraphicalUiElement> highlights = Highlights(placed);
        foreach (int line in new[] { 0, 1, 3 })
        {
            HighlightCoversLine(placed, highlights, line).ShouldBeTrue(
                $"{Describe(placed)}: no highlight on line {line}; highlights " +
                string.Join(" ", highlights.Select(item => $"[{item.AbsoluteTop}+{item.AbsoluteHeight}]")) +
                $" lines {string.Join(" ", Enumerable.Range(0, 4).Select(i => { var band = LineBand(placed, i); return $"[{band.Top}+{band.Height}]"; }))}");
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_CrlfSelectAll_EachLineHasAHighlightOnItsOwnLine(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        FocusByClick(placed);
        SetContent(placed.Box, "alpha\r\nbeta\r\ngamma");
        Driver.Step(2);

        Chord(GumKeys.A, ctrl: true);

        List<GraphicalUiElement> highlights = Highlights(placed);
        foreach (int line in new[] { 0, 1, 2 })
        {
            HighlightCoversLine(placed, highlights, line).ShouldBeTrue($"{Describe(placed)}: no highlight on CRLF line {line}");
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_KeyboardSelectionAcrossNewline_HighlightsBothLines(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        FocusByClick(placed);
        SetContent(placed.Box, "alpha\nbeta");
        Driver.Step(2);
        placed.Box.CaretIndex = 8;

        for (int i = 0; i < 6; i++)
        {
            Chord(GumKeys.Left, shift: true);
        }

        placed.Box.SelectionLength.ShouldBe(6, Describe(placed));
        List<GraphicalUiElement> highlights = Highlights(placed);
        HighlightCoversLine(placed, highlights, 0).ShouldBeTrue($"{Describe(placed)}: first line");
        HighlightCoversLine(placed, highlights, 1).ShouldBeTrue($"{Describe(placed)}: second line");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_SelectionOnLastLineOnly_HighlightIsOnTheLastLine(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        FocusByClick(placed);
        SetContent(placed.Box, "one\ntwo\nthree");
        Driver.Step(2);
        placed.Box.CaretIndex = 13;

        Chord(GumKeys.Home, shift: true);

        List<GraphicalUiElement> highlights = Highlights(placed);
        highlights.Count.ShouldBe(1, Describe(placed));
        HighlightCoversLine(placed, highlights, 2).ShouldBeTrue(
            $"{Describe(placed)}: highlight {highlights[0].AbsoluteTop}+{highlights[0].AbsoluteHeight} vs line {LineBand(placed, 2)}");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Wrapped_SelectAll_EachWrappedLineHasAHighlight(string theme)
    {
        Placed placed = Place(theme, "TextBox", box =>
        {
            ((TextBox)box).TextWrapping = TextWrapping.Wrap;
            box.Height = 120;
        });
        FocusByClick(placed);
        SetContent(placed.Box, "the quick brown fox jumps over the lazy dog and keeps running far away");
        Driver.Step(2);
        Text text = (Text)TextGue(placed).RenderableComponent!;
        int lineCount = text.WrappedText.Count;
        lineCount.ShouldBeGreaterThan(2, Describe(placed));

        Chord(GumKeys.A, ctrl: true);

        List<GraphicalUiElement> highlights = Highlights(placed);
        for (int line = 0; line < lineCount; line++)
        {
            HighlightCoversLine(placed, highlights, line).ShouldBeTrue($"{Describe(placed)}: wrapped line {line} of {lineCount}");
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_ScrolledToTheEnd_SelectionFollowsTheScrolledLines(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        FocusByClick(placed);
        SetContent(placed.Box, string.Join("\n", Enumerable.Range(0, 30).Select(i => "line " + i)));
        Driver.Step(2);

        placed.Box.CaretIndex = Content(placed.Box).Length;
        for (int i = 0; i < 2; i++)
        {
            Chord(GumKeys.Up, shift: true);
        }

        GraphicalUiElement caret = Child(placed, "CaretInstance")!;
        float boxTop = placed.Visual.AbsoluteTop;
        float boxBottom = boxTop + placed.Visual.AbsoluteHeight;
        caret.AbsoluteTop.ShouldBeInRange(boxTop - 1f, boxBottom, $"{Describe(placed)}: caret left the box");
        List<GraphicalUiElement> highlights = Highlights(placed);
        highlights.Count.ShouldBeGreaterThan(0, Describe(placed));
        foreach (GraphicalUiElement highlight in highlights)
        {
            highlight.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(boxTop - 1f, $"{Describe(placed)}: highlight above the box");
            (highlight.AbsoluteTop + highlight.AbsoluteHeight).ShouldBeLessThanOrEqualTo(boxBottom + 3f, $"{Describe(placed)}: highlight below the box: highlight {highlight.AbsoluteTop}+{highlight.AbsoluteHeight}, box {boxTop}+{placed.Visual.AbsoluteHeight}, caret {caret.AbsoluteTop}+{caret.AbsoluteHeight}, text top {TextGue(placed).AbsoluteTop}");
        }
        // The last three lines are 27..29; the selection covers 28 and 29 (and the end of 27).
        int lastLine = 29;
        HighlightCoversLine(placed, highlights, lastLine).ShouldBeTrue($"{Describe(placed)}: no highlight on the scrolled last line; highlights {string.Join(" ", highlights.Select(item => $"[{item.AbsoluteTop}+{item.AbsoluteHeight}]"))} line29 {LineBand(placed, 29)} text top {TextGue(placed).AbsoluteTop} h {TextGue(placed).AbsoluteHeight} box {placed.Visual.AbsoluteTop}+{placed.Visual.AbsoluteHeight} caret {Child(placed, "CaretInstance")!.AbsoluteTop} sel {placed.Box.SelectionStart}+{placed.Box.SelectionLength}");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_MouseDragAcrossLines_SelectsAndHighlightsEachLine(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        SetContent(placed.Box, "alpha one\nbeta two\ngamma three");
        Driver.Step(2);
        GraphicalUiElement text = TextGue(placed);
        (float top0, float height) = LineBand(placed, 0);
        (float top2, _) = LineBand(placed, 2);
        int startX = (int)text.AbsoluteLeft + 2;
        int startY = (int)(top0 + height / 2);
        int endX = (int)(text.AbsoluteLeft + 60);
        int endY = (int)(top2 + height / 2);

        Driver.MoveTo(startX, startY);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: true);
        Driver.MoveTo(endX, endY);
        Driver.MoveTo(endX, endY);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: false);

        placed.Box.SelectionLength.ShouldBeGreaterThan(15, Describe(placed));
        List<GraphicalUiElement> highlights = Highlights(placed);
        for (int line = 0; line < 3; line++)
        {
            HighlightCoversLine(placed, highlights, line).ShouldBeTrue($"{Describe(placed)}: dragged selection, line {line}");
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_ClickOnSecondLine_PutsCaretOnSecondLine(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        SetContent(placed.Box, "alpha one\nbeta two\ngamma three");
        Driver.Step(2);
        (float top1, float height) = LineBand(placed, 1);

        Driver.Click((int)TextGue(placed).AbsoluteLeft + 5, (int)(top1 + height / 2));

        placed.Box.CaretIndex.ShouldBeInRange(10, 19, Describe(placed));
        GraphicalUiElement caret = Child(placed, "CaretInstance")!;
        VerticalOverlap(caret.AbsoluteTop, caret.AbsoluteHeight, top1, height)
            .ShouldBeGreaterThanOrEqualTo(0.5f * height, $"{Describe(placed)}: caret {caret.AbsoluteTop}+{caret.AbsoluteHeight} vs line {top1}+{height}");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_SelectAllWithLargerFontScale_HighlightsStayOnTheirLines(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        FocusByClick(placed);
        SetContent(placed.Box, "alpha\nbeta");
        ((Text)TextGue(placed).RenderableComponent!).FontScale = 1.5f;
        Driver.Step(2);

        Chord(GumKeys.A, ctrl: true);

        List<GraphicalUiElement> highlights = Highlights(placed);
        HighlightCoversLine(placed, highlights, 0).ShouldBeTrue($"{Describe(placed)}: line 0");
        HighlightCoversLine(placed, highlights, 1).ShouldBeTrue($"{Describe(placed)}: line 1");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_Typing_TextStartsAtTheTopOfTheBox(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        FocusByClick(placed);

        Driver.Type("x");

        (float top, float height) = LineBand(placed, 0);
        top.ShouldBeInRange(placed.Visual.AbsoluteTop, placed.Visual.AbsoluteTop + height + 12f, $"{Describe(placed)}: first line of a multi-line box is not at the top");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void MultiLine_EnterKey_InsertsNewlineAndMovesCaretToTheNextLine(string theme)
    {
        Placed placed = Place(theme, "TextBox", MakeMultiLine);
        FocusByClick(placed);
        Driver.Type("ab");

        Driver.Type("\n");
        Driver.Type("cd");

        Content(placed.Box).ShouldBe("ab\ncd", Describe(placed));
        GraphicalUiElement caret = Child(placed, "CaretInstance")!;
        (float top1, float height) = LineBand(placed, 1);
        VerticalOverlap(caret.AbsoluteTop, caret.AbsoluteHeight, top1, height)
            .ShouldBeGreaterThanOrEqualTo(0.5f * height, $"{Describe(placed)}: caret not on line 2");
    }

    #endregion

    #region Placeholder, MaxLength, read-only, disabled, AcceptsTab

    [Theory]
    [MemberData(nameof(ThemesAndControls))]
    public void Placeholder_ShownWhenEmpty_HiddenOnceTextIsTypedAndShownAgainWhenCleared(string theme, string control)
    {
        Placed placed = Place(theme, control, box => box.Placeholder = "Type here");
        GraphicalUiElement? placeholder = Child(placed, "PlaceholderTextInstance");
        if (placeholder == null)
        {
            return;
        }

        placeholder.AbsoluteVisible.ShouldBeTrue($"{Describe(placed)}: empty");
        FocusByClick(placed);
        Driver.Type("a");
        placeholder.AbsoluteVisible.ShouldBeFalse($"{Describe(placed)}: typed; content [{Content(placed.Box)}] Visible {placeholder.Visible} text [{placed.Box.Placeholder}]");
        Chord(GumKeys.Back);
        placeholder.AbsoluteVisible.ShouldBeTrue($"{Describe(placed)}: cleared");
    }

    [Fact]
    public void Placeholder_CodeBuiltTextBox_HiddenOnceTextIsTyped()
    {
        TextBox textBox = new TextBox();
        textBox.Placeholder = "Type here";
        textBox.AddToRoot();
        Driver.Step(2);
        GraphicalUiElement placeholder = textBox.Visual.GetGraphicalUiElementByName("PlaceholderTextInstance")!;
        textBox.IsFocused = true;

        Driver.Type("a");

        placeholder.Visible.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TextChanged_ThemedTextBox_FiresWhenTextIsSetFromCode(string theme)
    {
        Placed placed = Place(theme, "TextBox");
        int fired = 0;
        ((TextBox)placed.Box).TextChanged += (_, _) => fired++;

        ((TextBox)placed.Box).Text = "abc";
        Driver.Step(1);

        fired.ShouldBe(1, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TextChanged_ThemedTextBox_FiresWhenTyped(string theme)
    {
        Placed placed = Place(theme, "TextBox");
        int fired = 0;
        ((TextBox)placed.Box).TextChanged += (_, _) => fired++;
        FocusByClick(placed);

        Driver.Type("abc");

        fired.ShouldBeGreaterThan(0, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Placeholder_ThemedTextBox_HiddenWhenTextIsSetFromCode(string theme)
    {
        Placed placed = Place(theme, "TextBox", box => box.Placeholder = "Type here");
        GraphicalUiElement? placeholder = Child(placed, "PlaceholderTextInstance");

        ((TextBox)placed.Box).Text = "abc";
        Driver.Step(2);

        placeholder!.AbsoluteVisible.ShouldBeFalse(Describe(placed));
    }

    private sealed class TextViewModel : Gum.Mvvm.ViewModel
    {
        public string? Text
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TwoWayBinding_ThemedTextBox_TypedTextReachesTheViewModel(string theme)
    {
        Placed placed = Place(theme, "TextBox");
        TextViewModel viewModel = new TextViewModel();
        TextBox textBox = (TextBox)placed.Box;
        textBox.BindingContext = viewModel;
        textBox.SetBinding(nameof(TextBox.Text), new Gum.Forms.Data.Binding(nameof(TextViewModel.Text)) { Mode = Gum.Forms.Data.BindingMode.TwoWay });
        FocusByClick(placed);

        Driver.Type("abc");

        viewModel.Text.ShouldBe("abc", Describe(placed));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void PasswordChanged_ThemedPasswordBox_FiresWhenTyped(string theme)
    {
        Placed placed = Place(theme, "PasswordBox");
        int fired = 0;
        ((PasswordBox)placed.Box).PasswordChanged += (_, _) => fired++;
        FocusByClick(placed);

        Driver.Type("abc");

        fired.ShouldBeGreaterThan(0, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void MaxLength_TypingBeyondTheLimit_IsTruncated(string theme, string control)
    {
        Placed placed = Place(theme, control, box => box.MaxLength = 5);
        FocusByClick(placed);

        Driver.Type("abcdefgh");

        Content(placed.Box).ShouldBe("abcde", Describe(placed));
        placed.Box.CaretIndex.ShouldBe(5, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(ThemesAndControls))]
    public void ReadOnly_TypingAndBackspace_DoNotChangeTheText(string theme, string control)
    {
        Placed placed = Place(theme, control);
        SetContent(placed.Box, "fixed");
        placed.Box.IsReadOnly = true;
        FocusByClick(placed);

        Driver.Type("zz");
        Chord(GumKeys.Back);
        Chord(GumKeys.Delete);

        Content(placed.Box).ShouldBe("fixed", Describe(placed));
    }

    [Theory]
    [MemberData(nameof(ThemesAndControls))]
    public void Disabled_ClickAndTyping_DoNotFocusOrChangeTheText(string theme, string control)
    {
        Placed placed = Place(theme, control);
        placed.Box.IsEnabled = false;
        Driver.Step(2);

        FocusByClick(placed);
        Driver.Type("zz");

        placed.Box.IsFocused.ShouldBeFalse(Describe(placed));
        Content(placed.Box).ShouldBe("", Describe(placed));
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void Disabled_TextInTheDisabledState_IsStillVisible(string theme, string control)
    {
        Placed placed = Place(theme, control);
        SetContent(placed.Box, "hello");
        placed.Box.IsEnabled = false;
        Driver.Step(2);

        GraphicalUiElement text = TextGue(placed);
        text.AbsoluteVisible.ShouldBeTrue(Describe(placed));
        ((Text)text.RenderableComponent!).Alpha.ShouldBeGreaterThan(0, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void AcceptsTab_True_InsertsATab_False_DoesNot(string theme)
    {
        Placed accepts = Place(theme, "TextBox", box => ((TextBox)box).AcceptsTab = true);
        FocusByClick(accepts);
        Driver.Type("a\tb");
        Content(accepts.Box).ShouldBe("a\tb", Describe(accepts));

        Placed ignores = Place(theme, "TextBox", box => { }, accepts.Project);
        FocusByClick(ignores);
        Driver.Type("a\tb");
        Content(ignores.Box).ShouldBe("ab", Describe(ignores));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void FocusedIndicator_VisibleOnlyWhileFocused(string theme)
    {
        Placed placed = Place(theme, "TextBox");
        GraphicalUiElement? indicator = theme == "Editor" ? null : Child(placed, "FocusedIndicator"); // Editor draws an Outline instead
        if (indicator == null)
        {
            return;
        }

        indicator.AbsoluteVisible.ShouldBeFalse($"{Describe(placed)}: before focus");
        FocusByClick(placed);
        indicator.AbsoluteVisible.ShouldBeTrue($"{Describe(placed)}: focused");
        placed.Box.IsFocused = false;
        Driver.Step(2);
        indicator.AbsoluteVisible.ShouldBeFalse($"{Describe(placed)}: after blur");
    }

    #endregion

    #region Code-driven changes

    private Placed PlaceWithAllSelectedThenShortenFromCode(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);
        Driver.Type("hello world");
        Chord(GumKeys.A, ctrl: true);
        SetContent(placed.Box, "hi");
        Driver.Step(2);
        return placed;
    }

    [Theory]
    [MemberData(nameof(ThemesAndControls))]
    public void TextSetFromCodeWhileFocused_ShorterText_CaretIsClampedToTheText(string theme, string control)
    {
        Placed placed = PlaceWithAllSelectedThenShortenFromCode(theme, control);

        placed.Box.CaretIndex.ShouldBeInRange(0, 2, $"{Describe(placed)}: selection {placed.Box.SelectionStart}+{placed.Box.SelectionLength}");
    }

    [Theory]
    [MemberData(nameof(TextBoxThemes))]
    public void CtrlA_ThenTyping_ReplacesTheSelection(string theme, string control)
    {
        Placed placed = Place(theme, control);
        FocusByClick(placed);
        Driver.Type("hello world");
        Chord(GumKeys.A, ctrl: true);

        Driver.Type("X");

        Content(placed.Box).ShouldBe("X", Describe(placed));
        Highlights(placed).ShouldBeEmpty(Describe(placed));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Copy_ThenPaste_RoundTripsTheSelection(string theme)
    {
        ClipboardImplementation.PushStringToClipboard("sentinel");
        Placed placed = Place(theme, "TextBox");
        FocusByClick(placed);
        Driver.Type("hello world");
        Chord(GumKeys.Home, shift: true);

        Chord(GumKeys.C, ctrl: true);
        Chord(GumKeys.End);
        Chord(GumKeys.V, ctrl: true);

        ClipboardImplementation.GetText(null).ShouldBe("hello world", Describe(placed));
        Content(placed.Box).ShouldBe("hello worldhello world", Describe(placed));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Cut_RemovesTheSelectionAndPutsItOnTheClipboard(string theme)
    {
        ClipboardImplementation.PushStringToClipboard("sentinel");
        Placed placed = Place(theme, "TextBox");
        FocusByClick(placed);
        Driver.Type("hello world");
        for (int i = 0; i < 5; i++)
        {
            Chord(GumKeys.Left, shift: true);
        }

        Chord(GumKeys.X, ctrl: true);

        ClipboardImplementation.GetText(null).ShouldBe("world", Describe(placed));
        Content(placed.Box).ShouldBe("hello ", Describe(placed));
    }

    #endregion

    #region PasswordBox

    [Theory]
    [MemberData(nameof(Themes))]
    public void PasswordBox_Typing_ShowsOneMaskCharacterPerCharacterAndNeverThePassword(string theme)
    {
        Placed placed = Place(theme, "PasswordBox");
        FocusByClick(placed);

        Driver.Type("hunter2");

        Text text = (Text)TextGue(placed).RenderableComponent!;
        string displayed = string.Concat(text.WrappedText);
        displayed.Length.ShouldBe(7, Describe(placed));
        displayed.Contains("hunter").ShouldBeFalse(Describe(placed));
        displayed.Distinct().Count().ShouldBe(1, Describe(placed));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void PasswordBox_CopyAndCutOfSelection_NeverPutThePasswordOnTheClipboard(string theme)
    {
        ClipboardImplementation.PushStringToClipboard("sentinel");
        Placed placed = Place(theme, "PasswordBox");
        FocusByClick(placed);
        Driver.Type("hunter2");
        Chord(GumKeys.A, ctrl: true);

        Chord(GumKeys.C, ctrl: true);
        Chord(GumKeys.X, ctrl: true);

        (ClipboardImplementation.GetText(null) ?? "").Contains("hunter2").ShouldBeFalse(Describe(placed));
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void PasswordBox_Selection_HighlightWidthMatchesTheMaskedText(string theme)
    {
        Placed placed = Place(theme, "PasswordBox");
        FocusByClick(placed);
        Driver.Type("hunter2");

        Chord(GumKeys.A, ctrl: true);

        List<GraphicalUiElement> highlights = Highlights(placed);
        highlights.Count.ShouldBe(1, Describe(placed));
        Text text = (Text)TextGue(placed).RenderableComponent!;
        highlights[0].AbsoluteWidth.ShouldBe(text.WrappedTextWidth, 4f, Describe(placed));
    }

    #endregion

    #region Runtime toggles of AcceptsReturn and TextWrapping

    public enum Mode { Single, AcceptsReturn, Wrap, AcceptsReturnAndWrap }

    private static void Apply(TextBoxBase box, Mode mode)
    {
        TextBox textBox = (TextBox)box;
        textBox.AcceptsReturn = mode == Mode.AcceptsReturn || mode == Mode.AcceptsReturnAndWrap;
        textBox.TextWrapping = mode == Mode.Wrap || mode == Mode.AcceptsReturnAndWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    private static string Snapshot(Placed placed)
    {
        GraphicalUiElement text = TextGue(placed);
        GraphicalUiElement? selection = Child(placed, "SelectionInstance");
        Text renderable = (Text)text.RenderableComponent!;
        return $"align {renderable.VerticalAlignment}, text x {text.X} y {text.Y} w {text.Width}{text.WidthUnits} h {text.Height}{text.HeightUnits}, " +
               $"selection h {selection?.Height}{selection?.HeightUnits} y {selection?.Y}{selection?.YUnits}";
    }

    public static IEnumerable<object[]> ModeTransitions() =>
        TextThemeNames.SelectMany(theme => Enum.GetValues<Mode>().SelectMany(from =>
            Enum.GetValues<Mode>().Where(to => to == Mode.Single && from != Mode.Single).Select(to => new object[] { theme, from, to })));

    [Theory]
    [MemberData(nameof(ModeTransitions))]
    public void RuntimeToggle_ThemedTextBox_EndsInTheSameLayoutAsAFreshBoxOfThatConfiguration(string theme, Mode from, Mode to)
    {
        Placed fresh = Place(theme, "TextBox", box => { if (to != Mode.Single) Apply(box, to); });
        string expected = Snapshot(fresh);

        Placed toggled = Place(theme, "TextBox", box => Apply(box, from), fresh.Project);
        Apply(toggled.Box, to);
        Driver.Step(2);

        Snapshot(toggled).ShouldBe(expected, $"{Describe(toggled)} {from} -> {to}");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RuntimeToggle_WrapOnThenOffWithTextAndFocus_ScrollsBackAndKeepsCaretInTheBox(string theme)
    {
        Placed placed = Place(theme, "TextBox", box => box.Height = 90);
        FocusByClick(placed);
        Driver.Type(LongLine);
        TextBox textBox = (TextBox)placed.Box;

        textBox.TextWrapping = TextWrapping.Wrap;
        Driver.Step(2);
        placed.Box.CaretIndex = 0;
        GraphicalUiElement text = TextGue(placed);
        float boxLeft = placed.Visual.AbsoluteLeft;
        text.AbsoluteLeft.ShouldBeInRange(boxLeft, boxLeft + 40f, $"{Describe(placed)}: wrapped text should start at the left edge");

        textBox.TextWrapping = TextWrapping.NoWrap;
        Driver.Step(2);
        placed.Box.CaretIndex = Content(placed.Box).Length;
        GraphicalUiElement caret = Child(placed, "CaretInstance")!;
        caret.AbsoluteLeft.ShouldBeInRange(boxLeft, boxLeft + placed.Visual.AbsoluteWidth, $"{Describe(placed)}: caret after un-wrapping");
    }

    #endregion
}
