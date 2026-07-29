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

    [Fact]
    public void Text_ViaDirectPropertySetVersusSetProperty_WithMaxWidth_ShouldWrapIdentically()
    {
        const string longText = "This is a long piece of text that should wrap at the max width";

        TextRuntime viaProperty = new();
        viaProperty.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        viaProperty.MaxWidth = 40;
        viaProperty.Text = longText;

        TextRuntime viaSetProperty = new();
        viaSetProperty.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        viaSetProperty.MaxWidth = 40;
        viaSetProperty.SetProperty("Text", longText);

        viaProperty.WrappedText.Count.ShouldBeGreaterThan(1);
        viaSetProperty.WrappedText.ShouldBe(viaProperty.WrappedText);
    }

    [Fact]
    public void Text_ViaSetProperty_WithFixedWidthAndHeightRelativeToChildren_ShouldWrapSameAsDirectPropertySet()
    {
        const string longText = "This is a long piece of text that should wrap at the fixed width";

        TextRuntime viaProperty = new();
        viaProperty.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        viaProperty.Width = 100;
        viaProperty.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        viaProperty.Text = longText;

        TextRuntime viaSetProperty = new();
        viaSetProperty.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        viaSetProperty.Width = 100;
        viaSetProperty.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        viaSetProperty.SetProperty("Text", longText);

        viaProperty.WrappedText.Count.ShouldBeGreaterThan(1);
        viaSetProperty.WrappedText.ShouldBe(viaProperty.WrappedText);
    }

    [Fact]
    public void Text_ViaDirectPropertySetVersusSetProperty_InStack_ShouldPositionSiblingIdentically()
    {
        float ySettingViaProperty = BuildStackAndGetSecondChildY(
            (first, value) => first.Text = value);
        float ySettingViaSetProperty = BuildStackAndGetSecondChildY(
            (first, value) => first.SetProperty("Text", value));

        ySettingViaSetProperty.ShouldBe(ySettingViaProperty);
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
