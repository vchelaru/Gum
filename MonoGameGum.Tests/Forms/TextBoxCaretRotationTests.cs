using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The caret and selection turn with the text (#6609). The renderer draws text turned by its
/// absolute rotation about the renderable's top-left, so each test turns the caret's or selection's
/// drawn top-left back the same way and compares it with where it sits in unrotated text.
/// </summary>
public class TextBoxCaretRotationTests : BaseTestClass
{
    const float Tolerance = 0.01f;

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(-90)]
    public void Caret_SingleLineTextRotated_ShouldTurnWithText(float rotation)
    {
        TextBox unrotated = CreateTextBox(multiLine: false, textRotation: 0);
        TextBox sut = CreateTextBox(multiLine: false, textRotation: rotation);

        GetCaret(sut).GetAbsoluteRotation().ShouldBe(rotation, Tolerance);
        AssertSameOffset(GetOffsetInTextFrame(sut, GetCaret(sut)), GetOffsetInTextFrame(unrotated, GetCaret(unrotated)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(-90)]
    public void Caret_MultiLineTextRotated_ShouldTurnWithText(float rotation)
    {
        TextBox unrotated = CreateTextBox(multiLine: true, textRotation: 0);
        TextBox sut = CreateTextBox(multiLine: true, textRotation: rotation);

        GetCaret(sut).GetAbsoluteRotation().ShouldBe(rotation, Tolerance);
        AssertSameOffset(GetOffsetInTextFrame(sut, GetCaret(sut)), GetOffsetInTextFrame(unrotated, GetCaret(unrotated)));
    }

    // Rotating the whole TextBox already turned the caret with it before #6609.
    [Fact]
    public void Caret_WholeTextBoxRotated_ShouldTurnWithText()
    {
        TextBox unrotated = CreateTextBox(multiLine: true, textRotation: 0);
        TextBox sut = CreateTextBox(multiLine: true, textRotation: 0);
        sut.Visual.Rotation = 30;
        sut.CaretIndex = 0;
        sut.CaretIndex = "abcdef\nghi".Length;

        GetCaret(sut).GetAbsoluteRotation().ShouldBe(30, Tolerance);
        AssertSameOffset(GetOffsetInTextFrame(sut, GetCaret(sut)), GetOffsetInTextFrame(unrotated, GetCaret(unrotated)));
    }

    // Typing past the right edge scrolls the text and caret together; the caret must stay on the
    // drawn text however many times it is placed again, with no offset piling up.
    [Fact]
    public void Caret_SingleLineTextRotated_ShouldNotDriftAfterScrollingAndRepeatedUpdates()
    {
        TextBox sut = CreateTextBox(multiLine: false, textRotation: 30);
        Text contained = GetCoreText(sut);
        (float _, float initialY) = GetOffsetInTextFrame(sut, GetCaret(sut));

        sut.CaretIndex = sut.Text!.Length;
        for (int i = 0; i < 40; i++)
        {
            sut.HandleCharEntered('w');
        }
        for (int i = 0; i < 5; i++)
        {
            sut.HandleKeyDown(global::Gum.Forms.Input.Keys.Left, false, false, false);
            sut.HandleKeyDown(global::Gum.Forms.Input.Keys.Home, false, false, false);
            sut.HandleKeyDown(global::Gum.Forms.Input.Keys.End, false, false, false);
        }
        sut.HandleKeyDown(global::Gum.Forms.Input.Keys.Left, false, false, false);

        GetVisual(sut).TextInstance.X.ShouldBeLessThan(0, "typing past the edge should have scrolled the text");
        (float x, float y) = GetOffsetInTextFrame(sut, GetCaret(sut));
        x.ShouldBe(contained.MeasureString(sut.Text!.Substring(0, sut.CaretIndex)), Tolerance);
        y.ShouldBe(initialY, Tolerance);
        GetCaret(sut).GetAbsoluteRotation().ShouldBe(30, Tolerance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void Selection_MultiLineTextRotated_ShouldTurnWithText(float rotation)
    {
        TextBox unrotated = CreateTextBox(multiLine: true, textRotation: 0);
        TextBox sut = CreateTextBox(multiLine: true, textRotation: rotation);
        foreach (TextBox textBox in new[] { unrotated, sut })
        {
            textBox.SelectionStart = 2;
            textBox.SelectionLength = "cdef\nghij".Length;
            // A second update must not move the selection.
            textBox.SelectionLength = "cdef\nghij".Length + 1;
            textBox.SelectionLength = "cdef\nghij".Length;
        }

        GraphicalUiElement[] expected = GetVisibleSelections(unrotated);
        GraphicalUiElement[] actual = GetVisibleSelections(sut);
        actual.Length.ShouldBe(2);
        actual.Length.ShouldBe(expected.Length);
        for (int i = 0; i < actual.Length; i++)
        {
            actual[i].GetAbsoluteRotation().ShouldBe(rotation, Tolerance);
            AssertSameOffset(GetOffsetInTextFrame(sut, actual[i]), GetOffsetInTextFrame(unrotated, expected[i]));
        }
    }

    [Fact]
    public void Selection_SingleLineTextRotated_ShouldTurnWithText()
    {
        TextBox unrotated = CreateTextBox(multiLine: false, textRotation: 0);
        TextBox sut = CreateTextBox(multiLine: false, textRotation: 30);
        foreach (TextBox textBox in new[] { unrotated, sut })
        {
            textBox.SelectionStart = 1;
            textBox.SelectionLength = 3;
        }

        GraphicalUiElement selection = GetVisibleSelections(sut)[0];
        selection.GetAbsoluteRotation().ShouldBe(30, Tolerance);
        AssertSameOffset(GetOffsetInTextFrame(sut, selection), GetOffsetInTextFrame(unrotated, GetVisibleSelections(unrotated)[0]));
    }

    static TextBox CreateTextBox(bool multiLine, float textRotation)
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
        GetVisual(textBox).TextInstance.Rotation = textRotation;
        textBox.Text = multiLine ? "abcdef\nghijkl\nmnopqr" : "abcdef";
        textBox.IsFocused = true;
        textBox.CaretIndex = multiLine ? "abcdef\nghi".Length : 3;
        return textBox;
    }

    // The element's drawn top-left turned back by the text's absolute rotation about the text's
    // drawn top-left: where it sits relative to the text's top-left in the text's own frame.
    static (float x, float y) GetOffsetInTextFrame(TextBox textBox, GraphicalUiElement element)
    {
        Text text = GetCoreText(textBox);
        IRenderableIpso renderable = (IRenderableIpso)element.RenderableComponent!;
        double radians = text.GetAbsoluteRotation() * Math.PI / 180;
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        float dx = renderable.GetAbsoluteX() - text.GetAbsoluteX();
        float dy = renderable.GetAbsoluteY() - text.GetAbsoluteY();
        return (dx * cos - dy * sin, dx * sin + dy * cos);
    }

    static void AssertSameOffset((float x, float y) actual, (float x, float y) expected)
    {
        actual.x.ShouldBe(expected.x, Tolerance);
        actual.y.ShouldBe(expected.y, Tolerance);
    }

    static GraphicalUiElement[] GetVisibleSelections(TextBox textBox) =>
        GetVisual(textBox).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Where(child => child.Name == "SelectionInstance" && child.Visible)
            .ToArray();

    static TextBoxBaseVisual GetVisual(TextBox textBox) => (TextBoxBaseVisual)textBox.Visual;

    static Text GetCoreText(TextBox textBox) =>
        (Text)GetVisual(textBox).TextInstance.RenderableComponent!;

    static GraphicalUiElement GetCaret(TextBox textBox) => GetVisual(textBox).CaretInstance;
}
