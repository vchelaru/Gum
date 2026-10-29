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
/// A component that exposes a shader parameter (#7010 slice 6) generates a property that forwards to the inner
/// container's shader parameter, and a screen that sets the exposed name generates the assignment.
/// </summary>
public class CodeGeneratorExposedShaderParameterTests : BaseTestClass
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

        return new CodeGenerator(codeGenNameVerifier, new LocalizationService(), elementSettingsManager, directoryProvider);
    }

    private string Generate(bool forScreen)
    {
        GumProjectSave project = Project;

        ComponentSave component = new ComponentSave { Name = "ShaderComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = component });
        component.DefaultState.SetValue("Holder.SourceShaderFile", "Shaders/Outline.slang");
        component.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Holder.Outline_Thickness",
            Type = "float",
            ShaderParameter = "Thickness",
            SetsValue = false,
            ExposedAsName = "OutlineThickness",
        });
        project.Components.Add(component);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Comp1", BaseType = "ShaderComponent", ParentContainer = screen });
        screen.DefaultState.SetValue("Comp1.OutlineThickness", 5f, screen.Instances[0], "float");
        project.Screens.Add(screen);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            return CreateCodeGenerator().GetGeneratedCodeForElement(
                forScreen ? screen : component,
                elementSettings: null!,
                new CodeOutputProjectSettings
                {
                    OutputLibrary = OutputLibrary.MonoGame,
                    RootNamespace = "MyGame",
                    ObjectInstantiationType = forScreen ? ObjectInstantiationType.FullyInCode : ObjectInstantiationType.FindByName,
                });
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void TheComponent_GeneratesAPropertyForwardingToTheInnerShaderParameter()
    {
        string code = Generate(forScreen: false);

        code.ShouldContain("public float OutlineThickness");
        // The runtime has no Outline_Thickness property, so the exposed property goes through SetProperty and has no getter.
        code.ShouldContain("set => Holder.SetProperty(\"Outline_Thickness\", value);");
        code.ShouldNotContain("get => Holder.Outline_Thickness");
        code.ShouldNotContain("Holder.Outline_Thickness =");
    }

    [Fact]
    public void TheScreen_AssignsTheExposedName()
    {
        string code = Generate(forScreen: true);

        code.ShouldContain("Comp1.OutlineThickness = 5");
    }
}
