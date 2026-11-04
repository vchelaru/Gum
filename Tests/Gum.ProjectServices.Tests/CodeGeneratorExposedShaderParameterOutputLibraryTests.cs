using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #7010: an exposed shader parameter (a scalar, or the components of a vector) reaches generated code the same
/// way for every output library and both instantiation types, for the component that exposes it and the screen
/// that sets it. Raylib supports only FindByName, which assigns nothing on a screen, so it has no screen rows.
/// </summary>
public class CodeGeneratorExposedShaderParameterOutputLibraryTests : BaseTestClass
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

    private string Generate(bool forScreen, OutputLibrary library, ObjectInstantiationType instantiation, bool vector)
    {
        GumProjectSave project = Project;

        ComponentSave component = new ComponentSave { Name = "ShaderComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = component });
        component.DefaultState.SetValue("Holder.SourceShaderFile", "Shaders/Outline.slang");
        if (vector)
        {
            string[] letters = { "X", "Y" };
            for (int i = 0; i < letters.Length; i++)
            {
                component.DefaultState.Variables.Add(new VariableSave
                {
                    Name = "Holder.Outline_Tint__" + letters[i],
                    Type = "float",
                    ShaderParameter = $"Tint#{i}/2",
                    Value = 0.5f,
                    ExposedAsName = "Tint__" + letters[i],
                });
            }
        }
        else
        {
            component.DefaultState.Variables.Add(new VariableSave
            {
                Name = "Holder.Outline_Thickness",
                Type = "float",
                ShaderParameter = "Thickness",
                SetsValue = false,
                ExposedAsName = "OutlineThickness",
            });
        }
        project.Components.Add(component);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave comp1 = new InstanceSave { Name = "Comp1", BaseType = "ShaderComponent", ParentContainer = screen };
        screen.Instances.Add(comp1);
        if (vector)
        {
            screen.DefaultState.SetValue("Comp1.Tint__X", 0.9f, comp1, "float");
            screen.DefaultState.SetValue("Comp1.Tint__Y", 0.1f, comp1, "float");
        }
        else
        {
            screen.DefaultState.SetValue("Comp1.OutlineThickness", 5f, comp1, "float");
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
                    OutputLibrary = library,
                    RootNamespace = "MyGame",
                    ObjectInstantiationType = instantiation,
                });
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame, ObjectInstantiationType.FullyInCode)]
    [InlineData(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName)]
    [InlineData(OutputLibrary.GumForms, ObjectInstantiationType.FullyInCode)]
    [InlineData(OutputLibrary.GumForms, ObjectInstantiationType.FindByName)]
    [InlineData(OutputLibrary.Raylib, ObjectInstantiationType.FindByName)]
    [InlineData(OutputLibrary.Skia, ObjectInstantiationType.FullyInCode)]
    [InlineData(OutputLibrary.Skia, ObjectInstantiationType.FindByName)]
    public void TheComponent_ExposingAScalar_ForwardsToTheInnerParameter(OutputLibrary library, ObjectInstantiationType instantiation)
    {
        string code = Generate(forScreen: false, library, instantiation, vector: false);

        code.ShouldContain("public float OutlineThickness");
        code.ShouldContain("set => Holder.SetProperty(\"Outline_Thickness\", value);");
        code.ShouldNotContain("Holder.Outline_Thickness");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.GumForms)]
    [InlineData(OutputLibrary.Skia)]
    public void TheScreen_SettingAnExposedScalar_AssignsTheExposedName(OutputLibrary library)
    {
        string code = Generate(forScreen: true, library, ObjectInstantiationType.FullyInCode, vector: false);

        code.ShouldContain("Comp1.OutlineThickness = 5");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame, ObjectInstantiationType.FullyInCode)]
    [InlineData(OutputLibrary.MonoGame, ObjectInstantiationType.FindByName)]
    [InlineData(OutputLibrary.GumForms, ObjectInstantiationType.FullyInCode)]
    [InlineData(OutputLibrary.GumForms, ObjectInstantiationType.FindByName)]
    [InlineData(OutputLibrary.Raylib, ObjectInstantiationType.FindByName)]
    [InlineData(OutputLibrary.Skia, ObjectInstantiationType.FullyInCode)]
    [InlineData(OutputLibrary.Skia, ObjectInstantiationType.FindByName)]
    public void TheComponent_ExposingAVector_GeneratesOneVectorProperty(OutputLibrary library, ObjectInstantiationType instantiation)
    {
        string code = Generate(forScreen: false, library, instantiation, vector: true);

        code.ShouldContain("public global::System.Numerics.Vector2 Tint");
        code.ShouldContain("Holder.SetProperty(\"Outline_Tint__X\", value.X);");
        code.ShouldContain("Holder.SetProperty(\"Outline_Tint__Y\", value.Y);");
        code.ShouldNotContain("public float Tint__X");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.GumForms)]
    [InlineData(OutputLibrary.Skia)]
    public void TheScreen_SettingAnExposedVector_EmitsOneAssignment(OutputLibrary library)
    {
        string code = Generate(forScreen: true, library, ObjectInstantiationType.FullyInCode, vector: true);

        code.ShouldContain("Comp1.Tint = new global::System.Numerics.Vector2(0.9f, 0.1f);");
        code.ShouldNotContain("Comp1.Tint__X");
    }
}
