using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// <see cref="CodeGenerator.AddFindByNameAssignment"/> emits a Forms-control lookup for
/// <see cref="ObjectInstantiationType.FindByName"/> + <see cref="OutputLibrary.MonoGameForms"/>
/// component instances. <c>GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName&lt;T&gt;</c>
/// is <c>[Obsolete]</c> in favor of <c>FindFormsControl&lt;T&gt;(name)</c>, which shipped mid
/// syntax-version-0 (May 2026, before the version-1 bump in June). Gating on
/// <c>ResolvedSyntaxVersion &gt;= 1</c> guarantees the target runtime has the method, at the cost of
/// version-0 projects (which may or may not have it) keeping the deprecated call — see
/// gum-runtime-syntax-version's "Gate on a safe floor" section.
/// </summary>
public class CodeGeneratorFindFormsControlTests : BaseTestClass
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

    private string GetFindByNameCodeForFormsComponentInstance(int syntaxVersion)
    {
        ComponentSave button = new ComponentSave { Name = "Controls/MyButton" };
        StateSave buttonDefault = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(buttonDefault);
        Project.Components.Add(button);

        ComponentSave main = new ComponentSave { Name = "MainComponent", BaseType = "Container" };
        StateSave mainDefault = new StateSave { Name = "Default", ParentContainer = main };
        main.States.Add(mainDefault);

        InstanceSave buttonInstance = new InstanceSave
        {
            Name = "ButtonInstance",
            BaseType = "Controls/MyButton",
            ParentContainer = main,
        };
        main.Instances.Add(buttonInstance);

        Project.Components.Add(main);
        ObjectFinder.Self.GumProjectSave = Project;

        try
        {
            CodeOutputProjectSettings settings = new CodeOutputProjectSettings
            {
                OutputLibrary = OutputLibrary.MonoGameForms,
                ObjectInstantiationType = ObjectInstantiationType.FindByName,
                RootNamespace = "MyGame",
                SyntaxVersion = syntaxVersion.ToString(),
            };

            return CreateCodeGenerator().GetGeneratedCodeForElement(main, elementSettings: null!, settings);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void SyntaxVersion0_FindByNameFormsComponent_EmitsDeprecatedTryGetFrameworkElementByName()
    {
        // Pins the pre-existing behavior: version 0 may predate FindFormsControl<T> (added
        // mid-version-0, in the 2026 May release), so it must keep the older, always-safe call.
        string code = GetFindByNameCodeForFormsComponentInstance(syntaxVersion: 0);

        code.ShouldContain(
            "global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<");
        code.ShouldNotContain("FindFormsControl<");
    }

    [Fact]
    public void SyntaxVersion1_FindByNameFormsComponent_EmitsFindFormsControl()
    {
        string code = GetFindByNameCodeForFormsComponentInstance(syntaxVersion: 1);

        code.ShouldContain(
            "global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<");
        code.ShouldNotContain("TryGetFrameworkElementByName<");
    }
}
