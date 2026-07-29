using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;

namespace MonoGameGum.Tests.V2.Runtimes;

public class TextRuntimeTests : BaseTestClass
{
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

    #region HeightUnits

    [Fact]
    public void HeightUnits_ShouldDefaultToRelativeToChildren()
    {
        TextRuntime sut = new();
        sut.HeightUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.RelativeToChildren);
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

    #region Text

    [Fact]
    public void Text_ShouldUpdateWrappedText_AfterAssignment()
    {
        TextRuntime sut = new();
        sut.Text = "Line1\nLine2";
        sut.WrappedText.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("TextNoTranslate")]
    public void Text_ViaDirectPropertySetVersusSetProperty_WithMaxWidth_ShouldWrapIdentically(string propertyName)
    {
        const string longText = "This is a long piece of text that should wrap at the max width";

        AssertWrapsIdentically(propertyName, longText, expectMultipleLines: true, sut =>
        {
            sut.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            sut.MaxWidth = 40;
        });
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("TextNoTranslate")]
    public void Text_ViaDirectPropertySetVersusSetProperty_WithFixedWidthAndHeightRelativeToChildren_ShouldWrapIdentically(string propertyName)
    {
        const string longText = "This is a long piece of text that should wrap at the fixed width";

        AssertWrapsIdentically(propertyName, longText, expectMultipleLines: true, sut =>
        {
            sut.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            sut.Width = 100;
            sut.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        });
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("TextNoTranslate")]
    public void Text_ViaDirectPropertySetVersusSetProperty_WithBbCodeAndMaxWidth_ShouldWrapIdentically(string propertyName)
    {
        const string longBbCodeText = "plain [IsBold=true]bold text that should wrap at the given max width value[/IsBold] plain";

        AssertWrapsIdentically(propertyName, longBbCodeText, expectMultipleLines: true, sut =>
        {
            sut.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            sut.MaxWidth = 40;
        });
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("TextNoTranslate")]
    public void Text_ViaDirectPropertySetVersusSetProperty_WithNullValue_ShouldBehaveIdentically(string propertyName)
    {
        AssertWrapsIdentically(propertyName, null, expectMultipleLines: false, sut =>
        {
            sut.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            sut.MaxWidth = 40;
        });
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("TextNoTranslate")]
    public void Text_ViaDirectPropertySetVersusSetProperty_InStack_ShouldPositionSiblingIdentically(string propertyName)
    {
        float ySettingViaDirect = BuildStackAndGetSecondChildY(
            (first, value) => SetDirect(first, propertyName, value));
        float ySettingViaSetProperty = BuildStackAndGetSecondChildY(
            (first, value) => first.SetProperty(propertyName, value));

        ySettingViaSetProperty.ShouldBe(ySettingViaDirect);
    }

    private static void AssertWrapsIdentically(string propertyName, string? text, bool expectMultipleLines, Action<TextRuntime> configure)
    {
        TextRuntime viaDirect = new();
        configure(viaDirect);
        SetDirect(viaDirect, propertyName, text);

        TextRuntime viaSetProperty = new();
        configure(viaSetProperty);
        viaSetProperty.SetProperty(propertyName, text);

        if (expectMultipleLines)
        {
            viaDirect.WrappedText.Count.ShouldBeGreaterThan(1);
        }
        viaSetProperty.WrappedText.ShouldBe(viaDirect.WrappedText);
    }

    private static void SetDirect(TextRuntime textRuntime, string propertyName, string? value)
    {
        if (propertyName == "TextNoTranslate")
        {
            textRuntime.SetTextNoTranslate(value);
        }
        else
        {
            textRuntime.Text = value;
        }
    }

    private static float BuildStackAndGetSecondChildY(Action<TextRuntime, string> setText)
    {
        ContainerRuntime stack = new();
        stack.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        stack.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        stack.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        TextRuntime first = new();
        first.Text = "Line1";
        TextRuntime second = new();
        second.Text = "Line2";
        stack.Children.Add(first);
        stack.Children.Add(second);

        stack.UpdateLayout();

        setText(first, "Line1\nLine1b\nLine1c");

        return second.Y;
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
}
