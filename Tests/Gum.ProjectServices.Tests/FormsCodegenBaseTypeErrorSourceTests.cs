using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class FormsCodegenBaseTypeErrorSourceTests : IDisposable
{
    private readonly GumProjectSave _project;

    public FormsCodegenBaseTypeErrorSourceTests()
    {
        _project = new GumProjectSave();
        _project.StandardElements.Add(new StandardElementSave { Name = "Container" });
        _project.StandardElements.Add(new StandardElementSave { Name = "Sprite" });
        ObjectFinder.Self.GumProjectSave = _project;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }

    [Theory]
    [InlineData("Sprite")]
    [InlineData("Container")]
    public void GetErrors_BehaviorlessComponent_ErrorsOnlyForNonContainerStandard(string baseType)
    {
        ComponentSave component = new ComponentSave { Name = "Components/MyComponent", BaseType = baseType };
        FormsCodegenBaseTypeErrorSource source = CreateSource(OutputLibrary.MonoGameForms);

        List<ErrorResult> errors = source.GetErrors(component, _project).ToList();

        if (baseType == "Container")
        {
            errors.ShouldBeEmpty();
        }
        else
        {
            errors.Count.ShouldBe(1);
            errors[0].ElementName.ShouldBe("Components/MyComponent");
            errors[0].Severity.ShouldBe(ErrorSeverity.Error);
            errors[0].Message.ShouldContain("Components/MyComponent");
            errors[0].Message.ShouldContain("Container");
            errors[0].Message.ShouldContain("Sprite");
        }
    }

    [Fact]
    public void GetErrors_SpriteComponentWithFormsBehavior_ReturnsNoErrors()
    {
        ComponentSave component = new ComponentSave { Name = "Components/MySpriteButton", BaseType = "Sprite" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        FormsCodegenBaseTypeErrorSource source = CreateSource(OutputLibrary.MonoGameForms);

        source.GetErrors(component, _project).ShouldBeEmpty();
    }

    [Fact]
    public void GetErrors_SpriteComponentWithLegacyOutputLibrary_ReturnsNoErrors()
    {
        ComponentSave component = new ComponentSave { Name = "Components/MySprite", BaseType = "Sprite" };
        FormsCodegenBaseTypeErrorSource source = CreateSource(OutputLibrary.MonoGame);

        source.GetErrors(component, _project).ShouldBeEmpty();
    }

    [Fact]
    public void GetErrors_Screen_ReturnsNoErrors()
    {
        ScreenSave screen = new ScreenSave { Name = "MyScreen", BaseType = "Sprite" };
        FormsCodegenBaseTypeErrorSource source = CreateSource(OutputLibrary.MonoGameForms);

        source.GetErrors(screen, _project).ShouldBeEmpty();
    }

    private static FormsCodegenBaseTypeErrorSource CreateSource(OutputLibrary outputLibrary) =>
        new FormsCodegenBaseTypeErrorSource(new CodeOutputProjectSettings { OutputLibrary = outputLibrary });
}
