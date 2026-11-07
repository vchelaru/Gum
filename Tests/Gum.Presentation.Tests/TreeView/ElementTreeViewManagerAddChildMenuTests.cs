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
        EngineProfile profile = new EngineProfile();
        IProjectRestrictions restrictions = TestProjectRestrictions.Create(profile);
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Container" });
        project.StandardElements.Add(new StandardElementSave { Name = "Rectangle" });

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project, restrictions);

        result.ShouldNotContain("ColoredRectangle");
    }

    [Fact]
    public void GetAvailableStandardInstanceTypes_LegacyProjectWithColoredRectangle_IncludesColoredRectangle()
    {
        EngineProfile profile = new EngineProfile();
        IProjectRestrictions restrictions = TestProjectRestrictions.Create(profile);
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "ColoredRectangle" });

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project, restrictions);

        result.ShouldContain("ColoredRectangle");
    }

    [Fact]
    public void GetAvailableStandardInstanceTypes_ProjectWithSupportedTypes_ReturnsOnlyThose()
    {
        EngineProfile profile = new EngineProfile();
        IProjectRestrictions restrictions = TestProjectRestrictions.Create(profile);
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Container" });
        project.StandardElements.Add(new StandardElementSave { Name = "Rectangle" });
        project.StandardElements.Add(new StandardElementSave { Name = "Sprite" });
        profile.SupportedStandardTypes.Add("Sprite");

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project, restrictions);

        result.ShouldBe(new[] { "Sprite" });
    }

    [Fact]
    public void GetAvailableStandardInstanceTypes_EmptySupportedTypes_ReturnsEveryType()
    {
        EngineProfile profile = new EngineProfile();
        IProjectRestrictions restrictions = TestProjectRestrictions.Create(profile);
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Container" });
        project.StandardElements.Add(new StandardElementSave { Name = "Sprite" });

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project, restrictions);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void GetAvailableStandardInstanceTypes_ExcludesComponent()
    {
        EngineProfile profile = new EngineProfile();
        IProjectRestrictions restrictions = TestProjectRestrictions.Create(profile);
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(new StandardElementSave { Name = "Component" });
        project.StandardElements.Add(new StandardElementSave { Name = "Container" });

        var result = ElementTreeViewManager.GetAvailableStandardInstanceTypes(project, restrictions);

        result.ShouldNotContain("Component");
        result.ShouldContain("Container");
    }
}
