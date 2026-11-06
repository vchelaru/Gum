using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Shouldly;
using System.Linq;
using Xunit;
using RenderingLibrary.Graphics;
using Text = RenderingLibrary.Graphics.Text;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

public class TextBoxSelectionHighlightGeometryTests : BeginnerDriverTestBase
{
    TextBox CreateBox(string text, bool multiLine = false, float height = 200)
    {
        TextBox textBox = new();
        textBox.Width = multiLine ? 300 : 120;
        if (multiLine)
        {
            textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
            textBox.AcceptsReturn = true;
            textBox.Height = height;
        }
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        AddToRoot(textBox, "Box");
        textBox.Text = text;
        textBox.IsFocused = true;
        Driver.Step(2);
        return textBox;
    }

    static TextBoxBaseVisual V(TextBox t) => (TextBoxBaseVisual)t.Visual;
    static Text Core(TextBox t) => (Text)V(t).TextInstance.RenderableComponent!;

    static System.Collections.Generic.List<GraphicalUiElement> Highlights(TextBox t) =>
        V(t).ClipContainer.Children.OfType<GraphicalUiElement>()
            .Where(c => c.Name == "SelectionInstance" && c.Visible).ToList();

    [Fact]
    public void MultiLineSelection_WithLargerFont_HighlightHeightIsOneLine()
    {
        TextBox textBox = CreateBox("one\ntwo\nthree", multiLine: true);
        V(textBox).TextInstance.FontScale = 2;
        Driver.Step(2);
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 5;
        Driver.Step(1);
        Text core = Core(textBox);
        float pitch = core.WrappedTextHeight / core.WrappedText.Count;
        Highlights(textBox).Count.ShouldBe(2);
        foreach (GraphicalUiElement h in Highlights(textBox))
        {
            h.GetAbsoluteHeight().ShouldBe(pitch, tolerance: 1f);
        }
    }

    [Fact]
    public void SingleLineSelection_WidthIsExactlyTheSelectedGlyphs()
    {
        TextBox textBox = CreateBox("hello world");
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 5;
        Driver.Step(1);
        float expected = Core(textBox).MeasureDrawnString("hello", HorizontalMeasurementStyle.Full);
        Highlights(textBox).Single().GetAbsoluteWidth().ShouldBe(expected, tolerance: 0.01f);
    }

    [Fact]
    public void SingleLine_ShiftEndOnLongText_HighlightFollowsTheScrolledText()
    {
        string text = new string('W', 5) + " 0123456789 0123456789 0123456789 0123456789";
        TextBox textBox = CreateBox(text);
        textBox.CaretIndex = 0;
        textBox.HandleKeyDown(GumKeys.End, true, false, false);
        Driver.Step(1);
        GraphicalUiElement h = Highlights(textBox).Single();
        float textLeft = V(textBox).TextInstance.AbsoluteLeft;
        h.AbsoluteLeft.ShouldBe(textLeft, tolerance: 1.5f);
        (h.AbsoluteLeft + h.GetAbsoluteWidth()).ShouldBe(V(textBox).CaretInstance.AbsoluteLeft, tolerance: 2f);
    }

    [Fact]
    public void MultiLine_CaretAtEndOfTallText_LastLineIsFullyInsideTheBox()
    {
        string text = string.Join("\n", Enumerable.Range(1, 12).Select(i => "line" + i));
        TextBox textBox = CreateBox(text, multiLine: true, height: 60);
        textBox.CaretIndex = text.Length;
        Driver.Step(2);
        Text core = Core(textBox);
        float textBottom = V(textBox).TextInstance.AbsoluteTop + core.WrappedTextHeight;
        float clipBottom = V(textBox).ClipContainer.AbsoluteTop + V(textBox).ClipContainer.GetAbsoluteHeight();
        textBottom.ShouldBeLessThanOrEqualTo(clipBottom + 0.5f);
    }

    [Fact]
    public void ShiftArrowAfterDrag_MovesTheDraggedEnd()
    {
        TextBox textBox = CreateBox("hello world again");
        int XOf(int i) { int s = textBox.CaretIndex; textBox.CaretIndex = i; int x = (int)V(textBox).CaretInstance.AbsoluteLeft; textBox.CaretIndex = s; return x; }
        int y = (int)(textBox.Visual.AbsoluteTop + textBox.Visual.AbsoluteHeight / 2);
        Driver.MoveTo(XOf(2) + 1, y);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: true);
        Driver.MoveTo(XOf(4) + 1, y);
        Driver.MoveTo(XOf(6) + 1, y);
        Driver.MoveTo(XOf(6) + 1, y);
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: false);
        textBox.SelectionStart.ShouldBe(2);
        textBox.SelectionLength.ShouldBe(4);
        textBox.HandleKeyDown(GumKeys.Right, true, false, false);
        textBox.SelectionStart.ShouldBe(2);
        textBox.SelectionLength.ShouldBe(5);
        textBox.HandleKeyDown(GumKeys.Left, true, false, false);
        textBox.HandleKeyDown(GumKeys.Left, true, false, false);
        textBox.SelectionStart.ShouldBe(2);
        textBox.SelectionLength.ShouldBe(3);
    }

    [Fact]
    public void Home_WhenCaretAlreadyAtStartWithSelection_ClearsTheSelection()
    {
        TextBox textBox = CreateBox("abc");
        textBox.CaretIndex = 3;
        textBox.HandleKeyDown(GumKeys.Home, true, false, false);
        textBox.SelectionLength.ShouldBe(3);
        textBox.HandleKeyDown(GumKeys.Home, false, false, false);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void End_WhenCaretAlreadyAtEndWithSelection_ClearsTheSelection()
    {
        TextBox textBox = CreateBox("abc");
        textBox.CaretIndex = 0;
        textBox.HandleKeyDown(GumKeys.End, true, false, false);
        textBox.SelectionLength.ShouldBe(3);
        textBox.HandleKeyDown(GumKeys.End, false, false, false);
        textBox.SelectionLength.ShouldBe(0);
    }
}
