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
/// A float vector shader parameter exposed as <c>Tint__X</c>, <c>Tint__Y</c> (#7519) generates ONE write-only
/// System.Numerics vector property on the component and one assignment on a screen; the saved file still holds N
/// ordinary exposed floats.
/// </summary>
public class CodeGeneratorExposedShaderVectorTests : BaseTestClass
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

    private string Generate(bool forScreen, int exposedComponents = 2, bool screenSetsOnlyX = false)
    {
        GumProjectSave project = Project;

        ComponentSave component = new ComponentSave { Name = "ShaderComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = component });
        component.DefaultState.SetValue("Holder.SourceShaderFile", "Shaders/Outline.slang");
        string[] letters = { "X", "Y", "Z", "W" };
        float[] defaults = { 0.5f, 0.25f, 0.75f, 1f };
        for (int i = 0; i < exposedComponents; i++)
        {
            component.DefaultState.Variables.Add(new VariableSave
            {
                Name = "Holder.Outline_Tint__" + letters[i],
                Type = "float",
                ShaderParameter = $"Tint#{i}/2",
                Value = defaults[i],
                ExposedAsName = "Tint__" + letters[i],
            });
        }
        project.Components.Add(component);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave comp1 = new InstanceSave { Name = "Comp1", BaseType = "ShaderComponent", ParentContainer = screen };
        screen.Instances.Add(comp1);
        screen.DefaultState.SetValue("Comp1.Tint__X", 0.9f, comp1, "float");
        if (!screenSetsOnlyX)
        {
            screen.DefaultState.SetValue("Comp1.Tint__Y", 0.1f, comp1, "float");
        }
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
    public void TheComponent_GeneratesOneWriteOnlyVectorProperty()
    {
        string code = Generate(forScreen: false);

        code.ShouldContain("public global::System.Numerics.Vector2 Tint");
        code.ShouldContain("Holder.SetProperty(\"Outline_Tint__X\", value.X);");
        code.ShouldContain("Holder.SetProperty(\"Outline_Tint__Y\", value.Y);");
        code.ShouldNotContain("Tint__X\n");
        code.ShouldNotContain("public float Tint__X");
        code.ShouldNotContain("get =>");
    }

    [Fact]
    public void TheScreen_AssignsTheVectorOnce()
    {
        string code = Generate(forScreen: true);

        code.ShouldContain("Comp1.Tint = new global::System.Numerics.Vector2(0.9f, 0.1f);");
        code.ShouldNotContain("Comp1.Tint__X");
    }

    [Fact]
    public void TheScreen_SettingOnlyOneComponent_FillsTheRestFromTheExposedValue()
    {
        string code = Generate(forScreen: true, screenSetsOnlyX: true);

        code.ShouldContain("Comp1.Tint = new global::System.Numerics.Vector2(0.9f, 0.25f);");
    }

    [Fact]
    public void ALoneExposedComponent_StaysAnOrdinaryFloatProperty()
    {
        string code = Generate(forScreen: false, exposedComponents: 1);

        code.ShouldContain("public float Tint__X");
        code.ShouldNotContain("System.Numerics");
    }
}
