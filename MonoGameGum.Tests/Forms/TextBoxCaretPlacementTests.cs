using Gum.Forms.Controls;
using Gum.Converters;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The default TextBox visual's caret is centered on the line the renderer draws (#6624), read from
/// <see cref="Text.UpdateIpsoForRenderingForTests"/>.
/// </summary>
public class TextBoxCaretPlacementTests : BaseTestClass
{
    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void CaretCenter_MultiLine_ShouldMatchDrawnLineCenter(VerticalAlignment verticalAlignment)
    {
        TextBox textBox = CreateTextBox(verticalAlignment, multiLine: true);
        Text contained = GetCoreText(textBox);
        float drawnLineCenter = contained.UpdateIpsoForRenderingForTests().Y + contained.LineHeightInPixels / 2.0f;

        GetCaret(textBox).GetAbsoluteCenterY().ShouldBe(drawnLineCenter, tolerance: 0.01f);
    }

    // Single-line mode leaves the caret where the visual places it; the default visual centers it
    // on the box, which is within half a pixel of the drawn line whatever the alignment.
    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void CaretCenter_SingleLine_ShouldBeWithinHalfPixelOfDrawnLineCenter(VerticalAlignment verticalAlignment)
    {
        TextBox textBox = CreateTextBox(verticalAlignment, multiLine: false);
        Text contained = GetCoreText(textBox);
        float drawnLineCenter = contained.UpdateIpsoForRenderingForTests().Y + contained.LineHeightInPixels / 2.0f;

        GetCaret(textBox).GetAbsoluteCenterY().ShouldBe(drawnLineCenter, tolerance: 0.51f);
    }

    // A caret authored with any vertical unit lands on the drawn line (#6630).
    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall)]
    [InlineData(GeneralUnitType.PixelsFromMiddle)]
    [InlineData(GeneralUnitType.PixelsFromLarge)]
    public void CaretCenter_MultiLineWithAuthoredYUnits_ShouldMatchDrawnLineCenter(GeneralUnitType yUnits)
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        GetCaret(textBox).YUnits = yUnits;
        textBox.CaretIndex = 0;
        textBox.CaretIndex = 1;
        Text contained = GetCoreText(textBox);
        float drawnLineCenter = contained.UpdateIpsoForRenderingForTests().Y + contained.LineHeightInPixels / 2.0f;

        GetCaret(textBox).GetAbsoluteCenterY().ShouldBe(drawnLineCenter, tolerance: 0.01f);
    }

    // A Visual may parent the caret somewhere other than the text's parent (#6630).
    [Fact]
    public void CaretCenter_MultiLineCaretInOtherParent_ShouldMatchDrawnLineCenter()
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        TextBoxBaseVisual visual = (TextBoxBaseVisual)textBox.Visual;
        ContainerRuntime caretParent = new();
        caretParent.Y = 30;
        caretParent.Height = 60;
        caretParent.Width = 100;
        visual.AddChild(caretParent);
        caretParent.AddChild(visual.CaretInstance);
        textBox.CaretIndex = 0;
        textBox.CaretIndex = 1;
        Text contained = GetCoreText(textBox);
        float drawnLineCenter = contained.UpdateIpsoForRenderingForTests().Y + contained.LineHeightInPixels / 2.0f;

        GetCaret(textBox).GetAbsoluteCenterY().ShouldBe(drawnLineCenter, tolerance: 0.01f);
    }

    // Each multi-line selection rectangle is centered on its drawn line, like the caret (#6630).
    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void SelectionCenter_MultiLine_ShouldMatchDrawnLineCenter(VerticalAlignment verticalAlignment)
    {
        TextBox textBox = CreateTextBox(verticalAlignment, multiLine: true);
        textBox.Text = "abc\ndef";
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 5;
        Text contained = GetCoreText(textBox);
        float firstLineCenter = contained.UpdateIpsoForRenderingForTests().Y + contained.LineHeightInPixels / 2.0f;

        GraphicalUiElement[] selections = GetVisibleSelections(textBox);
        selections.Length.ShouldBe(2);
        selections[0].GetAbsoluteCenterY().ShouldBe(firstLineCenter, tolerance: 0.01f);
        selections[1].GetAbsoluteCenterY().ShouldBe(firstLineCenter + contained.LineHeightInPixels, tolerance: 0.01f);
    }

    // Single-line selection fills the box less 2px on each side.
    [Fact]
    public void Selection_SingleLine_ShouldBeInsetTwoPixelsFromBox()
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Center, multiLine: false);
        textBox.Height = 40;
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 2;

        GraphicalUiElement selection = GetVisibleSelections(textBox).Single();
        selection.AbsoluteTop.ShouldBe(textBox.Visual.AbsoluteTop + 2, tolerance: 0.01f);
        selection.AbsoluteHeight.ShouldBe(36, tolerance: 0.01f);
    }

    static GraphicalUiElement[] GetVisibleSelections(TextBox textBox) =>
        ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Where(child => child.Name == "SelectionInstance" && child.Visible)
            .ToArray();

    static TextBox CreateTextBox(VerticalAlignment verticalAlignment, bool multiLine)
    {
        TextBox textBox = new();
        if (multiLine)
        {
            textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
            textBox.AcceptsReturn = true;
            textBox.Height = 200;
        }
        textBox.Width = 300;
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        ((TextBoxBaseVisual)textBox.Visual).TextInstance.VerticalAlignment = verticalAlignment;
        textBox.Text = "abc";
        textBox.IsFocused = true;
        textBox.CaretIndex = 1;
        return textBox;
    }

    static Text GetCoreText(TextBox textBox) =>
        (Text)((TextBoxBaseVisual)textBox.Visual).TextInstance.RenderableComponent!;

    static Gum.GueDeriving.SpriteRuntime GetCaret(TextBox textBox) =>
        ((TextBoxBaseVisual)textBox.Visual).CaretInstance;
}
