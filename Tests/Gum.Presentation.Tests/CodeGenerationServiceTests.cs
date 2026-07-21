using CodeOutputPlugin.Manager;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for CodeGenerationService, relocated out of
/// Gum/CodeOutputPlugin/Manager (Gum.csproj) into the headless Gum.Presentation assembly (#3905) -
/// it had no WPF dependency of its own, only interfaces (IGuiCommands/IDialogService/IRetryService)
/// that already lived in Gum.Presentation. Bumped from internal to public so the plugin (still in
/// Gum.csproj) can construct it across the new assembly boundary.
/// </summary>
public class CodeGenerationServiceTests : BaseTestClass
{
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IRetryService> _retryService = new();
    private readonly CodeGenerationService _codeGenerationService;

    public CodeGenerationServiceTests()
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

        _codeGenerationService = new CodeGenerationService(
            _guiCommands.Object,
            codeGenerator,
            _dialogService.Object,
            customCodeGenerator,
            codeGenNameVerifier,
            directoryProvider,
            _retryService.Object);
    }

    private static ComponentSave CreateComponent()
    {
        var component = new ComponentSave { Name = "MyComponent" };
        var defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        return component;
    }

    [Fact]
    public void GenerateCodeForElement_WithEmptyCodeProjectRoot_AndShowPopups_ShowsErrorMessage()
    {
        var element = CreateComponent();
        var elementSettings = new CodeOutputElementSettings();
        var projectSettings = new CodeOutputProjectSettings(); // CodeProjectRoot defaults to empty

        _codeGenerationService.GenerateCodeForElement(element, elementSettings, projectSettings, showPopups: true);

        _dialogService.Verify(
            x => x.ShowMessage("You must first specify a Code Project Root before generating code", null, null),
            Times.Once);
        _guiCommands.Verify(x => x.PrintOutput(It.IsAny<string>()), Times.Never);
        _retryService.Verify(x => x.TryMultipleTimes(It.IsAny<Action>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void GenerateCodeForElement_WithEmptyCodeProjectRoot_AndNoPopups_DoesNotShowDialog()
    {
        var element = CreateComponent();
        var elementSettings = new CodeOutputElementSettings();
        var projectSettings = new CodeOutputProjectSettings(); // CodeProjectRoot defaults to empty

        _codeGenerationService.GenerateCodeForElement(element, elementSettings, projectSettings, showPopups: false);

        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle?>()), Times.Never);
        _guiCommands.Verify(x => x.PrintOutput(It.IsAny<string>()), Times.Never);
        _retryService.Verify(x => x.TryMultipleTimes(It.IsAny<Action>(), It.IsAny<int>()), Times.Never);
    }
}
