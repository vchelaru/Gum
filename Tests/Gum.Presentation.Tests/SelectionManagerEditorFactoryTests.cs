using Gum.Commands;
using Gum.DataTypes;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="SelectionManager"/>'s WireframeEditor-kind switching after its relocation to
/// Gum.Presentation (ADR-0005, part of #3846). Building a <c>StandardWireframeEditor</c>/
/// <c>PolygonWireframeEditor</c> requires XNALIKE-only dependencies (a tool font service, project
/// guide-color settings) unreachable from headless Gum.Presentation, so construction moved behind
/// <see cref="IWireframeEditorFactory"/> — and the "is the current editor already the right kind"
/// check, previously a runtime type-check against those same tool-only concrete types
/// (<c>WireframeEditor is StandardWireframeEditor</c>), had to become an explicitly-tracked kind
/// instead. This suite pins that new tracking, not just the factory call.
/// </summary>
public class SelectionManagerEditorFactoryTests : BaseTestClass
{
    private class FakeWireframeEditor : WireframeEditor
    {
        public FakeWireframeEditor(ISelectionManager selectionManager)
            : base(
                Mock.Of<IHotkeyManager>(),
                selectionManager,
                Mock.Of<ISelectedState>(),
                Mock.Of<IElementCommands>(),
                Mock.Of<IGuiCommands>(),
                Mock.Of<IFileCommands>(),
                Mock.Of<ISetVariableLogic>(),
                Mock.Of<IUndoManager>(),
                Mock.Of<IVariableInCategoryPropagationLogic>(),
                Mock.Of<IWireframeObjectManager>(),
                Mock.Of<IUiSettingsService>(),
                new Layer(),
                System.Drawing.Color.White,
                System.Drawing.Color.White,
                new Camera(),
                Mock.Of<IGumCursorState>(),
                Mock.Of<IPluginManager>())
        {
        }

        public bool Destroyed { get; private set; }

        public override bool HasCursorOverHandles => false;

        public override void Destroy()
        {
            Destroyed = true;
            base.Destroy();
        }
    }

    private readonly Mock<ISelectedState> _mockSelectedState;
    private readonly Mock<IWireframeEditorFactory> _mockFactory;
    private readonly SelectionManager _selectionManager;
    private readonly List<InstanceSave> _selectedInstances = new();

    public SelectionManagerEditorFactoryTests()
    {
        _mockSelectedState = new Mock<ISelectedState>();
        _mockSelectedState.Setup(x => x.SelectedInstances).Returns(() => _selectedInstances);
        _mockSelectedState.Setup(x => x.GetTopLevelElementStack()).Returns(new List<ElementWithState>());

        _mockFactory = new Mock<IWireframeEditorFactory>();
        _mockFactory
            .Setup(f => f.CreateStandardEditor(It.IsAny<ISelectionManager>(), It.IsAny<Layer>(), It.IsAny<Camera>(), It.IsAny<IGumCursorState>()))
            .Returns<ISelectionManager, Layer, Camera, IGumCursorState>((sm, _, _, _) => new FakeWireframeEditor(sm));

        _selectionManager = new SelectionManager(
            _mockSelectedState.Object,
            Mock.Of<IUndoManager>(),
            Mock.Of<IContextMenuState>(),
            Mock.Of<Gum.Services.Dialogs.IDialogService>(),
            Mock.Of<IHotkeyManager>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IGuiCommands>(),
            _mockFactory.Object,
            Mock.Of<INineSliceCoordinateRefresher>(),
            Mock.Of<IPreciseHitTester>());

        _selectionManager.Initialize(
            new Layer(),
            new Camera(),
            Mock.Of<IGumCursorState>(),
            Mock.Of<ISelectionRectangleVisual>(),
            Mock.Of<IHighlightOutlineVisual>(),
            Mock.Of<IHighlightOverlayVisual>());
    }

    private GraphicalUiElement SelectNewStandardInstance()
    {
        var instance = new InstanceSave { Name = "Instance" };
        var gue = new GraphicalUiElement { Tag = instance };
        _selectionManager.SelectedGue = gue;
        return gue;
    }

    [Fact]
    public void SelectingStandardElement_CreatesEditorViaFactory()
    {
        SelectNewStandardInstance();

        _mockFactory.Verify(f => f.CreateStandardEditor(
            _selectionManager, It.IsAny<Layer>(), It.IsAny<Camera>(), It.IsAny<IGumCursorState>()), Times.Once);
        _selectionManager.WireframeEditor.ShouldNotBeNull();
    }

    [Fact]
    public void SelectingAnotherStandardElement_DoesNotRecreateEditor()
    {
        SelectNewStandardInstance();
        var firstEditor = _selectionManager.WireframeEditor;

        // Selecting a second, different standard-kind instance should reuse the existing editor
        // rather than destroying and rebuilding it.
        SelectNewStandardInstance();

        _mockFactory.Verify(f => f.CreateStandardEditor(
            _selectionManager, It.IsAny<Layer>(), It.IsAny<Camera>(), It.IsAny<IGumCursorState>()), Times.Once);
        _selectionManager.WireframeEditor.ShouldBeSameAs(firstEditor);
        ((FakeWireframeEditor)firstEditor!).Destroyed.ShouldBeFalse();
    }

    [Fact]
    public void DeselectingAll_DestroysEditorAndClearsIt()
    {
        SelectNewStandardInstance();
        var editor = (FakeWireframeEditor)_selectionManager.WireframeEditor!;

        _selectionManager.DeselectAll();

        editor.Destroyed.ShouldBeTrue();
        _selectionManager.WireframeEditor.ShouldBeNull();
    }

    [Fact]
    public void ReselectingAfterDeselect_CreatesANewEditor()
    {
        SelectNewStandardInstance();
        _selectionManager.DeselectAll();

        SelectNewStandardInstance();

        _mockFactory.Verify(f => f.CreateStandardEditor(
            _selectionManager, It.IsAny<Layer>(), It.IsAny<Camera>(), It.IsAny<IGumCursorState>()), Times.Exactly(2));
        _selectionManager.WireframeEditor.ShouldNotBeNull();
    }
}
