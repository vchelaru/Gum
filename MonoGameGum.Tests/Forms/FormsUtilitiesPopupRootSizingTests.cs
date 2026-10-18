using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

// Covers issue #6137: popup and modal roots must track the canvas even while empty, because a popup
// is clamped to its root's bounds the moment it is added, before Update could resize a root with children.
public class FormsUtilitiesPopupRootSizingTests : BaseTestClass
{
    [Theory]
    [InlineData(1600, 960)]
    [InlineData(400, 300)]
    public void Update_SizesEmptyPopupAndModalRootsToCanvas_WhenCanvasChanges(
        int canvasWidth, int canvasHeight)
    {
        MakeStale(FrameworkElement.PopupRoot);
        MakeStale(FrameworkElement.ModalRoot);
        GraphicalUiElement.CanvasWidth = canvasWidth;
        GraphicalUiElement.CanvasHeight = canvasHeight;

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        FrameworkElement.PopupRoot.Width.ShouldBe(canvasWidth);
        FrameworkElement.PopupRoot.Height.ShouldBe(canvasHeight);
        FrameworkElement.ModalRoot.Width.ShouldBe(canvasWidth);
        FrameworkElement.ModalRoot.Height.ShouldBe(canvasHeight);
    }

    [Fact]
    public void Update_SizesEmptyAdditionalPopupAndModalRootsToCanvas_WhenCanvasChanges()
    {
        ContainerRuntime customPopupRoot = new();
        ContainerRuntime customModalRoot = new();
        customPopupRoot.AddToManagers(SystemManagers.Default);
        customModalRoot.AddToManagers(SystemManagers.Default);
        FrameworkElement.AdditionalPopupRootPairs.Add((customPopupRoot, customModalRoot));
        MakeStale(customPopupRoot);
        MakeStale(customModalRoot);
        GraphicalUiElement.CanvasWidth = 1600;
        GraphicalUiElement.CanvasHeight = 960;

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        customPopupRoot.X.ShouldBe(0);
        customPopupRoot.Y.ShouldBe(0);
        customPopupRoot.Width.ShouldBe(1600);
        customPopupRoot.Height.ShouldBe(960);
        customModalRoot.Width.ShouldBe(1600);
        customModalRoot.Height.ShouldBe(960);
    }

    [Fact]
    public void ComboBoxPopup_OpensAtComboBoxPosition_WhenCanvasGrewAfterRootsWereSized()
    {
        MakeStale(FrameworkElement.PopupRoot);
        MakeStale(FrameworkElement.ModalRoot);
        GraphicalUiElement.CanvasWidth = 1600;
        GraphicalUiElement.CanvasHeight = 960;

        ComboBox comboBox = new();
        comboBox.X = 1000;
        comboBox.Y = 300;
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");
        comboBox.AddToRoot();

        // The first frame sizes Root; the click happens on a later frame, as it would for a user.
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        PushOn(comboBox.Visual);

        comboBox.IsDropDownOpen.ShouldBeTrue();
        comboBox.ListBox.Visual.AbsoluteLeft.ShouldBe(comboBox.Visual.AbsoluteLeft);
        comboBox.ListBox.Visual.AbsoluteTop.ShouldBe(comboBox.Visual.AbsoluteBottom);
    }

    // Matches the size the roots get in Initialize, before the canvas changes.
    private static void MakeStale(GraphicalUiElement root)
    {
        root.Width = 800;
        root.Height = 600;
    }

    private static void PushOn(InteractiveGue visualOver)
    {
        float x = visualOver.GetAbsoluteX() + visualOver.AbsoluteWidth / 2;
        float y = visualOver.GetAbsoluteY() + visualOver.AbsoluteHeight / 2;

        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.PrimaryPush).Returns(true);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.WindowPushed).Returns(visualOver);
        cursor.Setup(c => c.VisualOver).Returns(visualOver);
        cursor.Setup(c => c.X).Returns((int)x);
        cursor.Setup(c => c.Y).Returns((int)y);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        FormsUtilities.SetCursor(cursor.Object);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
    }
}
