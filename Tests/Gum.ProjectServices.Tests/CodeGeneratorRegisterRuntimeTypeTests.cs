using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for the <c>visual</c> runtime type emitted inside the generated
/// <c>RegisterRuntimeType</c> / <c>VisualTemplate</c> lambda. Regression: codegen used to
/// check only the immediate <c>BaseType</c>, so a chain like <c>MyHeader : Label : Text</c>
/// fell back to <c>ContainerRuntime</c> instead of <c>TextRuntime</c> (#2857).
/// </summary>
public class CodeGeneratorRegisterRuntimeTypeTests : BaseTestClass
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

    private static CodeOutputProjectSettings CreateForms()
    {
        return new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGameForms,
            RootNamespace = "MyGame",
        };
    }

    private static ComponentSave CreateComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        return component;
    }

    [Fact]
    public void DirectTextInheritance_EmitsTextRuntime()
    {
        GumProjectSave project = Project;
        ComponentSave label = CreateComponent("Label", "Text");
        project.Components.Add(label);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                label, elementSettings: null!, CreateForms());

            code.ShouldContain("TextRuntime();");
            code.ShouldNotContain("ContainerRuntime();");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void IndirectTextInheritance_EmitsTextRuntime()
    {
        // Regression for #2857: MyHeader : Label : Text should walk to Text and pick TextRuntime.
        GumProjectSave project = Project;
        ComponentSave label = CreateComponent("Label", "Text");
        ComponentSave myHeader = CreateComponent("MyHeader", "Label");
        project.Components.Add(label);
        project.Components.Add(myHeader);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                myHeader, elementSettings: null!, CreateForms());

            code.ShouldContain("TextRuntime();");
            code.ShouldNotContain("ContainerRuntime();");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void DerivedComponent_EmitsNewModifierOnRegisterRuntimeType()
    {
        // #2860: a component derived from another component must emit `public static new void RegisterRuntimeType()`
        // to avoid CS0108 (hiding inherited member without `new`).
        GumProjectSave project = Project;
        ComponentSave baseComponent = CreateComponent("MyBase", "Container");
        ComponentSave derivedComponent = CreateComponent("MyDerived", "MyBase");
        project.Components.Add(baseComponent);
        project.Components.Add(derivedComponent);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                derivedComponent, elementSettings: null!, CreateForms());

            code.ShouldContain("public static new void RegisterRuntimeType()");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void StandardBaseComponent_DoesNotEmitNewModifierOnRegisterRuntimeType()
    {
        // #2860: a component whose BaseType is a standard type (e.g. Container) has no
        // inherited RegisterRuntimeType to hide, so `new` must not be emitted.
        GumProjectSave project = Project;
        ComponentSave panel = CreateComponent("Panel", "Container");
        project.Components.Add(panel);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                panel, elementSettings: null!, CreateForms());

            code.ShouldContain("public static void RegisterRuntimeType()");
            code.ShouldNotContain("public static new void RegisterRuntimeType()");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void ContainerInheritance_StillEmitsContainerRuntime()
    {
        GumProjectSave project = Project;
        ComponentSave panel = CreateComponent("Panel", "Container");
        project.Components.Add(panel);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                panel, elementSettings: null!, CreateForms());

            code.ShouldContain("ContainerRuntime();");
            code.ShouldNotContain("TextRuntime();");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void MissingElementNullCheck_IsEmittedUnconditionally()
    {
        // #3576: the null check on ObjectFinder.Self.GetElementSave(...) used to be wrapped in
        // #if DEBUG, so a Release build hit an opaque NullReferenceException instead of the
        // descriptive message. It must be emitted unconditionally.
        GumProjectSave project = Project;
        ComponentSave panel = CreateComponent("Panel", "Container");
        project.Components.Add(panel);
        ObjectFinder.Self.GumProjectSave = project;

        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                panel, elementSettings: null!, CreateForms());

            code.ShouldNotContain("#if DEBUG");
            code.ShouldContain(
                "ObjectFinder.Self.GetElementSave(\"Panel\") ?? throw new System.InvalidOperationException(\"Could not find an element named Panel - did you forget to load a Gum project?\");");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
