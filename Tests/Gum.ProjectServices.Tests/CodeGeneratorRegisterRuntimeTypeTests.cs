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
}
