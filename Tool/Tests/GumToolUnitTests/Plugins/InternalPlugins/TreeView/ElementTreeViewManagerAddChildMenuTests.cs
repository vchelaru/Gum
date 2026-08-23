using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

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
