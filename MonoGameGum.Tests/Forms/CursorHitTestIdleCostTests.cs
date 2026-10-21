using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms.Controls;
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
    public void DoUiActivityRecursively_ShouldTestEachChildOnce_WhenCursorIsOverACard()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: true);
        foreach (CountingInteractive child in children)
        {
            child.HasEvents = true;
        }
        // The children overlap the card's origin; the last one added is on top and takes the cursor.
        Mock<ICursor> cursor = CreateCursor(cards[10].X + 5, 5);

        Walk(roots, cursor);

        cursor.Object.VisualOver.ShouldBe(children[10 * 8 + 7]);
        children[10 * 8 + 7].HasCursorOverCalls.ShouldBe(1);
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

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenEventsAreTurnedOffAndBackOn()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        CountingInteractive target = children[12 * 8 + 2];
        int clicks = 0;
        target.Click += (_, _) => clicks++;
        target.HasEvents = true;
        Mock<ICursor> cursor = CreateCursor(cards[12].X + 5, 5, click: true);
        Walk(roots, cursor);
        clicks.ShouldBe(1);

        target.HasEvents = false;
        Walk(roots, cursor);
        Walk(roots, cursor);
        clicks.ShouldBe(1, "an element without events is not clicked");

        target.HasEvents = true;
        Walk(roots, cursor);

        clicks.ShouldBe(2);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenElementWithEventsIsInsertedThroughTheChildrenCollection()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        Mock<ICursor> cursor = CreateCursor(cards[9].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        int clicks = 0;
        CountingInteractive late = CreateBox(0, 0, 10, hasEvents: true);
        late.Click += (_, _) => clicks++;

        children[9 * 8 + 4].Children.Add(late);
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenElementWithEventsIsAttachedWithTheParentProperty()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        Mock<ICursor> cursor = CreateCursor(cards[9].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        int clicks = 0;
        CountingInteractive late = CreateBox(0, 0, 10, hasEvents: true);
        late.Click += (_, _) => clicks++;

        late.Parent = children[9 * 8 + 4];
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenSubtreeWithEventsIsRemovedAndAddedBackElsewhere()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        CountingInteractive mover = CreateBox(0, 0, 10, hasEvents: true);
        int clicks = 0;
        mover.Click += (_, _) => clicks++;
        children[3 * 8].AddChild(mover);
        Mock<ICursor> cursor = CreateCursor(cards[25].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);

        children[3 * 8].RemoveChild(mover);
        Walk(roots, cursor);
        clicks.ShouldBe(0, "it is detached");
        children[25 * 8 + 6].AddChild(mover);
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenHiddenAncestorOfElementWithEventsIsShown()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        CountingInteractive hiddenParent = children[18 * 8];
        CountingInteractive target = CreateBox(0, 0, 10, hasEvents: true);
        hiddenParent.AddChild(target);
        int clicks = 0;
        target.Click += (_, _) => clicks++;
        hiddenParent.Visible = false;
        Mock<ICursor> cursor = CreateCursor(cards[18].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        clicks.ShouldBe(0);

        hiddenParent.Visible = true;
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenEventsAreEnabledWhileTheElementIsHiddenAndThenShown()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        CountingInteractive target = children[2 * 8 + 5];
        int clicks = 0;
        target.Click += (_, _) => clicks++;
        target.Visible = false;
        Mock<ICursor> cursor = CreateCursor(cards[2].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);

        target.HasEvents = true;
        Walk(roots, cursor);
        clicks.ShouldBe(0, "it is still hidden");
        target.Visible = true;
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseChildClick_WhenParentStartsExposingChildrenEvents()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: true);
        CountingInteractive child = children[6 * 8 + 7];
        child.HasEvents = true;
        int childClicks = 0;
        child.Click += (_, _) => childClicks++;
        cards[6].ExposeChildrenEvents = false;
        Mock<ICursor> cursor = CreateCursor(cards[6].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        childClicks.ShouldBe(0);
        cursor.Object.VisualOver.ShouldBe(cards[6]);

        cards[6].ExposeChildrenEvents = true;
        Walk(roots, cursor);

        childClicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenDisabledElementIsEnabledAgain()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        CountingInteractive target = children[44 * 8 + 1];
        target.HasEvents = true;
        target.IsEnabled = false;
        int clicks = 0;
        target.Click += (_, _) => clicks++;
        Mock<ICursor> cursor = CreateCursor(cards[44].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        clicks.ShouldBe(0);

        target.IsEnabled = true;
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldSelectNewItem_WhenListBoxItemsAreAddedAfterTheTreeWasSkipped()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        List<GraphicalUiElement> roots = new() { listBox.Visual };
        Mock<ICursor> away = CreateCursor(-5000, -5000);
        Walk(roots, away);
        Walk(roots, away);

        for (int i = 0; i < 3; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        InteractiveGue itemVisual = listBox.ListBoxItems[1]!.Visual;
        Mock<ICursor> cursor = CreateCursor(
            itemVisual.GetAbsoluteX() + itemVisual.AbsoluteWidth / 2,
            itemVisual.GetAbsoluteY() + itemVisual.AbsoluteHeight / 2,
            click: true);
        FrameworkElement.MainCursor = cursor.Object;
        Walk(roots, cursor);

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenButtonIsAddedToThePopupRootAfterTheTreeWasSkipped()
    {
        Gum.GumService gumService = Gum.GumService.Default;
        Mock<ICursor> away = CreateCursor(-5000, -5000);
        Walk(new List<GraphicalUiElement>(gumService.PopupRoot.Children), away);
        Walk(new List<GraphicalUiElement>(gumService.PopupRoot.Children), away);

        Button button = new();
        button.Visual.WidthUnits = DimensionUnitType.Absolute;
        button.Visual.HeightUnits = DimensionUnitType.Absolute;
        button.Visual.Width = 100;
        button.Visual.Height = 40;
        button.Visual.X = 10;
        button.Visual.Y = 10;
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        gumService.PopupRoot.Children.Add(button.Visual);
        Mock<ICursor> cursor = CreateCursor(50, 30, click: true);
        FrameworkElement.MainCursor = cursor.Object;
        Walk(new List<GraphicalUiElement>(gumService.PopupRoot.Children), cursor);

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
    public void DoUiActivityRecursively_ShouldConvertPerCamera_WhenRootsBelongToDifferentManagers()
    {
        Camera otherCamera = new()
        {
            ClientWidth = 800,
            ClientHeight = 600,
            CameraCenterOnScreen = CameraCenterOnScreen.TopLeft,
            X = 300
        };
        Mock<IRenderer> otherRenderer = new();
        otherRenderer.Setup(item => item.Camera).Returns(otherCamera);
        Mock<ISystemManagers> otherManagers = new();
        otherManagers.Setup(item => item.Renderer).Returns(otherRenderer.Object);
        otherCamera.ScreenToWorld(100, 50, out float otherWorldX, out float otherWorldY);
        (float mainWorldX, float mainWorldY) = ScreenToWorld(100, 50);
        otherWorldX.ShouldNotBe(mainWorldX, "the test needs the cameras to disagree");

        // The roots are walked last to first: the main camera's root misses, then the other camera's
        // root must be tested against its own camera's world point.
        CountingInteractive onOther = CreateBox(otherWorldX - 5, otherWorldY - 5, 10, hasEvents: true);
        onOther.AttachManagersOnly(otherManagers.Object);
        CountingInteractive onMain = CreateBox(mainWorldX + 5000, mainWorldY, 10, hasEvents: true);
        List<GraphicalUiElement> roots = new() { onOther, onMain };
        Mock<ICursor> cursor = CreateCursor(100, 50);

        Walk(roots, cursor);

        cursor.Object.VisualOver.ShouldBe(onOther);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldConvertPerHitTestTransform_WhenOnlyOneRootHasOne()
    {
        (float mainWorldX, float mainWorldY) = ScreenToWorld(100, 50);
        (float shiftedWorldX, float shiftedWorldY) = ScreenToWorld(300, 50);
        CountingInteractive transformed = CreateBox(shiftedWorldX - 5, shiftedWorldY - 5, 10, hasEvents: true);
        transformed.HitTestTransformMatrix = System.Numerics.Matrix3x2.CreateTranslation(200, 0);
        CountingInteractive plain = CreateBox(mainWorldX + 5000, mainWorldY, 10, hasEvents: true);
        // The plain root is walked first and misses; the transformed one must not reuse its conversion.
        List<GraphicalUiElement> roots = new() { transformed, plain };
        Mock<ICursor> cursor = CreateCursor(100, 50);

        Walk(roots, cursor);

        cursor.Object.VisualOver.ShouldBe(transformed);
    }

    [Fact]
    public void HasCursorOver_ShouldUseTheCurrentCamera_WhenCalledAfterAWalk()
    {
        Camera camera = SystemManagers.Default.Renderer.Camera;
        float originalX = camera.X;
        (float worldX, float worldY) = ScreenToWorld(100, 50);
        CountingInteractive box = CreateBox(worldX - 5, worldY - 5, 10, hasEvents: true);
        Mock<ICursor> cursor = CreateCursor(100, 50);
        Walk(new List<GraphicalUiElement> { box }, cursor);
        try
        {
            camera.X = originalX + 100;

            box.HasCursorOver(cursor.Object).ShouldBeFalse();
        }
        finally
        {
            camera.X = originalX;
        }
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

    [Fact]
    public void HasCursorOver_ShouldUseTheCurrentCamera_WhenAHandlerThrewDuringTheWalk()
    {
        Camera camera = SystemManagers.Default.Renderer.Camera;
        float originalX = camera.X;
        (float worldX, float worldY) = ScreenToWorld(100, 50);
        CountingInteractive box = CreateBox(worldX - 5, worldY - 5, 10, hasEvents: true);
        box.Click += (_, _) => throw new System.InvalidOperationException("handler failed");
        Mock<ICursor> cursor = CreateCursor(100, 50, click: true);
        try
        {
            Should.Throw<System.InvalidOperationException>(() => Walk(new List<GraphicalUiElement> { box }, cursor));
            camera.X = originalX + 100;

            box.HasCursorOver(cursor.Object).ShouldBeFalse();
        }
        finally
        {
            camera.X = originalX;
        }
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldClickEachWalksOwnTarget_WhenAPreviewHandlerRunsAnotherWalk()
    {
        (float worldX, float worldY) = ScreenToWorld(100, 50);
        (float otherWorldX, float otherWorldY) = ScreenToWorld(400, 300);
        CountingInteractive otherBox = CreateBox(otherWorldX - 5, otherWorldY - 5, 10, hasEvents: true);
        int otherClicks = 0;
        otherBox.Click += (_, _) => otherClicks++;
        Mock<ICursor> otherCursor = CreateCursor(400, 300, click: true);
        CountingInteractive parent = CreateBox(worldX - 20, worldY - 20, 40, hasEvents: true);
        parent.ClickPreview += (_, _) => Walk(new List<GraphicalUiElement> { otherBox }, otherCursor);
        CountingInteractive child = CreateBox(15, 15, 10, hasEvents: true);
        int childClicks = 0;
        child.Click += (_, _) => childClicks++;
        parent.AddChild(child);
        Mock<ICursor> cursor = CreateCursor(100, 50, click: true);

        Walk(new List<GraphicalUiElement> { parent }, cursor);

        otherClicks.ShouldBe(1);
        childClicks.ShouldBe(1);
        cursor.Object.VisualOver.ShouldBe(child);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldApplyAnAncestorsHitTestTransform_WhenTestingItsChildren()
    {
        (float mainWorldX, float mainWorldY) = ScreenToWorld(100, 50);
        (float shiftedWorldX, float shiftedWorldY) = ScreenToWorld(300, 50);
        CountingInteractive transformed = CreateBox(shiftedWorldX - 20, shiftedWorldY - 20, 40, hasEvents: false);
        transformed.HitTestTransformMatrix = System.Numerics.Matrix3x2.CreateTranslation(200, 0);
        CountingInteractive child = CreateBox(15, 15, 10, hasEvents: true);
        transformed.AddChild(child);
        CountingInteractive plain = CreateBox(mainWorldX + 5000, mainWorldY, 10, hasEvents: true);
        // The plain root is walked first and misses; the child must not reuse its untransformed conversion.
        List<GraphicalUiElement> roots = new() { transformed, plain };
        Mock<ICursor> cursor = CreateCursor(100, 50);

        Walk(roots, cursor);

        cursor.Object.VisualOver.ShouldBe(child);
    }

    #endregion
}
