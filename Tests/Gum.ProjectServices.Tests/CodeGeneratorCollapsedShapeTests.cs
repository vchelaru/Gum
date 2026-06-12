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
/// Tests for #2775: when the resolved runtime syntax version is &gt;= 2, MonoGame codegen emits the
/// collapsed two-slot shape runtimes (<c>CircleRuntime</c> / <c>RectangleRuntime</c>) for the legacy
/// <c>ColoredCircle</c> / <c>ColoredRectangle</c> / <c>RoundedRectangle</c> standard elements, and
/// translates their single-color variables to the fill/stroke channel properties. Versions below 2,
/// FindByName instantiation, and non-MonoGame output libraries keep the legacy emission.
/// </summary>
public class CodeGeneratorCollapsedShapeTests : BaseTestClass
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

    private static CodeOutputProjectSettings CreateMonoGameSettings(string syntaxVersion) => new CodeOutputProjectSettings
    {
        OutputLibrary = OutputLibrary.MonoGame,
        RootNamespace = "MyGame",
        SyntaxVersion = syntaxVersion,
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
    /// Mirrors the variables StandardElementsManager defines on the legacy shape standard elements.
    /// Codegen only emits an instance variable whose root is defined on the base element, so the
    /// base must declare every variable a test sets on an instance.
    /// </summary>
    private static void AddLegacyShapeVariables(ElementSave element, bool includeStrokeAndFill, bool includeCornerRadius)
    {
        AddVariable(element, "Red", 255, "int");
        AddVariable(element, "Green", 255, "int");
        AddVariable(element, "Blue", 255, "int");
        AddVariable(element, "Alpha", 255, "int");
        AddVariable(element, "Red1", 255, "int");
        AddVariable(element, "Green1", 255, "int");
        AddVariable(element, "Blue1", 255, "int");
        AddVariable(element, "Alpha1", 255, "int");

        if (includeStrokeAndFill)
        {
            AddVariable(element, "IsFilled", true, "bool");
            AddVariable(element, "StrokeWidth", 2.0f, "float");
        }

        if (includeCornerRadius)
        {
            AddVariable(element, "CornerRadius", 5.0f, "float");
        }
    }

    private StandardElementSave AddColoredCircleStandard()
    {
        StandardElementSave element = new StandardElementSave { Name = "ColoredCircle" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(defaultState);
        AddLegacyShapeVariables(element, includeStrokeAndFill: true, includeCornerRadius: false);
        Project.StandardElements.Add(element);
        return element;
    }

    private StandardElementSave AddRoundedRectangleStandard()
    {
        StandardElementSave element = new StandardElementSave { Name = "RoundedRectangle" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(defaultState);
        AddLegacyShapeVariables(element, includeStrokeAndFill: true, includeCornerRadius: true);
        Project.StandardElements.Add(element);
        return element;
    }

    private void AddColoredRectangleVariables()
    {
        StandardElementSave element = Project.StandardElements.First(item => item.Name == "ColoredRectangle");
        AddLegacyShapeVariables(element, includeStrokeAndFill: false, includeCornerRadius: false);
    }

    /// <summary>
    /// Builds a component containing a single instance of the given legacy shape type and returns
    /// the generated code for that instance.
    /// </summary>
    private string GenerateInstanceCode(
        string shapeBaseType,
        string instanceName,
        CodeOutputProjectSettings settings,
        params (string variableName, object value, string type)[] instanceVariables)
    {
        ComponentSave main = CreateComponent("MainComponent", "Container");

        InstanceSave shapeInstance = new InstanceSave
        {
            Name = instanceName,
            BaseType = shapeBaseType,
            ParentContainer = main,
        };
        main.Instances.Add(shapeInstance);

        foreach ((string variableName, object value, string type) in instanceVariables)
        {
            AddVariable(main, $"{instanceName}.{variableName}", value, type);
        }

        Project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            return CreateCodeGenerator().GetCodeForInstance(shapeInstance, main, settings);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_FindByNameV2_KeepsLegacyRuntime()
    {
        AddColoredCircleStandard();
        CodeOutputProjectSettings settings = CreateMonoGameSettings("2");
        settings.ObjectInstantiationType = ObjectInstantiationType.FindByName;

        string code = GenerateInstanceCode("ColoredCircle", "CircleInstance", settings);

        code.ShouldContain("new global::Gum.GueDeriving.ColoredCircleRuntime();");
        code.ShouldNotContain("new global::Gum.GueDeriving.CircleRuntime();");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_SkiaV2_KeepsLegacyRuntime()
    {
        AddColoredCircleStandard();
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.Skia,
            RootNamespace = "MyGame",
            SyntaxVersion = "2",
        };

        string code = GenerateInstanceCode("ColoredCircle", "CircleInstance", settings);

        code.ShouldContain("new global::Gum.GueDeriving.ColoredCircleRuntime();");
        code.ShouldNotContain("new global::Gum.GueDeriving.CircleRuntime();");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_V1_KeepsLegacyRuntimeAndColorVariables()
    {
        AddColoredCircleStandard();

        string code = GenerateInstanceCode(
            "ColoredCircle",
            "CircleInstance",
            CreateMonoGameSettings("1"),
            ("Red", 10, "int"));

        code.ShouldContain("new global::Gum.GueDeriving.ColoredCircleRuntime();");
        code.ShouldContain("CircleInstance.Red = 10;");
        code.ShouldNotContain("CircleInstance.FillRed");
        code.ShouldNotContain("CircleInstance.IsFilled = true;");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_V2_DropsGradientStartColor()
    {
        AddColoredCircleStandard();

        string code = GenerateInstanceCode(
            "ColoredCircle",
            "CircleInstance",
            CreateMonoGameSettings("2"),
            ("Red1", 100, "int"),
            ("Alpha1", 200, "int"));

        code.ShouldNotContain("Red1");
        code.ShouldNotContain("Alpha1");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_V2_EmitsCircleRuntime()
    {
        AddColoredCircleStandard();

        string code = GenerateInstanceCode("ColoredCircle", "CircleInstance", CreateMonoGameSettings("2"));

        code.ShouldContain("public CircleRuntime CircleInstance");
        code.ShouldContain("new global::Gum.GueDeriving.CircleRuntime();");
        code.ShouldNotContain("ColoredCircleRuntime");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_V2_EmitsFilledBaselineBlock()
    {
        AddColoredCircleStandard();

        string code = GenerateInstanceCode("ColoredCircle", "CircleInstance", CreateMonoGameSettings("2"));

        code.ShouldContain("CircleInstance.IsFilled = true;");
        code.ShouldContain("CircleInstance.StrokeWidth = 0f;");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_V2_Filled_DropsStrokeWidth()
    {
        AddColoredCircleStandard();

        string code = GenerateInstanceCode(
            "ColoredCircle",
            "CircleInstance",
            CreateMonoGameSettings("2"),
            ("StrokeWidth", 5.0f, "float"));

        code.ShouldNotContain("StrokeWidth = 5");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_V2_Filled_RoutesColorChannelsToFill()
    {
        AddColoredCircleStandard();

        string code = GenerateInstanceCode(
            "ColoredCircle",
            "CircleInstance",
            CreateMonoGameSettings("2"),
            ("Red", 10, "int"),
            ("Green", 20, "int"),
            ("Blue", 30, "int"),
            ("Alpha", 40, "int"));

        code.ShouldContain("CircleInstance.FillRed = 10;");
        code.ShouldContain("CircleInstance.FillGreen = 20;");
        code.ShouldContain("CircleInstance.FillBlue = 30;");
        code.ShouldContain("CircleInstance.FillAlpha = 40;");
        code.ShouldNotContain("CircleInstance.Red = 10;");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_V2_KeepsLegacyElementSaveLookup()
    {
        AddColoredCircleStandard();

        string code = GenerateInstanceCode("ColoredCircle", "CircleInstance", CreateMonoGameSettings("2"));

        // States and categories still come from the legacy .gumx standard element.
        code.ShouldContain("GetStandardElement(\"ColoredCircle\")");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_V2_NotFilled_KeepsStrokeWidth()
    {
        AddColoredCircleStandard();

        string code = GenerateInstanceCode(
            "ColoredCircle",
            "CircleInstance",
            CreateMonoGameSettings("2"),
            ("IsFilled", false, "bool"),
            ("StrokeWidth", 5.0f, "float"));

        code.ShouldContain("CircleInstance.IsFilled = false;");
        code.ShouldContain("StrokeWidth = 5");
    }

    [Fact]
    public void GetCodeForInstance_ColoredCircle_V2_NotFilled_RoutesColorChannelsToStroke()
    {
        AddColoredCircleStandard();

        string code = GenerateInstanceCode(
            "ColoredCircle",
            "CircleInstance",
            CreateMonoGameSettings("2"),
            ("IsFilled", false, "bool"),
            ("Red", 10, "int"));

        code.ShouldContain("CircleInstance.StrokeRed = 10;");
        code.ShouldNotContain("CircleInstance.FillRed");
        code.ShouldNotContain("CircleInstance.Red = 10;");
    }

    [Fact]
    public void GetCodeForInstance_ColoredRectangle_V2_EmitsRectangleRuntimeAndFillChannels()
    {
        AddColoredRectangleVariables();

        string code = GenerateInstanceCode(
            "ColoredRectangle",
            "RectangleInstance",
            CreateMonoGameSettings("2"),
            ("Red", 10, "int"));

        code.ShouldContain("new global::Gum.GueDeriving.RectangleRuntime();");
        code.ShouldNotContain("ColoredRectangleRuntime");
        code.ShouldContain("RectangleInstance.FillRed = 10;");
        code.ShouldContain("RectangleInstance.IsFilled = true;");
        code.ShouldContain("RectangleInstance.StrokeWidth = 0f;");
    }

    [Fact]
    public void GetCodeForInstance_RoundedRectangle_V2_CornerRadiusExplicit_PassesThrough()
    {
        AddRoundedRectangleStandard();

        string code = GenerateInstanceCode(
            "RoundedRectangle",
            "RoundedInstance",
            CreateMonoGameSettings("2"),
            ("CornerRadius", 8.0f, "float"));

        code.ShouldContain("RoundedInstance.CornerRadius = 8");
    }

    [Fact]
    public void GetCodeForInstance_RoundedRectangle_V2_EmitsCornerRadiusBaseline()
    {
        AddRoundedRectangleStandard();

        string code = GenerateInstanceCode("RoundedRectangle", "RoundedInstance", CreateMonoGameSettings("2"));

        // Legacy RoundedRectangle defaulted CornerRadius to 5; the collapsed RectangleRuntime
        // defaults to 0, so the baseline must carry the legacy default.
        code.ShouldContain("RoundedInstance.CornerRadius = 5f;");
    }

    [Fact]
    public void GetCodeForInstance_RoundedRectangle_V2_EmitsRectangleRuntime()
    {
        AddRoundedRectangleStandard();

        string code = GenerateInstanceCode("RoundedRectangle", "RoundedInstance", CreateMonoGameSettings("2"));

        code.ShouldContain("public RectangleRuntime RoundedInstance");
        code.ShouldContain("new global::Gum.GueDeriving.RectangleRuntime();");
        code.ShouldNotContain("RoundedRectangleRuntime");
    }

    [Fact]
    public void GetInheritance_ColoredCircle_V1_KeepsLegacyRuntime()
    {
        AddColoredCircleStandard();
        ObjectFinder.Self.GumProjectSave = Project;

        try
        {
            ComponentSave component = CreateComponent("MyShape", "ColoredCircle");

            string? result = CodeGenerator.GetInheritance(component, CreateMonoGameSettings("1"), resolvedSyntaxVersion: 1);

            result.ShouldBe("global::Gum.GueDeriving.ColoredCircleRuntime");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetInheritance_ColoredCircle_V2_FindByName_KeepsLegacyRuntime()
    {
        AddColoredCircleStandard();
        ObjectFinder.Self.GumProjectSave = Project;

        try
        {
            ComponentSave component = CreateComponent("MyShape", "ColoredCircle");
            CodeOutputProjectSettings settings = CreateMonoGameSettings("2");
            settings.ObjectInstantiationType = ObjectInstantiationType.FindByName;

            string? result = CodeGenerator.GetInheritance(component, settings, resolvedSyntaxVersion: 2);

            result.ShouldBe("global::Gum.GueDeriving.ColoredCircleRuntime");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetInheritance_ColoredCircle_V2_ReturnsCircleRuntime()
    {
        AddColoredCircleStandard();
        ObjectFinder.Self.GumProjectSave = Project;

        try
        {
            ComponentSave component = CreateComponent("MyShape", "ColoredCircle");

            string? result = CodeGenerator.GetInheritance(component, CreateMonoGameSettings("2"), resolvedSyntaxVersion: 2);

            result.ShouldBe("global::Gum.GueDeriving.CircleRuntime");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
