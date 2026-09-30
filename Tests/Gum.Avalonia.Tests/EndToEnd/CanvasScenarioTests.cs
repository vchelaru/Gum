using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Gum.Avalonia.Services;
using Gum.DataTypes;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;
using Vector2 = System.Numerics.Vector2;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the Editor canvas (inventory area CANV, plus the canvas hotkeys and
/// drops): the head's real canvas on a graphics device, driven with pointer and key input one frame
/// per event. Each one checks the saved values against what the gesture did, undoes where the tool
/// records undo, and ends with the shared oracles (<see cref="CanvasHarness.AssertOracles"/>).
/// </summary>
[Trait("Category", "EndToEnd")]
public class CanvasScenarioTests
{
    #region Selecting

    [SkippableFact]
    [Trait("Feature", "CANV-001")]
    [Trait("Feature", "CANV-002")]
    [Trait("Feature", "TREE-007")]
    public void Click_SelectsTheInstanceUnderIt_ShiftClickAdds_AndEmptyCanvasSelectsTheElement()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave left = canvas.AddInstance(button, "Left", "Rectangle", x: 20, y: 20, width: 60, height: 40);
            InstanceSave right = canvas.AddInstance(button, "Right", "Rectangle", x: 200, y: 20, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));

            canvas.Click(canvas.WindowPointOf(230, 40));
            canvas.Project.SelectedState.SelectedInstance.ShouldBeSameAs(right, canvas.Describe());
            // The tree follows a selection made on the canvas.
            canvas.Tree.View.Selection.SelectedNodes.ShouldBe(new[] { canvas.Tree.NodeFor(right) });

            // Quick enough to be a double click if position were ignored.
            canvas.Click(canvas.WindowPointOf(50, 40));
            canvas.Project.SelectedState.SelectedInstance.ShouldBeSameAs(left, canvas.Describe());

            canvas.HoldKey(Key.LeftShift, PhysicalKey.ShiftLeft, RawInputModifiers.Shift);
            canvas.Click(canvas.WindowPointOf(230, 40), RawInputModifiers.Shift);
            canvas.ReleaseKey(Key.LeftShift, PhysicalKey.ShiftLeft);
            canvas.Project.SelectedState.SelectedInstances.ShouldBe(new[] { left, right }, ignoreOrder: true);
            canvas.Tree.View.Selection.SelectedNodes.ShouldBe(new[] { canvas.Tree.NodeFor(left), canvas.Tree.NodeFor(right) }, ignoreOrder: true);

            canvas.Click(canvas.WindowPointOf(500, 400));
            canvas.Project.SelectedState.SelectedInstance.ShouldBeNull();
            canvas.Project.SelectedState.SelectedElement.ShouldBeSameAs(button);

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "CANV-003")]
    public void MarqueeDrag_SelectsTheInstancesItTouches()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave first = canvas.AddInstance(button, "First", "Rectangle", x: 200, y: 200, width: 40, height: 40);
            InstanceSave second = canvas.AddInstance(button, "Second", "Rectangle", x: 260, y: 200, width: 40, height: 40);
            canvas.AddInstance(button, "Far", "Rectangle", x: 400, y: 400, width: 40, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));

            canvas.Drag(canvas.WindowPointOf(180, 180), canvas.WindowPointOf(320, 260));

            canvas.Project.SelectedState.SelectedInstances.ShouldBe(new[] { first, second }, ignoreOrder: true, canvas.Describe());

            canvas.AssertOracles();
        });
    }

    #endregion

    #region Moving

    [SkippableFact]
    [Trait("Feature", "CANV-005")]
    [Trait("Feature", "CANV-006")]
    [Trait("Feature", "KEY-001")]
    [Trait("Feature", "KEY-002")]
    public void DragMove_SavesTheNewPosition_UndoRestoresTheFiles_AndShiftLocksToAnAxis()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            canvas.Click(canvas.WindowPointOf(70, 60));
            canvas.Project.SelectedState.SelectedInstance.ShouldBeSameAs(box, canvas.Describe());
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            canvas.Drag(canvas.WindowPointOf(70, 60), canvas.WindowPointOf(110, 90));

            canvas.SavedValue(button, "Box.X").ShouldBe(80f);
            canvas.SavedValue(button, "Box.Y").ShouldBe(70f);

            canvas.Undo();
            button.DefaultState!.GetValue("Box.X").ShouldBe(40f);
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing the move should restore the files");

            canvas.Redo();
            canvas.SavedValue(button, "Box.X").ShouldBe(80f);
            canvas.SavedValue(button, "Box.Y").ShouldBe(70f);

            // Mostly sideways with Shift held: only X changes. Shift pressed once the drag has
            // started locks the axis too.
            canvas.PressButton(canvas.WindowPointOf(110, 90));
            canvas.DragTo(canvas.WindowPointOf(130, 93));
            canvas.HoldKey(Key.LeftShift, PhysicalKey.ShiftLeft, RawInputModifiers.Shift);
            canvas.DragTo(canvas.WindowPointOf(150, 96), RawInputModifiers.Shift);
            canvas.ReleaseButton(RawInputModifiers.Shift);
            canvas.ReleaseKey(Key.LeftShift, PhysicalKey.ShiftLeft);

            canvas.SavedValue(button, "Box.X").ShouldBe(120f);
            canvas.SavedValue(button, "Box.Y").ShouldBe(70f);

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "CANV-002")]
    [Trait("Feature", "CANV-006")]
    public void ShiftHeldBeforeTheDrag_LocksTheMoveToAnAxis_OnASelectedInstance_AndAddsOtherwise()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            InstanceSave other = canvas.AddInstance(button, "Other", "Rectangle", x: 200, y: 40, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(box));

            canvas.HoldKey(Key.LeftShift, PhysicalKey.ShiftLeft, RawInputModifiers.Shift);
            canvas.Drag(canvas.WindowPointOf(70, 60), canvas.WindowPointOf(110, 66), RawInputModifiers.Shift);
            canvas.ReleaseKey(Key.LeftShift, PhysicalKey.ShiftLeft);

            canvas.Project.SelectedState.SelectedInstance.ShouldBeSameAs(box, canvas.Describe());
            canvas.SavedValue(button, "Box.X").ShouldBe(80f);
            canvas.SavedValue(button, "Box.Y").ShouldBe(40f);

            // A Shift click on the selected instance leaves the selection alone.
            canvas.HoldKey(Key.LeftShift, PhysicalKey.ShiftLeft, RawInputModifiers.Shift);
            canvas.Click(canvas.WindowPointOf(100, 50), RawInputModifiers.Shift);
            canvas.ReleaseKey(Key.LeftShift, PhysicalKey.ShiftLeft);
            canvas.Project.SelectedState.SelectedInstances.ShouldBe(new[] { box });

            // Shift on an instance that isn't selected still adds it, by a drag as by a click.
            canvas.HoldKey(Key.LeftShift, PhysicalKey.ShiftLeft, RawInputModifiers.Shift);
            canvas.Drag(canvas.WindowPointOf(230, 60), canvas.WindowPointOf(250, 70), RawInputModifiers.Shift);
            canvas.ReleaseKey(Key.LeftShift, PhysicalKey.ShiftLeft);
            canvas.Project.SelectedState.SelectedInstances.ShouldBe(new[] { box, other }, ignoreOrder: true, canvas.Describe());
            canvas.SavedValue(button, "Other.X").ShouldBe(200f);

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "CANV-005")]
    public void CtrlOrAltHeldBeforeTheDrag_StillMovesTheSelectedInstance()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(box));

            canvas.HoldKey(Key.LeftCtrl, PhysicalKey.ControlLeft, RawInputModifiers.Control);
            canvas.Drag(canvas.WindowPointOf(70, 60), canvas.WindowPointOf(90, 70), RawInputModifiers.Control);
            canvas.ReleaseKey(Key.LeftCtrl, PhysicalKey.ControlLeft);

            canvas.Project.SelectedState.SelectedInstance.ShouldBeSameAs(box, canvas.Describe());
            canvas.SavedValue(button, "Box.X").ShouldBe(60f);
            canvas.SavedValue(button, "Box.Y").ShouldBe(50f);

            canvas.HoldKey(Key.LeftAlt, PhysicalKey.AltLeft, RawInputModifiers.Alt);
            canvas.Drag(canvas.WindowPointOf(80, 60), canvas.WindowPointOf(100, 70), RawInputModifiers.Alt);
            canvas.ReleaseKey(Key.LeftAlt, PhysicalKey.AltLeft);

            canvas.Project.SelectedState.SelectedInstance.ShouldBeSameAs(box, canvas.Describe());
            canvas.SavedValue(button, "Box.X").ShouldBe(80f);
            canvas.SavedValue(button, "Box.Y").ShouldBe(60f);

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "CANV-005")]
    [Trait("Feature", "CANV-007")]
    [Trait("Feature", "CANV-014")]
    public void DragsInSmallSteps_KeepTheGrabbedPointUnderThePointer()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            InstanceSave shape = canvas.AddInstance(button, "Shape", "Polygon", x: 300, y: 300);

            // Two-pixel moves, as a mouse reports them: the first three stay inside the 6-pixel
            // dead zone, and the drag still ends exactly under the pointer.
            canvas.Tree.Click(canvas.Tree.NodeFor(box));
            canvas.Drag(canvas.WindowPointOf(70, 60), canvas.WindowPointOf(90, 60), steps: 10);
            canvas.SavedValue(button, "Box.X").ShouldBe(60f, canvas.Describe());
            canvas.SavedValue(button, "Box.Y").ShouldBe(40f);

            // Wandering back inside the dead zone mid-drag still moves the box. Pressed where the
            // last drag ended and released within 4 pixels of it: a drag's release is not the
            // first click of a double click, so this does not punch through (#5286).
            canvas.PressButton(canvas.WindowPointOf(90, 60));
            canvas.DragTo(canvas.WindowPointOf(100, 60));
            canvas.DragTo(canvas.WindowPointOf(94, 60));
            canvas.ReleaseButton();
            canvas.Project.SelectedState.SelectedInstance.ShouldBeSameAs(box, canvas.Describe());
            canvas.SavedValue(button, "Box.X").ShouldBe(64f, canvas.Describe());

            // The bottom-right resize handle sits just outside the corner (124, 80).
            canvas.Drag(canvas.WindowPointOf(130, 86), canvas.WindowPointOf(150, 96), steps: 5);
            canvas.SavedValue(button, "Box.Width").ShouldBe(80f, canvas.Describe());
            canvas.SavedValue(button, "Box.Height").ShouldBe(50f);

            // The standard polygon is the square (0,0) (32,0) (32,32) (0,32), closed.
            canvas.Tree.Click(canvas.Tree.NodeFor(shape));
            canvas.Drag(canvas.WindowPointOf(332, 332), canvas.WindowPointOf(352, 342), steps: 5);
            SavedPoints(canvas, button).ShouldBe(new[] { new Vector2(0, 0), new Vector2(32, 0), new Vector2(52, 42), new Vector2(0, 32), new Vector2(0, 0) });

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "KEY-012")]
    [Trait("Feature", "KEY-013")]
    public void ArrowKeys_NudgeTheSelectedInstance_ByOneOrWithShiftFivePixels()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            canvas.Click(canvas.WindowPointOf(70, 60));
            canvas.Project.SelectedState.SelectedInstance.ShouldBeSameAs(box);
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            canvas.Press(Key.Right, PhysicalKey.ArrowRight);
            canvas.Press(Key.Down, PhysicalKey.ArrowDown, RawInputModifiers.Shift);

            canvas.SavedValue(button, "Box.X").ShouldBe(41f);
            canvas.SavedValue(button, "Box.Y").ShouldBe(45f);

            canvas.Undo();
            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing both nudges should restore the files");

            canvas.AssertOracles();
        });
    }

    #endregion

    #region Resizing and rotating

    [SkippableFact]
    [Trait("Feature", "CANV-007")]
    [Trait("Feature", "CANV-008")]
    [Trait("Feature", "CANV-009")]
    public void ResizeHandles_ResizeFromTheCorner_ShiftKeepsTheAspectRatio_AndAltResizesFromTheCenter()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 100, y: 100, width: 80, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(box));
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            // The bottom-right handle sits just outside the corner (180, 140).
            canvas.Drag(canvas.WindowPointOf(186, 146), canvas.WindowPointOf(206, 156));

            canvas.SavedValue(button, "Box.X").ShouldBe(100f);
            canvas.SavedValue(button, "Box.Width").ShouldBe(100f, canvas.Describe());
            canvas.SavedValue(button, "Box.Height").ShouldBe(50f);

            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing the resize should restore the files");

            // Shift: the 2:1 shape holds while the corner is dragged mostly sideways.
            canvas.HoldKey(Key.LeftShift, PhysicalKey.ShiftLeft, RawInputModifiers.Shift);
            canvas.Drag(canvas.WindowPointOf(186, 146), canvas.WindowPointOf(226, 150), RawInputModifiers.Shift);
            canvas.ReleaseKey(Key.LeftShift, PhysicalKey.ShiftLeft);
            float width = (float)canvas.SavedValue(button, "Box.Width")!;
            float height = (float)canvas.SavedValue(button, "Box.Height")!;
            width.ShouldBeGreaterThan(80f);
            (width / height).ShouldBe(2f, tolerance: 0.05f);
            canvas.Undo();

            // Alt on the right edge handle (just right of (180, 120)): the left edge moves out as
            // far as the right one, so the center stays put.
            canvas.HoldKey(Key.LeftAlt, PhysicalKey.AltLeft, RawInputModifiers.Alt);
            canvas.Drag(canvas.WindowPointOf(186, 120), canvas.WindowPointOf(206, 120), RawInputModifiers.Alt);
            canvas.ReleaseKey(Key.LeftAlt, PhysicalKey.AltLeft);
            canvas.SavedValue(button, "Box.Width").ShouldBe(120f);
            canvas.SavedValue(button, "Box.X").ShouldBe(80f);

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "CANV-012")]
    [Trait("Feature", "CANV-013")]
    public void RotationHandle_RotatesAroundTheOrigin_AndShiftSnapsTo15Degrees()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 100, y: 100, width: 80, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(box));
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            // The handle sits 24 pixels right of the box, level with its origin (100, 100).
            // Dragging it straight up from the origin is a quarter turn counterclockwise.
            canvas.Drag(canvas.WindowPointOf(204, 100), canvas.WindowPointOf(100, 0), steps: 6);

            canvas.SavedValue(button, "Box.Rotation").ShouldBe(90f, canvas.Describe());

            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing the rotation should restore the files");

            // 20 degrees snaps to 15.
            float radians = MathF.PI * 20 / 180;
            Point target = canvas.WindowPointOf(100 + 100 * MathF.Cos(radians), 100 - 100 * MathF.Sin(radians));
            canvas.HoldKey(Key.LeftShift, PhysicalKey.ShiftLeft, RawInputModifiers.Shift);
            canvas.Drag(canvas.WindowPointOf(204, 100), target, RawInputModifiers.Shift);
            canvas.ReleaseKey(Key.LeftShift, PhysicalKey.ShiftLeft);

            canvas.SavedValue(button, "Box.Rotation").ShouldBe(15f);

            canvas.AssertOracles();
        });
    }

    #endregion

    #region Polygon points

    [SkippableFact]
    [Trait("Feature", "CANV-014")]
    public void PolygonPoints_DragAddAndDelete_SaveThePoints()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave shape = canvas.AddInstance(button, "Shape", "Polygon", x: 100, y: 100);
            canvas.Tree.Click(canvas.Tree.NodeFor(shape));
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            // The standard polygon is the square (0,0) (32,0) (32,32) (0,32), closed.
            canvas.Drag(canvas.WindowPointOf(132, 132), canvas.WindowPointOf(152, 142));
            SavedPoints(canvas, button).ShouldBe(new[] { new Vector2(0, 0), new Vector2(32, 0), new Vector2(52, 42), new Vector2(0, 32), new Vector2(0, 0) });

            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing the point drag should restore the files");

            // Hovering an edge's middle shows the add-point marker; a click there adds a point.
            canvas.MoveTo(canvas.WindowPointOf(116, 100));
            canvas.Click(canvas.WindowPointOf(116, 100));
            SavedPoints(canvas, button).ShouldBe(new[] { new Vector2(0, 0), new Vector2(16, 0), new Vector2(32, 0), new Vector2(32, 32), new Vector2(0, 32), new Vector2(0, 0) });

            // The new point is selected, so Delete removes it.
            canvas.Press(Key.Delete, PhysicalKey.Delete);
            SavedPoints(canvas, button).ShouldBe(new[] { new Vector2(0, 0), new Vector2(32, 0), new Vector2(32, 32), new Vector2(0, 32), new Vector2(0, 0) });
            button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Shape" }, "Delete removed the point, not the polygon");

            canvas.AssertOracles();
        });
    }

    private static List<Vector2> SavedPoints(CanvasHarness canvas, ElementSave element) =>
        ((IEnumerable<Vector2>)canvas.SavedElement(element).DefaultState!.GetVariableListSave("Shape.Points")!.ValueAsIList).ToList();

    #endregion

    #region Camera

    [SkippableFact]
    [Trait("Feature", "CANV-020")]
    [Trait("Feature", "CANV-021")]
    [Trait("Feature", "CANV-022")]
    [Trait("Feature", "CANV-023")]
    [Trait("Feature", "KEY-014")]
    [Trait("Feature", "KEY-015")]
    public void ZoomAndPan_MoveTheCamera_AndADragAtTwiceTheZoomMovesHalfAsFar()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(box));
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            // The toolbar's zoom-in button, then Ctrl+wheel and Ctrl+minus back out.
            canvas.Input.Click(canvas.ZoomInButton);
            canvas.Frame();
            canvas.Editor.PercentZoomLevel.Value.ShouldBeGreaterThan(100);
            canvas.Camera.Zoom.ShouldBe(canvas.Editor.PercentZoomLevel.Value / 100f);

            int beforeWheel = canvas.Editor.PercentZoomLevel.Value;
            canvas.Wheel(canvas.WindowPointOf(300, 300), 1, RawInputModifiers.Control);
            canvas.Editor.PercentZoomLevel.Value.ShouldBeGreaterThan(beforeWheel);

            canvas.Press(Key.OemMinus, PhysicalKey.Minus, RawInputModifiers.Control);
            canvas.Editor.PercentZoomLevel.Value.ShouldBeLessThan(canvas.Editor.ZoomLevels.Max(level => level.Value));

            // Ctrl+Right moves the camera; a middle-button drag pans it with the pointer.
            float cameraX = canvas.Camera.X;
            canvas.Press(Key.Right, PhysicalKey.ArrowRight, RawInputModifiers.Control);
            canvas.Camera.X.ShouldBeGreaterThan(cameraX);

            canvas.Editor.PercentZoom = 200;
            canvas.Frame();
            float panStartX = canvas.Camera.X;
            float panStartY = canvas.Camera.Y;
            Point panFrom = new Point(500, 400);
            canvas.Drag(panFrom, panFrom + new Point(-40, -20), button: MouseButton.Middle);
            canvas.Camera.X.ShouldBe(panStartX + 20, tolerance: 0.5f);
            canvas.Camera.Y.ShouldBe(panStartY + 10, tolerance: 0.5f);

            // Dragging the vertical scroll bar's thumb down scrolls the view down.
            float scrollStartY = canvas.Camera.Y;
            Thumb thumb = canvas.ScrollBarThumb(Orientation.Vertical);
            Point thumbCenter = canvas.Input.CenterOf(thumb);
            canvas.Input.Drag(thumbCenter, thumbCenter + new Point(0, 30));
            canvas.Frame();
            canvas.Camera.Y.ShouldBeGreaterThan(scrollStartY);

            // At 200%, 40 pointer pixels is 20 world units.
            canvas.ResetCamera();
            canvas.Editor.PercentZoom = 200;
            canvas.Frame();
            Point grab = canvas.WindowPointOf(70, 60);
            canvas.Drag(grab, grab + new Point(40, 40));
            canvas.SavedValue(button, "Box.X").ShouldBe(60f);
            canvas.SavedValue(button, "Box.Y").ShouldBe(60f);

            canvas.Undo();
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "zooming and panning change no file, and undo reverts the move");

            canvas.AssertOracles();
        });
    }

    // #5540: a Space released after focus left the canvas never reached it, so the next left drag
    // panned the camera instead of moving the selection.
    [SkippableFact]
    [Trait("Feature", "CANV-022")]
    public void SpaceReleasedAfterFocusLeftTheCanvas_DoesNotTurnTheNextDragIntoAPan()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 40, y: 40, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(box));
            canvas.HoldKey(Key.Space, PhysicalKey.Space, RawInputModifiers.None);

            canvas.SnapToGridCheckBox.Focus();
            canvas.Frame();
            canvas.Drag(canvas.WindowPointOf(70, 60), canvas.WindowPointOf(110, 90));

            canvas.SavedValue(button, "Box.X").ShouldBe(80f, canvas.Describe());
            canvas.SavedValue(button, "Box.Y").ShouldBe(70f);
            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "CANV-027")]
    [Trait("Feature", "CANV-028")]
    public void RulerClick_AddsAGuide_ThatDragsAndIsRemovedWhenDraggedOffTheCanvas()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();
            List<float> guidesBefore = canvas.TopRuler.GuideValues.ToList();

            // The top ruler runs along the canvas's top edge; a guide's value is its world X.
            canvas.Click(canvas.WindowPointOf(100, 0).WithY(canvas.CanvasTopInWindow + 4));
            canvas.TopRuler.GuideValues.Except(guidesBefore).ShouldBe(new[] { 100f }, canvas.Describe());

            // Grab the guide anywhere along its length and drag it right.
            Point onGuide = canvas.WindowPointOf(100, 300);
            canvas.Drag(onGuide, onGuide + new Point(30, 0));
            canvas.TopRuler.GuideValues.Except(guidesBefore).ShouldBe(new[] { 130f });

            // Dropped past the canvas's right edge, it is gone.
            Point moved = onGuide + new Point(30, 0);
            canvas.Drag(moved, new Point(canvas.CanvasRightInWindow + 4, moved.Y));
            canvas.TopRuler.GuideValues.Except(guidesBefore).ShouldBeEmpty();

            canvas.Tree.SnapshotFiles().ShouldMatch(start, "guides are not saved in the project");
            canvas.AssertOracles();
        });
    }

    #endregion

    #region Drops

    [SkippableFact]
    [Trait("Feature", "DRAG-016")]
    [Trait("Feature", "DRAG-012")]
    public void StandardChipDrop_AddsAnInstanceWhereItLands_AndParentsItToAContainerUnderIt()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave panel = canvas.AddInstance(button, "Panel", "Container", x: 300, y: 300, width: 200, height: 200);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            ProjectFileSnapshot start = canvas.Tree.SnapshotFiles();

            DragDropEffects effects = canvas.DropOnCanvas(canvas.WindowPointOf(60, 70), StandardChip("Sprite"));

            effects.ShouldBe(DragDropEffects.Copy);
            InstanceSave sprite = button.Instances.Except(new[] { panel }).ShouldHaveSingleItem();
            sprite.BaseType.ShouldBe("Sprite");
            canvas.SavedValue(button, $"{sprite.Name}.X").ShouldBe(60f);
            canvas.SavedValue(button, $"{sprite.Name}.Y").ShouldBe(70f);

            canvas.Undo();
            // Undo restores copies, so instances are compared by name from here on.
            button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Panel" });
            canvas.Tree.SnapshotFiles().ShouldMatch(start, "undoing the drop should restore the files");
            canvas.Redo();
            sprite = button.Instances.Single(instance => instance.BaseType == "Sprite");
            canvas.SavedValue(button, $"{sprite.Name}.X").ShouldBe(60f);

            // A drop parents the new instance only to the selected instance under it.
            canvas.Tree.Click(canvas.Tree.NodeFor(button.Instances.Single(instance => instance.Name == "Panel")));
            canvas.DropOnCanvas(canvas.WindowPointOf(350, 360), StandardChip("Text"));
            InstanceSave text = button.Instances.Single(instance => instance.BaseType == "Text");
            canvas.SavedValue(button, $"{text.Name}.Parent").ShouldBe(panel.Name);
            // Placed relative to the panel at (300, 300).
            canvas.SavedValue(button, $"{text.Name}.X").ShouldBe(50f);
            canvas.SavedValue(button, $"{text.Name}.Y").ShouldBe(60f);

            canvas.AssertOracles();
        });
    }

    [SkippableFact]
    [Trait("Feature", "DRAG-013")]
    public void TextureFileDrop_AddsASpriteShowingIt_AndOnASpriteSetsItsTexture()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave existing = canvas.AddInstance(button, "Existing", "Sprite", x: 300, y: 300, width: 64, height: 64);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            string texture = Path.Combine(canvas.Project.ProjectFolder, "Hero.png");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "ExampleSpriteFrame.png"), texture);

            canvas.DropOnCanvas(canvas.WindowPointOf(60, 70), FileDrop(texture)).ShouldBe(DragDropEffects.Copy);

            InstanceSave added = button.Instances.Except(new[] { existing }).ShouldHaveSingleItem();
            added.BaseType.ShouldBe("Sprite");
            canvas.SavedValue(button, $"{added.Name}.SourceFile").ShouldBe("Hero.png");
            canvas.SavedValue(button, $"{added.Name}.X").ShouldBe(60f);
            canvas.SavedValue(button, $"{added.Name}.Y").ShouldBe(70f);

            canvas.Project.Dialogs.AnswerNext<ChoiceDialogViewModel>(dialog =>
            {
                dialog.SelectedValue = dialog.OptionValues.Single(option => option.StartsWith("Set source file on Existing", StringComparison.Ordinal));
                return true;
            });
            canvas.DropOnCanvas(canvas.WindowPointOf(330, 330), FileDrop(texture));
            button.Instances.Count.ShouldBe(2, "a drop on a sprite sets its texture rather than adding one");
            canvas.SavedValue(button, "Existing.SourceFile").ShouldBe("Hero.png");

            canvas.AssertOracles();
        });
    }

    // What the Standards palette puts in its drag (AvaloniaStandardsPalette).
    private static DataTransfer StandardChip(string typeName)
    {
        DataTransfer data = new DataTransfer();
        data.Add(DataTransferItem.Create(AvaloniaDragFormats.StandardElementName, typeName));
        return data;
    }

    // What a file manager puts in its drag.
    private static DataTransfer FileDrop(string path)
    {
        Mock<IStorageFile> file = new Mock<IStorageFile>();
        file.SetupGet(f => f.Path).Returns(new Uri(path));
        DataTransfer data = new DataTransfer();
        data.Add(DataTransferItem.CreateFile(file.Object));
        return data;
    }

    #endregion

    private static void OnCanvas(Action<CanvasHarness> scenario)
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            scenario(canvas);
        });
    }
}
