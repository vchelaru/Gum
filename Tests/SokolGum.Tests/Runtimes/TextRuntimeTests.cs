using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SokolGum;
using SokolGum.Tests.Support;

namespace SokolGum.Tests.Runtimes;

public class TextRuntimeTests : BaseTestClass
{
    [Fact]
    public void ContainedRenderable_ShouldBeText()
    {
        var sut = new TextRuntime();
        sut.RenderableComponent.ShouldBeOfType<Text>();
    }

    [Fact]
    public void RawText_ShouldDefaultToHelloWorld()
    {
        // Matches the shared TextRuntime default — constructor pre-seeds
        // the contained Text with "Hello World" so visual consumers don't
        // render an empty box before the first RawText assignment.
        var sut = new TextRuntime();
        sut.RawText.ShouldBe("Hello World");
    }

    [Fact]
    public void RawText_ShouldRoundTrip()
    {
        var sut = new TextRuntime { RawText = "hello" };
        sut.RawText.ShouldBe("hello");
    }

    [Fact]
    public void FontSize_ShouldDefaultToDefaultFontSize()
    {
        // AssignFontInConstructor = true, so the ctor stamps
        // DefaultFontSize (18) onto every new TextRuntime.
        var sut = new TextRuntime();
        sut.FontSize.ShouldBe(TextRuntime.DefaultFontSize);
    }

    [Fact]
    public void FontSize_ShouldRoundTrip()
    {
        var sut = new TextRuntime { FontSize = 24 };
        sut.FontSize.ShouldBe(24);
    }

    [Fact]
    public void Font_ShouldDefaultToDefaultFontFamily()
    {
        // With AssignFontInConstructor = true, Font is set to DefaultFont
        // ("Arial"). Actual fontstash resolution only happens when a
        // SokolGum.SystemManagers is active; unit tests exercise the
        // property shape rather than render output.
        var sut = new TextRuntime();
        sut.Font.ShouldBe(TextRuntime.DefaultFont);
    }

    [Fact]
    public void HorizontalAlignment_ShouldDefaultToLeft()
    {
        var sut = new TextRuntime();
        sut.HorizontalAlignment.ShouldBe(HorizontalAlignment.Left);
    }

    [Fact]
    public void HorizontalAlignment_ShouldRoundTrip()
    {
        var sut = new TextRuntime { HorizontalAlignment = HorizontalAlignment.Center };
        sut.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
    }

    [Fact]
    public void VerticalAlignment_ShouldDefaultToTop()
    {
        var sut = new TextRuntime();
        sut.VerticalAlignment.ShouldBe(VerticalAlignment.Top);
    }

    [Fact]
    public void VerticalAlignment_ShouldRoundTrip()
    {
        var sut = new TextRuntime { VerticalAlignment = VerticalAlignment.Bottom };
        sut.VerticalAlignment.ShouldBe(VerticalAlignment.Bottom);
    }

    [Fact]
    public void TextOverflowHorizontalMode_ShouldDefaultToTruncateWord()
    {
        var sut = new TextRuntime();
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    [Fact]
    public void TextOverflowHorizontalMode_ShouldRoundTrip()
    {
        var sut = new TextRuntime { TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter };
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.EllipsisLetter);
    }

    [Fact]
    public void TextOverflowVerticalMode_ShouldDefaultToSpillOver()
    {
        var sut = new TextRuntime();
        sut.TextOverflowVerticalMode.ShouldBe(TextOverflowVerticalMode.SpillOver);
    }

    [Fact]
    public void OutlineThickness_ShouldDefaultToZero()
    {
        var sut = new TextRuntime();
        sut.OutlineThickness.ShouldBe(0);
    }

    [Fact]
    public void OutlineThickness_ShouldRoundTrip()
    {
        var sut = new TextRuntime { OutlineThickness = 2 };
        sut.OutlineThickness.ShouldBe(2);
    }

    [Fact]
    public void WrapTextInsideBlock_ShouldDefaultToTrue()
    {
        var sut = new TextRuntime();
        sut.WrapTextInsideBlock.ShouldBeTrue();
    }

    [Fact]
    public void LineHeightMultiplier_ShouldDefaultToOne()
    {
        var sut = new TextRuntime();
        sut.LineHeightMultiplier.ShouldBe(1f);
    }

    [Fact]
    public void LineHeightMultiplier_ShouldRoundTrip()
    {
        var sut = new TextRuntime { LineHeightMultiplier = 1.5f };
        sut.LineHeightMultiplier.ShouldBe(1.5f);
    }

    [Fact]
    public void MaxLettersToShow_ShouldBeNullByDefault()
    {
        var sut = new TextRuntime();
        sut.MaxLettersToShow.ShouldBeNull();
    }

    [Fact]
    public void MaxLettersToShow_ShouldRoundTrip()
    {
        var sut = new TextRuntime { MaxLettersToShow = 10 };
        sut.MaxLettersToShow.ShouldBe(10);
    }

    [Fact]
    public void Text_ShouldImplementIText()
    {
        // Layout engine (GraphicalUiElement.cs:2342) checks `is IText` to
        // decide whether to read WrappedTextHeight for RelativeToChildren.
        // Without this, Text renderables report 0 height in layout.
        Text sut = new Text();
        sut.ShouldBeAssignableTo<IText>();
    }

    [Fact]
    public void WrappedTextHeight_UsesMeasurerLineHeight_SingleLine()
    {
        ITextMeasurer? previousMeasurer = Text.DefaultMeasurer;
        try
        {
            Text.DefaultMeasurer = new FakeTextMeasurer { LineHeight = 24f };
            Text sut = new Text { RawText = "Hello" };
            ((IText)sut).WrappedTextHeight.ShouldBe(24f);
        }
        finally { Text.DefaultMeasurer = previousMeasurer; }
    }

    [Fact]
    public void WrappedTextHeight_ScalesWithLineCount()
    {
        ITextMeasurer? previousMeasurer = Text.DefaultMeasurer;
        try
        {
            Text.DefaultMeasurer = new FakeTextMeasurer { LineHeight = 24f };
            Text sut = new Text { RawText = "Line1\nLine2\nLine3" };
            ((IText)sut).WrappedTextHeight.ShouldBe(72f);
        }
        finally { Text.DefaultMeasurer = previousMeasurer; }
    }

    [Fact]
    public void TextRuntime_WithRelativeToChildrenHeight_UsesMeasuredHeight()
    {
        ITextMeasurer? previousMeasurer = Text.DefaultMeasurer;
        try
        {
            Text.DefaultMeasurer = new FakeTextMeasurer { LineHeight = 24f };
            TextRuntime sut = new TextRuntime
            {
                Width = 200,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.RelativeToChildren,
                RawText = "Hello",
            };
            sut.UpdateLayout();
            sut.GetAbsoluteHeight().ShouldBe(24f);
        }
        finally { Text.DefaultMeasurer = previousMeasurer; }
    }
}
