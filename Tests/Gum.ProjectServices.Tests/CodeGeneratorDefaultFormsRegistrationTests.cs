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
/// A component that is its behavior's default implementation registers itself as the default
/// visual for that Forms type. Runtimes at syntax version 1+ get DefaultFormsTemplates, since
/// DefaultFormsComponents is obsolete there.
/// </summary>
public class CodeGeneratorDefaultFormsRegistrationTests : BaseTestClass
{
    private static CodeGenerator CreateCodeGenerator(int syntaxVersion)
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);

        Mock<ISyntaxVersionDetectionService> detection = new Mock<ISyntaxVersionDetectionService>();
        detection
            .Setup(d => d.Detect(It.IsAny<CodeOutputProjectSettings>(), It.IsAny<string?>()))
            .Returns(new SyntaxVersionResult { Version = syntaxVersion });

        return new CodeGenerator(
            new CodeGenerationNameVerifier(mockNameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider,
            syntaxVersionDetectionService: detection.Object);
    }

    private string GenerateDefaultButton(int syntaxVersion)
    {
        Project.Behaviors.Add(new BehaviorSave
        {
            Name = StandardFormsBehaviorNames.ButtonBehaviorName,
            DefaultImplementation = "Controls/Button"
        });

        ComponentSave button = new ComponentSave { Name = "Controls/Button", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ButtonBehaviorName });
        Project.Components.Add(button);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.MonoGame,
            RootNamespace = "MyGame",
        };

        return CreateCodeGenerator(syntaxVersion).GetGeneratedCodeForElement(button, new CodeOutputElementSettings(), settings);
    }

    [Fact]
    public void GetGeneratedCodeForElement_SyntaxVersion1OrLater_RegistersDefaultFormsTemplate()
    {
        string code = GenerateDefaultButton(syntaxVersion: 4);

        code.ShouldContain(
            "global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.Button)] = " +
            "new global::Gum.Forms.VisualTemplate((vm, createForms) => new ButtonRuntime(fullInstantiation: true, tryCreateFormsObject: createForms));");
        code.ShouldNotContain("DefaultFormsComponents");
    }

    [Fact]
    public void GetGeneratedCodeForElement_SyntaxVersion0_RegistersDefaultFormsComponent()
    {
        string code = GenerateDefaultButton(syntaxVersion: 0);

        code.ShouldContain(
            "global::Gum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(global::Gum.Forms.Controls.Button)] = typeof(ButtonRuntime);");
        code.ShouldNotContain("DefaultFormsTemplates");
    }
}
