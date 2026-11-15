using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Issue #7200: an element flagged Debug Only has its generated file wrapped in #if DEBUG, and every
/// generated reference to it from another element is wrapped too, so a release build compiles without it.
/// </summary>
public class CodeGeneratorDebugOnlyTests : BaseTestClass
{
    private readonly string _projectDirectory;
    private readonly CodeOutputElementSettingsManager _settingsManager;

    public CodeGeneratorDebugOnlyTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumDebugOnlyTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Components", "Debugging"));
        Directory.CreateDirectory(Path.Combine(_projectDirectory, "Screens"));
        _settingsManager = new CodeOutputElementSettingsManager(new FixedProjectDirectoryProvider(_projectDirectory + Path.DirectorySeparatorChar));
    }

    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }

    private CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(_projectDirectory + Path.DirectorySeparatorChar);
        return new CodeGenerator(new CodeGenerationNameVerifier(mockNameVerifier.Object), new LocalizationService(), _settingsManager, directoryProvider);
    }

    private ComponentSave AddComponent(string name, string baseType = "Container", bool debugOnly = false)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
        if (debugOnly)
        {
            _settingsManager.WriteSettingsForElement(component, new CodeOutputElementSettings { DebugOnly = true });
        }
        return component;
    }

    private ScreenSave AddScreenWithInstance(string instanceName, string instanceBaseType)
    {
        ScreenSave screen = new ScreenSave { Name = "Host" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = instanceBaseType, ParentContainer = screen });
        screen.DefaultState.SetValue(instanceName + ".Width", 12f, screen.Instances[0], "float");
        Project.Screens.Add(screen);
        return screen;
    }

    private string Generate(ElementSave element, ObjectInstantiationType instantiation = ObjectInstantiationType.FullyInCode)
    {
        ObjectFinder.Self.GumProjectSave = Project;
        CodeOutputElementSettings settings = _settingsManager.LoadOrCreateSettingsFor(element);
        return CreateCodeGenerator().GetGeneratedFileText(element, settings, new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.GumForms,
            RootNamespace = "MyGame",
            ObjectInstantiationType = instantiation,
        });
    }

    private static int Count(string text, string find) =>
        text.Split(find).Length - 1;

    /// <summary>The lines mentioning <paramref name="needle"/> that are not between a #if DEBUG and its #endif.</summary>
    private static List<string> UnguardedLinesMentioning(string code, string needle)
    {
        List<string> unguarded = new List<string>();
        int depth = 0;
        foreach (string line in code.Split("\r\n"))
        {
            if (line.StartsWith("#if DEBUG"))
            {
                depth++;
            }
            else if (line.StartsWith("#endif"))
            {
                depth--;
            }
            else if (depth == 0 && line.Contains(needle))
            {
                unguarded.Add(line);
            }
        }
        return unguarded;
    }

    [Fact]
    public void NonDebugElement_ReferencingNothingDebug_HasNoDebugDirectives()
    {
        AddComponent("Widget");
        ScreenSave screen = AddScreenWithInstance("W", "Widget");

        string code = Generate(screen);

        code.ShouldNotContain("#if DEBUG");
        code.ShouldContain("W = new MyGame.Components.Widget();");
    }

    [Fact]
    public void DebugElement_WholeGeneratedFile_IsWrappedAfterTheHeaderLine()
    {
        ComponentSave widget = AddComponent("Widget", debugOnly: true);

        string code = Generate(widget);

        code.ShouldStartWith("//Code for Widget (Container)\r\n#if DEBUG\r\n");
        code.TrimEnd().ShouldEndWith("#endif");
        Count(code, "#if DEBUG").ShouldBe(1);
    }

    [Theory]
    [InlineData(ObjectInstantiationType.FullyInCode)]
    [InlineData(ObjectInstantiationType.FindByName)]
    public void InstanceOfDebugElement_EveryReferenceIsWrapped(ObjectInstantiationType instantiation)
    {
        AddComponent("Widget", debugOnly: true);
        ScreenSave screen = AddScreenWithInstance("W", "Widget");

        string code = Generate(screen, instantiation);

        code.ShouldNotStartWith("//Code for Host\r\n#if DEBUG");
        Count(code, "#if DEBUG").ShouldBeGreaterThan(2);
        UnguardedLinesMentioning(code, " W {").ShouldBeEmpty();
        UnguardedLinesMentioning(code, "W = ").ShouldBeEmpty();
        UnguardedLinesMentioning(code, "(W)").ShouldBeEmpty();
        UnguardedLinesMentioning(code, "<Widget>").ShouldBeEmpty();
    }

    [Fact]
    public void InstanceOfDebugElement_FullyInCode_WrapsDeclarationCreationAndParenting()
    {
        AddComponent("Widget", debugOnly: true);
        ScreenSave screen = AddScreenWithInstance("W", "Widget");

        string code = Generate(screen);

        code.ShouldContain("#if DEBUG\r\n    public Widget W { get; protected set; }\r\n#endif");
        code.ShouldContain("#if DEBUG\r\n        W = new MyGame.Components.Widget();\r\n        W.Name = \"W\";\r\n#endif");
        code.ShouldContain("#if DEBUG\r\n        this.AddChild(W);\r\n#endif");
    }

    [Fact]
    public void InstanceOfDebugElement_VariableAssignedOnIt_IsWrapped()
    {
        AddComponent("Widget", debugOnly: true);
        ScreenSave screen = AddScreenWithInstance("W", "Widget");
        Project.Components[0].DefaultState.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 5f, SetsValue = true });
        screen.DefaultState.SetValue("W.Width", 12f, screen.Instances[0], "float");

        string code = Generate(screen);

        code.ShouldContain("W.Visual.Width = 12f;");
        UnguardedLinesMentioning(code, "W.Visual").ShouldBeEmpty();
    }

    [Fact]
    public void InstanceOfNonDebugElement_NextToADebugOne_IsNotWrapped()
    {
        AddComponent("Widget", debugOnly: true);
        AddComponent("Plain");
        ScreenSave screen = AddScreenWithInstance("W", "Widget");
        screen.Instances.Add(new InstanceSave { Name = "P", BaseType = "Plain", ParentContainer = screen });

        string code = Generate(screen);

        UnguardedLinesMentioning(code, " P {").ShouldNotBeEmpty();
        UnguardedLinesMentioning(code, " W {").ShouldBeEmpty();
        code.ShouldNotContain("#if DEBUG\r\n    public Plain P");
    }

    [Fact]
    public void ComponentDerivedFromDebugElement_IsWrappedBecauseItCannotExistWithoutItsBase()
    {
        AddComponent("Widget", debugOnly: true);
        ComponentSave derived = AddComponent("Fancy", baseType: "Widget");

        string code = Generate(derived);

        code.ShouldStartWith("//Code for Fancy (Widget)\r\n#if DEBUG\r\n");
        code.TrimEnd().ShouldEndWith("#endif");
    }

    [Fact]
    public void InstanceOfComponentDerivedFromDebugElement_IsWrapped()
    {
        AddComponent("Widget", debugOnly: true);
        AddComponent("Fancy", baseType: "Widget");
        ScreenSave screen = AddScreenWithInstance("F", "Fancy");

        string code = Generate(screen);

        code.ShouldContain("#if DEBUG\r\n    public Fancy F { get; protected set; }\r\n#endif");
    }

    [Fact]
    public void UsingForANamespaceOnlyADebugElementProvides_IsWrapped()
    {
        AddComponent("Widget", debugOnly: true);
        ScreenSave screen = AddScreenWithInstance("W", "Widget");

        string code = Generate(screen);

        code.ShouldContain("#if DEBUG\r\nusing MyGame.Components;\r\n#endif");
    }

    [Fact]
    public void UsingForANamespaceANonDebugElementAlsoProvides_StaysPlain()
    {
        AddComponent("Widget", debugOnly: true);
        AddComponent("Plain");
        ScreenSave screen = AddScreenWithInstance("W", "Widget");
        screen.Instances.Add(new InstanceSave { Name = "P", BaseType = "Plain", ParentContainer = screen });

        string code = Generate(screen);

        code.ShouldContain("\r\nusing MyGame.Components;\r\n");
        code.ShouldNotContain("#if DEBUG\r\nusing MyGame.Components;");
    }

    [Fact]
    public void GetElementsToRegenerate_ReturnsTheToggledElementThenEverythingThatReferencesItDirectlyOrThroughBases()
    {
        ComponentSave widget = AddComponent("Widget");
        ComponentSave fancy = AddComponent("Fancy", baseType: "Widget");
        ScreenSave screen = AddScreenWithInstance("W", "Widget");
        ScreenSave other = new ScreenSave { Name = "Other" };
        other.Instances.Add(new InstanceSave { Name = "F", BaseType = "Fancy", ParentContainer = other });
        Project.Screens.Add(other);
        ScreenSave unrelated = new ScreenSave { Name = "Unrelated" };
        Project.Screens.Add(unrelated);
        ObjectFinder.Self.GumProjectSave = Project;

        IReadOnlyList<ElementSave> elements = new DebugOnlyElementLogic(_settingsManager).GetElementsToRegenerate(widget, Project);

        elements[0].ShouldBe(widget);
        elements.ShouldBe(new ElementSave[] { widget, fancy, screen, other }, ignoreOrder: true);
    }
}
