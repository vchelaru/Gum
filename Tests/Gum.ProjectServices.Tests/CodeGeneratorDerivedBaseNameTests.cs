using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A derived element must inherit the class name the generator gives its base element, not the raw
/// Gum <c>BaseType</c> (#6656): the base class is named <c>{Name}Runtime</c> for the Gum runtimes and
/// <c>{Name}</c> for Gum Forms, without the folder.
/// </summary>
public class CodeGeneratorDerivedBaseNameTests : BaseTestClass
{
    private ScreenSave AddScreen(string name, string? baseType = null)
    {
        ScreenSave screen = new ScreenSave { Name = name, BaseType = baseType };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        Project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = Project;
        return screen;
    }

    private ComponentSave AddComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;
        return component;
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame, 3)]
    [InlineData(OutputLibrary.MonoGame, 0)]
    [InlineData(OutputLibrary.Raylib, 3)]
    [InlineData(OutputLibrary.Silk, 3)]
    [InlineData(OutputLibrary.Skia, 3)]
    [InlineData(OutputLibrary.Skia, 0)]
    public void GetInheritance_ScreenOverGeneratedScreen_UsesRuntimeClassNameOfBase(OutputLibrary library, int syntaxVersion)
    {
        AddScreen("ScreenBase");
        ScreenSave derived = AddScreen("ScreenDerived", "ScreenBase");

        string? result = CodeGenerator.GetInheritance(derived, new CodeOutputProjectSettings { OutputLibrary = library }, syntaxVersion);

        result.ShouldBe("ScreenBaseRuntime");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.Raylib)]
    [InlineData(OutputLibrary.Silk)]
    [InlineData(OutputLibrary.Skia)]
    public void GetInheritance_ScreenOverGeneratedScreenInFolder_StripsFolder(OutputLibrary library)
    {
        AddScreen("Screens/ScreenBase");
        ScreenSave derived = AddScreen("Screens/ScreenDerived", "Screens/ScreenBase");

        string? result = CodeGenerator.GetInheritance(derived, new CodeOutputProjectSettings { OutputLibrary = library });

        result.ShouldBe("ScreenBaseRuntime");
    }

    [Fact]
    public void GetInheritance_GumForms_ScreenOverGeneratedScreenInFolder_UsesBareClassNameOfBase()
    {
        AddScreen("Screens/ScreenBase");
        ScreenSave derived = AddScreen("Screens/ScreenDerived", "Screens/ScreenBase");

        string? result = CodeGenerator.GetInheritance(derived, new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.GumForms });

        result.ShouldBe("ScreenBase");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.Raylib)]
    [InlineData(OutputLibrary.Silk)]
    [InlineData(OutputLibrary.GumForms)]
    public void GetInheritance_ScreenOverBaseListedAsNotCodeGenerated_KeepsRawBaseType(OutputLibrary library)
    {
        // A hand-written base class, even when a screen of that name also exists in the project.
        AddScreen("HandWrittenBase");
        ScreenSave derived = AddScreen("ScreenDerived", "HandWrittenBase");
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = library,
            BaseTypesNotCodeGenerated = "Other\nHandWrittenBase",
        };

        string? result = CodeGenerator.GetInheritance(derived, settings);

        result.ShouldBe("HandWrittenBase");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.Raylib)]
    [InlineData(OutputLibrary.Silk)]
    [InlineData(OutputLibrary.GumForms)]
    public void GetInheritance_ScreenOverBaseNotInProject_KeepsRawBaseType(OutputLibrary library)
    {
        ScreenSave derived = AddScreen("ScreenDerived", "MyGame.Screens.HandWrittenBase");

        string? result = CodeGenerator.GetInheritance(derived, new CodeOutputProjectSettings { OutputLibrary = library });

        result.ShouldBe("MyGame.Screens.HandWrittenBase");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame, "BaseRuntime")]
    [InlineData(OutputLibrary.Raylib, "BaseRuntime")]
    [InlineData(OutputLibrary.Silk, "BaseRuntime")]
    [InlineData(OutputLibrary.Skia, "BaseRuntime")]
    [InlineData(OutputLibrary.GumForms, "Base")]
    public void GetInheritance_ComponentOverGeneratedComponentInFolder_UsesClassNameOfBase(OutputLibrary library, string expected)
    {
        AddComponent("Elements/Base", "Container");
        ComponentSave derived = AddComponent("Elements/Derived", "Elements/Base");

        string? result = CodeGenerator.GetInheritance(derived, new CodeOutputProjectSettings { OutputLibrary = library });

        result.ShouldBe(expected);
    }

    // The inherited name must be the one the base class declaration uses, which sanitizes it.
    private static CodeGenerationNameVerifier CreateNameVerifier()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out It.Ref<string>.IsAny, out It.Ref<CommonValidationError>.IsAny))
            .Returns((string name, out string whyNot, out CommonValidationError error) =>
            {
                whyNot = null!;
                error = CommonValidationError.None;
                if (name.Length > 0 && name[0] != '_' && !char.IsLetter(name[0]))
                {
                    whyNot = "Name may not begin with a digit";
                    error = CommonValidationError.InvalidStartingCharacterForCSharp;
                }
                else if (name == "class")
                {
                    whyNot = "Name is a C# reserved keyword";
                    error = CommonValidationError.ReservedCSharpKeyword;
                }
                return error == CommonValidationError.None;
            });
        return new CodeGenerationNameVerifier(mockNameVerifier.Object);
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame, "My Screen", "My_ScreenRuntime")]
    [InlineData(OutputLibrary.Raylib, "1X", "_1XRuntime")]
    [InlineData(OutputLibrary.Silk, "Screens/My Screen", "My_ScreenRuntime")]
    [InlineData(OutputLibrary.Skia, "1X", "_1XRuntime")]
    [InlineData(OutputLibrary.GumForms, "My Screen", "My_Screen")]
    [InlineData(OutputLibrary.GumForms, "1X", "_1X")]
    [InlineData(OutputLibrary.GumForms, "class", "@class")]
    public void GetInheritance_ScreenOverGeneratedScreenWithUnsafeName_UsesSanitizedDeclaredName(OutputLibrary library, string baseName, string expected)
    {
        AddScreen(baseName);
        ScreenSave derived = AddScreen("Derived", baseName);

        string? result = CodeGenerator.GetInheritance(
            derived, new CodeOutputProjectSettings { OutputLibrary = library }, nameVerifier: CreateNameVerifier());

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame, "My Comp", "My_CompRuntime")]
    [InlineData(OutputLibrary.Raylib, "1X", "_1XRuntime")]
    [InlineData(OutputLibrary.Skia, "My Comp", "My_CompRuntime")]
    [InlineData(OutputLibrary.GumForms, "My Comp", "My_Comp")]
    [InlineData(OutputLibrary.GumForms, "class", "@class")]
    public void GetInheritance_ComponentOverGeneratedComponentWithUnsafeName_UsesSanitizedDeclaredName(OutputLibrary library, string baseName, string expected)
    {
        AddComponent(baseName, "Container");
        ComponentSave derived = AddComponent("Derived", baseName);

        string? result = CodeGenerator.GetInheritance(
            derived, new CodeOutputProjectSettings { OutputLibrary = library }, nameVerifier: CreateNameVerifier());

        result.ShouldBe(expected);
    }
}
