using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Verifies that <see cref="Text"/> satisfies <see cref="IFormsText"/> (issue #3653). Before this
/// change, SkiaGum's Text only implemented <see cref="IText"/>, so
/// <c>TextBoxBase.RefreshInternalVisualReferences</c>'s cast to <see cref="IFormsText"/> failed
/// under <c>FULL_DIAGNOSTICS</c> (defined in SkiaGum's Debug and Release configs), crashing
/// <c>new TextBox()</c>/<c>new PasswordBox()</c> on construction.
/// </summary>
public class TextIFormsTextTests
{
    [Fact]
    public void Text_IsIFormsText()
    {
        Text sut = new();

        (sut is IFormsText).ShouldBeTrue();
    }

    [Fact]
    public void WrappedText_SingleLineFittingWidth_ReturnsOneLineMatchingRawText()
    {
        Text sut = new();
        sut.Width = 1000;
        sut.RawText = "Hello world";

        IFormsText formsText = sut;

        formsText.WrappedText.Count.ShouldBe(1);
        formsText.WrappedText[0].ShouldBe("Hello world");
    }

    [Fact]
    public void WrappedText_WrapsAcrossMultipleLines_WhenWidthIsNarrow()
    {
        Text sut = new();
        sut.FontSize = 20;
        sut.Width = 60;
        sut.RawText = "Hello world this is a long line of text";

        IFormsText formsText = sut;

        formsText.WrappedText.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void WrappedText_LinesConcatenate_ToOriginalRawText()
    {
        Text sut = new();
        sut.FontSize = 20;
        sut.Width = 60;
        string rawText = "Hello world this is a long line of text";
        sut.RawText = rawText;

        IFormsText formsText = sut;

        string reconstructed = string.Concat(formsText.WrappedText);
        reconstructed.ShouldBe(rawText);
    }

    [Fact]
    public void LineHeightInPixels_IsPositive()
    {
        Text sut = new();
        sut.FontSize = 18;

        IWrappedText wrappedText = sut;

        wrappedText.LineHeightInPixels.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void MeasureString_LongerString_ReturnsLargerWidth()
    {
        Text sut = new();
        sut.FontSize = 18;

        IWrappedText wrappedText = sut;

        float shortWidth = wrappedText.MeasureString("Hi");
        float longWidth = wrappedText.MeasureString("Hello, this is a much longer string");

        longWidth.ShouldBeGreaterThan(shortWidth);
    }

    [Fact]
    public void MeasureString_EmptyString_ReturnsZero()
    {
        Text sut = new();

        IWrappedText wrappedText = sut;

        wrappedText.MeasureString("").ShouldBe(0f);
    }

    [Fact]
    public void MaxLettersToShow_RoundTrips()
    {
        Text sut = new();

        IFormsText formsText = sut;
        formsText.MaxLettersToShow = 5;

        formsText.MaxLettersToShow.ShouldBe(5);
    }

    [Fact]
    public void MaxNumberOfLines_SettableThroughIFormsText()
    {
        Text sut = new();

        IFormsText formsText = sut;
        formsText.MaxNumberOfLines = 3;

        formsText.MaxNumberOfLines.ShouldBe(3);
        sut.MaxNumberOfLines.ShouldBe(3);
    }
}
