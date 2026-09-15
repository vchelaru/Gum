using Gum.Commands;
using Gum.DataTypes;
using Gum.Input;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests;

/// <summary>
/// SelectionManager has a two-stage init: the constructor wires its tool-side dependencies, but
/// Initialize(...) - called once the XNA/render host is up - is what supplies the highlight visuals.
/// On the Avalonia head, the tree view can fire a hover event (and call in through
/// PluginManager.SetHighlightedIpso) before that second stage runs, since it's a sibling control that
/// can attach independently of the wireframe canvas - unlike WPF, where this ordering apparently never
/// occurs. Regression test for the resulting NullReferenceException.
/// </summary>
public class SelectionManagerBeforeInitializeTests : BaseTestClass
{
    private readonly SelectionManager _selectionManager;

    public SelectionManagerBeforeInitializeTests()
    {
        var mockSelectedState = new Mock<ISelectedState>();
        mockSelectedState.Setup(x => x.SelectedInstances).Returns(() => new List<InstanceSave>());

        _selectionManager = new SelectionManager(
            mockSelectedState.Object,
            Mock.Of<IUndoManager>(),
            Mock.Of<IContextMenuState>(),
            Mock.Of<Gum.Services.Dialogs.IDialogService>(),
            Mock.Of<IHotkeyManager>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IWireframeEditorFactory>(),
            Mock.Of<INineSliceCoordinateRefresher>(),
            Mock.Of<IPreciseHitTester>());

        // Deliberately not calling Initialize(...) here.
    }

    [Fact]
    public void SetHighlightedIpso_BeforeInitializeIsCalled_ShouldNotThrow()
    {
        var gue = new GraphicalUiElement();

        Should.NotThrow(() => _selectionManager.HighlightedIpso = gue);
    }

    [Fact]
    public void SetHighlightedIpso_BeforeInitializeIsCalled_ShouldNotFireHighlightedIpsoChanged()
    {
        int callCount = 0;
        _selectionManager.HighlightedIpsoChanged += (ipso) => callCount++;

        _selectionManager.HighlightedIpso = new GraphicalUiElement();

        callCount.ShouldBe(0);
    }
}
