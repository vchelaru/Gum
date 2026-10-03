using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #4880: <c>X.ColorOperation = ...</c> only compiles against runtimes whose Sprite/NineSlice
/// runtimes expose the property, so codegen emits it only when the resolved syntax version is
/// at least 5.
/// </summary>
public class CodeGeneratorColorOperationTests : BaseTestClass
{
    private const string AddAssignment = "ColorOperation = global::RenderingLibrary.Graphics.ColorOperation.Add;";

    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        LocalizationService localizationService = new LocalizationService();

        return new CodeGenerator(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);
    }

    private void DeclareColorOperationOnStandards()
    {
        foreach (string standardName in new[] { "Sprite", "NineSlice" })
        {
            StandardElementSave standard = Project.StandardElements.First(item => item.Name == standardName);
            standard.DefaultState.Variables.Add(new VariableSave
            {
                Name = "ColorOperation",
                Type = "ColorOperation",
                Value = ColorOperation.Modulate,
                SetsValue = true,
            });
        }
    }

    private static ComponentSave CreateComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static VariableSave CreateAddVariable(string name) => new VariableSave
    {
        Name = name,
        Type = "ColorOperation",
        Value = ColorOperation.Add,
        SetsValue = true,
    };

    private string Generate(ComponentSave component, int syntaxVersion)
    {
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            RootNamespace = "MyGame",
            SyntaxVersion = syntaxVersion.ToString(),
        };

        return CreateCodeGenerator().GetGeneratedCodeForElement(component, new CodeOutputElementSettings(), settings);
    }

    private ComponentSave CreateComponentWithInstance(string instanceBaseType)
    {
        DeclareColorOperationOnStandards();
        ComponentSave main = CreateComponent("MainComponent", "Container");
        main.Instances.Add(new InstanceSave { Name = "Instance", BaseType = instanceBaseType, ParentContainer = main });
        main.DefaultState.Variables.Add(CreateAddVariable("Instance.ColorOperation"));
        return main;
    }

    [Theory]
    [InlineData("Sprite")]
    [InlineData("NineSlice")]
    public void GetGeneratedCodeForElement_InstanceColorOperation_IsEmittedAtSyntaxVersion5(string instanceBaseType)
    {
        string code = Generate(CreateComponentWithInstance(instanceBaseType), syntaxVersion: 5);

        code.ShouldContain("Instance." + AddAssignment);
    }

    [Theory]
    [InlineData("Sprite")]
    [InlineData("NineSlice")]
    public void GetGeneratedCodeForElement_InstanceColorOperation_IsOmittedBelowSyntaxVersion5(string instanceBaseType)
    {
        string code = Generate(CreateComponentWithInstance(instanceBaseType), syntaxVersion: 4);

        code.ShouldNotContain("ColorOperation");
    }

    [Fact]
    public void GetGeneratedCodeForElement_ElementLevelColorOperation_FollowsSyntaxVersionGate()
    {
        DeclareColorOperationOnStandards();
        ComponentSave component = CreateComponent("MySprite", "Sprite");
        component.DefaultState.Variables.Add(CreateAddVariable("ColorOperation"));
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            RootNamespace = "MyGame",
            SyntaxVersion = "4",
        };
        CodeGenerator generator = CreateCodeGenerator();
        string oldCode = generator.GetGeneratedCodeForElement(component, new CodeOutputElementSettings(), settings);
        settings.SyntaxVersion = "5";
        string newCode = generator.GetGeneratedCodeForElement(component, new CodeOutputElementSettings(), settings);

        oldCode.ShouldNotContain("ColorOperation");
        newCode.ShouldContain(AddAssignment);
    }
}
