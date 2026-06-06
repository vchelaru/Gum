using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace GumToolUnitTests.Managers;

public class ObjectFinderTests : BaseTestClass
{
    [Fact]
    public void GetQualifiedElementName_ShouldUseComponentsPrefix_ForComponent()
    {
        ComponentSave component = new() { Name = "MyComp" };

        ObjectFinder.Self.GetQualifiedElementName(component).ShouldBe("Components/MyComp");
    }

    [Fact]
    public void GetQualifiedElementName_ShouldUseScreensPrefix_ForScreen()
    {
        ScreenSave screen = new() { Name = "MyScreen" };

        ObjectFinder.Self.GetQualifiedElementName(screen).ShouldBe("Screens/MyScreen");
    }

    [Fact]
    public void GetQualifiedElementName_ShouldUseStandardsPrefix_ForStandard()
    {
        StandardElementSave standard = new() { Name = "ColoredRectangle" };

        ObjectFinder.Self.GetQualifiedElementName(standard).ShouldBe("Standards/ColoredRectangle");
    }
}
