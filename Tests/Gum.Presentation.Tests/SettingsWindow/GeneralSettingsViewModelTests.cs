using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Managers;
using Gum.Messages;
using Gum.Services;
using Gum.SettingsWindow;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests.SettingsWindow;

public class GeneralSettingsViewModelTests
{
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IFileSystemRevealService> _reveal;
    private readonly WeakReferenceMessenger _messenger;
    private readonly List<StandardsPaletteSettingChangedMessage> _messages;

    public GeneralSettingsViewModelTests()
    {
        _projectManager = new Mock<IProjectManager>();
        _reveal = new Mock<IFileSystemRevealService>();
        _messenger = new WeakReferenceMessenger();
        _messages = new List<StandardsPaletteSettingChangedMessage>();
        _messenger.Register<StandardsPaletteSettingChangedMessage>(this, (_, message) => _messages.Add(message));
    }

    private GeneralSettingsViewModel CreateViewModel() =>
        new GeneralSettingsViewModel(_projectManager.Object, _messenger, _reveal.Object);

    [Fact]
    public void Constructor_ReadsCurrentSettings()
    {
        _projectManager.SetupGet(x => x.AutoSave).Returns(true);
        _projectManager.SetupGet(x => x.EffectiveUseStandardsPalette).Returns(false);

        GeneralSettingsViewModel viewModel = CreateViewModel();

        viewModel.AutoSave.ShouldBeTrue();
        viewModel.UseStandardsPalette.ShouldBeFalse();
    }

    [Fact]
    public void AutoSave_Set_WritesThroughAndSaves()
    {
        GeneralSettingsViewModel viewModel = CreateViewModel();

        viewModel.AutoSave = true;

        _projectManager.VerifySet(x => x.AutoSave = true, Times.Once);
        _projectManager.Verify(x => x.SaveGeneralSettings(), Times.Once);
    }

    [Fact]
    public void UseStandardsPalette_Set_WritesThroughSavesAndNotifies()
    {
        _projectManager.SetupGet(x => x.EffectiveUseStandardsPalette).Returns(false);
        GeneralSettingsViewModel viewModel = CreateViewModel();
        _projectManager.SetupGet(x => x.EffectiveUseStandardsPalette).Returns(true);

        viewModel.UseStandardsPalette = true;

        _projectManager.VerifySet(x => x.UseStandardsPalette = true, Times.Once);
        _projectManager.Verify(x => x.SaveGeneralSettings(), Times.Once);
        _messages.Count.ShouldBe(1);
        _messages[0].UseStandardsPalette.ShouldBeTrue();
    }

    [Fact]
    public void OpenSettingsFolderCommand_OpensTheGlobalSettingsFolder()
    {
        GeneralSettingsViewModel viewModel = CreateViewModel();

        viewModel.OpenSettingsFolderCommand.Execute(null);

        _reveal.Verify(x => x.OpenFolder(FileManager.UserApplicationDataForThisApplication), Times.Once);
    }
}
