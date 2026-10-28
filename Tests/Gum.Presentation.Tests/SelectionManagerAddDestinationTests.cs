using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Logic;
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests;

/// <summary>
/// A pick on the canvas makes what it lands on the add destination even when that is already
/// selected, which raises no selection change for <see cref="IAddDestinationTracker"/> to hear
/// (#7007, the canvas side of #7003). Each test anchors the tracker the way an add does, drives the
/// real <see cref="SelectionManager"/> over a real <see cref="SelectedState"/> with simulated cursor
/// frames, and reads back whether the destination was forgotten.
/// </summary>
public class SelectionManagerAddDestinationTests : BaseTestClass
{
    private const float CellSize = 100;

    private readonly Mock<IGumCursorState> _cursor = new();
    private readonly Mock<IHotkeyManager> _hotkeyManager = new();
    private readonly Camera _camera = new();
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly AddDestinationTracker _tracker;
    private readonly SelectedState _selectedState;
    private readonly SelectionManager _selectionManager;
    private readonly ScreenSave _screen;
    private readonly InstanceSave _first;
    private readonly InstanceSave _second;
    private readonly InstanceSave _third;
    private readonly GraphicalUiElement _screenGue;
    private readonly List<GraphicalUiElement> _allIpsos = new();
    private TestHandleEditor? _editor;
    private bool _isShiftHeld;

    public SelectionManagerAddDestinationTests()
    {
        _tracker = new AddDestinationTracker(_messenger);

        _screen = new ScreenSave { Name = "Screen1" };
        _screen.States.Add(new StateSave { Name = "Default", ParentContainer = _screen });
        _first = AddInstance("First");
        _second = AddInstance("Second");
        _third = AddInstance("Third");
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Screens.Add(_screen);

        _screenGue = new GraphicalUiElement(new InvisibleRenderable()) { Name = "Screen1", Tag = _screen };
        _allIpsos.Add(_screenGue);
        _allIpsos.Add(CreateGue(_first, left: 0));
        _allIpsos.Add(CreateGue(_second, left: CellSize * 2));
        _allIpsos.Add(CreateGue(_third, left: CellSize * 4));

        _selectedState = new SelectedState(
            Mock.Of<IGuiCommands>(),
            Mock.Of<IPluginManager>(),
            _messenger,
            new Lazy<IBehaviorVariablePropertyGridSink>(() => Mock.Of<IBehaviorVariablePropertyGridSink>()));

        Mock<IWireframeObjectManager> wireframe = new();
        wireframe.SetupGet(x => x.AllIpsos).Returns(_allIpsos);
        wireframe.Setup(x => x.IsRepresentation(It.IsAny<IPositionedSizedObject>()))
            .Returns((IPositionedSizedObject ipso) => ipso is GraphicalUiElement gue && _allIpsos.Contains(gue));
        wireframe.Setup(x => x.GetSelectedRepresentations())
            .Returns(() => _selectedState.SelectedInstances.Select(instance => GueFor(instance)).ToArray());
        wireframe.Setup(x => x.GetSelectedRepresentation())
            .Returns(() => _selectedState.SelectedInstance == null ? null : GueFor(_selectedState.SelectedInstance));
        wireframe.Setup(x => x.GetRepresentation(It.IsAny<InstanceSave>(), It.IsAny<List<ElementWithState>>()))
            .Returns((InstanceSave instance, List<ElementWithState>? _) => GueFor(instance));
        wireframe.Setup(x => x.GetRepresentation(It.IsAny<ElementSave>())).Returns(_screenGue);
        wireframe.Setup(x => x.GetAllVisibleElements()).Returns(() => _allIpsos);

        Mock<IPreciseHitTester> hitTester = new();
        hitTester.Setup(x => x.HasCursorOver(It.IsAny<GraphicalUiElement>(), It.IsAny<float>(), It.IsAny<float>()))
            .Returns((GraphicalUiElement gue, float x, float y) => gue.HasCursorOver(x, y));
        hitTester.Setup(x => x.IntersectsRectangle(It.IsAny<GraphicalUiElement>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<float>()))
            .Returns((GraphicalUiElement gue, float left, float top, float right, float bottom) =>
                gue.GetAbsoluteLeft() < right && gue.GetAbsoluteRight() > left && gue.GetAbsoluteTop() < bottom && gue.GetAbsoluteBottom() > top);

        KeyCombination shift = KeyCombination.Shift();
        _hotkeyManager.SetupGet(h => h.MultiSelect).Returns(shift);
        _hotkeyManager.Setup(h => h.IsPressedInControl(shift)).Returns(() => _isShiftHeld);

        Mock<IWireframeEditorFactory> factory = new();
        factory
            .Setup(f => f.CreateStandardEditor(It.IsAny<ISelectionManager>(), It.IsAny<Layer>(), It.IsAny<Camera>(), It.IsAny<IGumCursorState>()))
            .Returns<ISelectionManager, Layer, Camera, IGumCursorState>((selectionManager, _, _, cursor) =>
            {
                _editor = new TestHandleEditor(
                    _hotkeyManager.Object, selectionManager, _selectedState, Mock.Of<IElementCommands>(),
                    Mock.Of<IGuiCommands>(), Mock.Of<IFileCommands>(), Mock.Of<ISetVariableLogic>(),
                    Mock.Of<IUndoManager>(), Mock.Of<IVariableInCategoryPropagationLogic>(),
                    wireframe.Object, Mock.Of<IUiSettingsService>(), new Layer(),
                    System.Drawing.Color.White, System.Drawing.Color.White, _camera, cursor,
                    Mock.Of<IPluginManager>(), new CanvasDisplayScale());
                return _editor;
            });

        _selectionManager = new SelectionManager(
            _selectedState,
            Mock.Of<IUndoManager>(),
            Mock.Of<IContextMenuState>(),
            Mock.Of<Gum.Services.Dialogs.IDialogService>(),
            _hotkeyManager.Object,
            wireframe.Object,
            Mock.Of<IGuiCommands>(),
            factory.Object,
            Mock.Of<INineSliceCoordinateRefresher>(),
            hitTester.Object,
            _messenger);

        _selectionManager.Initialize(
            new Layer(),
            _camera,
            _cursor.Object,
            Mock.Of<ISelectionRectangleVisual>(),
            Mock.Of<IHighlightOutlineVisual>(),
            Mock.Of<IHighlightOverlayVisual>(),
            new CanvasDisplayScale());

        _cursor.SetupGet(c => c.IsInWindow).Returns(true);
        _selectedState.SelectedElement = _screen;
    }

    #region Clicks on what is already selected

    [Fact]
    public void ClickingTheAlreadySelectedInstance_MakesItTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        Click(_first);

        AssertDestinationForgotten();
    }

    [Fact]
    public void ShiftClickingTheAlreadySelectedInstance_MakesItTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        Click(_first, shift: true);

        AssertDestinationForgotten();
    }

    [Fact]
    public void ClickingAnInstanceOfAMultiSelection_MakesItTheDestination()
    {
        _selectedState.SelectedInstances = new List<InstanceSave> { _first, _second };
        _tracker.Anchor(_screen);

        Click(_second);

        AssertDestinationForgotten();
        _selectedState.SelectedInstances.ShouldBe(new[] { _first, _second });
    }

    [Fact]
    public void DoubleClickingTheAlreadySelectedInstance_MakesItTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        DoubleClick(_first);

        AssertDestinationForgotten();
    }

    [Fact]
    public void RightClickingTheAlreadySelectedInstance_MakesItTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        RightClick(_first);

        AssertDestinationForgotten();
    }

    [Fact]
    public void PressingTheAlreadySelectedInstanceAndDraggingIt_MakesItTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        Hover(_first);
        Frame(_first, push: true, down: true);
        Frame(_second, down: true);
        Frame(_second, click: true);

        AssertDestinationForgotten();
    }

    #endregion

    #region Marquee

    [Fact]
    public void AMarqueeSelectingTheSameInstance_MakesItTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        Marquee(fromX: CellSize * 1.5f, toX: -10);

        _selectedState.SelectedInstances.ShouldBe(new[] { _first });
        AssertDestinationForgotten();
    }

    [Fact]
    public void AMarqueeSelectingTheSameSet_MakesTheSetTheDestination()
    {
        _selectedState.SelectedInstances = new List<InstanceSave> { _first, _second };
        _tracker.Anchor(_screen);

        Marquee(fromX: CellSize * 2.5f, toX: -10);

        _selectedState.SelectedInstances.ShouldBe(new[] { _first, _second });
        AssertDestinationForgotten();
    }

    [Fact]
    public void AMarqueeOverEmptyCanvas_WithNothingSelected_MakesTheElementTheDestination()
    {
        _tracker.Anchor(_first);

        Marquee(fromX: CellSize * 1.4f, toX: CellSize * 1.6f);

        _selectedState.SelectedInstances.ShouldBeEmpty();
        AssertDestinationForgotten();
    }

    [Fact]
    public void AMarqueeOverEmptyCanvas_WithAnInstanceSelected_MakesTheElementTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _first);

        Marquee(fromX: CellSize * 1.4f, toX: CellSize * 1.6f);

        _selectedState.SelectedInstances.ShouldBeEmpty();
        AssertDestinationForgotten();
    }

    [Fact]
    public void AShiftMarqueeOverEmptyCanvas_MakesTheSelectionTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        Marquee(fromX: CellSize * 1.4f, toX: CellSize * 1.6f, shift: true);

        _selectedState.SelectedInstances.ShouldBe(new[] { _first });
        AssertDestinationForgotten();
    }

    [Fact]
    public void AShiftMarqueeOverAnotherInstance_MakesTheSetTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        Marquee(fromX: CellSize * 2.5f, toX: CellSize * 1.6f, shift: true);

        _selectedState.SelectedInstances.ShouldBe(new[] { _first, _second });
        AssertDestinationForgotten();
    }

    #endregion

    #region Behavior that already worked

    [Fact]
    public void ClickingAnotherInstance_MakesItTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        Click(_second);

        _selectedState.SelectedInstance.ShouldBeSameAs(_second);
        AssertDestinationForgotten();
    }

    [Fact]
    public void ClickingEmptyCanvas_AfterAnAdd_MakesTheElementTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _first);

        Click(worldX: CellSize * 3, worldY: CellSize * 3);

        _selectedState.SelectedInstance.ShouldBeNull();
        AssertDestinationForgotten();
    }

    [Fact]
    public void PressingAHandleOfTheSelectedInstance_KeepsTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);
        _editor!.IsOverHandle = true;

        Hover(_first);
        Frame(_first, push: true, down: true);
        Frame(_first, click: true);

        _tracker.Destination.ShouldBeSameAs(_screen);
        _tracker.HasSelectionChangedSinceAnchor.ShouldBeFalse();
    }

    [Fact]
    public void ClickingALockedSelectedInstance_MakesItTheDestination()
    {
        _first.Locked = true;
        SelectAsAnAddWould(_first, destination: _screen);

        Click(_first);

        _selectedState.SelectedInstance.ShouldBeSameAs(_first);
        AssertDestinationForgotten();
    }

    [Fact]
    public void HoveringAndDraggingNothing_KeepsTheDestination()
    {
        SelectAsAnAddWould(_first, destination: _screen);

        Hover(_first);
        Hover(_second);

        _tracker.Destination.ShouldBeSameAs(_screen);
        _tracker.HasSelectionChangedSinceAnchor.ShouldBeFalse();
    }

    #endregion

    #region Helpers

    private InstanceSave AddInstance(string name)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = "Container", ParentContainer = _screen };
        _screen.Instances.Add(instance);
        return instance;
    }

    private GraphicalUiElement CreateGue(InstanceSave instance, float left)
    {
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Name = instance.Name,
            Tag = instance,
            X = left,
            Y = 0,
            Width = CellSize,
            Height = CellSize,
            Parent = _screenGue,
        };
        gue.ElementGueContainingThis = _screenGue;
        return gue;
    }

    private GraphicalUiElement GueFor(InstanceSave instance) =>
        _allIpsos.Single(gue => gue.Tag == instance);

    /// <summary>
    /// Selects <paramref name="instance"/> and anchors the tracker on <paramref name="destination"/>, the
    /// state an add leaves behind: the new instance is selected but the destination is where it went.
    /// </summary>
    private void SelectAsAnAddWould(InstanceSave instance, object destination)
    {
        _selectedState.SelectedInstance = instance;
        _selectionManager.SelectedGue = GueFor(instance);
        _tracker.Anchor(destination);
    }

    private void AssertDestinationForgotten()
    {
        _tracker.Destination.ShouldBeNull();
        _tracker.HasSelectionChangedSinceAnchor.ShouldBeTrue();
    }

    private void SetCursorAt(float worldX, float worldY, bool push, bool down, bool click, bool doubleClick, bool secondaryPush)
    {
        _camera.WorldToScreen(worldX, worldY, out float screenX, out float screenY);
        _cursor.SetupGet(c => c.X).Returns(screenX);
        _cursor.SetupGet(c => c.Y).Returns(screenY);
        _cursor.SetupGet(c => c.PrimaryPush).Returns(push);
        _cursor.SetupGet(c => c.PrimaryDown).Returns(down);
        _cursor.SetupGet(c => c.PrimaryDownIgnoringIsInWindow).Returns(down);
        _cursor.SetupGet(c => c.PrimaryClick).Returns(click);
        _cursor.SetupGet(c => c.PrimaryDoubleClick).Returns(doubleClick);
        _cursor.SetupGet(c => c.SecondaryPush).Returns(secondaryPush);
    }

    private void Frame(InstanceSave over, bool push = false, bool down = false, bool click = false,
        bool doubleClick = false, bool secondaryPush = false) =>
        Frame(GueFor(over).X + CellSize / 2, CellSize / 2, push, down, click, doubleClick, secondaryPush);

    private void Frame(float worldX, float worldY, bool push = false, bool down = false, bool click = false,
        bool doubleClick = false, bool secondaryPush = false)
    {
        SetCursorAt(worldX, worldY, push, down, click, doubleClick, secondaryPush);
        _selectionManager.Activity(forceNoHighlight: false);
        _selectionManager.LateActivity();
    }

    private void Hover(InstanceSave over) => Frame(over);

    private void Click(InstanceSave over, bool shift = false)
    {
        _isShiftHeld = shift;
        Hover(over);
        Frame(over, push: true, down: true);
        Frame(over, click: true);
        _isShiftHeld = false;
    }

    private void Click(float worldX, float worldY)
    {
        Frame(worldX, worldY);
        Frame(worldX, worldY, push: true, down: true);
        Frame(worldX, worldY, click: true);
    }

    private void DoubleClick(InstanceSave over)
    {
        Hover(over);
        Frame(over, push: true, down: true);
        Frame(over, click: true);
        Frame(over, push: true, down: true, doubleClick: true);
        Frame(over, click: true);
    }

    private void RightClick(InstanceSave over)
    {
        Hover(over);
        Frame(over, secondaryPush: true);
    }

    /// <summary>Drags from below the cells (empty canvas) up and across them.</summary>
    private void Marquee(float fromX, float toX, bool shift = false)
    {
        _isShiftHeld = shift;
        float startY = CellSize * 1.5f;
        float endY = CellSize / 2;
        Frame(fromX, startY);
        Frame(fromX, startY, push: true, down: true);
        Frame((fromX + toX) / 2, endY, down: true);
        Frame(toX, endY, down: true);
        Frame(toX, endY, click: true);
        _isShiftHeld = false;
    }

    #endregion
}

/// <summary>A <see cref="TestWireframeEditor"/> whose handle hit can be switched on.</summary>
internal class TestHandleEditor : TestWireframeEditor
{
    public TestHandleEditor(
        IHotkeyManager hotkeyManager,
        ISelectionManager selectionManager,
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUndoManager undoManager,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        IUiSettingsService uiSettingsService,
        Layer layer,
        System.Drawing.Color lineColor,
        System.Drawing.Color textColor,
        Camera camera,
        IGumCursorState cursor,
        IPluginManager pluginManager,
        ICanvasDisplayScale displayScale)
        : base(hotkeyManager, selectionManager, selectedState, elementCommands, guiCommands,
              fileCommands, setVariableLogic, undoManager, variableInCategoryPropagationLogic,
              wireframeObjectManager, uiSettingsService, layer, lineColor, textColor, camera,
              cursor, pluginManager, displayScale)
    {
    }

    public bool IsOverHandle { get; set; }

    public override bool HasCursorOverHandles => IsOverHandle;
}
