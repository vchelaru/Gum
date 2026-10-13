using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace Gum.Presentation.Tests.Fonts;

public class BmfcSaveTests
{
    [Fact]
    public void GetFontCacheFileNameFor_ShouldAppendTtfSuffix_WhenFontFilePathIsProvided()
    {
        string result = BmfcSave.GetFontCacheFileNameFor(
            18, "Arial", 0, useFontSmoothing: true, fontFilePath: "fonts/MyFont.ttf");

        result.ShouldContain("MyFont_ttf");
        result.ShouldEndWith(".fnt");
    }

    [Fact]
    public void GetFontCacheFileNameFor_ShouldNotAppendTtfSuffix_WhenFontFilePathIsNull()
    {
        string result = BmfcSave.GetFontCacheFileNameFor(
            18, "Arial", 0, useFontSmoothing: true);

        result.ShouldNotContain("_ttf");
        result.ShouldContain("Arial");
    }

    [Fact]
    public void GetFontCacheFileNameFor_ShouldProduceDifferentNames_ForSystemFontAndTtfWithSameName()
    {
        string systemResult = BmfcSave.GetFontCacheFileNameFor(
            18, "MyFont", 0, useFontSmoothing: true);

        string fileResult = BmfcSave.GetFontCacheFileNameFor(
            18, "MyFont", 0, useFontSmoothing: true, fontFilePath: "MyFont.ttf");

        systemResult.ShouldNotBe(fileResult);
    }

    [Fact]
    public void GetFontCacheFileNameFor_ShouldSanitizeSpacesInTtfFileName()
    {
        string result = BmfcSave.GetFontCacheFileNameFor(
            18, "Arial", 0, useFontSmoothing: true, fontFilePath: "fonts/My Font.ttf");

        result.ShouldContain("My_Font_ttf");
    }

    [Fact]
    public void GetFontCacheFileNameFor_ShouldUseTtfFileNameNotFontNameParam_WhenFontFilePathIsProvided()
    {
        string result = BmfcSave.GetFontCacheFileNameFor(
            18, "Arial", 0, useFontSmoothing: true, fontFilePath: "fonts/CustomFont.ttf");

        result.ShouldContain("CustomFont");
        result.ShouldNotContain("Arial");
    }

    [Fact]
    public void IsFontFilePath_ShouldReturnFalse_WhenValueIsEmpty()
    {
        BmfcSave.IsFontFilePath("").ShouldBeFalse();
    }

    [Fact]
    public void IsFontFilePath_ShouldReturnFalse_WhenValueIsNull()
    {
        BmfcSave.IsFontFilePath(null).ShouldBeFalse();
    }

    [Fact]
    public void IsFontFilePath_ShouldReturnFalse_WhenValueIsSystemFontName()
    {
        BmfcSave.IsFontFilePath("Arial").ShouldBeFalse();
    }

    [Fact]
    public void IsFontFilePath_ShouldReturnTrue_WhenValueEndsWith_Ttf_CaseInsensitive()
    {
        BmfcSave.IsFontFilePath("fonts/MyFont.TTF").ShouldBeTrue();
    }

    [Fact]
    public void IsFontFilePath_ShouldReturnTrue_WhenValueEndsWith_Ttf_Lowercase()
    {
        BmfcSave.IsFontFilePath("fonts/MyFont.ttf").ShouldBeTrue();
    }

    [Theory]
    [InlineData("fonts/MyFont.otf")]
    [InlineData("fonts/MyFont.OTF")]
    [InlineData("fonts/MyFont.ttc")]
    [InlineData("fonts/MyFont.woff")]
    [InlineData("fonts/MyFont.WOFF")]
    public void IsFontFilePath_ShouldReturnTrue_WhenValueIsOtherSupportedFontFormat(string value)
    {
        BmfcSave.IsFontFilePath(value).ShouldBeTrue();
    }

    [Theory]
    [InlineData("fonts/MyFont.fnt")]
    [InlineData("fonts/MyFont.woff2")]
    [InlineData("fonts/MyFont.png")]
    [InlineData("fonts/ttf")]
    public void IsFontFilePath_ShouldReturnFalse_WhenValueIsNotARasterizableFontFile(string value)
    {
        BmfcSave.IsFontFilePath(value).ShouldBeFalse();
    }

    [Theory]
    [InlineData("fonts/MyFont.otf")]
    [InlineData("fonts/MyFont.woff")]
    public void ResolveTtfSourcePath_ShouldReturnPath_WhenCustomFontFileIsOtherSupportedFormat(string customFontFile)
    {
        BmfcSave.ResolveTtfSourcePath(useCustomFont: true, customFontFile, font: "Arial")
            .ShouldBe(customFontFile);
    }
}
