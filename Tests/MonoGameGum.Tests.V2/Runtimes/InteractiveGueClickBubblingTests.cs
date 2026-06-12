using Gum.Forms;
using Gum.GueDeriving;
using Gum.Wireframe;
using MonoGameGum.Input;
using Moq;
using Shouldly;

namespace MonoGameGum.Tests.V2.Runtimes;

public class InteractiveGueClickBubblingTests : BaseTestClass
{
    [Fact]
    public void Click_ShouldNotRaiseOnParent_WhenChildClicked()
    {
        (ContainerRuntime parent, ContainerRuntime child) = CreateParentChild();

        bool parentClicked = false;
        bool childClicked = false;
        parent.Click += (_, _) => parentClicked = true;
        child.Click += (_, _) => childClicked = true;

        Mock<ICursor> cursor = CreateCursorOver(child);
        PushAndClick(cursor);

        // Click is single-target: only the deepest element with events receives it.
        childClicked.ShouldBeTrue();
        parentClicked.ShouldBeFalse();
    }

    [Fact]
    public void Click_ShouldNotRaise_WhenReleasedAfterCursorMovesOff()
    {
        (ContainerRuntime parent, ContainerRuntime child) = CreateParentChild();

        bool childClicked = false;
        child.Click += (_, _) => childClicked = true;

        Mock<ICursor> cursor = CreateCursorOver(child);

        // Frame 1: push while over the child.
        cursor.Setup(x => x.PrimaryPush).Returns(true);
        cursor.Setup(x => x.PrimaryDown).Returns(true);
        cursor.Setup(x => x.PrimaryClick).Returns(false);
        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        // Frame 2: move off the child, then release.
        PositionCursorAt(cursor, 500, 500);
        cursor.Setup(x => x.PrimaryPush).Returns(false);
        cursor.Setup(x => x.PrimaryDown).Returns(false);
        cursor.Setup(x => x.PrimaryClick).Returns(true);
        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        // A click requires push and release on the same element (push origin).
        childClicked.ShouldBeFalse();
    }

    [Fact]
    public void ClickBubbling_ShouldNotRaise_WhenReleasedAfterCursorMovesOff()
    {
        (ContainerRuntime parent, ContainerRuntime child) = CreateParentChild();

        bool parentBubbled = false;
        bool childBubbled = false;
        parent.ClickBubbling += (_, _) => parentBubbled = true;
        child.ClickBubbling += (_, _) => childBubbled = true;

        Mock<ICursor> cursor = CreateCursorOver(child);

        // Frame 1: push while over the child.
        cursor.Setup(x => x.PrimaryPush).Returns(true);
        cursor.Setup(x => x.PrimaryDown).Returns(true);
        cursor.Setup(x => x.PrimaryClick).Returns(false);
        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        // Frame 2: move off, then release. No click resolved, so nothing bubbles.
        PositionCursorAt(cursor, 500, 500);
        cursor.Setup(x => x.PrimaryPush).Returns(false);
        cursor.Setup(x => x.PrimaryDown).Returns(false);
        cursor.Setup(x => x.PrimaryClick).Returns(true);
        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        childBubbled.ShouldBeFalse();
        parentBubbled.ShouldBeFalse();
    }

    [Fact]
    public void ClickBubbling_ShouldRaiseOnParent_WhenChildClicked()
    {
        (ContainerRuntime parent, ContainerRuntime child) = CreateParentChild();

        bool parentBubbled = false;
        bool childBubbled = false;
        // Parent subscribes ONLY to ClickBubbling (the issue's use case: be notified
        // when anything on the parent's surface is clicked, even over a child).
        parent.ClickBubbling += (_, _) => parentBubbled = true;
        child.ClickBubbling += (_, _) => childBubbled = true;

        Mock<ICursor> cursor = CreateCursorOver(child);
        PushAndClick(cursor);

        childBubbled.ShouldBeTrue();
        parentBubbled.ShouldBeTrue();
    }

    [Fact]
    public void ClickBubbling_ShouldNotSuppressNormalClick_WhenHandled()
    {
        (ContainerRuntime parent, ContainerRuntime child) = CreateParentChild();

        bool childClicked = false;
        // Child handles its own ClickBubbling. The single-target Click is a separate
        // channel and must still fire.
        child.Click += (_, _) => childClicked = true;
        child.ClickBubbling += (_, args) => args.Handled = true;

        Mock<ICursor> cursor = CreateCursorOver(child);
        PushAndClick(cursor);

        childClicked.ShouldBeTrue();
    }

    [Fact]
    public void ClickBubbling_ShouldStopBubbling_WhenChildSetsHandled()
    {
        (ContainerRuntime parent, ContainerRuntime child) = CreateParentChild();

        bool parentBubbled = false;
        child.ClickBubbling += (_, args) => args.Handled = true;
        parent.ClickBubbling += (_, _) => parentBubbled = true;

        Mock<ICursor> cursor = CreateCursorOver(child);
        PushAndClick(cursor);

        // Child set Handled, so the event must not reach the parent.
        parentBubbled.ShouldBeFalse();
    }

    private static (ContainerRuntime parent, ContainerRuntime child) CreateParentChild()
    {
        ContainerRuntime parent = new ContainerRuntime();
        parent.X = 0;
        parent.Y = 0;
        parent.Width = 200;
        parent.Height = 200;

        ContainerRuntime child = new ContainerRuntime();
        child.X = 0;
        child.Y = 0;
        child.Width = 100;
        child.Height = 100;

        parent.Children.Add(child);
        GumService.Default.Root.Children.Add(parent);
        GumService.Default.Root.UpdateLayout();

        return (parent, child);
    }

    private static Mock<ICursor> CreateCursorOver(GraphicalUiElement visual)
    {
        Mock<ICursor> cursor = new();
        FormsUtilities.SetCursor(cursor.Object);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.Setup(x => x.LastInputDevice).Returns(InputDevice.Mouse);
        PositionCursorAt(cursor, (int)(visual.AbsoluteLeft + 1), (int)(visual.AbsoluteTop + 1));
        return cursor;
    }

    private static void PositionCursorAt(Mock<ICursor> cursor, int x, int y)
    {
        cursor.Setup(c => c.X).Returns(x);
        cursor.Setup(c => c.Y).Returns(y);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
    }

    private static void PushAndClick(Mock<ICursor> cursor)
    {
        // Buffered input: a push and click can resolve in the same frame.
        cursor.Setup(x => x.PrimaryPush).Returns(true);
        cursor.Setup(x => x.PrimaryDown).Returns(true);
        cursor.Setup(x => x.PrimaryClick).Returns(true);
        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
    }
}
