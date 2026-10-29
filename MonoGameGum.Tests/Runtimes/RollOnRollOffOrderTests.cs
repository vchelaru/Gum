using System.Collections.Generic;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// The order and the conditions in which the cursor walk raises RollOn, RollOff, HoverOver and RollOver
/// when the element under the cursor changes (#7057). When the cursor moves straight from one element
/// to another, RollOff on the old element is raised before RollOn on the new one (leave, then enter).
/// </summary>
public class RollOnRollOffOrderTests : BaseTestClass
{
    #region Helpers

    private readonly List<string> _log = new();
    private readonly Mock<IInputReceiverKeyboard> _keyboard = new();
    private readonly List<GraphicalUiElement> _roots = new();
    private readonly Mock<ICursor> _cursor;

    public RollOnRollOffOrderTests()
    {
        _cursor = CreateCursor();
    }

    private static Mock<ICursor> CreateCursor()
    {
        Mock<ICursor> cursor = new();
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.SetupProperty(c => c.VisualRightPushed);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        return cursor;
    }

    private static InteractiveGue CreateBox(float x, float y, float size, bool hasEvents = true)
    {
        InteractiveGue box = new(new InvisibleRenderable());
        box.WidthUnits = DimensionUnitType.Absolute;
        box.HeightUnits = DimensionUnitType.Absolute;
        box.X = x;
        box.Y = y;
        box.Width = size;
        box.Height = size;
        box.HasEvents = hasEvents;
        return box;
    }

    // Adds the box to the roots, which are tested from the last one to the first.
    private InteractiveGue AddRoot(string name, float x, float y, float size, bool all = false)
    {
        InteractiveGue box = CreateBox(x, y, size);
        _roots.Add(box);
        Track(box, name, all);
        return box;
    }

    // Logs RollOn and RollOff, and with all also HoverOver, RollOver, Dragging and LosePush.
    private void Track(InteractiveGue box, string name, bool all = false)
    {
        box.RollOn += (_, _) => _log.Add("RollOn " + name);
        box.RollOff += (_, _) => _log.Add("RollOff " + name);
        if (all)
        {
            box.HoverOver += (_, _) => _log.Add("HoverOver " + name);
            box.RollOver += (_, _) => _log.Add("RollOver " + name);
            box.Dragging += (_, _) => _log.Add("Dragging " + name);
            box.LosePush += (_, _) => _log.Add("LosePush " + name);
        }
    }

    private static void SetPosition(Mock<ICursor> cursor, float x, float y, int xChange = 0)
    {
        cursor.Setup(c => c.X).Returns((int)x);
        cursor.Setup(c => c.Y).Returns((int)y);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        cursor.Setup(c => c.XChange).Returns(xChange);
    }

    // Walks one frame with the cursor at the position and returns the events it raised.
    private List<string> Frame(float x, float y, int xChange = 0)
    {
        return Frame(_cursor, x, y, xChange);
    }

    private List<string> Frame(Mock<ICursor> cursor, float x, float y, int xChange = 0)
    {
        SetPosition(cursor, x, y, xChange);
        _log.Clear();
        GueInteractiveExtensionMethods.DoUiActivityRecursively(_roots, cursor.Object, _keyboard.Object, 0);
        return new List<string>(_log);
    }

    private void SetButtons(Mock<ICursor> cursor, bool push, bool down)
    {
        cursor.Setup(c => c.PrimaryPush).Returns(push);
        cursor.Setup(c => c.PrimaryDown).Returns(down);
        cursor.Setup(c => c.PrimaryClick).Returns(false);
    }

    #endregion

    #region Moving between two elements (#7057)

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenCursorMovesBetweenAdjacentSiblingsInOneFrame()
    {
        AddRoot("A", 0, 0, 10);
        InteractiveGue buttonB = AddRoot("B", 10, 0, 10);
        Frame(5, 5).ShouldBe(new[] { "RollOn A" });

        List<string> events = Frame(15, 5);

        events.ShouldBe(new[] { "RollOff A", "RollOn B" });
        _cursor.Object.VisualOver.ShouldBe(buttonB);
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenCursorMovesBackFromBToA()
    {
        AddRoot("A", 0, 0, 10);
        AddRoot("B", 10, 0, 10);
        Frame(5, 5);
        Frame(15, 5);

        Frame(5, 5).ShouldBe(new[] { "RollOff B", "RollOn A" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenTheElementsAreFarApart()
    {
        AddRoot("A", 0, 0, 10);
        AddRoot("B", 100, 0, 10);
        Frame(5, 5);

        Frame(105, 5).ShouldBe(new[] { "RollOff A", "RollOn B" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenTheNewElementIsTheChildOfAnotherContainer()
    {
        AddRoot("A", 0, 0, 10);
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.X = 100;
        container.Width = 50;
        container.Height = 50;
        container.HasEvents = false;
        InteractiveGue buttonB = CreateBox(10, 0, 10);
        container.AddChild(buttonB);
        Track(buttonB, "B");
        _roots.Add(container);
        Frame(5, 5);

        Frame(115, 5).ShouldBe(new[] { "RollOff A", "RollOn B" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenOverlappingRootsHandOverTheCursor()
    {
        // B is the later root so it is tested first and takes the cursor where they overlap.
        AddRoot("A", 0, 0, 20);
        AddRoot("B", 10, 0, 20);
        Frame(5, 5).ShouldBe(new[] { "RollOn A" });

        Frame(15, 5).ShouldBe(new[] { "RollOff A", "RollOn B" });
    }

    [Fact]
    public void Walk_ShouldRaiseOnlyRollOnAndRollOffOnce_WhenTheCursorStaysOnTheNewElement()
    {
        AddRoot("A", 0, 0, 10);
        AddRoot("B", 10, 0, 10);
        Frame(5, 5);
        Frame(15, 5);

        Frame(16, 5).ShouldBeEmpty();
        Frame(17, 5).ShouldBeEmpty();
    }

    [Fact]
    public void Walk_ShouldRaiseHoverOverAndRollOverAfterRollOn_WhenCursorMovesBetweenSiblingsWithMovement()
    {
        AddRoot("A", 0, 0, 10, all: true);
        AddRoot("B", 10, 0, 10, all: true);
        Frame(5, 5, xChange: 1);

        List<string> events = Frame(15, 5, xChange: 10);

        events.ShouldBe(new[] { "RollOff A", "RollOn B", "HoverOver B", "RollOver B" });
    }

    [Fact]
    public void Walk_ShouldRaiseTheSequenceFrameByFrame_WhenCursorCrossesTwoSiblingsAndLeaves()
    {
        AddRoot("A", 0, 0, 10, all: true);
        AddRoot("B", 10, 0, 10, all: true);

        Frame(5, 5, xChange: 5).ShouldBe(new[] { "RollOn A", "HoverOver A", "RollOver A" });
        Frame(7, 5, xChange: 2).ShouldBe(new[] { "HoverOver A", "RollOver A" });
        Frame(15, 5, xChange: 8).ShouldBe(new[] { "RollOff A", "RollOn B", "HoverOver B", "RollOver B" });
        Frame(15, 5, xChange: 0).ShouldBe(new[] { "HoverOver B" });
        Frame(50, 5, xChange: 35).ShouldBe(new[] { "RollOff B" });
        Frame(50, 5, xChange: 0).ShouldBeEmpty();
    }

    #endregion

    #region Moving to and from empty space

    [Fact]
    public void Walk_ShouldRaiseOnlyRollOff_WhenCursorMovesFromAnElementToEmptySpace()
    {
        AddRoot("A", 0, 0, 10);
        Frame(5, 5);

        Frame(50, 5).ShouldBe(new[] { "RollOff A" });
        _cursor.Object.VisualOver.ShouldBeNull();
    }

    [Fact]
    public void Walk_ShouldRaiseOnlyRollOn_WhenCursorMovesFromEmptySpaceToAnElement()
    {
        AddRoot("A", 0, 0, 10);
        Frame(50, 5).ShouldBeEmpty();

        Frame(5, 5).ShouldBe(new[] { "RollOn A" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOff_WhenCursorLeavesTheWindow()
    {
        AddRoot("A", 0, 0, 10);
        Frame(5, 5);

        Frame(-1000, -1000).ShouldBe(new[] { "RollOff A" });
        _cursor.Object.VisualOver.ShouldBeNull();
    }

    [Fact]
    public void Walk_ShouldRaiseRollOnAgainWithoutRollOff_WhenVisualOverWasClearedBetweenFrames()
    {
        // Whoever clears VisualOver outside of the walk (a game resetting the cursor) skips the RollOff.
        AddRoot("A", 0, 0, 10);
        Frame(5, 5);
        _cursor.Object.VisualOver = null;

        Frame(5, 5).ShouldBe(new[] { "RollOn A" });
    }

    #endregion

    #region Parent and child

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenCursorMovesFromAParentToItsChild()
    {
        InteractiveGue parent = AddRoot("Parent", 0, 0, 30);
        InteractiveGue child = CreateBox(10, 0, 10);
        parent.AddChild(child);
        Track(child, "Child");
        Frame(5, 5).ShouldBe(new[] { "RollOn Parent" });

        Frame(15, 5).ShouldBe(new[] { "RollOff Parent", "RollOn Child" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenCursorMovesFromAChildToItsParent()
    {
        InteractiveGue parent = AddRoot("Parent", 0, 0, 30);
        InteractiveGue child = CreateBox(10, 0, 10);
        parent.AddChild(child);
        Track(child, "Child");
        Frame(15, 5);

        Frame(5, 5).ShouldBe(new[] { "RollOff Child", "RollOn Parent" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenCursorMovesBetweenTwoSiblingChildren()
    {
        InteractiveGue parent = AddRoot("Parent", 0, 0, 30);
        InteractiveGue childC = CreateBox(0, 0, 10);
        InteractiveGue childD = CreateBox(10, 0, 10);
        parent.AddChild(childC);
        parent.AddChild(childD);
        Track(childC, "C");
        Track(childD, "D");
        Frame(5, 5).ShouldBe(new[] { "RollOn C" });

        Frame(15, 5).ShouldBe(new[] { "RollOff C", "RollOn D" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenCursorMovesFromAChildToAnUnrelatedRoot()
    {
        InteractiveGue parent = AddRoot("Parent", 0, 0, 30);
        InteractiveGue child = CreateBox(0, 0, 10);
        parent.AddChild(child);
        Track(child, "Child");
        AddRoot("Other", 100, 0, 10);
        Frame(5, 5).ShouldBe(new[] { "RollOn Child" });

        Frame(105, 5).ShouldBe(new[] { "RollOff Child", "RollOn Other" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenCursorMovesFromAParentToAnUnrelatedSiblingOfItsChild()
    {
        InteractiveGue parent = AddRoot("Parent", 0, 0, 30);
        InteractiveGue child = CreateBox(20, 0, 10);
        parent.AddChild(child);
        Track(child, "Child");
        AddRoot("Other", 100, 0, 10);
        Frame(5, 5).ShouldBe(new[] { "RollOn Parent" });

        Frame(105, 5).ShouldBe(new[] { "RollOff Parent", "RollOn Other" });
    }

    #endregion

    #region The hovered element changes while the cursor does not move

    [Fact]
    public void Walk_ShouldRaiseOnlyRollOff_WhenTheHoveredElementIsHidden()
    {
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10);
        Frame(5, 5);

        buttonA.Visible = false;

        Frame(5, 5).ShouldBe(new[] { "RollOff A" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenTheHoveredElementIsHiddenRevealingAnotherUnderIt()
    {
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10);
        AddRoot("Under", 0, 0, 10);
        // Under is the later root, so swap: A must be the one on top.
        _roots.Reverse();
        Frame(5, 5).ShouldBe(new[] { "RollOn A" });

        buttonA.Visible = false;

        Frame(5, 5).ShouldBe(new[] { "RollOff A", "RollOn Under" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenTheHoveredElementIsRemovedRevealingAnotherUnderIt()
    {
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10);
        AddRoot("Under", 0, 0, 10);
        _roots.Reverse();
        Frame(5, 5);

        _roots.Remove(buttonA);

        Frame(5, 5).ShouldBe(new[] { "RollOff A", "RollOn Under" });
    }

    [Fact]
    public void Walk_ShouldRaiseOnlyRollOff_WhenTheHoveredElementIsRemoved()
    {
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10);
        Frame(5, 5);

        _roots.Remove(buttonA);

        Frame(5, 5).ShouldBe(new[] { "RollOff A" });
    }

    [Fact]
    public void Walk_ShouldRaiseOnlyRollOff_WhenHasEventsIsTurnedOffOnTheHoveredElement()
    {
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10);
        Frame(5, 5);

        buttonA.HasEvents = false;

        Frame(5, 5).ShouldBe(new[] { "RollOff A" });
    }

    [Fact]
    public void Walk_ShouldRaiseRollOffBeforeRollOn_WhenHasEventsIsTurnedOffRevealingAnotherUnderIt()
    {
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10);
        AddRoot("Under", 0, 0, 10);
        _roots.Reverse();
        Frame(5, 5);

        buttonA.HasEvents = false;

        Frame(5, 5).ShouldBe(new[] { "RollOff A", "RollOn Under" });
    }

    [Fact]
    public void Walk_ShouldStillRaiseRollOnAndRollOff_ForADisabledElement()
    {
        // IsEnabled gates push, click and the bubbling events, not hover.
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10);
        AddRoot("B", 10, 0, 10);
        buttonA.IsEnabled = false;

        Frame(5, 5).ShouldBe(new[] { "RollOn A" });
        Frame(15, 5).ShouldBe(new[] { "RollOff A", "RollOn B" });
    }

    #endregion

    #region Pushed (dragging with the button held)

    [Fact]
    public void Walk_ShouldRaiseRollOffThenLosePushThenRollOn_WhenDraggingFromThePushedElementOntoAnother()
    {
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10, all: true);
        AddRoot("B", 10, 0, 10, all: true);
        SetButtons(_cursor, push: true, down: true);
        Frame(5, 5);
        _cursor.Object.WindowPushed.ShouldBe(buttonA);
        SetButtons(_cursor, push: false, down: true);

        List<string> events = Frame(15, 5, xChange: 10);

        events.ShouldBe(new[] { "RollOff A", "LosePush A", "RollOn B", "HoverOver B", "RollOver B", "Dragging A" });
        _cursor.Object.WindowPushed.ShouldBe(buttonA);
    }

    [Fact]
    public void Walk_ShouldKeepTheSequenceFrameByFrame_WhenDraggingAcrossTwoElementsAndBack()
    {
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10, all: true);
        AddRoot("B", 10, 0, 10, all: true);
        SetButtons(_cursor, push: true, down: true);
        Frame(5, 5);
        SetButtons(_cursor, push: false, down: true);

        Frame(15, 5).ShouldBe(new[] { "RollOff A", "LosePush A", "RollOn B", "HoverOver B", "Dragging A" });
        Frame(5, 5).ShouldBe(new[] { "RollOff B", "RollOn A", "HoverOver A", "Dragging A" });
        _cursor.Object.WindowPushed.ShouldBe(buttonA);
    }

    #endregion

    #region Two cursors

    [Fact]
    public void Walk_ShouldTrackEachCursorsHoverSeparately_WhenTwoCursorsWalkTheSameElements()
    {
        InteractiveGue buttonA = AddRoot("A", 0, 0, 10);
        AddRoot("B", 10, 0, 10);
        Mock<ICursor> touch = CreateCursor();
        Frame(_cursor, 5, 5).ShouldBe(new[] { "RollOn A" });

        // The second cursor enters B, then A while the mouse is still on A: the element has no hover count.
        Frame(touch, 15, 5).ShouldBe(new[] { "RollOn B" });
        Frame(touch, 5, 5).ShouldBe(new[] { "RollOff B", "RollOn A" });
        Frame(_cursor, 5, 5).ShouldBeEmpty();
        touch.Object.VisualOver.ShouldBe(buttonA);
        _cursor.Object.VisualOver.ShouldBe(buttonA);
    }

    #endregion
}
