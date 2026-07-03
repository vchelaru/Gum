using Gum.ProjectServices.CodeGeneration;
using CodeOutputPlugin.ViewModels;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumToolUnitTests.Plugins.CodeOutputPlugins;

public class CodeWindowViewModelTests
{
    private readonly AutoMocker _mocker;
    private CodeWindowViewModel _viewModel;

    public CodeWindowViewModelTests()
    {
        _mocker = new AutoMocker();
        _viewModel = _mocker.CreateInstance<CodeWindowViewModel>();

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
    public void ShouldShowSetup_ReturnsTrue_WhenSettingsIsNullButCsprojExists()
    {
        string gumDirectory = @"C:\game\Content\GumProject\";
        string csprojDirectory = @"C:\game\";

        SetupCsprojDiscovery(gumDirectory, csprojDirectory);

        bool result = _viewModel.ShouldShowSetup(settings: null, hasClickedManualSetup: false);

        result.ShouldBeTrue();
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
    public void ShouldShowSetup_ReturnsTrue_WhenShprojAboveGumxAndCodeProjectRootEmpty()
    {
        string gumDirectory = @"C:\game\Content\GumProject\";
        string shprojDirectory = @"C:\game\";

        _mocker.GetMock<IProjectState>()
            .Setup(p => p.ProjectDirectory)
            .Returns(gumDirectory);

        Mock<IFileCommands> fileCommandsMock = _mocker.GetMock<IFileCommands>();
        FilePath shprojPath = new FilePath(shprojDirectory);
        fileCommandsMock
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

    [Fact]
    public void ShouldShowSetup_ReturnsFalse_WhenNoCsprojExistsAboveGumx()
    {
        string gumDirectory = @"C:\game\Content\GumProject\";

        _mocker.GetMock<IProjectState>()
            .Setup(p => p.ProjectDirectory)
            .Returns(gumDirectory);
        _mocker.GetMock<IFileCommands>()
            .Setup(f => f.GetFiles(It.IsAny<string>()))
            .Returns(Array.Empty<string>());

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { CodeProjectRoot = string.Empty };

        bool result = _viewModel.ShouldShowSetup(settings, hasClickedManualSetup: false);

        result.ShouldBeFalse();
    }

    private void SetupCsprojDiscovery(string gumDirectory, string csprojDirectory)
    {
        _mocker.GetMock<IProjectState>()
            .Setup(p => p.ProjectDirectory)
            .Returns(gumDirectory);

        Mock<IFileCommands> fileCommandsMock = _mocker.GetMock<IFileCommands>();
        FilePath csprojPath = new FilePath(csprojDirectory);
        fileCommandsMock
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
