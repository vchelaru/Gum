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
}
