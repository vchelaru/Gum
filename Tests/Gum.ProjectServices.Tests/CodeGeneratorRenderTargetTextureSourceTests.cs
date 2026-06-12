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
/// Tests for #3035: a Sprite's <c>RenderTargetTextureSource</c> variable stores the name of a
/// sibling render-target Container instance. Codegen must emit a direct object-reference assignment
/// to that instance's generated field (mirroring how the <c>Parent</c> variable resolves a sibling
/// instance), not a string literal — <c>RenderTargetTextureSource</c> is an <c>IRenderableIpso</c>.
/// </summary>
public class CodeGeneratorRenderTargetTextureSourceTests : BaseTestClass
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
    /// Mirrors StandardElementsManager defining RenderTargetTextureSource on the Sprite standard
    /// element. Codegen only emits an instance variable whose root is defined on the base element.
    /// </summary>
    private void RegisterSpriteRenderTargetTextureSourceVariable()
    {
        StandardElementSave sprite = Project.StandardElements.First(item => item.Name == "Sprite");
        AddVariable(sprite, "RenderTargetTextureSource", null, "string");
    }

    [Fact]
    public void GetCodeForInstance_RenderTargetTextureSource_EmitsSiblingInstanceReference()
    {
        GumProjectSave project = Project;
        RegisterSpriteRenderTargetTextureSourceVariable();

        ComponentSave main = CreateComponent("MainComponent", "Container");

        InstanceSave renderTargetContainer = new InstanceSave
        {
            Name = "RenderTargetContainer",
            BaseType = "Container",
            ParentContainer = main,
        };
        main.Instances.Add(renderTargetContainer);
        AddVariable(main, "RenderTargetContainer.IsRenderTarget", true, "bool");

        InstanceSave spriteInstance = new InstanceSave
        {
            Name = "SpriteInstance",
            BaseType = "Sprite",
            ParentContainer = main,
        };
        main.Instances.Add(spriteInstance);
        AddVariable(main, "SpriteInstance.RenderTargetTextureSource", "RenderTargetContainer", "string");

        project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetCodeForInstance(spriteInstance, main, CreateMonoGame());

            // Direct object reference to the sibling instance field, not a string literal.
            code.ShouldContain("this.SpriteInstance.RenderTargetTextureSource = this.RenderTargetContainer;");
            code.ShouldNotContain("RenderTargetTextureSource = \"RenderTargetContainer\"");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetCodeForInstance_RenderTargetTextureSourceEmpty_EmitsNoAssignment()
    {
        GumProjectSave project = Project;
        RegisterSpriteRenderTargetTextureSourceVariable();

        ComponentSave main = CreateComponent("MainComponent", "Container");

        InstanceSave spriteInstance = new InstanceSave
        {
            Name = "SpriteInstance",
            BaseType = "Sprite",
            ParentContainer = main,
        };
        main.Instances.Add(spriteInstance);
        // Empty value: the sprite uses no render target, so nothing should be assigned (and
        // certainly not a broken "= ;" line).
        AddVariable(main, "SpriteInstance.RenderTargetTextureSource", "", "string");

        project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetCodeForInstance(spriteInstance, main, CreateMonoGame());

            code.ShouldNotContain("RenderTargetTextureSource");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
