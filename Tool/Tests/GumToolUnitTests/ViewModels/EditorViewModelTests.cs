using System.Collections.Generic;
using EditorTabPlugin_XNA.ViewModels;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace GumToolUnitTests.ViewModels;

/// <summary>
/// Pins EditorViewModel's grid-toolbar properties (moved out of Project Properties per #4137 review
/// feedback): setting them writes back to the loaded GumProjectSave, notifies plugins via
/// ProjectPropertySet, and autosaves - mirroring how ProjectPropertiesChangeLogic already treats
/// RestrictToUnitValues/ShowCheckerBackground.
/// </summary>
public class EditorViewModelTests
{
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager = new();
    private readonly Mock<IGridSnapWarningService> _gridSnapWarningService = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly GumProjectSave _gumProject = new();
    private readonly EditorViewModel _sut;

    public EditorViewModelTests()
    {
        _gridSnapWarningService.Setup(s => s.GetInfo()).Returns(new GridSnapWarningInfo(false, null));
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(_gumProject);

        _sut = new EditorViewModel(
            _pluginManager.Object,
            _fileCommands.Object,
            _wireframeObjectManager.Object,
            _gridSnapWarningService.Object,
            _projectManager.Object);
    }

    [Fact]
    public void SnapToGrid_WritesToGumProjectSave_AndNotifiesAndAutosaves()
    {
        _sut.SnapToGrid = true;

        _gumProject.SnapToGrid.ShouldBeTrue();
        _pluginManager.Verify(p => p.ProjectPropertySet(nameof(GumProjectSave.SnapToGrid)), Times.Once);
        _fileCommands.Verify(f => f.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public void GridSize_WritesToGumProjectSave_AndNotifiesAndAutosaves()
    {
        _sut.GridSize = 32;

        _gumProject.GridSize.ShouldBe(32);
        _pluginManager.Verify(p => p.ProjectPropertySet(nameof(GumProjectSave.GridSize)), Times.Once);
        _fileCommands.Verify(f => f.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public void SnapToGrid_DoesNothing_WhenValueIsUnchanged()
    {
        _sut.SnapToGrid = false;

        _pluginManager.Verify(p => p.ProjectPropertySet(It.IsAny<string>()), Times.Never);
        _fileCommands.Verify(f => f.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void HandleProjectLoad_SeedsGridPropertiesFromProject_WithoutNotifyingOrAutosaving()
    {
        GumProjectSave loadedProject = new()
        {
            SnapToGrid = true,
            GridSize = 24,
            CustomCanvasSizes = new List<CustomCanvasSize>
            {
                new() { FriendlyName = "Project Default" }
            }
        };

        _sut.HandleProjectLoad(loadedProject);

        _sut.SnapToGrid.ShouldBeTrue();
        _sut.GridSize.ShouldBe(24);
        _pluginManager.Verify(p => p.ProjectPropertySet(It.IsAny<string>()), Times.Never);
        _fileCommands.Verify(f => f.TryAutoSaveProject(It.IsAny<bool>()), Times.Never);
    }
}
