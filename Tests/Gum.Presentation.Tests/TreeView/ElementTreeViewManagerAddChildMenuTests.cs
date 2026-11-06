using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class ElementTreeViewManagerAddChildMenuTests : BaseTestClass
{
    [Fact]
    public void GetAvailableStandardInstanceTypes_ProjectWithoutColoredRectangle_DoesNotIncludeColoredRectangle()
    {
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Container" });
        project.StandardElements.Add(new StandardElementSave { Name = "Rectangle" });

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project);

        result.ShouldNotContain("ColoredRectangle");
    }

    [Fact]
    public void GetAvailableStandardInstanceTypes_LegacyProjectWithColoredRectangle_IncludesColoredRectangle()
    {
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "ColoredRectangle" });

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project);

        result.ShouldContain("ColoredRectangle");
    }

    [Fact]
    public void GetAvailableStandardInstanceTypes_ProjectWithSupportedTypes_ReturnsOnlyThose()
    {
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Container" });
        project.StandardElements.Add(new StandardElementSave { Name = "Rectangle" });
        project.StandardElements.Add(new StandardElementSave { Name = "Sprite" });
        project.SupportedStandardTypes.Add("Sprite");

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project);

        result.ShouldBe(new[] { "Sprite" });
    }

    [Fact]
    public void GetAvailableStandardInstanceTypes_EmptySupportedTypes_ReturnsEveryType()
    {
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Container" });
        project.StandardElements.Add(new StandardElementSave { Name = "Sprite" });

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void GetAvailableStandardInstanceTypes_ExcludesComponent()
    {
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Component" });
        project.StandardElements.Add(new StandardElementSave { Name = "Container" });

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project);

        result.ShouldNotContain("Component");
        result.ShouldContain("Container");
    }
}
