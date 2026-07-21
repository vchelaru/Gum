using Gum.DataTypes;
using Gum.Plugins.Fonts;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Relocated out of the WPF-hosted MainFontPlugin into the headless Gum.Presentation assembly
/// (ADR-0005 Phase 3) so this business logic is unit testable. The "Clear Font Cache" handler stays
/// on the plugin (see FontCacheLogic's summary) and isn't exercised here.
/// </summary>
public class FontCacheLogicTests
{
    private readonly Mock<IFontManager> _fontManager = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly FontCacheLogic _logic;

    public FontCacheLogicTests()
    {
        _logic = new FontCacheLogic(_fontManager.Object, _dialogService.Object, _projectState.Object);
    }

    [Fact]
    public async Task CreateMissingFontFilesForLoadedProject_CreatesMissingFontsForLoadedProject()
    {
        GumProjectSave project = new();
        _projectState.Setup(x => x.GumProjectSave).Returns(project);

        await _logic.CreateMissingFontFilesForLoadedProject();

        _fontManager.Verify(x => x.CreateAllMissingFontFiles(project, false), Times.Once);
    }

    [Fact]
    public void GetOrCreateFontCacheFolder_ReturnsAbsoluteFontCacheFolder()
    {
        _fontManager.Setup(x => x.AbsoluteFontCacheFolder).Returns(System.IO.Path.GetTempPath());

        string folder = _logic.GetOrCreateFontCacheFolder();

        folder.ShouldBe(System.IO.Path.GetTempPath());
    }

    [Fact]
    public async Task RefreshFontCache_NoProjectLoaded_ShowsMessageAndDoesNotCreateFonts()
    {
        _projectState.Setup(x => x.GumProjectSave).Returns((GumProjectSave?)null);

        await _logic.RefreshFontCache(forceRecreate: false);

        _dialogService.Verify(
            x => x.ShowMessage("A Gum project must first be loaded before recreating font files", null, null),
            Times.Once);
        _fontManager.Verify(
            x => x.CreateAllMissingFontFiles(It.IsAny<GumProjectSave>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task RefreshFontCache_ProjectLoaded_CreatesMissingFontFilesWithForceRecreateFlag()
    {
        GumProjectSave project = new();
        _projectState.Setup(x => x.GumProjectSave).Returns(project);

        await _logic.RefreshFontCache(forceRecreate: true);

        _fontManager.Verify(x => x.CreateAllMissingFontFiles(project, true), Times.Once);
        _dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()), Times.Never);
    }
}
