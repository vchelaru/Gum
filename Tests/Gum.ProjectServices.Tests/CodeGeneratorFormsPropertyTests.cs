using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #6312: a value authored for a behavior <see cref="BehaviorSave.FormsProperties"/> declaration (ToolTip,
/// LargeChange, IsEnabled, ...) on a Forms control instance, or in a component's own default state, must be
/// written into FullyInCode GumForms output as an assignment on the control. The runtime equivalent is
/// BehaviorFormsPropertyApplier; a declared default that nothing authored is already the code default of the
/// control, so it is not emitted.
/// </summary>
public class CodeGeneratorFormsPropertyTests : BaseTestClass
{
    public CodeGeneratorFormsPropertyTests()
    {
        // A variable the Container standard defines, so authored values for it are real visual variables.
        Project.StandardElements.First(item => item.Name == "Container").DefaultState!.Variables.Add(
            new VariableSave { Name = "Width", Type = "float", Value = 0f, SetsValue = true });
    }

    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);

        return new CodeGenerator(
            new CodeGenerationNameVerifier(mockNameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider);
    }

    private static CodeOutputProjectSettings CreateGumForms() => new CodeOutputProjectSettings
    {
        OutputLibrary = OutputLibrary.GumForms,
        ObjectInstantiationType = ObjectInstantiationType.FullyInCode,
        RootNamespace = "MyGame",
    };

    private static VariableSave Declare(string name, string type, object? defaultValue = null)
    {
        return new VariableSave { Name = name, Type = type, Value = defaultValue, SetsValue = defaultValue != null };
    }

    private BehaviorSave AddBehavior(string behaviorName, params VariableSave[] declarations)
    {
        BehaviorSave behavior = new BehaviorSave { Name = behaviorName };
        behavior.FormsProperties.AddRange(declarations);
        Project.Behaviors.Add(behavior);
        return behavior;
    }

    private ComponentSave AddComponent(string name, string baseType, string? behaviorName)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        if (behaviorName != null)
        {
            component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });
        }
        Project.Components.Add(component);
        return component;
    }

    private static InstanceSave AddInstance(ElementSave container, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = container };
        container.Instances.Add(instance);
        return instance;
    }

    private static void Author(ElementSave element, string name, string type, object value)
    {
        element.DefaultState!.Variables.Add(new VariableSave
        {
            Name = name,
            Type = type,
            Value = value,
            SetsValue = true,
        });
    }

    private string GenerateElement(ElementSave element, CodeOutputProjectSettings? settings = null)
    {
        ObjectFinder.Self.GumProjectSave = Project;
        return CreateCodeGenerator().GetGeneratedCodeForElement(
            element,
            new CodeOutputElementSettings(),
            settings ?? CreateGumForms());
    }

    private string GenerateInstance(InstanceSave instance, ElementSave container, CodeOutputProjectSettings? settings = null)
    {
        ObjectFinder.Self.GumProjectSave = Project;
        return CreateCodeGenerator().GetCodeForInstance(instance, container, settings ?? CreateGumForms());
    }

    /// <summary>A screen holding one instance of a component with the given Forms behavior.</summary>
    private (ScreenSave Screen, InstanceSave Instance) CreateScreen(string behaviorName, params VariableSave[] declarations)
    {
        AddBehavior(behaviorName, declarations);
        AddComponent("ControlStandard", "Container", behaviorName);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        Project.Screens.Add(screen);
        InstanceSave instance = AddInstance(screen, "ControlInstance", "ControlStandard");
        return (screen, instance);
    }

    private (ScreenSave Screen, InstanceSave Instance) CreateScrollViewerScreen(params VariableSave[] declarations) =>
        CreateScreen("ScrollViewerBehavior", declarations);

    [Fact]
    public void GetGeneratedCodeForElement_FormsPropertyDeclaredButNothingAuthored_EmitsNoAssignment()
    {
        // Pins that a project authoring nothing is untouched: the declared defaults equal the control's own.
        (ScreenSave screen, _) = CreateScrollViewerScreen(
            Declare("ToolTip", "string"),
            Declare("IsEnabled", "bool", true),
            Declare("LargeChange", "double", 10d));
        Author(screen, "ControlInstance.Width", "float", 100f);

        string code = GenerateElement(screen);

        code.ShouldContain("Width = 100");
        code.ShouldNotContain("ToolTip");
        code.ShouldNotContain("IsEnabled");
        code.ShouldNotContain("LargeChange");
    }

    [Fact]
    public void GetCodeForInstance_AuthoredFormsProperties_AreAssignedOnTheControl()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(
            Declare("ToolTip", "string"),
            Declare("IsEnabled", "bool", true),
            Declare("SmallChange", "double", 10d),
            Declare("LargeChange", "double", 10d));
        Author(screen, "ControlInstance.ToolTip", "string", "Scroll here");
        Author(screen, "ControlInstance.IsEnabled", "bool", false);
        Author(screen, "ControlInstance.SmallChange", "double", 7d);
        Author(screen, "ControlInstance.LargeChange", "double", 25d);

        string code = GenerateInstance(instance, screen);

        // Properties of the Forms control, so not through .Visual.
        code.ShouldContain("this.ControlInstance.ToolTip = @\"Scroll here\";");
        code.ShouldContain("this.ControlInstance.IsEnabled = false;");
        code.ShouldContain("this.ControlInstance.SmallChange = 7d;");
        code.ShouldContain("this.ControlInstance.LargeChange = 25d;");
        code.ShouldNotContain("Visual.LargeChange");
    }

    [Fact]
    public void GetGeneratedCodeForElement_AuthoredFormsProperties_AppearInApplyDefaultVariables()
    {
        (ScreenSave screen, _) = CreateScrollViewerScreen(Declare("LargeChange", "double", 10d));
        Author(screen, "ControlInstance.LargeChange", "double", 25d);

        string code = GenerateElement(screen);

        int applyIndex = code.IndexOf("private void ApplyDefaultVariables()", StringComparison.Ordinal);
        applyIndex.ShouldBeGreaterThan(-1);
        code.IndexOf("this.ControlInstance.LargeChange = 25d;", StringComparison.Ordinal).ShouldBeGreaterThan(applyIndex);
    }

    [Fact]
    public void GetCodeForInstance_FormsPropertiesFollowTheRegularVariables()
    {
        // Runtime order: the visual's variables first, then the Forms properties.
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(Declare("LargeChange", "double", 10d));
        Author(screen, "ControlInstance.LargeChange", "double", 25d);
        Author(screen, "ControlInstance.Width", "float", 100f);

        string code = GenerateInstance(instance, screen);

        code.IndexOf("Width = 100", StringComparison.Ordinal).ShouldBeGreaterThan(-1);
        code.IndexOf("LargeChange = 25d", StringComparison.Ordinal)
            .ShouldBeGreaterThan(code.IndexOf("Width = 100", StringComparison.Ordinal));
    }

    [Fact]
    public void GetCodeForInstance_NullableDeclaredNumber_AssignsPlainLiteral()
    {
        // A "double?" declaration targets a plain double property on the control; the literal must compile for both.
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(Declare("LargeChange", "double?", 10d));
        Author(screen, "ControlInstance.LargeChange", "double?", 25d);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.LargeChange = 25d;");
    }

    [Fact]
    public void GetCodeForInstance_TextBoxProperties_AreAssigned()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScreen(
            "TextBoxBehavior",
            Declare("MaxLength", "int?"),
            Declare("Placeholder", "string"),
            Declare("IsReadOnly", "bool", false),
            Declare("AcceptsReturn", "bool", false));
        Author(screen, "ControlInstance.MaxLength", "int?", 12);
        Author(screen, "ControlInstance.Placeholder", "string", "Type here");
        Author(screen, "ControlInstance.IsReadOnly", "bool", true);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.MaxLength = 12;");
        code.ShouldContain("this.ControlInstance.Placeholder = @\"Type here\";");
        code.ShouldContain("this.ControlInstance.IsReadOnly = true;");
        code.ShouldNotContain("AcceptsReturn");
    }

    [Fact]
    public void GetCodeForInstance_LiteralFollowsTheControlPropertyType_NotTheDeclaredType()
    {
        // StackPanel.Spacing is a float; a declaration saying double must not produce a double literal.
        (ScreenSave screen, InstanceSave instance) = CreateScreen("StackPanelBehavior", Declare("Spacing", "double", 0d));
        Author(screen, "ControlInstance.Spacing", "double", 4.5d);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.Spacing = 4.5f;");
    }

    [Fact]
    public void GetCodeForInstance_NameTheControlLacks_IsSkipped()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(
            Declare("NoSuchProperty", "double", 1d),
            Declare("LargeChange", "double", 10d));
        Author(screen, "ControlInstance.NoSuchProperty", "double", 5d);
        Author(screen, "ControlInstance.LargeChange", "double", 25d);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.LargeChange = 25d;");
        code.ShouldNotContain("NoSuchProperty");
    }

    [Theory]
    [InlineData(25f, "25d")]
    [InlineData(25, "25d")]
    [InlineData(2.5, "2.5d")]
    [InlineData("25", "25d")]
    public void GetCodeForInstance_NumberOfAnyStoredType_IsWrittenAsTheDeclaredType(object stored, string expected)
    {
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(Declare("LargeChange", "double", 10d));
        Author(screen, "ControlInstance.LargeChange", "double", stored);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain($"this.ControlInstance.LargeChange = {expected};");
    }

    [Fact]
    public void GetCodeForInstance_StringWithQuotes_IsEscaped()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(Declare("ToolTip", "string"));
        Author(screen, "ControlInstance.ToolTip", "string", "Say \"hi\"");

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.ToolTip = @\"Say \"\"hi\"\"\";");
    }

    [Theory]
    [InlineData("Wrap")]
    [InlineData(2)]
    public void GetCodeForInstance_EnumFormsProperty_IsWrittenAsQualifiedEnumMember(object stored)
    {
        // The value is a name or the stored int, and the enum comes from the control property's type.
        (ScreenSave screen, InstanceSave instance) = CreateScreen(
            "TextBoxBehavior",
            Declare("TextWrapping", "TextWrapping", "NoWrap"));
        Author(screen, "ControlInstance.TextWrapping", "TextWrapping", stored);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;");
    }

    [Fact]
    public void GetCodeForInstance_EnumInControlsNamespace_IsQualified()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(
            Declare("VerticalScrollBarVisibility", "ScrollBarVisibility", "Auto"));
        Author(screen, "ControlInstance.VerticalScrollBarVisibility", "ScrollBarVisibility", "Hidden");

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.VerticalScrollBarVisibility = global::Gum.Forms.Controls.ScrollBarVisibility.Hidden;");
    }

    [Fact]
    public void GetCodeForInstance_NullableEnumProperty_AssignsTheEnumMember()
    {
        // ItemsControl.Orientation is Orientation?.
        (ScreenSave screen, InstanceSave instance) = CreateScreen("ItemsControlBehavior", Declare("Orientation", "Orientation?"));
        Author(screen, "ControlInstance.Orientation", "Orientation?", "Horizontal");

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.Orientation = global::Gum.Forms.Controls.Orientation.Horizontal;");
    }

    [Fact]
    public void GetCodeForInstance_BoxedEnumValue_IsWrittenAsQualifiedEnumMember()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScreen(
            "TextBoxBehavior",
            Declare("TextWrapping", "TextWrapping", "NoWrap"));
        Author(screen, "ControlInstance.TextWrapping", "TextWrapping", Gum.Forms.TextWrapping.Wrap);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;");
    }

    [Fact]
    public void GetCodeForInstance_ValueThatDoesNotConvert_IsSkippedWithoutThrowing()
    {
        // Mirrors the runtime applier, which skips a value it cannot coerce.
        (ScreenSave screen, InstanceSave instance) = CreateScreen(
            "TextBoxBehavior",
            Declare("MaxLength", "int?"),
            Declare("TextWrapping", "TextWrapping", "NoWrap"),
            Declare("IsEnabled", "bool", true));
        Author(screen, "ControlInstance.MaxLength", "int?", "not a number");
        Author(screen, "ControlInstance.TextWrapping", "TextWrapping", "Sideways");
        Author(screen, "ControlInstance.IsEnabled", "bool", "maybe");

        string code = GenerateInstance(instance, screen);

        code.ShouldNotContain("MaxLength");
        code.ShouldNotContain("TextWrapping");
        code.ShouldNotContain("IsEnabled");
    }

    [Fact]
    public void GetCodeForInstance_DerivedComponent_InheritsDeclarationsFromBaseBehaviors()
    {
        (ScreenSave screen, _) = CreateScrollViewerScreen(Declare("LargeChange", "double", 10d));
        AddComponent("FancyScroll", "ControlStandard", behaviorName: null);
        InstanceSave fancy = AddInstance(screen, "FancyInstance", "FancyScroll");
        Author(screen, "FancyInstance.LargeChange", "double", 25d);

        string code = GenerateInstance(fancy, screen);

        code.ShouldContain("this.FancyInstance.LargeChange = 25d;");
    }

    [Fact]
    public void GetCodeForInstance_AuthoredNameNoBehaviorDeclares_IsNotEmitted()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(Declare("LargeChange", "double", 10d));
        Author(screen, "ControlInstance.NotDeclared", "double", 25d);

        string code = GenerateInstance(instance, screen);

        code.ShouldNotContain("NotDeclared");
    }

    [Fact]
    public void GetCodeForInstance_ComponentWithoutAMappedFormsBehavior_OnlyReachesFrameworkElementMembers()
    {
        // The generated class derives from FrameworkElement, which has ToolTip but no LargeChange.
        AddBehavior("SomeCustomBehavior", Declare("LargeChange", "double", 10d), Declare("ToolTip", "string"));
        AddComponent("PlainThing", "Container", "SomeCustomBehavior");
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        Project.Screens.Add(screen);
        InstanceSave instance = AddInstance(screen, "PlainInstance", "PlainThing");
        Author(screen, "PlainInstance.LargeChange", "double", 25d);
        Author(screen, "PlainInstance.ToolTip", "string", "tip");

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.PlainInstance.ToolTip = @\"tip\";");
        code.ShouldNotContain("LargeChange");
    }

    [Fact]
    public void GetCodeForInstance_LegacyMonoGameOutput_IsNotEmitted()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(Declare("LargeChange", "double", 10d));
        Author(screen, "ControlInstance.LargeChange", "double", 25d);

        string code = GenerateInstance(instance, screen, new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            RootNamespace = "MyGame",
        });

        code.ShouldNotContain("LargeChange");
    }

    [Fact]
    public void GetGeneratedCodeForElement_AuthoredOnComponentDefaultState_IsAssignedOnThis()
    {
        AddBehavior("ScrollViewerBehavior", Declare("LargeChange", "double", 10d), Declare("ToolTip", "string"));
        ComponentSave scroll = AddComponent("ControlStandard", "Container", "ScrollViewerBehavior");
        Author(scroll, "LargeChange", "double", 25d);
        Author(scroll, "ToolTip", "string", "tip");

        string code = GenerateElement(scroll);

        code.ShouldContain("this.LargeChange = 25d;");
        code.ShouldContain("this.ToolTip = @\"tip\";");
        code.ShouldNotContain("Visual.LargeChange");
    }

    [Fact]
    public void GetGeneratedCodeForElement_AuthoredOnDerivedComponentDefaultState_IsAssignedOnThis()
    {
        AddBehavior("ScrollViewerBehavior", Declare("LargeChange", "double", 10d));
        AddComponent("ControlStandard", "Container", "ScrollViewerBehavior");
        ComponentSave fancy = AddComponent("FancyScroll", "ControlStandard", behaviorName: null);
        Author(fancy, "LargeChange", "double", 25d);

        string code = GenerateElement(fancy);

        code.ShouldContain("this.LargeChange = 25d;");
    }

    [Fact]
    public void GetGeneratedCodeForElement_ComponentDeclaredButNothingAuthored_EmitsNoAssignment()
    {
        AddBehavior("ScrollViewerBehavior", Declare("LargeChange", "double", 10d), Declare("ToolTip", "string"));
        ComponentSave scroll = AddComponent("ControlStandard", "Container", "ScrollViewerBehavior");
        Author(scroll, "Width", "float", 100f);

        string code = GenerateElement(scroll);

        code.ShouldNotContain("LargeChange");
        code.ShouldNotContain("ToolTip");
    }

    public static IEnumerable<object[]> ShippedFormsProperties()
    {
        foreach (string path in Directory.GetFiles(Path.Combine(FindTemplatesDirectory(), "FormsBehaviors"), "*.behx"))
        {
            BehaviorSave behavior = GumFileSerializer.DeserializeBehaviorSave(File.ReadAllText(path), projectVersion: 2)!;
            ComponentSave probe = new ComponentSave();
            probe.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behavior.Name });
            CodeGenerator.GetGumFormsTypeFromBehaviors(probe, out string? formsType, out _);
            if (formsType == null)
            {
                // No control is generated for this behavior, so there is no property to assign.
                continue;
            }

            foreach (VariableSave declaration in behavior.FormsProperties)
            {
                yield return new object[] { behavior.Name, declaration.Name, declaration.Type };
            }
        }
    }

    [Theory]
    [MemberData(nameof(ShippedFormsProperties))]
    public void GetCodeForInstance_EveryShippedFormsProperty_IsAssignedWhenAuthored(
        string behaviorName, string propertyName, string declaredType)
    {
        (ScreenSave screen, InstanceSave instance) = CreateScreen(behaviorName, Declare(propertyName, declaredType));
        Author(screen, "ControlInstance." + propertyName, declaredType, SampleValue(declaredType));

        string code = GenerateInstance(instance, screen);

        code.ShouldContain($"this.ControlInstance.{propertyName} = ", Case.Sensitive);
    }

    [Fact]
    public void GetCodeForInstance_PropertyAlsoAuthoredInTheComponentDefaultState_IsAssignedOnTheControl()
    {
        // The shipped CheckBox component authors IsChecked itself. That makes it a variable of the component,
        // but not one of the Visual, so an instance's value must not go through .Visual.
        (ScreenSave screen, InstanceSave instance) = CreateScreen("CheckBoxBehavior", Declare("IsChecked", "bool", false));
        ComponentSave component = Project.Components.First(item => item.Name == "ControlStandard");
        Author(component, "IsChecked", "bool", false);
        Author(screen, "ControlInstance.IsChecked", "bool", true);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.IsChecked = true;", Case.Sensitive);
        code.ShouldNotContain("Visual.IsChecked");
    }

    [Fact]
    public void GetCodeForInstance_PropertyAuthoredOnInnerInstanceOverBaseDefault_IsAssignedOnTheControl()
    {
        (ScreenSave screen, _) = CreateScrollViewerScreen(Declare("ToolTip", "string"), Declare("LargeChange", "double", 10d));
        ComponentSave scroll = Project.Components.First(item => item.Name == "ControlStandard");
        Author(scroll, "ToolTip", "string", "base");
        ComponentSave holder = AddComponent("Holder", "Container", behaviorName: null);
        InstanceSave inner = AddInstance(holder, "OwnInst", "ControlStandard");
        Author(holder, "OwnInst.ToolTip", "string", "inner");

        string code = GenerateInstance(inner, holder);

        code.ShouldContain("this.OwnInst.ToolTip = @\"inner\";", Case.Sensitive);
        code.ShouldNotContain("Visual.ToolTip");
    }

    [Fact]
    public void GetGeneratedCodeForElement_DerivedComponentOverridingAuthoredBase_IsAssignedOnThis()
    {
        AddBehavior("ScrollViewerBehavior", Declare("LargeChange", "double", 10d));
        ComponentSave scroll = AddComponent("ScrollViewerStandard", "Container", "ScrollViewerBehavior");
        Author(scroll, "LargeChange", "double", 30d);
        ComponentSave derived = AddComponent("Derived", "ScrollViewerStandard", behaviorName: null);
        Author(derived, "LargeChange", "double", 44d);
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        Project.Screens.Add(screen);
        InstanceSave instance = AddInstance(screen, "DerivedInstance", "Derived");
        Author(screen, "DerivedInstance.LargeChange", "double", 77d);

        string derivedCode = GenerateElement(derived);
        string instanceCode = GenerateInstance(instance, screen);

        derivedCode.ShouldContain("this.LargeChange = 44d;", Case.Sensitive);
        derivedCode.ShouldNotContain("Visual.LargeChange");
        instanceCode.ShouldContain("this.DerivedInstance.LargeChange = 77d;", Case.Sensitive);
        instanceCode.ShouldNotContain("Visual.LargeChange");
    }

    [Fact]
    public void GetGeneratedCodeForElement_StateSettingAuthoredProperty_DoesNotGoThroughVisual()
    {
        (ScreenSave screen, _) = CreateScrollViewerScreen(Declare("ToolTip", "string"));
        ComponentSave scroll = Project.Components.First(item => item.Name == "ControlStandard");
        Author(scroll, "ToolTip", "string", "base");
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave state = new StateSave { Name = "S1", ParentContainer = screen };
        state.Variables.Add(new VariableSave
        {
            Name = "ControlInstance.ToolTip",
            Type = "string",
            Value = "in state",
            SetsValue = true,
        });
        category.States.Add(state);
        screen.Categories.Add(category);

        string code = GenerateElement(screen);

        code.ShouldNotContain("Visual.ToolTip");
    }

    [Fact]
    public void GetCodeForInstance_NameThatIsAlsoAVisualVariable_StaysOnTheVisualOnce()
    {
        // Width is a Container variable (see the constructor) and also a FrameworkElement property. The
        // existing pass keeps owning it, so it is neither dropped nor assigned twice.
        (ScreenSave screen, InstanceSave instance) = CreateScreen("PanelBehavior", Declare("Width", "double"));
        Author(screen, "ControlInstance.Width", "float", 100f);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("Visual.Width = 100f;", Case.Sensitive);
        code.ShouldNotContain("this.ControlInstance.Width = ");
        System.Text.RegularExpressions.Regex.Matches(code, "Width = 100").Count.ShouldBe(1);
    }

    [Fact]
    public void GetCodeForInstance_NameExposedByTheComponent_StaysOnTheExposedProperty()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(Declare("LargeChange", "double", 10d));
        ComponentSave scroll = Project.Components.First(item => item.Name == "ControlStandard");
        AddInstance(scroll, "Inner", "Container");
        scroll.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Inner.Width",
            Type = "float",
            SetsValue = true,
            ExposedAsName = "LargeChange",
        });
        Author(screen, "ControlInstance.LargeChange", "float", 25f);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain("this.ControlInstance.LargeChange = 25f;", Case.Sensitive);
        code.ShouldNotContain("Visual.LargeChange");
        code.ShouldNotContain("LargeChange = 25d");
    }

    [Fact]
    public void GetCodeForInstance_NonFiniteNumbers_AreWrittenAsNamedConstants()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScreen(
            "StackPanelBehavior", Declare("Spacing", "float", 0f));
        Author(screen, "ControlInstance.Spacing", "float", float.NaN);
        string spacing = GenerateInstance(instance, screen);

        spacing.ShouldContain("this.ControlInstance.Spacing = float.NaN;", Case.Sensitive);
    }

    [Theory]
    [InlineData(double.NaN, "double.NaN")]
    [InlineData(double.PositiveInfinity, "double.PositiveInfinity")]
    [InlineData(double.NegativeInfinity, "double.NegativeInfinity")]
    public void GetCodeForInstance_NonFiniteDouble_IsWrittenAsNamedConstant(double stored, string expected)
    {
        (ScreenSave screen, InstanceSave instance) = CreateScrollViewerScreen(Declare("LargeChange", "double", 10d));
        Author(screen, "ControlInstance.LargeChange", "double", stored);

        string code = GenerateInstance(instance, screen);

        code.ShouldContain($"this.ControlInstance.LargeChange = {expected};", Case.Sensitive);
    }

    [Theory]
    [InlineData("PanelBehavior", "ToolTip", "string", "custom tip", "this.ToolTip = @\"custom tip\";")]
    [InlineData("ScrollViewerBehavior", "LargeChange", "double", 25d, "this.LargeChange = 25;")]
    [InlineData("StackPanelBehavior", "Spacing", "float", 4.5f, "this.Spacing = 4.5f;")]
    public void GetGeneratedCodeForElement_CustomVariableNamedLikeAFormsProperty_IsAssignedOnce(
        string behaviorName, string name, string type, object value, string expectedLine)
    {
        // A custom variable is assigned by its own pass; the Forms property pass must leave it alone.
        AddBehavior(behaviorName, Declare(name, type));
        ComponentSave component = AddComponent("ControlStandard", "Container", behaviorName);
        component.DefaultState!.Variables.Add(new VariableSave
        {
            Name = name,
            Type = type,
            Value = value,
            SetsValue = true,
            IsCustomVariable = true,
        });

        string code = GenerateElement(component);

        code.ShouldContain(expectedLine, Case.Sensitive);
        System.Text.RegularExpressions.Regex.Matches(code, "this\\." + name + " = ").Count.ShouldBe(1);
    }

    [Fact]
    public void GetCodeForInstance_CustomVariableNamedLikeAFormsProperty_IsAssignedOnce()
    {
        (ScreenSave screen, InstanceSave instance) = CreateScreen("PanelBehavior", Declare("ToolTip", "string"));
        ComponentSave component = Project.Components.First(item => item.Name == "ControlStandard");
        component.DefaultState!.Variables.Add(new VariableSave
        {
            Name = "ToolTip",
            Type = "string",
            Value = "custom tip",
            SetsValue = true,
            IsCustomVariable = true,
        });
        Author(screen, "ControlInstance.ToolTip", "string", "instance tip");

        string code = GenerateInstance(instance, screen);

        System.Text.RegularExpressions.Regex.Matches(code, "ToolTip = ").Count.ShouldBe(1);
        code.ShouldContain("ToolTip = @\"instance tip\";", Case.Sensitive);
    }

    [Fact]
    public void GetGeneratedCodeForElement_MeadowScrollViewerAndScrollBarInstances_AssignAuthoredFormsProperties()
    {
        // The #6312 repro on a real theme and the shipped behaviors: values authored on a ScrollViewer and
        // a ScrollBar instance reach the generated code.
        ProjectLoadResult result = new ProjectLoader().Load(
            Path.Combine(FindTemplatesDirectory(), "FormsThemes", "Meadow", "GumProject.gumx"));
        result.Success.ShouldBeTrue();
        GumProjectSave meadow = result.Project!;
        foreach (string path in Directory.GetFiles(Path.Combine(FindTemplatesDirectory(), "FormsBehaviors"), "*.behx"))
        {
            meadow.Behaviors.Add(GumFileSerializer.DeserializeBehaviorSave(File.ReadAllText(path), projectVersion: 2)!);
        }
        ObjectFinder.Self.GumProjectSave = meadow;

        ScreenSave screen = new ScreenSave { Name = "DemoScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        AddInstance(screen, "ScrollViewerInstance", "Meadow/Controls/ScrollViewer");
        AddInstance(screen, "ScrollBarInstance", "Meadow/Controls/ScrollBar");
        AddInstance(screen, "CheckInstance", "Meadow/Controls/CheckBox");
        Author(screen, "ScrollViewerInstance.LargeChange", "double", 25d);
        Author(screen, "ScrollViewerInstance.SmallChange", "double", 7d);
        Author(screen, "ScrollBarInstance.ToolTip", "string", "Drag me");
        Author(screen, "ScrollBarInstance.LargeChange", "double", 30d);
        Author(screen, "CheckInstance.IsChecked", "bool", true);

        string code = CreateCodeGenerator().GetGeneratedCodeForElement(screen, new CodeOutputElementSettings(), CreateGumForms());

        code.ShouldContain("this.ScrollViewerInstance.LargeChange = 25d;", Case.Sensitive);
        code.ShouldContain("this.ScrollViewerInstance.SmallChange = 7d;", Case.Sensitive);
        code.ShouldContain("this.ScrollBarInstance.ToolTip = @\"Drag me\";", Case.Sensitive);
        code.ShouldContain("this.ScrollBarInstance.LargeChange = 30d;", Case.Sensitive);
        code.ShouldContain("this.CheckInstance.IsChecked = true;", Case.Sensitive);
        code.ShouldNotContain("Visual.IsChecked");
    }

    private static object SampleValue(string declaredType)
    {
        string type = declaredType.TrimEnd('?');
        switch (type)
        {
            case "bool":
                return true;
            case "string":
                return "sample";
            case "int":
            case "long":
            case "float":
            case "double":
                return 3;
            default:
                Type enumType = typeof(Gum.Forms.Controls.FrameworkElement).Assembly.GetTypes()
                    .First(item => item.IsEnum && item.Name == type);
                return Enum.GetNames(enumType)[0];
        }
    }

    private static string FindTemplatesDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 12; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = Path.GetDirectoryName(current) ?? throw new InvalidOperationException("Templates directory not found");
        }
        throw new InvalidOperationException("Templates directory not found");
    }
}
