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
/// A component with MenuItemSubmenuPopupBehavior has the ScrollViewer structure, so its generated class
/// derives from ScrollViewer (#6730).
/// </summary>
public class CodeGeneratorMenuItemSubmenuPopupTests : BaseTestClass
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

    private string GenerateDefaultSubmenuPopup(OutputLibrary outputLibrary)
    {
        Project.Behaviors.Add(new BehaviorSave
        {
            Name = StandardFormsBehaviorNames.MenuItemSubmenuPopupBehaviorName,
            DefaultImplementation = "Controls/MenuItemSubmenuPopup"
        });

        ComponentSave popup = new ComponentSave { Name = "Controls/MenuItemSubmenuPopup", BaseType = "Container" };
        popup.States.Add(new StateSave { Name = "Default", ParentContainer = popup });
        popup.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.MenuItemSubmenuPopupBehaviorName });
        Project.Components.Add(popup);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = outputLibrary,
            RootNamespace = "MyGame",
        };

        return CreateCodeGenerator(syntaxVersion: 4).GetGeneratedCodeForElement(popup, new CodeOutputElementSettings(), settings);
    }

    [Fact]
    public void GetGeneratedCodeForElement_GumForms_DerivesFromScrollViewer()
    {
        string code = GenerateDefaultSubmenuPopup(OutputLibrary.GumForms);

        code.ShouldContain(": global::Gum.Forms.Controls.ScrollViewer");
    }

    [Fact]
    public void GetGeneratedCodeForElement_MonoGame_ExposesScrollViewerFormsControl()
    {
        string code = GenerateDefaultSubmenuPopup(OutputLibrary.MonoGame);

        code.ShouldContain("public global::Gum.Forms.Controls.ScrollViewer FormsControl => FormsControlAsObject as global::Gum.Forms.Controls.ScrollViewer;");
    }

    [Theory]
    [InlineData(OutputLibrary.GumForms)]
    [InlineData(OutputLibrary.MonoGame)]
    public void GetGeneratedCodeForElement_DefaultImplementation_DoesNotRegisterAsDefaultScrollViewer(OutputLibrary outputLibrary)
    {
        string code = GenerateDefaultSubmenuPopup(outputLibrary);

        // The project's ScrollViewer component stays the visual for every other ScrollViewer.
        code.ShouldNotContain("[typeof(global::Gum.Forms.Controls.ScrollViewer)]");
    }
}
