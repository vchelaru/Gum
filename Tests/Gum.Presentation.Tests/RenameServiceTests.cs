using CodeOutputPlugin.Manager;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Moq;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for RenameService, relocated out of Gum/CodeOutputPlugin/Manager
/// (Gum.csproj) into the headless Gum.Presentation assembly (#3905) - no WPF dependency of its own,
/// only interfaces (IDialogService) and engine types already headless. Bumped from internal to public
/// (class and the two Handle* methods) so the plugin, still in Gum.csproj, can call it across the new
/// assembly boundary.
/// </summary>
public class RenameServiceTests : BaseTestClass
{
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly RenameService _renameService;

    public RenameServiceTests()
    {
        Mock<INameVerifier> nameVerifier = new();
        string whyNotValid;
        CommonValidationError error;
        nameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);

        CodeGenerationNameVerifier codeGenNameVerifier = new(nameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new(directoryProvider);
        LocalizationService localizationService = new();

        CodeGenerator codeGenerator = new(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);

        CustomCodeGenerator customCodeGenerator = new(codeGenerator, codeGenNameVerifier);

        // codeGenerationService is unused by the early-out this test exercises, so null! is safe here
        // (mirrors ParentSetLogicTests's same null!-for-unreached-dependency pattern).
        _renameService = new RenameService(
            codeGenerationService: null!,
            codeGenerator,
            customCodeGenerator,
            codeGenNameVerifier,
            _dialogService.Object,
            directoryProvider);
    }

    [Fact]
    public void HandleRename_WithEmptyCodeProjectRoot_DoesNothing()
    {
        var element = new ComponentSave { Name = "MyComponent" };

        _renameService.HandleRename(
            element,
            oldName: "OldName",
            codeOutputProjectSettings: new CodeOutputProjectSettings(), // CodeProjectRoot defaults to empty
            visualApi: VisualApi.Gum);

        _dialogService.VerifyNoOtherCalls();
    }
}
