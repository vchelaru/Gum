using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Raylib_cs;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Tests.Runtimes;

public class TextRuntimeTests : BaseTestClass
{
    public TextRuntimeTests()
    {
        // not sure if this can run on github actions:
        if (!Raylib.IsWindowReady())
        {
            Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);
            Raylib.InitWindow(800, 600, "Test Window");
        }
    }

    #region AbsoluteWidth

    [Fact]
    public void AbsoluteWidth_ShouldBeChangedByText_IfRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        sut.Width = 0;
        sut.Text = "Short";
        float shortWidth = sut.GetAbsoluteWidth();

        sut.Text = "This is much longer";
        float longWidth = sut.GetAbsoluteWidth();

        longWidth.ShouldBeGreaterThan(shortWidth);
    }

    [Fact]
    public void AbsoluteWidth_ShouldNotIncludeNewlines()
    {
        TextRuntime textRuntime = new();

        textRuntime.Text = "Hello";

        var widthBefore = textRuntime.GetAbsoluteWidth();

        textRuntime.Text = "Hello\na";

        var widthAfter = textRuntime.GetAbsoluteWidth();

        widthBefore.ShouldBe(widthAfter, "Because a trailing newline should not affect the width of a text");
    }

    #endregion

    #region AssignFontInConstructor

    [Fact]
    public void AssignFontInConstructor_WhenFalse_ShouldNotSetFont()
    {
        var saved = TextRuntime.AssignFontInConstructor;
        try
        {
            TextRuntime.AssignFontInConstructor = false;
            TextRuntime sut = new();
            sut.FontFamily.ShouldBeNullOrEmpty();
        }
        finally
        {
            TextRuntime.AssignFontInConstructor = saved;
        }
    }

    [Fact]
    public void AssignFontInConstructor_WhenTrue_ShouldSetDefaultFont()
    {
        var saved = TextRuntime.AssignFontInConstructor;
        try
        {
            TextRuntime.AssignFontInConstructor = true;
            TextRuntime sut = new();
            sut.FontFamily.ShouldBe(TextRuntime.DefaultFont);
        }
        finally
        {
            TextRuntime.AssignFontInConstructor = saved;
        }
    }

    #endregion

    #region CustomFontFile

    [Fact]
    public void CustomFontFile_ShouldBeNullByDefault()
    {
        TextRuntime sut = new();
        sut.CustomFontFile.ShouldBeNull();
    }

    #endregion

    #region Defaults

    [Fact]
    public void DefaultFont_ShouldBeArial()
    {
        TextRuntime.DefaultFont.ShouldBe("Arial");
    }

    [Fact]
    public void DefaultFontSize_ShouldBe18()
    {
        TextRuntime.DefaultFontSize.ShouldBe(18);
    }

    #endregion

    #region FontFamily

    [Fact]
    public void Font_ShouldDelegateToFontFamily()
    {
        TextRuntime sut = new();
        sut.FontFamily = "Comic Sans MS";
        sut.Font.ShouldBe("Comic Sans MS");
    }

    [Fact]
    public void FontFamily_ShouldSetAndGetFont()
    {
        TextRuntime sut = new();
        sut.FontFamily = "Impact";
        sut.FontFamily.ShouldBe("Impact");
    }

    #endregion

    #region FontSize

    [Fact]
    public void FontSize_ShouldDefaultTo18()
    {
        TextRuntime sut = new();
        sut.FontSize.ShouldBe(18);
    }

    [Fact]
    public void FontSize_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.FontSize = 24;
        sut.FontSize.ShouldBe(24);
    }

    #endregion

    #region HeightUnits

    [Fact]
    public void HeightUnits_ShouldDefaultToRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.HeightUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
    }

    #endregion

    #region IsBold

    [Fact]
    public void IsBold_ShouldDefaultToFalse()
    {
        TextRuntime sut = new();
        sut.IsBold.ShouldBeFalse();
    }

    [Fact]
    public void IsBold_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.IsBold = true;
        sut.IsBold.ShouldBeTrue();
    }

    #endregion

    #region IsItalic

    [Fact]
    public void IsItalic_ShouldDefaultToFalse()
    {
        TextRuntime sut = new();
        sut.IsItalic.ShouldBeFalse();
    }

    [Fact]
    public void IsItalic_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.IsItalic = true;
        sut.IsItalic.ShouldBeTrue();
    }

    #endregion

    #region MaxNumberOfLines

    [Fact]
    public void MaxNumberOfLines_ShouldBeNullByDefault()
    {
        TextRuntime sut = new();
        sut.MaxNumberOfLines.ShouldBeNull();
    }

    [Fact]
    public void MaxNumberOfLines_WhenSetToOne_ShouldLimitWrappedTextToOneLine()
    {
        TextRuntime sut = new();
        sut.Text = "Line1\nLine2\nLine3";
        sut.MaxNumberOfLines = 1;
        sut.WrappedText.Count.ShouldBe(1);
    }

    #endregion

    #region OutlineThickness

    [Fact]
    public void OutlineThickness_ShouldDefaultToZero()
    {
        TextRuntime sut = new();
        sut.OutlineThickness.ShouldBe(0);
    }

    [Fact]
    public void OutlineThickness_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.OutlineThickness = 2;
        sut.OutlineThickness.ShouldBe(2);
    }

    #endregion

    #region TextOverflowHorizontalMode

    [Fact]
    public void TextOverflowHorizontalMode_Default_ShouldBeTruncateWord()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    [Fact]
    public void TextOverflowHorizontalMode_WhenSetToEllipsis_ShouldReadBackAsEllipsis()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.EllipsisLetter);
    }

    [Fact]
    public void TextOverflowHorizontalMode_WhenSetBackToTruncate_ShouldReadBackAsTruncate()
    {
        TextRuntime sut = new();
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;
        sut.TextOverflowHorizontalMode = TextOverflowHorizontalMode.TruncateWord;
        sut.TextOverflowHorizontalMode.ShouldBe(TextOverflowHorizontalMode.TruncateWord);
    }

    #endregion

    #region WidthUnits

    [Fact]
    public void WidthUnits_ShouldDefaultToRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
    }

    #endregion

    #region SetTextNoTranslate

    [Fact]
    public void SetTextNoTranslate_ShouldUpdateTextProperty()
    {
        TextRuntime sut = new();
        sut.SetTextNoTranslate("Translated Text");
        sut.Text.ShouldBe("Translated Text");
    }

    [Fact]
    public void SetTextNoTranslate_WhenNull_ShouldSetTextToNull()
    {
        TextRuntime sut = new();
        sut.Text = "Some text";
        sut.SetTextNoTranslate(null);
        sut.Text.ShouldBeNull();
    }

    #endregion

    #region UseFontSmoothing

    [Fact]
    public void UseFontSmoothing_ShouldDefaultToTrue()
    {
        TextRuntime sut = new();
        sut.UseFontSmoothing.ShouldBeTrue();
    }

    [Fact]
    public void UseFontSmoothing_ShouldRoundTrip()
    {
        TextRuntime sut = new();
        sut.UseFontSmoothing = false;
        sut.UseFontSmoothing.ShouldBeFalse();
    }

    #endregion

    #region WrappedText

    [Fact]
    public void WrappedText_ShouldContainTextLines_AfterTextAssignment()
    {
        TextRuntime sut = new();
        sut.Text = "Line1\nLine2";
        sut.WrappedText.ShouldNotBeEmpty();
    }

    #endregion
}
