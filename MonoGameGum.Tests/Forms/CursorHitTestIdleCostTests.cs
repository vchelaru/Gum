using System.Collections.Generic;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The cursor walk does no work for elements that cannot react to the cursor, and converts the
/// cursor position to world space once per walk rather than once per element tested (#6177).
/// </summary>
public class CursorHitTestIdleCostTests : BaseTestClass
{
    #region Helpers

    // Counts how often the cursor walk tests it.
    private sealed class CountingInteractive : InteractiveGue
    {
        public int HasCursorOverCalls;

        public CountingInteractive() : base(new InvisibleRenderable())
        {
        }

        public override bool HasCursorOver(ICursor cursor, Layer? layer = null)
        {
            HasCursorOverCalls++;
            return base.HasCursorOver(cursor, layer);
        }
    }

    private static CountingInteractive CreateBox(float x, float y, float size, bool hasEvents)
    {
        CountingInteractive box = new();
        box.WidthUnits = DimensionUnitType.Absolute;
        box.HeightUnits = DimensionUnitType.Absolute;
        box.X = x;
        box.Y = y;
        box.Width = size;
        box.Height = size;
        box.HasEvents = hasEvents;
        return box;
    }

    // 52 cards of 9 elements plus nothing else: the 468 elements a card board has under its roots.
    private static (List<GraphicalUiElement> Roots, List<CountingInteractive> Cards, List<CountingInteractive> Children) CreateBoard(
        bool cardsHaveEvents)
    {
        List<GraphicalUiElement> roots = new();
        List<CountingInteractive> cards = new();
        List<CountingInteractive> children = new();
        for (int i = 0; i < 52; i++)
        {
            CountingInteractive card = CreateBox(i * 12, 0, 10, cardsHaveEvents);
            for (int j = 0; j < 8; j++)
            {
                CountingInteractive child = CreateBox(0, 0, 10, hasEvents: false);
                card.AddChild(child);
                children.Add(child);
            }
            roots.Add(card);
            cards.Add(card);
        }
        return (roots, cards, children);
    }

    private static Mock<ICursor> CreateCursor(float screenX, float screenY, bool click = false)
    {
        Mock<ICursor> cursor = new();
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.SetupProperty(c => c.VisualRightPushed);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        SetCursorPosition(cursor, screenX, screenY);
        cursor.Setup(c => c.PrimaryPush).Returns(click);
        cursor.Setup(c => c.PrimaryDown).Returns(click);
        cursor.Setup(c => c.PrimaryClick).Returns(click);
        return cursor;
    }

    private static void SetCursorPosition(Mock<ICursor> cursor, float screenX, float screenY)
    {
        cursor.Setup(c => c.X).Returns((int)screenX);
        cursor.Setup(c => c.Y).Returns((int)screenY);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(screenX);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(screenY);
    }

    private static void Walk(List<GraphicalUiElement> roots, Mock<ICursor> cursor)
    {
        Mock<IInputReceiverKeyboard> keyboard = new();
        GueInteractiveExtensionMethods.DoUiActivityRecursively(roots, cursor.Object, keyboard.Object, 0);
    }

    private static (float X, float Y) ScreenToWorld(float screenX, float screenY, Layer? layer = null)
    {
        Camera camera = SystemManagers.Default.Renderer.Camera;
        float worldX;
        float worldY;
        if (layer != null)
        {
            layer.ScreenToWorld(camera, screenX, screenY, out worldX, out worldY);
        }
        else
        {
            camera.ScreenToWorld(screenX, screenY, out worldX, out worldY);
        }
        return (worldX, worldY);
    }

    private static int TotalCalls(List<CountingInteractive> elements)
    {
        int total = 0;
        foreach (CountingInteractive element in elements)
        {
            total += element.HasCursorOverCalls;
        }
        return total;
    }

    #endregion

    #region Work count

    [Fact]
    public void DoUiActivityRecursively_ShouldTestNothing_WhenNoElementUnderTheRootsHasEvents()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        Mock<ICursor> cursor = CreateCursor(-5000, -5000);

        Walk(roots, cursor);
        Walk(roots, cursor);

        TotalCalls(cards).ShouldBe(0);
        TotalCalls(children).ShouldBe(0);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldNotTestChildrenWithoutEvents_WhenCursorIsOverACard()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: true);
        (float screenX, float screenY) = (cards[10].X + 5, 5);
        Mock<ICursor> cursor = CreateCursor(screenX, screenY);

        Walk(roots, cursor);

        cursor.Object.VisualOver.ShouldBe(cards[10]);
        TotalCalls(children).ShouldBe(0);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldReadTheCursorPositionOnce_WhenManyRootsAreTested()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: true);
        Mock<ICursor> cursor = CreateCursor(-5000, -5000);

        Walk(roots, cursor);

        TotalCalls(cards).ShouldBe(52, "every card still has to be tested");
        cursor.VerifyGet(c => c.X, Times.AtMost(1));
        cursor.VerifyGet(c => c.Y, Times.AtMost(1));
    }

    #endregion

    #region Events appear later

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenEventsAreEnabledAfterTheSubtreeWasSkipped()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        Mock<ICursor> cursor = CreateCursor(cards[7].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        int clicks = 0;
        children[7 * 8 + 3].Click += (_, _) => clicks++;

        children[7 * 8 + 3].HasEvents = true;
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenElementWithEventsIsAddedToASkippedParent()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        Mock<ICursor> cursor = CreateCursor(cards[20].X + 5, 5, click: true);
        Walk(roots, cursor);
        int clicks = 0;
        CountingInteractive late = CreateBox(0, 0, 10, hasEvents: true);
        late.Click += (_, _) => clicks++;

        children[20 * 8 + 1].AddChild(late);
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenElementWithEventsIsReparentedIntoASkippedSubtree()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        CountingInteractive mover = CreateBox(0, 0, 10, hasEvents: true);
        int clicks = 0;
        mover.Click += (_, _) => clicks++;
        cards[3].AddChild(mover);
        Mock<ICursor> cursor = CreateCursor(cards[30].X + 5, 5, click: true);
        Walk(roots, cursor);
        clicks.ShouldBe(0);

        mover.Parent = cards[30];
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenHiddenElementWithEventsIsShown()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        CountingInteractive hidden = children[40 * 8];
        int clicks = 0;
        hidden.Click += (_, _) => clicks++;
        hidden.HasEvents = true;
        hidden.Visible = false;
        Mock<ICursor> cursor = CreateCursor(cards[40].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        clicks.ShouldBe(0);

        hidden.Visible = true;
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenEventsAreEnabledOnAnAncestorOfASkippedElement()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        Mock<ICursor> cursor = CreateCursor(cards[5].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        int clicks = 0;
        cards[5].Click += (_, _) => clicks++;

        cards[5].HasEvents = true;
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    #endregion

    #region Position conversion

    [Fact]
    public void DoUiActivityRecursively_ShouldUseTheCurrentCamera_OnEachWalk()
    {
        Camera camera = SystemManagers.Default.Renderer.Camera;
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: true);
        (float worldX, float worldY) = ScreenToWorld(300, 40);
        // A box that only covers the cursor once the camera has moved right by 100.
        CountingInteractive target = CreateBox(worldX + 100 - 5, worldY - 5, 10, hasEvents: true);
        roots.Add(target);
        Mock<ICursor> cursor = CreateCursor(300, 40);
        float originalX = camera.X;
        try
        {
            Walk(roots, cursor);
            cursor.Object.VisualOver.ShouldNotBe(target);

            camera.X = originalX + 100;
            Walk(roots, cursor);
            cursor.Object.VisualOver.ShouldBe(target);

            camera.X = originalX;
            Walk(roots, cursor);
            cursor.Object.VisualOver.ShouldNotBe(target);
        }
        finally
        {
            camera.X = originalX;
        }
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldConvertPerLayer_WhenRootsAreOnLayersWithDifferentCameras()
    {
        Layer shiftedLayer = new()
        {
            LayerCameraSettings = new LayerCameraSettings { Position = new System.Numerics.Vector2(200, 0) }
        };
        (float mainWorldX, float mainWorldY) = ScreenToWorld(100, 50);
        (float shiftedWorldX, float shiftedWorldY) = ScreenToWorld(100, 50, shiftedLayer);
        shiftedWorldX.ShouldNotBe(mainWorldX, "the test needs the layers to disagree");

        // The roots are walked last to first. The one on the main camera is tested first and misses, then
        // the one on the shifted layer must be tested against that layer's own world point.
        CountingInteractive onShifted = CreateBox(shiftedWorldX - 5, shiftedWorldY - 5, 10, hasEvents: false);
        CountingInteractive shiftedChild = CreateBox(0, 0, 10, hasEvents: true);
        onShifted.AddChild(shiftedChild);
        onShifted.AddToManagers(SystemManagers.Default, shiftedLayer);
        CountingInteractive onMain = CreateBox(mainWorldX + 5000, mainWorldY, 10, hasEvents: true);
        List<GraphicalUiElement> roots = new() { onShifted, onMain };
        Mock<ICursor> cursor = CreateCursor(100, 50);

        Walk(roots, cursor);

        cursor.Object.VisualOver.ShouldBe(shiftedChild);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldUseTheCameraAfterAPreviewHandlerMovesIt()
    {
        Camera camera = SystemManagers.Default.Renderer.Camera;
        float originalX = camera.X;
        (float worldX, float worldY) = ScreenToWorld(100, 50);
        // The parent's preview handler moves the camera before its child is tested.
        CountingInteractive parent = CreateBox(worldX - 20, worldY - 20, 40, hasEvents: true);
        parent.ClickPreview += (_, _) => camera.X = originalX + 1000;
        CountingInteractive child = CreateBox(1000, 0, 10, hasEvents: true);
        parent.AddChild(child);
        // Child is at world x = parent.X + 1000; after the camera moves right by 1000, the cursor's world point is there.
        child.X = 1000 + 20 - 5;
        child.Y = 20 - 5;
        int clicks = 0;
        child.Click += (_, _) => clicks++;
        List<GraphicalUiElement> roots = new() { parent };
        Mock<ICursor> cursor = CreateCursor(100, 50, click: true);

        try
        {
            Walk(roots, cursor);
        }
        finally
        {
            camera.X = originalX;
        }

        clicks.ShouldBe(1);
    }

    #endregion
}
