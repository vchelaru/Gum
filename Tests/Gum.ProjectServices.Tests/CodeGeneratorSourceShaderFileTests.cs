using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for #3210: a render-target Container's <c>SourceShaderFile</c> variable stores a file
/// reference (e.g. a <c>.fx</c> path) to a post-process shader. The runtime property is literally
/// <c>SourceShaderFile</c>, so codegen emits a plain string assignment with no remap — but an empty
/// value must emit nothing (mirroring how <c>RenderTargetTextureSource</c> skips an empty source).
/// </summary>
public class CodeGeneratorSourceShaderFileTests : BaseTestClass
{
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

    private static CodeOutputProjectSettings CreateMonoGame() => new CodeOutputProjectSettings
    {
        OutputLibrary = OutputLibrary.MonoGame,
        RootNamespace = "MyGame",
    };

    private static ComponentSave CreateComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        return component;
    }

    private static void AddVariable(ElementSave element, string name, object? value, string type) =>
        element.DefaultState.Variables.Add(new VariableSave
        {
            Name = name,
            Value = value,
            SetsValue = true,
            Type = type,
        });

    /// <summary>
    /// Mirrors StandardElementsManager defining SourceShaderFile on the Container standard element.
    /// Codegen only emits an instance variable whose root is defined on the base element.
    /// </summary>
    private void RegisterContainerSourceShaderFileVariable()
    {
        StandardElementSave container = Project.StandardElements.First(item => item.Name == "Container");
        AddVariable(container, "SourceShaderFile", "", "string");
    }

    [Fact]
    public void GetCodeForInstance_SourceShaderFile_EmitsStringAssignment()
    {
        GumProjectSave project = Project;
        RegisterContainerSourceShaderFileVariable();

        ComponentSave main = CreateComponent("MainComponent", "Container");

        InstanceSave renderTargetContainer = new InstanceSave
        {
            Name = "RenderTargetContainer",
            BaseType = "Container",
            ParentContainer = main,
        };
        main.Instances.Add(renderTargetContainer);
        AddVariable(main, "RenderTargetContainer.IsRenderTarget", true, "bool");
        AddVariable(main, "RenderTargetContainer.SourceShaderFile", "Shaders/Grayscale.fx", "string");

        project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetCodeForInstance(renderTargetContainer, main, CreateMonoGame());

            // The runtime property is named identically, so a plain string assignment (verbatim
            // string literal, matching how Gum emits other string variables).
            code.ShouldContain("this.RenderTargetContainer.SourceShaderFile = @\"Shaders/Grayscale.fx\";");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetCodeForInstance_SourceShaderFileEmpty_EmitsNoAssignment()
    {
        GumProjectSave project = Project;
        RegisterContainerSourceShaderFileVariable();

        ComponentSave main = CreateComponent("MainComponent", "Container");

        InstanceSave renderTargetContainer = new InstanceSave
        {
            Name = "RenderTargetContainer",
            BaseType = "Container",
            ParentContainer = main,
        };
        main.Instances.Add(renderTargetContainer);
        AddVariable(main, "RenderTargetContainer.IsRenderTarget", true, "bool");
        // Empty value: no shader selected, so nothing should be assigned (and certainly not a
        // pointless "= \"\";" line).
        AddVariable(main, "RenderTargetContainer.SourceShaderFile", "", "string");

        project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetCodeForInstance(renderTargetContainer, main, CreateMonoGame());

            code.ShouldNotContain("SourceShaderFile");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
