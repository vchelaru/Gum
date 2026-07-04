using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for #3501: <c>base.RefreshInternalVisualReferences();</c> must only be emitted for
/// <see cref="OutputLibrary.MonoGameForms"/>, since that method only exists on the Forms
/// <c>FrameworkElement</c> hierarchy (<c>MonoGameGum/Forms/Controls/FrameworkElement.cs</c>). The
/// generator previously gated the call on <c>OutputLibrary != OutputLibrary.Skia</c>, which wrongly
/// emitted the call for plain <see cref="OutputLibrary.MonoGame"/> (and Raylib/WPF/Maui/XamarinForms)
/// projects, producing generated code that failed to compile with CS0117 because the base type
/// (<c>GraphicalUiElement</c>/<c>ContainerRuntime</c>) has no such member.
/// </summary>
public class CodeGeneratorRefreshInternalVisualReferencesTests : BaseTestClass
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

    private static ComponentSave CreateComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        return component;
    }

    private static CodeOutputProjectSettings CreateProjectSettings(OutputLibrary outputLibrary) => new CodeOutputProjectSettings
    {
        OutputLibrary = outputLibrary,
        ObjectInstantiationType = ObjectInstantiationType.FullyInCode,
        RootNamespace = "MyGame",
    };

    [Fact]
    public void GetGeneratedCodeForElement_MonoGame_DoesNotEmitRefreshInternalVisualReferences()
    {
        GumProjectSave project = Project;

        ComponentSave component = CreateComponent("MainComponent", "Container");
        project.Components.Add(component);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                component,
                new CodeOutputElementSettings(),
                CreateProjectSettings(OutputLibrary.MonoGame));

            code.ShouldNotContain("RefreshInternalVisualReferences");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetGeneratedCodeForElement_MonoGameForms_EmitsRefreshInternalVisualReferences()
    {
        GumProjectSave project = Project;

        ComponentSave component = CreateComponent("MainComponent", "Container");
        project.Components.Add(component);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                component,
                new CodeOutputElementSettings(),
                CreateProjectSettings(OutputLibrary.MonoGameForms));

            code.ShouldContain("base.RefreshInternalVisualReferences();");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
