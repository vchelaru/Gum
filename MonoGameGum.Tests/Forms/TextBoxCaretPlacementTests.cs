using Gum.Forms.Controls;
using Gum.Converters;
using Gum.DataTypes;
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

    // The caret sits at the same screen X wherever the Visual parents it and whatever X units it
    // authors (#6646).
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
        TextBoxBaseVisual visual = (TextBoxBaseVisual)sut.Visual;
        if (inOtherParent)
        {
            MoveToOtherParent(visual, visual.CaretInstance);
        }
        visual.CaretInstance.XUnits = xUnits;
        sut.CaretIndex = 0;
        sut.CaretIndex = 2;
        expected.CaretIndex = 0;
        expected.CaretIndex = 2;

        visual.CaretInstance.AbsoluteLeft.ShouldBe(GetCaret(expected).AbsoluteLeft, tolerance: 0.01f);
        // The units authored on the Visual are kept, not replaced by the control.
        visual.CaretInstance.XUnits.ShouldBe(xUnits);
    }

    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall)]
    [InlineData(GeneralUnitType.PixelsFromMiddle)]
    [InlineData(GeneralUnitType.PixelsFromLarge)]
    [InlineData(GeneralUnitType.Percentage)]
    public void CaretLeft_SingleLineCaretInOtherParent_ShouldMatchDefaultPlacement(GeneralUnitType xUnits)
    {
        TextBox expected = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        TextBox sut = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        TextBoxBaseVisual visual = (TextBoxBaseVisual)sut.Visual;
        MoveToOtherParent(visual, visual.CaretInstance);
        visual.CaretInstance.XUnits = xUnits;
        sut.CaretIndex = 0;
        sut.CaretIndex = 2;
        expected.CaretIndex = 0;
        expected.CaretIndex = 2;

        visual.CaretInstance.AbsoluteLeft.ShouldBe(GetCaret(expected).AbsoluteLeft, tolerance: 0.01f);
    }

    // Scrolling the text to keep the caret in view moves the caret by pixels, in whatever units it uses.
    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall)]
    [InlineData(GeneralUnitType.PixelsFromMiddle)]
    [InlineData(GeneralUnitType.PixelsFromLarge)]
    [InlineData(GeneralUnitType.Percentage)]
    public void CaretLeft_SingleLineScrolledPastEdgeWithAuthoredXUnits_ShouldMatchDefaultPlacement(GeneralUnitType xUnits)
    {
        TextBox expected = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        TextBox sut = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        ((TextBoxBaseVisual)sut.Visual).CaretInstance.XUnits = xUnits;

        foreach (TextBox textBox in new[] { expected, sut })
        {
            textBox.CaretIndex = textBox.Text!.Length;
            for (int i = 0; i < 40; i++)
            {
                textBox.HandleCharEntered('w');
            }
        }

        ((TextBoxBaseVisual)sut.Visual).TextInstance.X.ShouldBeLessThan(0, "typing past the edge should have scrolled the text");
        GetCaret(sut).AbsoluteLeft.ShouldBe(GetCaret(expected).AbsoluteLeft, tolerance: 0.01f);
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
    public void SelectionLeft_MultiLineWithAuthoredXUnitsAndParent_ShouldMatchDefaultPlacement(
        GeneralUnitType xUnits, bool inOtherParent)
    {
        TextBox expected = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        TextBox sut = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        TextBoxBaseVisual visual = (TextBoxBaseVisual)sut.Visual;
        if (inOtherParent)
        {
            MoveToOtherParent(visual, visual.SelectionInstance);
        }
        visual.SelectionInstance.XUnits = xUnits;
        foreach (TextBox textBox in new[] { expected, sut })
        {
            textBox.Text = "abc\ndef";
            textBox.SelectionStart = 1;
            textBox.SelectionLength = 5;
        }

        GraphicalUiElement[] expectedSelections = GetVisibleSelections(expected);
        GraphicalUiElement[] actual = GetVisibleSelections(sut, inOtherParent);
        actual.Length.ShouldBe(2);
        for (int i = 0; i < actual.Length; i++)
        {
            actual[i].AbsoluteLeft.ShouldBe(expectedSelections[i].AbsoluteLeft, tolerance: 0.01f);
        }
    }

    [Theory]
    [InlineData(GeneralUnitType.PixelsFromSmall)]
    [InlineData(GeneralUnitType.Percentage)]
    public void SelectionLeft_SingleLineSelectionInOtherParent_ShouldMatchDefaultPlacement(GeneralUnitType xUnits)
    {
        TextBox expected = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        TextBox sut = CreateTextBox(VerticalAlignment.Top, multiLine: false);
        TextBoxBaseVisual visual = (TextBoxBaseVisual)sut.Visual;
        MoveToOtherParent(visual, visual.SelectionInstance);
        visual.SelectionInstance.XUnits = xUnits;
        foreach (TextBox textBox in new[] { expected, sut })
        {
            textBox.SelectionStart = 1;
            textBox.SelectionLength = 2;
        }

        GetVisibleSelections(sut, inOtherParent: true).Single().AbsoluteLeft
            .ShouldBe(GetVisibleSelections(expected).Single().AbsoluteLeft, tolerance: 0.01f);
    }

    // A Forms control must never throw during layout, and a unit the converter cannot convert
    // still places the caret and selection correctly (#6646).
    [Theory]
    [InlineData(GeneralUnitType.PercentageOfFile)]
    [InlineData(GeneralUnitType.PercentageOfOtherDimension)]
    [InlineData(GeneralUnitType.PixelsFromBaseline)]
    [InlineData(GeneralUnitType.MaintainFileAspectRatio)]
    public void CaretAndSelection_UnsupportedXUnitsInOtherParent_ShouldNotThrowAndStillMatchDefaultPlacement(
        GeneralUnitType xUnits)
    {
        TextBox expected = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        TextBox sut = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        TextBoxBaseVisual visual = (TextBoxBaseVisual)sut.Visual;
        MoveToOtherParent(visual, visual.CaretInstance);
        MoveToOtherParent(visual, visual.SelectionInstance);
        visual.CaretInstance.XUnits = xUnits;
        visual.SelectionInstance.XUnits = xUnits;

        Should.NotThrow(() =>
        {
            foreach (TextBox textBox in new[] { expected, sut })
            {
                textBox.Text = "abc\ndef";
                textBox.CaretIndex = 0;
                textBox.CaretIndex = 2;
                textBox.SelectionStart = 1;
                textBox.SelectionLength = 5;
            }
        });

        float.IsFinite(visual.CaretInstance.X).ShouldBeTrue();
        visual.CaretInstance.AbsoluteLeft.ShouldBe(GetCaret(expected).AbsoluteLeft, tolerance: 0.01f);
        GraphicalUiElement[] actual = GetVisibleSelections(sut, inOtherParent: true);
        actual.ShouldAllBe(selection => float.IsFinite(selection.X));
        actual[0].AbsoluteLeft.ShouldBe(GetVisibleSelections(expected)[0].AbsoluteLeft, tolerance: 0.01f);
    }

    // A parent with no width gives a Percentage X nothing to be a percentage of; X must stay finite.
    [Fact]
    public void CaretAndSelection_PercentageXInZeroWidthParent_ShouldNotThrowAndKeepFiniteX()
    {
        TextBox sut = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        TextBoxBaseVisual visual = (TextBoxBaseVisual)sut.Visual;
        MoveToOtherParent(visual, visual.CaretInstance, parentWidth: 0);
        MoveToOtherParent(visual, visual.SelectionInstance, parentWidth: 0);
        visual.CaretInstance.XUnits = GeneralUnitType.Percentage;
        visual.SelectionInstance.XUnits = GeneralUnitType.Percentage;

        Should.NotThrow(() =>
        {
            sut.Text = "abc\ndef";
            sut.CaretIndex = 0;
            sut.CaretIndex = 2;
            sut.SelectionStart = 1;
            sut.SelectionLength = 5;
        });
        float.IsFinite(visual.CaretInstance.X).ShouldBeTrue();
        GetVisibleSelections(sut, inOtherParent: true).ShouldAllBe(selection => float.IsFinite(selection.X));
    }

    // The caret sits on its line whichever edge of itself YOrigin positions (#6648). The caret is as
    // tall as the line, so its center is the line's center for every origin. TextBaseline on a
    // non-text caret is its bottom edge (as in GraphicalUiElement), so it behaves like Bottom (#6660).
    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    [InlineData(VerticalAlignment.TextBaseline)]
    public void CaretCenter_MultiLineWithAuthoredYOrigin_ShouldMatchDrawnLineCenter(VerticalAlignment yOrigin)
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        Text contained = GetCoreText(textBox);
        GraphicalUiElement caret = GetCaret(textBox);
        caret.HeightUnits = DimensionUnitType.Absolute;
        caret.Height = contained.LineHeightInPixels;
        caret.YOrigin = yOrigin;
        textBox.CaretIndex = 0;
        textBox.CaretIndex = 1;
        float drawnLineCenter = contained.UpdateIpsoForRenderingForTests().Y + contained.LineHeightInPixels / 2.0f;

        caret.GetAbsoluteCenterY().ShouldBe(drawnLineCenter, tolerance: 0.01f);
    }

    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    [InlineData(VerticalAlignment.TextBaseline)]
    public void SelectionCenter_MultiLineWithAuthoredYOrigin_ShouldMatchDrawnLineCenter(VerticalAlignment yOrigin)
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        Text contained = GetCoreText(textBox);
        GraphicalUiElement selection = ((TextBoxBaseVisual)textBox.Visual).SelectionInstance;
        selection.HeightUnits = DimensionUnitType.Absolute;
        selection.Height = contained.LineHeightInPixels;
        selection.YOrigin = yOrigin;
        textBox.Text = "abc\ndef";
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 2;
        float drawnLineCenter = contained.UpdateIpsoForRenderingForTests().Y + contained.LineHeightInPixels / 2.0f;

        GetVisibleSelections(textBox).Single().GetAbsoluteCenterY().ShouldBe(drawnLineCenter, tolerance: 0.01f);
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

    // A Forms control must never throw during layout, so a vertical unit the converter cannot
    // convert falls back to the raw value (#6630).
    [Theory]
    [InlineData(GeneralUnitType.PercentageOfFile)]
    [InlineData(GeneralUnitType.PercentageOfOtherDimension)]
    [InlineData(GeneralUnitType.PixelsFromBaseline)]
    [InlineData(GeneralUnitType.MaintainFileAspectRatio)]
    public void Caret_UnsupportedYUnits_ShouldNotThrowAndKeepFiniteY(GeneralUnitType yUnits)
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        GraphicalUiElement caret = GetCaret(textBox);
        caret.YUnits = yUnits;

        Should.NotThrow(() =>
        {
            textBox.CaretIndex = 0;
            textBox.CaretIndex = 1;
        });
        float.IsFinite(caret.Y).ShouldBeTrue();
    }

    [Theory]
    [InlineData(GeneralUnitType.PercentageOfFile)]
    [InlineData(GeneralUnitType.PercentageOfOtherDimension)]
    [InlineData(GeneralUnitType.PixelsFromBaseline)]
    [InlineData(GeneralUnitType.MaintainFileAspectRatio)]
    public void Selection_UnsupportedYUnits_ShouldNotThrowAndKeepFiniteY(GeneralUnitType yUnits)
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        textBox.Text = "abc\ndef";
        ((TextBoxBaseVisual)textBox.Visual).SelectionInstance.YUnits = yUnits;

        Should.NotThrow(() =>
        {
            textBox.SelectionStart = 1;
            textBox.SelectionLength = 5;
        });
        GetVisibleSelections(textBox).ShouldAllBe(selection => float.IsFinite(selection.Y));
    }

    // A rotated text turns the caret through the same conversion, so it must not throw either.
    [Theory]
    [InlineData(GeneralUnitType.PercentageOfFile)]
    [InlineData(GeneralUnitType.PercentageOfOtherDimension)]
    public void Caret_UnsupportedYUnitsWithRotatedText_ShouldNotThrow(GeneralUnitType yUnits)
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        GetCaret(textBox).YUnits = yUnits;

        Should.NotThrow(() =>
        {
            ((TextBoxBaseVisual)textBox.Visual).TextInstance.Rotation = 30;
            textBox.CaretIndex = 0;
            textBox.CaretIndex = 1;
        });
        float.IsFinite(GetCaret(textBox).Y).ShouldBeTrue();
    }

    // A parent with no height gives a Percentage Y nothing to be a percentage of; Y must stay finite.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Caret_PercentageYInZeroHeightParent_ShouldNotThrowAndKeepFiniteY(bool rotatedText)
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        TextBoxBaseVisual visual = (TextBoxBaseVisual)textBox.Visual;
        ContainerRuntime zeroHeightParent = new();
        zeroHeightParent.Height = 0;
        zeroHeightParent.Width = 100;
        visual.AddChild(zeroHeightParent);
        zeroHeightParent.AddChild(visual.CaretInstance);
        visual.CaretInstance.YUnits = GeneralUnitType.Percentage;

        Should.NotThrow(() =>
        {
            if (rotatedText)
            {
                visual.TextInstance.Rotation = 30;
            }
            textBox.CaretIndex = 0;
            textBox.CaretIndex = 1;
        });
        float.IsFinite(visual.CaretInstance.Y).ShouldBeTrue();
    }

    [Fact]
    public void Selection_PercentageYInZeroHeightParent_ShouldNotThrowAndKeepFiniteY()
    {
        TextBox textBox = CreateTextBox(VerticalAlignment.Top, multiLine: true);
        textBox.Text = "abc\ndef";
        TextBoxBaseVisual visual = (TextBoxBaseVisual)textBox.Visual;
        ContainerRuntime zeroHeightParent = new();
        zeroHeightParent.Height = 0;
        zeroHeightParent.Width = 100;
        visual.ClipContainer.AddChild(zeroHeightParent);
        zeroHeightParent.AddChild(visual.SelectionInstance);
        visual.SelectionInstance.YUnits = GeneralUnitType.Percentage;

        Should.NotThrow(() =>
        {
            textBox.SelectionStart = 1;
            textBox.SelectionLength = 5;
        });
        float.IsFinite(visual.SelectionInstance.Y).ShouldBeTrue();
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

    // Moves an instance into a container, offset from the text's parent, inside the visual.
    static ContainerRuntime MoveToOtherParent(TextBoxBaseVisual visual, GraphicalUiElement instance,
        float parentWidth = 100)
    {
        ContainerRuntime parent = new();
        parent.X = 30;
        parent.Y = 30;
        parent.Width = parentWidth;
        parent.Height = 60;
        visual.ClipContainer.AddChild(parent);
        parent.AddChild(instance);
        return parent;
    }

    static GraphicalUiElement[] GetVisibleSelections(TextBox textBox, bool inOtherParent) =>
        ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .SelectMany(child => inOtherParent ? child.Children.OfType<GraphicalUiElement>() : new[] { child })
            .Where(child => child.Name == "SelectionInstance" && child.Visible)
            .ToArray();

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
