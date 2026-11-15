using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class PreviewWindowTitleTests
{
    [Fact]
    public void For_WhenTheElementIsShown_NamesIt()
    {
        PreviewWindowTitle.For("Controls/Button", isElementMissing: false).ShouldBe("Gum Preview - Controls/Button");
    }

    [Theory]
    [InlineData("MainMenu")]
    [InlineData("")]
    public void For_WhenThereIsNothingToShow_IsTheNeutralTitle(string elementName)
    {
        PreviewWindowTitle.For(elementName, isElementMissing: true).ShouldBe("Gum Preview");
    }

    [Fact]
    public void For_WithALanguage_AppendsItInParentheses()
    {
        PreviewWindowTitle.For("Controls/Button", isElementMissing: false, "Spanish").ShouldBe("Gum Preview - Controls/Button (Spanish)");
    }

    [Fact]
    public void For_WithALanguageAndNothingToShow_AppendsItToTheNeutralTitle()
    {
        PreviewWindowTitle.For("", isElementMissing: true, "Spanish").ShouldBe("Gum Preview (Spanish)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void For_WithoutALanguage_IsTheOldTitle(string? language)
    {
        PreviewWindowTitle.For("Controls/Button", isElementMissing: false, language).ShouldBe("Gum Preview - Controls/Button");
    }

    [Fact]
    public void ForLoadFailure_PutsTheReasonInTheTitle()
    {
        PreviewWindowTitle.ForLoadFailure("could not load a.gumx: missing").ShouldBe("Gum Preview - could not load a.gumx: missing");
    }
}
