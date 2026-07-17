using CodeOutputPlugin.ViewModels;
using Gum.Commands;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for CodeWindowViewModel, relocated out of Gum.csproj into the
/// headless Gum.Presentation assembly (ADR-0005, #3754). GenerateCodeUiVisibility and
/// ShowNoGenerationAvailableUiVisibility were WPF Visibility-typed; converted to bool
/// (IsGenerateCodeUiVisible/IsNoGenerationAvailableUiVisible) per ADR-0004, with the view binding
/// through the stock BooleanToVisibilityConverter.
/// </summary>
public class CodeWindowViewModelTests
{
    private readonly Mock<IProjectState> _projectState;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<ICodeGenerationAutoSetupService> _autoSetupService;
    private readonly CodeWindowViewModel _viewModel;

    public CodeWindowViewModelTests()
    {
        _projectState = new Mock<IProjectState>();
        _fileCommands = new Mock<IFileCommands>();
        _dialogService = new Mock<IDialogService>();
        _guiCommands = new Mock<IGuiCommands>();
        _autoSetupService = new Mock<ICodeGenerationAutoSetupService>();

        _viewModel = new CodeWindowViewModel(
            _projectState.Object,
            _fileCommands.Object,
            _dialogService.Object,
            _guiCommands.Object,
            _autoSetupService.Object);
    }

    [Fact]
    public void IsGenerateCodeUiVisible_IsFalse_WhenViewingStandardElement()
    {
        _viewModel.IsViewingStandardElement = true;

        _viewModel.IsGenerateCodeUiVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsGenerateCodeUiVisible_IsTrue_WhenNotViewingStandardElement()
    {
        _viewModel.IsViewingStandardElement = false;

        _viewModel.IsGenerateCodeUiVisible.ShouldBeTrue();
    }

    [Fact]
    public void IsNoGenerationAvailableUiVisible_IsFalse_WhenNotViewingStandardElement()
    {
        _viewModel.IsViewingStandardElement = false;

        _viewModel.IsNoGenerationAvailableUiVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsNoGenerationAvailableUiVisible_IsTrue_WhenViewingStandardElement()
    {
        _viewModel.IsViewingStandardElement = true;

        _viewModel.IsNoGenerationAvailableUiVisible.ShouldBeTrue();
    }

    [Fact]
    public void ShouldShowSetup_ReturnsFalse_WhenCodeProjectRootIsPopulated()
    {
        string gumDirectory = @"C:\game\Content\GumProject\";
        string csprojDirectory = @"C:\game\";
        SetupCsprojDiscovery(gumDirectory, csprojDirectory);
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { CodeProjectRoot = @"..\..\" };

        bool result = _viewModel.ShouldShowSetup(settings, hasClickedManualSetup: false);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldShowSetup_ReturnsFalse_WhenManualSetupHasBeenClicked()
    {
        string gumDirectory = @"C:\game\Content\GumProject\";
        string csprojDirectory = @"C:\game\";
        SetupCsprojDiscovery(gumDirectory, csprojDirectory);
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { CodeProjectRoot = string.Empty };

        bool result = _viewModel.ShouldShowSetup(settings, hasClickedManualSetup: true);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldShowSetup_ReturnsFalse_WhenNoCsprojExistsAboveGumx()
    {
        string gumDirectory = @"C:\game\Content\GumProject\";
        _projectState.Setup(p => p.ProjectDirectory).Returns(gumDirectory);
        _fileCommands.Setup(f => f.GetFiles(It.IsAny<string>())).Returns(Array.Empty<string>());
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { CodeProjectRoot = string.Empty };

        bool result = _viewModel.ShouldShowSetup(settings, hasClickedManualSetup: false);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldShowSetup_ReturnsTrue_WhenCsprojAboveGumxAndCodeProjectRootEmpty()
    {
        string gumDirectory = @"C:\game\Content\GumProject\";
        string csprojDirectory = @"C:\game\";
        SetupCsprojDiscovery(gumDirectory, csprojDirectory);
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { CodeProjectRoot = string.Empty };

        bool result = _viewModel.ShouldShowSetup(settings, hasClickedManualSetup: false);

        result.ShouldBeTrue();
    }

    [Fact]
    public void ShouldShowSetup_ReturnsTrue_WhenSettingsIsNullButCsprojExists()
    {
        string gumDirectory = @"C:\game\Content\GumProject\";
        string csprojDirectory = @"C:\game\";
        SetupCsprojDiscovery(gumDirectory, csprojDirectory);

        bool result = _viewModel.ShouldShowSetup(settings: null, hasClickedManualSetup: false);

        result.ShouldBeTrue();
    }

    [Fact]
    public void ShouldShowSetup_ReturnsTrue_WhenShprojAboveGumxAndCodeProjectRootEmpty()
    {
        string gumDirectory = @"C:\game\Content\GumProject\";
        string shprojDirectory = @"C:\game\";
        _projectState.Setup(p => p.ProjectDirectory).Returns(gumDirectory);
        FilePath shprojPath = new FilePath(shprojDirectory);
        _fileCommands
            .Setup(f => f.GetFiles(It.IsAny<string>()))
            .Returns<string>(path =>
            {
                FilePath asFilePath = new FilePath(path);
                if (asFilePath == shprojPath)
                {
                    return new[] { Path.Combine(shprojDirectory, "Shared.shproj") };
                }
                return Array.Empty<string>();
            });
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { CodeProjectRoot = string.Empty };

        bool result = _viewModel.ShouldShowSetup(settings, hasClickedManualSetup: false);

        result.ShouldBeTrue();
    }

    private void SetupCsprojDiscovery(string gumDirectory, string csprojDirectory)
    {
        _projectState.Setup(p => p.ProjectDirectory).Returns(gumDirectory);
        FilePath csprojPath = new FilePath(csprojDirectory);
        _fileCommands
            .Setup(f => f.GetFiles(It.IsAny<string>()))
            .Returns<string>(path =>
            {
                FilePath asFilePath = new FilePath(path);
                if (asFilePath == csprojPath)
                {
                    return new[] { Path.Combine(csprojDirectory, "Game.csproj") };
                }
                return Array.Empty<string>();
            });
    }
}
