using Gum.Converters;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Tests.Forms.Linked;

namespace SkiaGum.Tests.Forms;

/// <summary>
/// Caret and selection placement over Skia's Text (#6511). The MonoGame TextBoxCaretPlacementTests read the
/// drawn-line position from the MonoGame core Text (<c>UpdateIpsoForRenderingForTests</c>), which Skia does not
/// have, so this checks the same placement rules relatively: the caret stays inside the text box, follows the
/// line, and lands at the same X wherever the visual parents it.
/// </summary>
public class TextBoxCaretPlacementSkiaTests : SkiaFormsTestBase
{
    static TextBoxBaseVisual VisualOf(TextBox textBox) => (TextBoxBaseVisual)textBox.Visual;

    static TextBox CreateTextBox(VerticalAlignment verticalAlignment, bool multiLine)
    {
        TextBox textBox = new();
        if (multiLine)
        {
            textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
            textBox.AcceptsReturn = true;
            textBox.Height = 200;
        }
        textBox.Width = 300;
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        VisualOf(textBox).TextInstance.VerticalAlignment = verticalAlignment;
        textBox.Text = "abc\ndef";
        textBox.IsFocused = true;
        textBox.CaretIndex = 1;
        return textBox;
    }

    static float CenterY(GraphicalUiElement element) => (element.AbsoluteTop + element.AbsoluteBottom) / 2f;

    static ContainerRuntime MoveToOtherParent(TextBoxBaseVisual visual, GraphicalUiElement instance)
    {
        ContainerRuntime parent = new();
        parent.X = 30;
        parent.Y = 30;
        parent.Width = 100;
        parent.Height = 60;
        visual.ClipContainer.AddChild(parent);
        parent.AddChild(instance);
        return parent;
    }

    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void CaretCenter_ShouldStayInsideTextBox(VerticalAlignment verticalAlignment)
    {
        TextBox textBox = CreateTextBox(verticalAlignment, multiLine: true);
        GraphicalUiElement caret = VisualOf(textBox).CaretInstance;

        CenterY(caret).ShouldBeGreaterThan(textBox.Visual.AbsoluteTop);
        CenterY(caret).ShouldBeLessThan(textBox.Visual.AbsoluteBottom);
    }

    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void CaretCenter_MultiLine_ShouldMoveDownOneLine_OnSecondLine(VerticalAlignment verticalAlignment)
    {
        TextBox textBox = CreateTextBox(verticalAlignment, multiLine: true);
        textBox.CaretIndex = 1;
        float firstLine = CenterY(VisualOf(textBox).CaretInstance);

        textBox.CaretIndex = 5;
        float secondLine = CenterY(VisualOf(textBox).CaretInstance);

        (secondLine - firstLine).ShouldBeGreaterThan(5f);
        (secondLine - firstLine).ShouldBeLessThan(80f);
    }

    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall, false)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, false)]
    [InlineData(GeneralUnitType.PixelsFromLarge, false)]
    [InlineData(GeneralUnitType.Percentage, false)]
    [InlineData(GeneralUnitType.PixelsFromSmall, true)]
    [InlineData(GeneralUnitType.PixelsFromMiddle, true)]
    [InlineData(GeneralUnitType.PixelsFromLarge, true)]
    [InlineData(GeneralUnitType.Percentage, true)]
    public void CaretLeft_MultiLineWithAuthoredXUnitsAndParent_ShouldMatchDefaultPlacement(
        GeneralUnitType xUnits, bool inOtherParent)
    {
        TextBox expected = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        TextBox sut = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        TextBoxBaseVisual visual = VisualOf(sut);
        if (inOtherParent)
        {
            MoveToOtherParent(visual, visual.CaretInstance);
        }
        visual.CaretInstance.XUnits = xUnits;
        sut.CaretIndex = 0;
        sut.CaretIndex = 2;
        expected.CaretIndex = 0;
        expected.CaretIndex = 2;

        visual.CaretInstance.AbsoluteLeft.ShouldBe(VisualOf(expected).CaretInstance.AbsoluteLeft, tolerance: 0.01f);
        visual.CaretInstance.XUnits.ShouldBe(xUnits);
    }

    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall)]
    [InlineData(GeneralUnitType.PixelsFromMiddle)]
    [InlineData(GeneralUnitType.PixelsFromLarge)]
    [InlineData(GeneralUnitType.Percentage)]
    public void CaretLeft_SingleLineScrolledPastEdgeWithAuthoredXUnits_ShouldMatchDefaultPlacement(GeneralUnitType xUnits)
    {
        TextBox expected = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        TextBox sut = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        VisualOf(sut).CaretInstance.XUnits = xUnits;

        foreach (TextBox textBox in new[] { expected, sut })
        {
            textBox.CaretIndex = textBox.Text!.Length;
            for (int i = 0; i < 40; i++)
            {
                textBox.HandleCharEntered('w');
            }
        }

        VisualOf(sut).TextInstance.X.ShouldBeLessThan(0, "typing past the edge should have scrolled the text");
        VisualOf(sut).CaretInstance.AbsoluteLeft.ShouldBe(VisualOf(expected).CaretInstance.AbsoluteLeft, tolerance: 0.01f);
    }

    [Fact]
    public void SelectionRectangles_ShouldBeVisibleOnEachSelectedLine_Multiline()
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);

        textBox.SelectionStart = 1;
        textBox.SelectionLength = 5;

        VisualOf(textBox).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Count(child => child.Name == "SelectionInstance" && child.Visible)
            .ShouldBe(2);
    }

    [Fact]
    public void Caret_ShouldBeVisibleWhenNotFocused_OnlyIfIsCaretVisibleWhenNotFocused()
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        textBox.IsFocused = false;
        VisualOf(textBox).CaretInstance.Visible.ShouldBeFalse();

        textBox.IsCaretVisibleWhenNotFocused = true;

        VisualOf(textBox).CaretInstance.Visible.ShouldBeTrue();
    }

    [Fact]
    public void Caret_ShouldBeHiddenWhenReadOnly_UnlessIsCaretVisibleWhenReadOnly()
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        textBox.IsFocused = false;
        textBox.IsReadOnly = true;
        textBox.IsFocused = true;
        VisualOf(textBox).CaretInstance.Visible.ShouldBeFalse();

        textBox.IsCaretVisibleWhenReadOnly = true;
        textBox.IsFocused = false;
        textBox.IsFocused = true;

        VisualOf(textBox).CaretInstance.Visible.ShouldBeTrue();
    }
}
