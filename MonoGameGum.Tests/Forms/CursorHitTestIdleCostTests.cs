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
    public void DoUiActivityRecursively_ShouldRaiseChildClick_WhenParentStartsRaisingChildrenEventsOutsideOfBounds()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: true);
        CountingInteractive card = cards[3];
        CountingInteractive child = children[3 * 8];
        child.HasEvents = true;
        child.X = 2000;
        int childClicks = 0;
        child.Click += (_, _) => childClicks++;
        Mock<ICursor> cursor = CreateCursor(card.X + 2005, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        childClicks.ShouldBe(0, "the child is outside the card's bounds");

        card.RaiseChildrenEventsOutsideOfBounds = true;
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
        // The cursor hovers the empty list, so the walk learns that nothing inside it has events.
        Mock<ICursor> hover = CreateCursor(
            listBox.Visual.GetAbsoluteX() + listBox.Visual.AbsoluteWidth / 2,
            listBox.Visual.GetAbsoluteY() + listBox.Visual.AbsoluteHeight / 2);
        Walk(roots, hover);
        Walk(roots, hover);

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

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenClonedElementWithEventsIsAddedToASkippedParent()
    {
        (List<GraphicalUiElement> roots, List<CountingInteractive> cards, List<CountingInteractive> children) =
            CreateBoard(cardsHaveEvents: false);
        CountingInteractive source = CreateBox(0, 0, 10, hasEvents: true);
        Mock<ICursor> cursor = CreateCursor(cards[15].X + 5, 5, click: true);
        Walk(roots, cursor);
        Walk(roots, cursor);
        int clicks = 0;

        GraphicalUiElement clone = source.Clone();
        ((InteractiveGue)clone).Click += (_, _) => clicks++;
        children[15 * 8 + 2].AddChild(clone);
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

    #region Hidden elements (#6767)

    // A screen-like root: tagged with a ScreenSave so its children are hit-tested outside its bounds.
    private static (CountingInteractive Screen, List<CountingInteractive> Children) CreateScreen(float x)
    {
        CountingInteractive screen = CreateBox(x, 0, 10, hasEvents: false);
        screen.Tag = new ScreenSave();
        List<CountingInteractive> children = new();
        for (int i = 0; i < 8; i++)
        {
            CountingInteractive child = CreateBox(i * 12, 0, 10, hasEvents: true);
            screen.AddChild(child);
            children.Add(child);
        }
        return (screen, children);
    }

    private static void SetButtons(Mock<ICursor> cursor, bool push, bool down, bool click)
    {
        cursor.Setup(c => c.PrimaryPush).Returns(push);
        cursor.Setup(c => c.PrimaryDown).Returns(down);
        cursor.Setup(c => c.PrimaryClick).Returns(click);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldTestNothing_WhenEveryRootIsAHiddenScreen()
    {
        List<GraphicalUiElement> roots = new();
        List<CountingInteractive> tested = new();
        for (int i = 0; i < 25; i++)
        {
            (CountingInteractive screen, List<CountingInteractive> children) = CreateScreen(0);
            screen.Visible = false;
            roots.Add(screen);
            tested.Add(screen);
            tested.AddRange(children);
        }
        Mock<ICursor> cursor = CreateCursor(5, 5);

        Walk(roots, cursor);
        Walk(roots, cursor);

        TotalCalls(tested).ShouldBe(0);
        cursor.Object.VisualOver.ShouldBeNull();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldNotTestAHiddenChildWithEvents()
    {
        CountingInteractive parent = CreateBox(0, 0, 100, hasEvents: false);
        CountingInteractive hidden = CreateBox(0, 0, 10, hasEvents: true);
        CountingInteractive visible = CreateBox(50, 0, 10, hasEvents: true);
        parent.AddChild(visible);
        parent.AddChild(hidden);
        hidden.Visible = false;
        Mock<ICursor> cursor = CreateCursor(5, 5);

        Walk(new List<GraphicalUiElement> { parent }, cursor);

        hidden.HasCursorOverCalls.ShouldBe(0);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldNotTestARoot_WhenItIsHiddenAfterBeingWalked()
    {
        (CountingInteractive screen, List<CountingInteractive> children) = CreateScreen(0);
        List<GraphicalUiElement> roots = new() { screen };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        Walk(roots, cursor);
        cursor.Object.VisualOver.ShouldBe(children[0]);
        screen.HasCursorOverCalls = 0;
        foreach (CountingInteractive child in children)
        {
            child.HasCursorOverCalls = 0;
        }

        screen.Visible = false;
        Walk(roots, cursor);

        screen.HasCursorOverCalls.ShouldBe(0);
        TotalCalls(children).ShouldBe(0);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseClick_WhenAHiddenRootIsShownAgain()
    {
        (CountingInteractive screen, List<CountingInteractive> children) = CreateScreen(0);
        int clicks = 0;
        children[0].Click += (_, _) => clicks++;
        List<GraphicalUiElement> roots = new() { screen };
        Mock<ICursor> cursor = CreateCursor(5, 5, click: true);
        Walk(roots, cursor);
        clicks.ShouldBe(1);

        screen.Visible = false;
        Walk(roots, cursor);
        clicks.ShouldBe(1, "a hidden root is not clicked");

        screen.Visible = true;
        Walk(roots, cursor);

        clicks.ShouldBe(2);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldClickALowerRoot_WhenAHiddenRootCoversIt()
    {
        CountingInteractive lower = CreateBox(0, 0, 10, hasEvents: true);
        int clicks = 0;
        lower.Click += (_, _) => clicks++;
        CountingInteractive upper = CreateBox(0, 0, 10, hasEvents: true);
        upper.Visible = false;
        List<GraphicalUiElement> roots = new() { lower, upper };
        Mock<ICursor> cursor = CreateCursor(5, 5, click: true);

        Walk(roots, cursor);

        clicks.ShouldBe(1);
        cursor.Object.VisualOver.ShouldBe(lower);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseRollOff_WhenTheHoveredElementsRootIsHidden()
    {
        (CountingInteractive screen, List<CountingInteractive> children) = CreateScreen(0);
        int rollOffs = 0;
        children[0].RollOff += (_, _) => rollOffs++;
        List<GraphicalUiElement> roots = new() { screen };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        Walk(roots, cursor);
        cursor.Object.VisualOver.ShouldBe(children[0]);

        screen.Visible = false;
        Walk(roots, cursor);

        rollOffs.ShouldBe(1);
        cursor.Object.VisualOver.ShouldBeNull();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseRollOff_WhenTheHoveredElementIsHidden()
    {
        (CountingInteractive screen, List<CountingInteractive> children) = CreateScreen(0);
        int rollOffs = 0;
        children[0].RollOff += (_, _) => rollOffs++;
        List<GraphicalUiElement> roots = new() { screen };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        Walk(roots, cursor);

        children[0].Visible = false;
        Walk(roots, cursor);

        rollOffs.ShouldBe(1);
        cursor.Object.VisualOver.ShouldBeNull();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseRollOffNextFrame_WhenAClickHandlerHidesItsRoot()
    {
        (CountingInteractive screen, List<CountingInteractive> children) = CreateScreen(0);
        int clicks = 0;
        int rollOffs = 0;
        int removedAsPushed = 0;
        children[0].Click += (_, _) =>
        {
            clicks++;
            screen.Visible = false;
        };
        children[0].RollOff += (_, _) => rollOffs++;
        children[0].RemovedAsPushed += (_, _) => removedAsPushed++;
        List<GraphicalUiElement> roots = new() { screen };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        SetButtons(cursor, push: true, down: true, click: false);
        Walk(roots, cursor);

        SetButtons(cursor, push: false, down: false, click: true);
        Walk(roots, cursor);
        clicks.ShouldBe(1);
        removedAsPushed.ShouldBe(1);
        rollOffs.ShouldBe(0, "the element was over when the frame began");

        SetButtons(cursor, push: false, down: false, click: false);
        Walk(roots, cursor);

        rollOffs.ShouldBe(1);
        cursor.Object.VisualOver.ShouldBeNull();
        cursor.Object.WindowPushed.ShouldBeNull();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldKeepDraggingThenRemoveAsPushed_WhenThePushedElementsRootIsHidden()
    {
        (CountingInteractive screen, List<CountingInteractive> children) = CreateScreen(0);
        int dragging = 0;
        int clicks = 0;
        int removedAsPushed = 0;
        int rollOffs = 0;
        children[0].Dragging += (_, _) => dragging++;
        children[0].Click += (_, _) => clicks++;
        children[0].RemovedAsPushed += (_, _) => removedAsPushed++;
        children[0].RollOff += (_, _) => rollOffs++;
        List<GraphicalUiElement> roots = new() { screen };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        SetButtons(cursor, push: true, down: true, click: false);
        Walk(roots, cursor);
        cursor.Object.WindowPushed.ShouldBe(children[0]);
        dragging.ShouldBe(1);

        screen.Visible = false;
        SetButtons(cursor, push: false, down: true, click: false);
        Walk(roots, cursor);
        dragging.ShouldBe(2, "the push is held, so the pushed element keeps dragging");
        cursor.Object.WindowPushed.ShouldBe(children[0]);
        removedAsPushed.ShouldBe(0);
        rollOffs.ShouldBe(1);

        SetButtons(cursor, push: false, down: false, click: true);
        Walk(roots, cursor);

        removedAsPushed.ShouldBe(1);
        clicks.ShouldBe(0, "a hidden element is not clicked");
        cursor.Object.WindowPushed.ShouldBeNull();
        dragging.ShouldBe(2);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseRemovedAsPushed_WhenAPushHandlerHidesTheElement()
    {
        CountingInteractive box = CreateBox(0, 0, 10, hasEvents: true);
        int removedAsPushed = 0;
        int clicks = 0;
        box.Push += (_, _) => box.Visible = false;
        box.Click += (_, _) => clicks++;
        box.RemovedAsPushed += (_, _) => removedAsPushed++;
        List<GraphicalUiElement> roots = new() { box };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        SetButtons(cursor, push: true, down: true, click: false);
        Walk(roots, cursor);
        cursor.Object.WindowPushed.ShouldBe(box);

        SetButtons(cursor, push: false, down: false, click: true);
        Walk(roots, cursor);

        removedAsPushed.ShouldBe(1);
        clicks.ShouldBe(0);
        cursor.Object.WindowPushed.ShouldBeNull();
        cursor.Object.VisualOver.ShouldBeNull();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseRollOnAgain_WhenAHiddenHoveredRootIsShown()
    {
        (CountingInteractive screen, List<CountingInteractive> children) = CreateScreen(0);
        int rollOns = 0;
        children[0].RollOn += (_, _) => rollOns++;
        List<GraphicalUiElement> roots = new() { screen };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        Walk(roots, cursor);
        rollOns.ShouldBe(1);

        screen.Visible = false;
        Walk(roots, cursor);
        screen.Visible = true;
        Walk(roots, cursor);

        rollOns.ShouldBe(2);
        cursor.Object.VisualOver.ShouldBe(children[0]);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldClickALowerSibling_WhenAHiddenParentsVisibleChildrenCoverIt()
    {
        CountingInteractive root = CreateBox(0, 0, 100, hasEvents: false);
        CountingInteractive lower = CreateBox(0, 0, 10, hasEvents: true);
        CountingInteractive panel = CreateBox(0, 0, 10, hasEvents: false);
        CountingInteractive inner = CreateBox(0, 0, 10, hasEvents: true);
        int lowerClicks = 0;
        int innerClicks = 0;
        lower.Click += (_, _) => lowerClicks++;
        inner.Click += (_, _) => innerClicks++;
        root.AddChild(lower);
        root.AddChild(panel);
        panel.AddChild(inner);
        panel.Visible = false;
        List<GraphicalUiElement> roots = new() { root };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        SetButtons(cursor, push: true, down: true, click: false);
        Walk(roots, cursor);

        SetButtons(cursor, push: false, down: false, click: true);
        Walk(roots, cursor);

        lowerClicks.ShouldBe(1);
        innerClicks.ShouldBe(0);
        cursor.Object.VisualOver.ShouldBe(lower);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldNotClickAFormsButton_WhenItsScreenIsHiddenWhilePushed()
    {
        CountingInteractive screen = CreateBox(0, 0, 200, hasEvents: false);
        screen.Tag = new ScreenSave();
        Button button = new();
        button.Visual.X = 0;
        button.Visual.Y = 0;
        screen.AddChild(button.Visual);
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        List<GraphicalUiElement> roots = new() { screen };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        FrameworkElement.MainCursor = cursor.Object;
        Walk(roots, cursor);
        SetButtons(cursor, push: true, down: true, click: false);
        Walk(roots, cursor);
        cursor.Object.WindowPushed.ShouldBe(button.Visual);

        screen.Visible = false;
        SetButtons(cursor, push: false, down: true, click: false);
        Walk(roots, cursor);
        SetButtons(cursor, push: false, down: false, click: true);
        Walk(roots, cursor);
        SetButtons(cursor, push: false, down: false, click: false);
        Walk(roots, cursor);

        clicks.ShouldBe(0);
        cursor.Object.WindowPushed.ShouldBeNull();
        cursor.Object.VisualOver.ShouldBeNull();
    }

    // Reports the cursor over itself no matter its visibility.
    private sealed class AlwaysOverInteractive : InteractiveGue
    {
        public AlwaysOverInteractive() : base(new InvisibleRenderable())
        {
        }

        public override bool HasCursorOver(ICursor cursor, Layer? layer = null) => true;
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldNotConsultAHiddenElementsHasCursorOverOverride()
    {
        AlwaysOverInteractive root = new() { HasEvents = true };
        root.Visible = false;
        int clicks = 0;
        root.Click += (_, _) => clicks++;
        List<GraphicalUiElement> roots = new() { root };
        Mock<ICursor> cursor = CreateCursor(5, 5);
        SetButtons(cursor, push: true, down: true, click: false);
        Walk(roots, cursor);

        SetButtons(cursor, push: false, down: false, click: true);
        Walk(roots, cursor);

        clicks.ShouldBe(0, "a hidden element takes no part in the cursor walk, even with a custom hit test");
        cursor.Object.VisualOver.ShouldBeNull();
    }

    #endregion

    #region Outside of bounds

    // A container 10x10 at the origin that raises its children's events outside its bounds. Its children sit
    // outside it: the first is the only one with events, the rest have none under them.
    private static (CountingInteractive Root, CountingInteractive WithEvents, List<CountingInteractive> NoEvents) CreateOutsideBoundsRoot(
        bool rootHasEvents = false, int eventlessCount = 40)
    {
        CountingInteractive root = CreateBox(0, 0, 10, rootHasEvents);
        root.RaiseChildrenEventsOutsideOfBounds = true;
        CountingInteractive withEvents = CreateBox(100, 0, 10, hasEvents: true);
        root.AddChild(withEvents);
        List<CountingInteractive> noEvents = new();
        for (int i = 0; i < eventlessCount; i++)
        {
            CountingInteractive child = CreateBox(200 + i * 12, 0, 10, hasEvents: false);
            root.AddChild(child);
            noEvents.Add(child);
        }
        return (root, withEvents, noEvents);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldNotTestChildrenWithoutEvents_WhenCursorIsOutsideAnOutsideBoundsRoot()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        Mock<ICursor> cursor = CreateCursor(5000, 5000);

        Walk(new List<GraphicalUiElement> { root }, cursor);

        TotalCalls(noEvents).ShouldBe(0);
        cursor.Object.VisualOver.ShouldBeNull();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldClickChildWithEvents_WhenItIsOutsideTheRootsBounds()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        int clicks = 0;
        withEvents.Click += (_, _) => clicks++;
        Mock<ICursor> cursor = CreateCursor(105, 5, click: true);

        Walk(new List<GraphicalUiElement> { root }, cursor);

        clicks.ShouldBe(1);
        cursor.Object.VisualOver.ShouldBe(withEvents);
        TotalCalls(noEvents).ShouldBe(0);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldNotTakeTheCursorForTheRoot_WhenOnlyAChildWithoutEventsIsUnderIt()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot(rootHasEvents: true);
        Mock<ICursor> cursor = CreateCursor(205, 5);

        Walk(new List<GraphicalUiElement> { root }, cursor);

        cursor.Object.VisualOver.ShouldBeNull("a child with no events is not a hit target, so it does not extend the root's hit area");
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldHitADescendantWithEvents_WhenItIsUnderAnEventlessChildOutsideTheBounds()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot(rootHasEvents: true);
        withEvents.HasEvents = false;
        CountingInteractive grandchild = CreateBox(0, 0, 10, hasEvents: true);
        withEvents.AddChild(grandchild);
        Mock<ICursor> cursor = CreateCursor(105, 5);

        Walk(new List<GraphicalUiElement> { root }, cursor);

        cursor.Object.VisualOver.ShouldBe(grandchild);
    }

    [Fact]
    public void HasCursorOver_ShouldStillReportTrue_WhenCursorIsOverAChildWithoutEventsOutsideTheBounds()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot(rootHasEvents: true);
        Mock<ICursor> cursor = CreateCursor(205, 5);

        root.HasCursorOver(cursor.Object).ShouldBeTrue();
    }

    [Fact]
    public void HasCursorOver_ShouldStillReportTrue_WhenCalledAfterAWalk()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot(rootHasEvents: true);
        Mock<ICursor> cursor = CreateCursor(205, 5);
        Walk(new List<GraphicalUiElement> { root }, cursor);

        root.HasCursorOver(cursor.Object).ShouldBeTrue();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldClick_WhenAChildOutsideTheBoundsGainsEventsAfterBeingSkipped()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        Mock<ICursor> cursor = CreateCursor(205, 5, click: true);
        List<GraphicalUiElement> roots = new() { root };
        Walk(roots, cursor);
        int clicks = 0;
        noEvents[0].Click += (_, _) => clicks++;

        noEvents[0].HasEvents = true;
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldClick_WhenADescendantWithEventsIsAddedUnderAnEventlessChildOutsideTheBounds()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        Mock<ICursor> cursor = CreateCursor(205, 5, click: true);
        List<GraphicalUiElement> roots = new() { root };
        Walk(roots, cursor);
        int clicks = 0;
        CountingInteractive late = CreateBox(0, 0, 10, hasEvents: true);
        late.Click += (_, _) => clicks++;

        noEvents[0].AddChild(late);
        Walk(roots, cursor);

        clicks.ShouldBe(1);

        noEvents[0].RemoveChild(late);
        Walk(roots, cursor);

        clicks.ShouldBe(1, "removing the descendant makes the child eventless again");
        cursor.Object.VisualOver.ShouldBeNull();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldClick_WhenAHiddenChildWithEventsOutsideTheBoundsIsShown()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        int clicks = 0;
        withEvents.Click += (_, _) => clicks++;
        withEvents.Visible = false;
        Mock<ICursor> cursor = CreateCursor(105, 5, click: true);
        List<GraphicalUiElement> roots = new() { root };
        Walk(roots, cursor);
        clicks.ShouldBe(0);

        withEvents.Visible = true;
        Walk(roots, cursor);

        clicks.ShouldBe(1);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldRaiseRollOff_WhenTheHoveredChildOutsideTheBoundsIsHidden()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        int rollOffs = 0;
        withEvents.RollOff += (_, _) => rollOffs++;
        Mock<ICursor> cursor = CreateCursor(105, 5);
        List<GraphicalUiElement> roots = new() { root };
        Walk(roots, cursor);
        cursor.Object.VisualOver.ShouldBe(withEvents);

        withEvents.Visible = false;
        Walk(roots, cursor);

        rollOffs.ShouldBe(1);
        cursor.Object.VisualOver.ShouldBeNull();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldKeepDraggingThenRemoveAsPushed_WhenThePushedChildOutsideTheBoundsIsHidden()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        int dragging = 0;
        int clicks = 0;
        int removedAsPushed = 0;
        withEvents.Dragging += (_, _) => dragging++;
        withEvents.Click += (_, _) => clicks++;
        withEvents.RemovedAsPushed += (_, _) => removedAsPushed++;
        List<GraphicalUiElement> roots = new() { root };
        Mock<ICursor> cursor = CreateCursor(105, 5);
        SetButtons(cursor, push: true, down: true, click: false);
        Walk(roots, cursor);
        cursor.Object.WindowPushed.ShouldBe(withEvents);

        withEvents.Visible = false;
        SetButtons(cursor, push: false, down: true, click: false);
        Walk(roots, cursor);
        dragging.ShouldBe(2);

        SetButtons(cursor, push: false, down: false, click: true);
        Walk(roots, cursor);

        removedAsPushed.ShouldBe(1);
        clicks.ShouldBe(0);
        cursor.Object.WindowPushed.ShouldBeNull();
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldNotTestOrClickAHiddenOutsideBoundsRoot()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        int clicks = 0;
        withEvents.Click += (_, _) => clicks++;
        root.Visible = false;
        Mock<ICursor> cursor = CreateCursor(105, 5, click: true);

        Walk(new List<GraphicalUiElement> { root }, cursor);

        clicks.ShouldBe(0);
        withEvents.HasCursorOverCalls.ShouldBe(0);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldClickALowerRoot_WhenAnOutsideBoundsRootAboveItHasNothingUnderTheCursor()
    {
        (CountingInteractive upper, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        CountingInteractive lower = CreateBox(205, 0, 10, hasEvents: true);
        int clicks = 0;
        lower.Click += (_, _) => clicks++;
        Mock<ICursor> cursor = CreateCursor(205, 5, click: true);

        Walk(new List<GraphicalUiElement> { lower, upper }, cursor);

        clicks.ShouldBe(1);
        cursor.Object.VisualOver.ShouldBe(lower);
    }

    [Fact]
    public void DoUiActivityRecursively_ShouldNotTestEventlessChildren_WhenANestedOutsideBoundsContainerIsTested()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot();
        CountingInteractive nested = CreateBox(1000, 0, 10, hasEvents: false);
        nested.RaiseChildrenEventsOutsideOfBounds = true;
        CountingInteractive nestedTarget = CreateBox(300, 0, 10, hasEvents: true);
        nested.AddChild(nestedTarget);
        List<CountingInteractive> nestedEventless = new();
        for (int i = 0; i < 20; i++)
        {
            CountingInteractive child = CreateBox(400 + i * 12, 0, 10, hasEvents: false);
            nested.AddChild(child);
            nestedEventless.Add(child);
        }
        root.AddChild(nested);
        int clicks = 0;
        nestedTarget.Click += (_, _) => clicks++;
        Mock<ICursor> cursor = CreateCursor(1305, 5, click: true);

        Walk(new List<GraphicalUiElement> { root }, cursor);

        clicks.ShouldBe(1);
        TotalCalls(nestedEventless).ShouldBe(0);
        TotalCalls(noEvents).ShouldBe(0);
    }

    // Asks another element whether the cursor is over it from inside its own override.
    private sealed class AsksAnotherInteractive : InteractiveGue
    {
        public InteractiveGue? Other;
        public bool? OtherAnswer;

        public AsksAnotherInteractive() : base(new InvisibleRenderable())
        {
        }

        public override bool HasCursorOver(ICursor cursor, Layer? layer = null)
        {
            OtherAnswer = Other?.HasCursorOver(cursor);
            return base.HasCursorOver(cursor, layer);
        }
    }

    [Fact]
    public void HasCursorOver_ShouldReportTrueOverAnEventlessChild_WhenCalledFromInsideAnotherElementsOverrideDuringAWalk()
    {
        (CountingInteractive other, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot(rootHasEvents: true);
        AsksAnotherInteractive asker = new() { HasEvents = true };
        asker.Other = other;
        Mock<ICursor> cursor = CreateCursor(205, 5);

        Walk(new List<GraphicalUiElement> { asker }, cursor);

        asker.OtherAnswer.ShouldBe(true, "only the element the walk is testing skips children without events");
    }

    #endregion

    #region Verify 6788 walk keying

    private sealed class ThrowingInteractive : InteractiveGue
    {
        public bool Throw;
        public ThrowingInteractive() : base(new InvisibleRenderable()) { }
        public override bool HasCursorOver(ICursor cursor, Layer? layer = null)
        {
            if (Throw) throw new System.InvalidOperationException("boom");
            return base.HasCursorOver(cursor, layer);
        }
    }

    private sealed class MutatingInteractive : InteractiveGue
    {
        public System.Action? OnTest;
        public MutatingInteractive() : base(new InvisibleRenderable()) { }
        public override bool HasCursorOver(ICursor cursor, Layer? layer = null)
        {
            OnTest?.Invoke();
            OnTest = null;
            return base.HasCursorOver(cursor, layer);
        }
    }

    [Fact]
    public void Verify_ThreeLevelsDeep_SkipsEventlessSiblingsAtEachLevel_AndFindsDeepTarget()
    {
        CountingInteractive a = CreateBox(0, 0, 10, false); a.RaiseChildrenEventsOutsideOfBounds = true;
        CountingInteractive b = CreateBox(100, 0, 10, false); b.RaiseChildrenEventsOutsideOfBounds = true;
        CountingInteractive c = CreateBox(100, 0, 10, false); c.RaiseChildrenEventsOutsideOfBounds = true;
        CountingInteractive target = CreateBox(100, 0, 10, true);
        List<CountingInteractive> dead = new();
        foreach (CountingInteractive parent in new[] { a, b, c })
        {
            for (int i = 0; i < 5; i++)
            {
                CountingInteractive d = CreateBox(1000 + i * 12, 0, 10, false);
                parent.AddChild(d); dead.Add(d);
            }
        }
        a.AddChild(b); b.AddChild(c); c.AddChild(target);
        int clicks = 0;
        target.Click += (_, _) => clicks++;
        Mock<ICursor> cursor = CreateCursor(305, 5, click: true);
        Walk(new List<GraphicalUiElement> { a }, cursor);
        clicks.ShouldBe(1);
        TotalCalls(dead).ShouldBe(0);
    }

    [Fact]
    public void Verify_ExceptionInOverride_DoesNotLeakWalkTarget()
    {
        ThrowingInteractive thrower = new() { HasEvents = true };
        thrower.WidthUnits = DimensionUnitType.Absolute; thrower.HeightUnits = DimensionUnitType.Absolute;
        thrower.Width = 10; thrower.Height = 10; thrower.RaiseChildrenEventsOutsideOfBounds = true;
        CountingInteractive eventless = CreateBox(200, 0, 10, false);
        thrower.AddChild(eventless);
        thrower.Throw = true;
        Mock<ICursor> cursor = CreateCursor(205, 5);
        Should.Throw<System.Exception>(() => Walk(new List<GraphicalUiElement> { thrower }, cursor));
        // outside the walk, thrower must report over an eventless child (target restored to null)
        thrower.Throw = false;
        thrower.HasCursorOver(cursor.Object).ShouldBeTrue();
        // next frame walk still skips eventless child
        eventless.HasCursorOverCalls = 0;
        Walk(new List<GraphicalUiElement> { thrower }, cursor);
        eventless.HasCursorOverCalls.ShouldBe(0);
    }

    [Fact]
    public void Verify_TargetRemovedMidWalkInsideOverride_DoesNotThrow()
    {
        (CountingInteractive root, CountingInteractive withEvents, List<CountingInteractive> noEvents) = CreateOutsideBoundsRoot(eventlessCount: 3);
        MutatingInteractive m = new() { HasEvents = true };
        m.WidthUnits = DimensionUnitType.Absolute; m.HeightUnits = DimensionUnitType.Absolute;
        m.Width = 10; m.Height = 10; m.X = 500; m.RaiseChildrenEventsOutsideOfBounds = true;
        CountingInteractive far = CreateBox(900, 0, 10, true);
        m.AddChild(far);
        root.AddChild(m);
        m.OnTest = () => m.Parent = null;
        Mock<ICursor> cursor = CreateCursor(905, 5);
        Should.NotThrow(() => Walk(new List<GraphicalUiElement> { root }, cursor));
        // subsequent frame consistent
        Should.NotThrow(() => Walk(new List<GraphicalUiElement> { root }, cursor));
        root.HasCursorOver(CreateCursor(205, 5).Object).ShouldBeTrue();
    }

    [Fact]
    public void Verify_TwoRoots_DoNotCrossContaminate()
    {
        (CountingInteractive r1, _, List<CountingInteractive> dead1) = CreateOutsideBoundsRoot(eventlessCount: 5);
        (CountingInteractive r2, CountingInteractive t2, List<CountingInteractive> dead2) = CreateOutsideBoundsRoot(eventlessCount: 5);
        int clicks = 0;
        t2.Click += (_, _) => clicks++;
        Mock<ICursor> cursor = CreateCursor(105, 5, click: true);
        Walk(new List<GraphicalUiElement> { r1, r2 }, cursor);
        TotalCalls(dead1).ShouldBe(0);
        TotalCalls(dead2).ShouldBe(0);
        clicks.ShouldBe(1);
        r1.HasCursorOver(CreateCursor(205, 5).Object).ShouldBeTrue();
    }

    #endregion
}
