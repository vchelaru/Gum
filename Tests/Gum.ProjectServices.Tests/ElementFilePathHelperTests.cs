using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

public class ElementFilePathHelperTests
{
    [Fact]
    public void GetFullPathXmlFile_ComponentElement_ReturnsComponentsSubfolder()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };

        FilePath? result = ElementFilePathHelper.GetFullPathXmlFile(component, @"C:\MyProject\");

        result.ShouldNotBeNull();
        result!.FullPath.ShouldContain("Components");
        result.FullPath.ShouldEndWith("Button.gucx");
    }

    [Fact]
    public void GetFullPathXmlFile_NullElement_ReturnsNull()
    {
        FilePath? result = ElementFilePathHelper.GetFullPathXmlFile(null, @"C:\MyProject\");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetFullPathXmlFile_NullProjectDirectory_ReturnsNull()
    {
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };

        FilePath? result = ElementFilePathHelper.GetFullPathXmlFile(screen, null);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetFullPathXmlFile_EmptyProjectDirectory_ReturnsNull()
    {
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };

        FilePath? result = ElementFilePathHelper.GetFullPathXmlFile(screen, "");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetFullPathXmlFile_ScreenElement_ReturnsScreensSubfolder()
    {
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };

        FilePath? result = ElementFilePathHelper.GetFullPathXmlFile(screen, @"C:\MyProject\");

        result.ShouldNotBeNull();
        result!.FullPath.ShouldContain("Screens");
        result.FullPath.ShouldEndWith("MainMenu.gusx");
    }

    [Fact]
    public void GetFullPathXmlFile_StandardElement_ReturnsStandardsSubfolder()
    {
        StandardElementSave standard = new StandardElementSave { Name = "Text" };

        FilePath? result = ElementFilePathHelper.GetFullPathXmlFile(standard, @"C:\MyProject\");

        result.ShouldNotBeNull();
        result!.FullPath.ShouldContain("Standards");
        result.FullPath.ShouldEndWith("Text.gutx");
    }
}
