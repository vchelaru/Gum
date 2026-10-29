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
/// #7080: a saved shader parameter value (<c>Holder.Outline_Thickness</c>, marker <c>Thickness</c>) is no variable
/// the Container standard element declares, so the runtime only takes it through <c>SetProperty</c>. Codegen has to
/// emit it that way, or a code-only project loses what a loaded .gucx applies.
/// </summary>
public class CodeGeneratorShaderParameterValueTests : BaseTestClass
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

    /// <summary>Mirrors StandardElementsManager defining the shader variables on the Container standard element.</summary>
    private void RegisterContainerShaderVariables()
    {
        StandardElementSave container = Project.StandardElements.First(item => item.Name == "Container");
        container.DefaultState.Variables.Add(new VariableSave { Name = "SourceShaderFile", Value = "", SetsValue = true, Type = "string" });
        container.DefaultState.Variables.Add(new VariableSave { Name = "IsRenderTarget", Value = false, SetsValue = true, Type = "bool" });
    }

    private ComponentSave CreateComponent()
    {
        RegisterContainerShaderVariables();

        ComponentSave component = new ComponentSave { Name = "ShaderComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        InstanceSave holder = new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = component };
        component.Instances.Add(holder);
        component.DefaultState.SetValue("Holder.IsRenderTarget", true, holder, "bool");
        component.DefaultState.SetValue("Holder.SourceShaderFile", "Shaders/Outline.slang", holder, "string");
        Project.Components.Add(component);
        return component;
    }

    private static void AddShaderVariable(ComponentSave component, string name, string marker, object? value, string type, string? exposedAs = null)
    {
        component.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Holder." + name,
            Value = value,
            Type = type,
            ShaderParameter = marker,
            SetsValue = exposedAs == null,
            ExposedAsName = exposedAs,
        });
    }

    private string GetHolderCode(ComponentSave component, OutputLibrary library)
    {
        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            return CreateCodeGenerator().GetCodeForInstance(
                component.Instances[0],
                component,
                new CodeOutputProjectSettings { OutputLibrary = library, RootNamespace = "MyGame" });
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    private string GetComponentCode(ComponentSave component, OutputLibrary library, ObjectInstantiationType instantiationType)
    {
        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            return CreateCodeGenerator().GetGeneratedCodeForElement(
                component,
                elementSettings: null!,
                new CodeOutputProjectSettings
                {
                    OutputLibrary = library,
                    RootNamespace = "MyGame",
                    ObjectInstantiationType = instantiationType,
                });
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.GumForms)]
    [InlineData(OutputLibrary.Skia)]
    [InlineData(OutputLibrary.Raylib)]
    public void GetCodeForInstance_SavedScalarShaderParameter_EmitsSetPropertyWithTheSavedName(OutputLibrary library)
    {
        ComponentSave component = CreateComponent();
        AddShaderVariable(component, "Outline_Thickness", "Thickness", 3f, "float");

        string code = GetHolderCode(component, library);

        code.ShouldContain("this.Holder.SetProperty(\"Outline_Thickness\", 3f);");
        code.ShouldNotContain("Holder.Outline_Thickness =");
    }

    [Fact]
    public void GetCodeForInstance_SavedShaderParameter_StillEmitsTheFileAndRenderTargetFlag()
    {
        ComponentSave component = CreateComponent();
        AddShaderVariable(component, "Outline_Thickness", "Thickness", 3f, "float");

        string code = GetHolderCode(component, OutputLibrary.MonoGame);

        code.ShouldContain("this.Holder.SourceShaderFile = @\"Shaders/Outline.slang\";");
        code.ShouldContain("this.Holder.IsRenderTarget = true;");
    }

    [Fact]
    public void GetCodeForInstance_SavedVectorComponents_EmitOneSetPropertyEach()
    {
        ComponentSave component = CreateComponent();
        AddShaderVariable(component, "Outline_Tint__X", "Tint#0/4", 0.5f, "float");
        AddShaderVariable(component, "Outline_Tint__Y", "Tint#1/4", 0.25f, "float");

        string code = GetHolderCode(component, OutputLibrary.MonoGame);

        code.ShouldContain("this.Holder.SetProperty(\"Outline_Tint__X\", 0.5f);");
        code.ShouldContain("this.Holder.SetProperty(\"Outline_Tint__Y\", 0.25f);");
    }

    [Fact]
    public void GetCodeForInstance_SavedIntAndBoolShaderParameters_EmitTheirLiterals()
    {
        ComponentSave component = CreateComponent();
        AddShaderVariable(component, "Outline_Steps", "Steps", 4, "int");
        AddShaderVariable(component, "Outline_Glow", "Glow", true, "bool");

        string code = GetHolderCode(component, OutputLibrary.MonoGame);

        code.ShouldContain("this.Holder.SetProperty(\"Outline_Steps\", 4);");
        code.ShouldContain("this.Holder.SetProperty(\"Outline_Glow\", true);");
    }

    [Fact]
    public void GetCodeForInstance_ExposedShaderParameterWithNoValue_EmitsNoAssignment()
    {
        ComponentSave component = CreateComponent();
        AddShaderVariable(component, "Outline_Thickness", "Thickness", null, "float", exposedAs: "OutlineThickness");

        string code = GetHolderCode(component, OutputLibrary.MonoGame);

        code.ShouldNotContain("Outline_Thickness");
    }

    [Fact]
    public void FullyInCode_Component_ApplyDefaultVariablesSetsTheFileAndTheSavedParameter()
    {
        ComponentSave component = CreateComponent();
        AddShaderVariable(component, "Outline_Thickness", "Thickness", 3f, "float");

        string code = GetComponentCode(component, OutputLibrary.GumForms, ObjectInstantiationType.FullyInCode);

        code.ShouldContain("this.Holder.SourceShaderFile = @\"Shaders/Outline.slang\";");
        code.ShouldContain("this.Holder.SetProperty(\"Outline_Thickness\", 3f);");
    }

    [Fact]
    public void FullyInCode_ComponentWithExposedAndPinnedParameters_EmitsPinnedValueAndExposedProperty()
    {
        ComponentSave component = CreateComponent();
        AddShaderVariable(component, "Outline_Thickness", "Thickness", 3f, "float");
        AddShaderVariable(component, "Outline_Steps", "Steps", null, "int", exposedAs: "OutlineSteps");

        string code = GetComponentCode(component, OutputLibrary.GumForms, ObjectInstantiationType.FullyInCode);

        code.ShouldContain("this.Holder.SetProperty(\"Outline_Thickness\", 3f);");
        code.ShouldContain("set => Holder.SetProperty(\"Outline_Steps\", value);");
    }
}
