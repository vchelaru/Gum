using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="ObjectFinder.GetElementSave(string)"/> element lookup behavior
/// that the CLI <c>codegen --element</c> flag relies on.
/// </summary>
public class ObjectFinderElementLookupTests : BaseTestClass
{
    [Fact]
    public void GetElementSave_FindsComponentByName()
    {
        Project.Components.Add(new ComponentSave { Name = "Button" });
        ObjectFinder.Self.GumProjectSave = Project;

        ElementSave? result = ObjectFinder.Self.GetElementSave("Button");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Button");
    }

    [Fact]
    public void GetElementSave_FindsFolderQualifiedComponent()
    {
        Project.Components.Add(new ComponentSave { Name = "Controls/Button" });
        ObjectFinder.Self.GumProjectSave = Project;

        ElementSave? result = ObjectFinder.Self.GetElementSave("Controls/Button");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Controls/Button");
    }

    [Fact]
    public void GetElementSave_FindsScreenByName()
    {
        Project.Screens.Add(new ScreenSave { Name = "MainMenu" });
        ObjectFinder.Self.GumProjectSave = Project;

        ElementSave? result = ObjectFinder.Self.GetElementSave("MainMenu");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("MainMenu");
    }

    [Fact]
    public void GetElementSave_IsCaseInsensitive()
    {
        Project.Components.Add(new ComponentSave { Name = "HealthBar" });
        ObjectFinder.Self.GumProjectSave = Project;

        ElementSave? result = ObjectFinder.Self.GetElementSave("healthbar");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("HealthBar");
    }

    [Fact]
    public void GetElementSave_ReturnsNullForUnknownName()
    {
        Project.Components.Add(new ComponentSave { Name = "Button" });
        ObjectFinder.Self.GumProjectSave = Project;

        ElementSave? result = ObjectFinder.Self.GetElementSave("NonExistent");

        result.ShouldBeNull();
    }
}
