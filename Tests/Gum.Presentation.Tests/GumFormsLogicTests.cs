using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Relocated out of the WPF-hosted MainGumFormsPlugin into the headless Gum.Presentation assembly
/// (ADR-0005 Phase 3) so this business logic is unit testable.
/// </summary>
public class GumFormsLogicTests
{
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IFormsThemeImporter> _themeImporter = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly GumFormsLogic _logic;

    public GumFormsLogicTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());

        _logic = new GumFormsLogic(
            _formsFileService.Object,
            _themeImporter.Object,
            _projectState.Object);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetIfProjectHasForms_AsksWhetherTheProjectHasTheDefaultThemesElements(bool projectHasThem)
    {
        _formsFileService.Setup(x => x.ProjectHasElementsOf("Standard")).Returns(projectHasThem);

        _logic.GetIfProjectHasForms().ShouldBe(projectHasThem);
    }

    [Fact]
    public void ShouldShowAddFormsMenuItem_ProjectHasNoFullFileName_ReturnsTrue()
    {
        GumProjectSave save = new() { FullFileName = "" };
        _formsFileService.Setup(x => x.ProjectHasElementsOf("Standard")).Returns(true);

        _logic.ShouldShowAddFormsMenuItem(save).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ShouldShowAddFormsMenuItem_HidesTheItemOnceTheProjectHasForms(bool projectHasForms, bool expectVisible)
    {
        _formsFileService.Setup(x => x.ProjectHasElementsOf("Standard")).Returns(projectHasForms);
        GumProjectSave save = new() { FullFileName = @"C:\SomeProject.gumx" };

        _logic.ShouldShowAddFormsMenuItem(save).ShouldBe(expectVisible);
    }

    [Fact]
    public void TryCreateAddFormsViewModel_ProjectNeedsToSave_ReturnsFalseWithBlockedMessage()
    {
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(true);

        bool result = _logic.TryCreateAddFormsViewModel(out AddFormsViewModel? viewModel, out string? blockedMessage);

        result.ShouldBeFalse();
        viewModel.ShouldBeNull();
        blockedMessage.ShouldBe("You must first save the project before importing forms");
    }

    [Fact]
    public void TryCreateAddFormsViewModel_ProjectSaved_ReturnsTrueWithViewModel()
    {
        _projectState.Setup(x => x.NeedsToSaveProject).Returns(false);
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");

        bool result = _logic.TryCreateAddFormsViewModel(out AddFormsViewModel? viewModel, out string? blockedMessage);

        result.ShouldBeTrue();
        viewModel.ShouldNotBeNull();
        blockedMessage.ShouldBeNull();
    }
}
