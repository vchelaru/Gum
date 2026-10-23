using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Issue #6594: projects saved before the v3 standard surface keep Red/Green/Blue/Alpha on the plain
/// Circle and Rectangle standards. Those members are obsolete on CircleRuntime/RectangleRuntime (they
/// route to the stroke), so from syntax version 3, the first stamped after the stroke channel
/// properties shipped, codegen emits StrokeRed/StrokeGreen/StrokeBlue/StrokeAlpha instead.
/// </summary>
public class CodeGeneratorShapeColorTests : BaseTestClass
{
    [Theory]
    [InlineData("Circle", OutputLibrary.GumForms)]
    [InlineData("Rectangle", OutputLibrary.GumForms)]
    [InlineData("Circle", OutputLibrary.MonoGame)]
    [InlineData("Rectangle", OutputLibrary.Skia)]
    public void GetCodeForInstance_V3_RoutesColorChannelsToStroke(string standardName, OutputLibrary library)
    {
        string code = GenerateInstanceCode(standardName, library, syntaxVersion: "3",
            ("Red", 10), ("Green", 20), ("Blue", 30), ("Alpha", 40));

        code.ShouldContain("ShapeInstance.StrokeRed = 10;");
        code.ShouldContain("ShapeInstance.StrokeGreen = 20;");
        code.ShouldContain("ShapeInstance.StrokeBlue = 30;");
        code.ShouldContain("ShapeInstance.StrokeAlpha = 40;");
        code.ShouldNotContain("ShapeInstance.Red ");
        code.ShouldNotContain("ShapeInstance.Alpha ");
    }

    // Version 2 was stamped before StrokeRed shipped, so a version-2 runtime may lack it.
    [Fact]
    public void GetCodeForInstance_V2_KeepsColorChannels()
    {
        string code = GenerateInstanceCode("Circle", OutputLibrary.MonoGame, syntaxVersion: "2", ("Red", 10));

        code.ShouldContain("ShapeInstance.Red = 10;");
        code.ShouldNotContain("StrokeRed");
    }

    [Fact]
    public void GetCodeForElement_CircleBasedComponentV3_RoutesOwnColorToStroke()
    {
        AddColorVariables("Circle");
        ComponentSave component = CreateComponent("MyCircle", "Circle");
        component.DefaultState!.Variables.Add(new VariableSave { Name = "Red", Value = 10, SetsValue = true, Type = "int" });

        string code = GenerateElementCode(component, CreateSettings(OutputLibrary.MonoGame, "3"));

        code.ShouldContain("this.StrokeRed = 10;");
        code.ShouldNotContain("this.Red = 10;");
    }

    [Fact]
    public void GetCodeForElement_CategoryStateSettingInstanceRed_RoutesToStroke()
    {
        AddColorVariables("Circle");
        ComponentSave component = CreateComponent("MainComponent", "Container");
        component.Instances.Add(new InstanceSave { Name = "ShapeInstance", BaseType = "Circle", ParentContainer = component });
        StateSaveCategory category = new StateSaveCategory { Name = "Highlight" };
        StateSave state = new StateSave { Name = "On", ParentContainer = component };
        state.Variables.Add(new VariableSave { Name = "ShapeInstance.Red", Value = 10, SetsValue = true, Type = "int" });
        category.States.Add(state);
        component.Categories.Add(category);

        string code = GenerateElementCode(component, CreateSettings(OutputLibrary.GumForms, "3"));

        code.ShouldContain("ShapeInstance.StrokeRed = 10;");
        code.ShouldNotContain("ShapeInstance.Red ");
    }

    // On Maui a Xamarin Forms container forwards through a BindableProperty change handler.
    [Theory]
    [InlineData(OutputLibrary.GumForms)]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.Maui)]
    public void GetCodeForElement_ExposedInstanceRed_ForwardsToStroke(OutputLibrary library)
    {
        string code = GenerateExposedRedCode("Circle", library, "3", isFilled: null);

        AssertExposedRedForwardsTo(code, library, "StrokeRed");
        code.ShouldNotContain("ShapeInstance.Red ");
        code.ShouldNotContain("ShapeInstance.Red;");
    }

    [Fact]
    public void GetCodeForElement_ExposedInstanceRedV2_KeepsRed()
    {
        string code = GenerateExposedRedCode("Circle", OutputLibrary.MonoGame, "2", isFilled: null);

        AssertExposedRedForwardsTo(code, OutputLibrary.MonoGame, "Red");
    }

    // A ColoredCircle is generated as the collapsed CircleRuntime (#2775) on MonoGame and Gum Forms;
    // its exposed color follows the same fill/stroke routing as its assignments. Maui keeps the
    // legacy ColoredCircleRuntime, whose Red is live.
    [Theory]
    [InlineData(OutputLibrary.MonoGame, null, "FillRed")]
    [InlineData(OutputLibrary.GumForms, false, "StrokeRed")]
    [InlineData(OutputLibrary.Maui, null, "Red")]
    public void GetCodeForElement_ExposedCollapsedShapeRed_ForwardsToChannel(OutputLibrary library, bool? isFilled, string expectedMember)
    {
        string code = GenerateExposedRedCode("ColoredCircle", library, "3", isFilled);

        AssertExposedRedForwardsTo(code, library, expectedMember);
    }

    private static void AssertExposedRedForwardsTo(string code, OutputLibrary library, string member)
    {
        if (library == OutputLibrary.Maui)
        {
            code.ShouldContain($"casted.ShapeInstance.{member} = (int)newValue;");
        }
        else
        {
            code.ShouldContain($"get => ShapeInstance.{member};");
            code.ShouldContain($"set => ShapeInstance.{member} = value;");
        }
    }

    private string GenerateExposedRedCode(string standardName, OutputLibrary library, string syntaxVersion, bool? isFilled)
    {
        AddColorVariables(standardName);
        ComponentSave component = CreateComponent("MainComponent", "Container");
        component.Instances.Add(new InstanceSave { Name = "ShapeInstance", BaseType = standardName, ParentContainer = component });
        component.DefaultState!.Variables.Add(new VariableSave
        {
            Name = "ShapeInstance.Red", Value = 10, SetsValue = true, Type = "int", ExposedAsName = "ShapeRed",
        });
        if (isFilled != null)
        {
            component.DefaultState.Variables.Add(new VariableSave
            {
                Name = "ShapeInstance.IsFilled", Value = isFilled.Value, SetsValue = true, Type = "bool",
            });
        }
        if (library == OutputLibrary.Maui)
        {
            component.DefaultState.Variables.Add(new VariableSave
            {
                Name = "IsXamarinFormsControl", Value = true, SetsValue = true, Type = "bool",
            });
        }

        return GenerateElementCode(component, CreateSettings(library, syntaxVersion));
    }

    private string GenerateElementCode(ComponentSave component, CodeOutputProjectSettings settings)
    {
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            return CreateCodeGenerator().GetGeneratedCodeForElement(component, new CodeOutputElementSettings(), settings);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    private string GenerateInstanceCode(string standardName, OutputLibrary library, string syntaxVersion,
        params (string Name, int Value)[] variables)
    {
        AddColorVariables(standardName);
        ComponentSave main = CreateComponent("MainComponent", "Container");
        InstanceSave instance = new InstanceSave { Name = "ShapeInstance", BaseType = standardName, ParentContainer = main };
        main.Instances.Add(instance);
        foreach ((string name, int value) in variables)
        {
            main.DefaultState!.Variables.Add(new VariableSave { Name = "ShapeInstance." + name, Value = value, SetsValue = true, Type = "int" });
        }
        Project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            return CreateCodeGenerator().GetCodeForInstance(instance, main, CreateSettings(library, syntaxVersion));
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    // What a project saved before the v3 standard surface has on its Circle/Rectangle standard.
    private void AddColorVariables(string standardName)
    {
        StandardElementSave element = new StandardElementSave { Name = standardName };
        element.States.Add(new StateSave { Name = "Default", ParentContainer = element });
        Project.StandardElements.Add(element);
        foreach (string name in new[] { "Red", "Green", "Blue", "Alpha" })
        {
            element.DefaultState!.Variables.Add(new VariableSave { Name = name, Value = 255, SetsValue = true, Type = "int" });
        }
    }

    private static CodeOutputProjectSettings CreateSettings(OutputLibrary library, string syntaxVersion) => new CodeOutputProjectSettings
    {
        OutputLibrary = library,
        RootNamespace = "MyGame",
        SyntaxVersion = syntaxVersion,
        ObjectInstantiationType = ObjectInstantiationType.FullyInCode,
    };

    private static ComponentSave CreateComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> nameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        nameVerifier
            .Setup(item => item.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        return new CodeGenerator(
            new CodeGenerationNameVerifier(nameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider);
    }
}
