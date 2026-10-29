using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A category generates a nested enum named like it and a <c>&lt;Category&gt;State</c> property. Those public
/// names cannot equal the generated class's own name (CS0542) or each other (CS0102), and renaming a public
/// type to step around that would change the API, so the checker reports it as GUM0018 (#7141).
/// </summary>
public class CategoryNameCollisionErrorTests : BaseTestClass
{
    private readonly HeadlessErrorChecker _sut;

    public CategoryNameCollisionErrorTests()
    {
        _sut = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object);
    }

    [Theory]
    [InlineData("Mode", "Mode")]
    [InlineData("Hud/Mode", "Mode")]
    [InlineData("Score Panel", "Score_Panel")]
    public void GetErrorsFor_ShouldReportGum0015_WhenCategoryIsNamedLikeItsElement(string elementName, string categoryName)
    {
        ComponentSave component = AddComponent(elementName, categoryName);

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0018");
        error.Severity.ShouldBe(ErrorSeverity.Error);
        error.ElementName.ShouldBe(elementName);
        error.Message.ShouldContain(categoryName);
    }

    [Fact]
    public void GetErrorsFor_ShouldReportGum0015_WhenCategoryStatePropertyIsNamedLikeItsElement()
    {
        // Category "Mode" generates the property "ModeState", which a component named "ModeState" cannot have.
        ComponentSave component = AddComponent("ModeState", "Mode");

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0018");
        error.Message.ShouldContain("ModeState");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportGum0015_WhenCategoryEnumIsNamedLikeAnotherCategorysStateProperty()
    {
        // Category "Mode" generates the property "ModeState"; category "ModeState" generates an enum of that name.
        ComponentSave component = AddComponent("Panel", "Mode", "ModeState");

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0018");
        error.Message.ShouldContain("ModeState");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportGum0015_WhenTwoCategoriesGenerateTheSameName()
    {
        ComponentSave component = AddComponent("Panel", "Mode Type", "Mode_Type");

        _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem().Code.ShouldBe("GUM0018");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportGum0015_ThroughGetAllErrors()
    {
        AddComponent("Mode", "Mode");

        _sut.GetAllErrors(Project).ShouldContain(error => error.Code == "GUM0018");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportGum0015_ForAScreen()
    {
        ScreenSave screen = new ScreenSave { Name = "Mode" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Categories.Add(new StateSaveCategory { Name = "Mode" });
        Project.Screens.Add(screen);

        _sut.GetErrorsFor(screen, Project).ShouldHaveSingleItem().Code.ShouldBe("GUM0018");
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportGum0015_WhenNamesAreDistinct()
    {
        ComponentSave component = AddComponent("Panel", "Mode", "Size");
        // A state named like its category is a legal enum member.
        component.Categories[0].States.Add(new StateSave { Name = "Mode", ParentContainer = component });

        _sut.GetErrorsFor(component, Project).ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportGum0015_ForAStandardElement()
    {
        // Standards are never generated, so a category named like one is harmless.
        StandardElementSave standard = new StandardElementSave { Name = "Mode" };
        standard.States.Add(new StateSave { Name = "Default", ParentContainer = standard });
        standard.Categories.Add(new StateSaveCategory { Name = "Mode" });
        Project.StandardElements.Add(standard);

        _sut.GetErrorsFor(standard, Project).ShouldNotContain(error => error.Code == "GUM0018");
    }

    private ComponentSave AddComponent(string name, params string[] categoryNames)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        foreach (string categoryName in categoryNames)
        {
            component.Categories.Add(new StateSaveCategory { Name = categoryName });
        }
        Project.Components.Add(component);
        return component;
    }
}
