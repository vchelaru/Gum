using System.Linq;
using System.Numerics;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using Gum.Wireframe.Editors;
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using GumCommands = Gum.Commands;

namespace Gum.Presentation.Tests;

/// <summary>
/// #5934: dragging a polygon's scale handle rewrites its Points and moves its X/Y so the opposite
/// edge stays put, as one edit.
/// </summary>
public class PolygonScaleInputHandlerTests : BaseTestClass
{
    private readonly SystemManagers? _previousDefault;

    private readonly InstanceSave _instance;
    private readonly ComponentSave _component;
    private readonly LinePolygon _polygon;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly Mock<GumCommands.IFileCommands> _fileCommands;
    private readonly Mock<IGumCursorState> _cursor;
    private readonly Mock<IHotkeyManager> _hotkeys;
    private readonly KeyCombination _resizeFromCenter;
    private readonly EditorContext _context;
    private readonly Mock<IResizeHandlesVisual> _handles;
    private Vector2 _lastCursorOffset;

    public PolygonScaleInputHandlerTests()
    {
        _previousDefault = SystemManagers.Default;
        SystemManagers.Default = new SystemManagers
        {
            Renderer = new Renderer(),
            ShapeManager = new ShapeManager()
        };
        SystemManagers.Default.ShapeManager.Managers = SystemManagers.Default;
        SystemManagers.Default.Renderer.AddLayer();

        _polygon = new LinePolygon();
        _polygon.SetPoints(new[]
        {
            new Vector2(0, 0), new Vector2(40, 0), new Vector2(40, 40), new Vector2(0, 40), new Vector2(0, 0)
        });

        _instance = new InstanceSave { Name = "Poly", BaseType = "Polygon" };
        _component = new ComponentSave { Name = "Component" };
        _component.Instances.Add(_instance);
        _component.States.Add(new StateSave { Name = "Default", ParentContainer = _component });

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(s => s.SelectedElement).Returns(_component);
        selectedState.SetupGet(s => s.SelectedInstance).Returns(_instance);
        selectedState.SetupGet(s => s.SelectedStateSave).Returns(_component.DefaultState);

        _cursor = new Mock<IGumCursorState>();
        _cursor.SetupGet(c => c.X).Returns(100);
        _cursor.SetupGet(c => c.Y).Returns(100);
        _cursor.SetupGet(c => c.PrimaryDown).Returns(true);

        _resizeFromCenter = KeyCombination.Alt();
        _hotkeys = new Mock<IHotkeyManager>();
        _hotkeys.SetupGet(h => h.ResizeFromCenter).Returns(_resizeFromCenter);
        _hotkeys.SetupGet(h => h.LockMovementToAxis).Returns(KeyCombination.Shift());

        _elementCommands = new Mock<IElementCommands>();
        _fileCommands = new Mock<GumCommands.IFileCommands>();

        _context = EditorContextTestHelper.Create(
            selectedState: selectedState.Object,
            hotkeyManager: _hotkeys.Object,
            cursor: _cursor.Object,
            elementCommands: _elementCommands.Object,
            fileCommands: _fileCommands.Object);
        _context.SelectedObjects.Add(new GraphicalUiElement(_polygon) { Tag = _instance });

        _handles = new Mock<IResizeHandlesVisual>();
        _handles.SetupGet(h => h.Visible).Returns(true);
    }

    public override void Dispose()
    {
        SystemManagers.Default = _previousDefault!;
        base.Dispose();
    }

    private PolygonScaleInputHandler Grab(ResizeSide side)
    {
        _handles.Setup(h => h.GetSideOver(It.IsAny<float>(), It.IsAny<float>())).Returns(side);
        PolygonScaleInputHandler handler = new PolygonScaleInputHandler(_context, _handles.Object);
        _context.GrabbedState.HandlePush();
        handler.HandlePush(0, 0).ShouldBeTrue();
        return handler;
    }

    // x and y are the total offset from where the cursor was pushed, as the cursor reports it.
    private void MoveCursorBy(PolygonScaleInputHandler handler, float x, float y)
    {
        _cursor.SetupGet(c => c.XChange).Returns(x - _lastCursorOffset.X);
        _cursor.SetupGet(c => c.YChange).Returns(y - _lastCursorOffset.Y);
        _cursor.SetupGet(c => c.X).Returns(100 + x);
        _cursor.SetupGet(c => c.Y).Returns(100 + y);
        _lastCursorOffset = new Vector2(x, y);
        handler.HandleDrag();
    }

    private Vector2[] SavedPoints()
    {
        VariableListSave<Vector2> list = (VariableListSave<Vector2>)_component.DefaultState!.VariableLists
            .Single(item => item.Name == "Poly.Points");
        return list.ValueAsIList!.Cast<Vector2>().ToArray();
    }

    [Fact]
    public void HandleRelease_ShouldSaveScaledPointsAndAutoSave_WhenRightEdgeWasDragged()
    {
        PolygonScaleInputHandler handler = Grab(ResizeSide.Right);

        MoveCursorBy(handler, 40, 0);
        handler.HandleRelease();

        _polygon.PointAt(1).ShouldBe(new Vector2(80, 0));
        SavedPoints()[2].ShouldBe(new Vector2(80, 40));
        _fileCommands.Verify(f => f.TryAutoSaveElement(_component), Times.Once);
    }

    [Fact]
    public void HandleDrag_ShouldMovePositionByTheShift_WhenLeftEdgeIsDragged()
    {
        PolygonScaleInputHandler handler = Grab(ResizeSide.Left);

        MoveCursorBy(handler, -40, 0);

        _elementCommands.Verify(e => e.ModifyVariable("X", It.Is<float>(v => System.Math.Abs(v + 40) < 0.001f), _instance),
            Times.Once);
    }

    [Fact]
    public void HandleDrag_ShouldNotRepeatThePositionChange_WhenDraggedInSeveralFrames()
    {
        PolygonScaleInputHandler handler = Grab(ResizeSide.Left);

        MoveCursorBy(handler, -20, 0);
        MoveCursorBy(handler, -40, 0);

        // The two calls move the position by -20 and then another -20; they never add up to more.
        float total = 0;
        foreach (var invocation in _elementCommands.Invocations.Where(i => i.Method.Name == "ModifyVariable"))
        {
            total += (float)invocation.Arguments[1];
        }
        total.ShouldBe(-40, 0.001f);
    }

    [Fact]
    public void HandleDrag_ShouldScaleAboutTheCenter_WhenResizeFromCenterIsHeld()
    {
        _hotkeys.Setup(h => h.IsPressedInControl(_resizeFromCenter)).Returns(true);
        PolygonScaleInputHandler handler = Grab(ResizeSide.Right);

        MoveCursorBy(handler, 20, 0);

        _polygon.PointAt(1).ShouldBe(new Vector2(80, 0));
        _elementCommands.Verify(e => e.ModifyVariable("X", It.Is<float>(v => System.Math.Abs(v + 20) < 0.001f), _instance),
            Times.Once);
    }

    [Fact]
    public void HandleDrag_ShouldScaleBothAxesEqually_WhenLockMovementToAxisIsHeld()
    {
        KeyCombination lockAspect = KeyCombination.Shift();
        _hotkeys.SetupGet(h => h.LockMovementToAxis).Returns(lockAspect);
        _hotkeys.Setup(h => h.IsPressedInControl(lockAspect)).Returns(true);
        PolygonScaleInputHandler handler = Grab(ResizeSide.BottomRight);

        MoveCursorBy(handler, 40, 10);

        _polygon.PointAt(2).ShouldBe(new Vector2(80, 80));
    }

    [Fact]
    public void HandleRelease_ShouldNotSaveAnything_WhenTheCursorNeverMovedEnoughToDrag()
    {
        PolygonScaleInputHandler handler = Grab(ResizeSide.Right);

        MoveCursorBy(handler, 2, 0);
        handler.HandleRelease();

        _polygon.PointAt(1).ShouldBe(new Vector2(40, 0));
        _fileCommands.Verify(f => f.TryAutoSaveElement(It.IsAny<ElementSave>()), Times.Never);
    }

    [Fact]
    public void HandlePush_ShouldNotClaimTheGesture_WhenAPointNodeIsUnderTheCursor()
    {
        _handles.Setup(h => h.GetSideOver(It.IsAny<float>(), It.IsAny<float>())).Returns(ResizeSide.TopLeft);
        PolygonScaleInputHandler handler = new PolygonScaleInputHandler(_context, _handles.Object,
            isOverPointNode: (_, _) => true);

        handler.HandlePush(0, 0).ShouldBeFalse();
        handler.IsActive.ShouldBeFalse();
        handler.GetCursorToShow(0, 0).ShouldBeNull();
    }

    [Fact]
    public void HandlePush_ShouldNotClaimTheGesture_WhenNoHandleIsUnderTheCursor()
    {
        _handles.Setup(h => h.GetSideOver(It.IsAny<float>(), It.IsAny<float>())).Returns(ResizeSide.None);
        PolygonScaleInputHandler handler = new PolygonScaleInputHandler(_context, _handles.Object);

        handler.HandlePush(0, 0).ShouldBeFalse();
        handler.IsActive.ShouldBeFalse();
    }
}
