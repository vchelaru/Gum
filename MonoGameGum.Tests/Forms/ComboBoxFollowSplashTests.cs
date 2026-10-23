using System;
using System.Linq;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using FlatRedBall.Glue.StateInterpolation;
using MonoGameGum.Tests.Input;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Splash-damage sweep (#6644) around the open ComboBox drop-down following its ComboBox: the cases
/// the pinned tests in ComboBoxTests do not cover (resize, open/close cycles, edges, input and focus
/// after a re-placement, tree changes while open, idle gating).
/// </summary>
public class ComboBoxFollowSplashTests : BaseTestClass
{
    #region Helpers

    private static ComboBox CreateComboBox(params string[] items)
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        foreach (string item in items)
        {
            comboBox.Items!.Add(item);
        }
        return comboBox;
    }

    private static void Tick(double seconds = 0.016) => Gum.GumService.Default.Root.AnimateSelf(seconds);

    private static (float X, float Y) OffsetFromComboBox(ComboBox comboBox) =>
        (comboBox.ListBox.Visual.AbsoluteX - comboBox.Visual.AbsoluteX,
         comboBox.ListBox.Visual.AbsoluteY - comboBox.Visual.AbsoluteY);

    private static void ShouldKeepOffset(ComboBox comboBox, (float X, float Y) offset)
    {
        (float X, float Y) now = OffsetFromComboBox(comboBox);
        now.X.ShouldBe(offset.X, 0.5f);
        now.Y.ShouldBe(offset.Y, 0.5f);
    }

    private static void PushOn(InteractiveGue? visualOver, int frames = 1)
    {
        float x = GraphicalUiElement.CanvasWidth - 1;
        float y = GraphicalUiElement.CanvasHeight - 1;
        if (visualOver != null)
        {
            x = visualOver.GetAbsoluteX() + visualOver.AbsoluteWidth / 2;
            y = visualOver.GetAbsoluteY() + visualOver.AbsoluteHeight / 2;
        }

        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.PrimaryPush).Returns(true);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.WindowPushed).Returns(visualOver);
        cursor.Setup(c => c.VisualOver).Returns(visualOver);
        cursor.Setup(c => c.X).Returns((int)x);
        cursor.Setup(c => c.Y).Returns((int)y);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);

        for (int i = 0; i < frames; i++)
        {
            Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        }
    }

    private static (ContainerRuntime Container, ComboBox ComboBox) CreateInContainer()
    {
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.Children!.Add(container);
        ComboBox comboBox = new();
        container.Children!.Add(comboBox.Visual);
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");
        comboBox.Items.Add("Cherry");
        return (container, comboBox);
    }

    #endregion

    #region Rect changes

    [Fact]
    public void Follow_ComboBoxWidens_PopupWidthFollows()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteWidth.ShouldBe(comboBox.Visual.AbsoluteWidth, 0.5f);

        comboBox.Width += 60;
        Tick();

        popup.AbsoluteWidth.ShouldBe(comboBox.Visual.AbsoluteWidth, 0.5f);
    }

    [Fact]
    public void Follow_ComboBoxChangesHeight_PopupKeepsItsOffset()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);

        comboBox.Height += 20;
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_ComboBoxMovesOnlyInX_PopupFollows()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);

        comboBox.X += 75;
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_ComboBoxMovesOnlyInY_PopupFollows()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);

        comboBox.Y += 40;
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_CanvasGrowsWithTopRightAnchor_PopupFollows()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.Anchor(Anchor.TopRight);
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        float comboXAtOpen = comboBox.Visual.AbsoluteX;

        GraphicalUiElement.CanvasWidth = 1100;
        Gum.GumService.Default.Root.UpdateLayout();
        // GumService.Update sizes the popup root to the canvas each frame, before the roots animate.
        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
        comboBox.Visual.AbsoluteX.ShouldNotBe(comboXAtOpen);
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_ComboBoxMovesTwiceBeforeATick_PopupMatchesTheLastPosition()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);

        comboBox.X += 20;
        comboBox.X += 35;
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_ItemsAddedWhileOpenThenComboBoxMoves_PopupFollows()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        comboBox.Items!.Add("Cherry");
        comboBox.Items.Add("Date");

        comboBox.X += 50;
        Tick();

        ShouldKeepOffset(comboBox, offset);
        comboBox.Items.Count.ShouldBe(4);
    }

    #endregion

    #region Screen edges

    [Fact]
    public void Follow_ComboBoxMovesPastTheLeftEdge_PopupIsClampedToTheCanvas()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;

        comboBox.X = -50;
        Tick();

        comboBox.ListBox.Visual.AbsoluteX.ShouldBeGreaterThanOrEqualTo(-0.5f);
    }

    [Fact]
    public void Follow_ComboBoxMovesPastTheRightEdge_PopupIsClampedToTheCanvas()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement popup = comboBox.ListBox.Visual;

        comboBox.X = GraphicalUiElement.CanvasWidth - 40;
        Tick();

        (popup.AbsoluteX + popup.AbsoluteWidth).ShouldBeLessThanOrEqualTo(GraphicalUiElement.CanvasWidth + 0.5f);
    }

    [Fact]
    public void Follow_ClampedThenComboBoxMovesBack_PopupReturnsToItsOffset()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        comboBox.X = GraphicalUiElement.CanvasWidth - 40;
        Tick();

        comboBox.X = 100;
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    #endregion

    #region Open/close cycles

    [Fact]
    public void Close_AfterFollowing_RestoresTheListBoxLayoutValues()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        GeneralUnitType xUnits = popup.XUnits;
        GeneralUnitType yUnits = popup.YUnits;
        DimensionUnitType widthUnits = popup.WidthUnits;
        DimensionUnitType heightUnits = popup.HeightUnits;
        float x = popup.X;
        float y = popup.Y;
        float width = popup.Width;
        float height = popup.Height;
        comboBox.IsDropDownOpen = true;
        comboBox.X += 80;
        Tick();
        comboBox.Width += 40;
        Tick();

        comboBox.IsDropDownOpen = false;

        popup.XUnits.ShouldBe(xUnits);
        popup.YUnits.ShouldBe(yUnits);
        popup.WidthUnits.ShouldBe(widthUnits);
        popup.HeightUnits.ShouldBe(heightUnits);
        popup.X.ShouldBe(x);
        popup.Y.ShouldBe(y);
        popup.Width.ShouldBe(width);
        popup.Height.ShouldBe(height);
    }

    [Fact]
    public void Reopen_AfterFollowingAndClosing_PlacesPopupAtTheNewPosition()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        comboBox.X += 90;
        Tick();
        comboBox.IsDropDownOpen = false;

        comboBox.IsDropDownOpen = true;

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Reopen_AfterMovingWhileClosed_PlacesPopupAtTheNewPosition()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        comboBox.IsDropDownOpen = false;
        comboBox.X += 120;
        Tick();

        comboBox.IsDropDownOpen = true;

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Reopen_ThenMoves_FollowsAgain()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        comboBox.X += 30;
        Tick();
        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;

        comboBox.Y += 25;
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_ManyTimes_PopupIsInPopupRootExactlyOnceWithInfiniteZ()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement popup = comboBox.ListBox.Visual;

        for (int i = 0; i < 4; i++)
        {
            comboBox.X += 10;
            Tick();
        }

        int occurrences = FrameworkElement.PopupRoot.Children!.Count(child => ReferenceEquals(child, popup));
        occurrences.ShouldBe(1);
        popup.Parent.ShouldBeSameAs(FrameworkElement.PopupRoot);
        popup.Z.ShouldBe(float.PositiveInfinity);
        comboBox.Visual.Children!.ShouldNotContain(popup);
    }

    #endregion

    #region Input and state after following

    [Fact]
    public void Follow_PushOutsideAfterFollowing_ClosesTheDropDown()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        comboBox.X += 60;
        Tick();

        PushOn(null, frames: 2);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void Follow_PushOnRowAfterFollowing_SelectsItAndCloses()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        comboBox.X += 60;
        Tick();

        PushOn(comboBox.ListBox.ListBoxItems[2].Visual);

        comboBox.Text.ShouldBe("Cherry");
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void Follow_DoesNotRaiseSelectionChanged()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;
        int raised = 0;
        comboBox.SelectionChanged += (_, _) => raised++;
        comboBox.IsDropDownOpen = true;

        comboBox.X += 60;
        Tick();
        comboBox.Y += 10;
        Tick();

        raised.ShouldBe(0);
        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void Follow_FocusedComboBox_KeepsFocusAndRegainsItAfterClose()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsFocused = true;
        comboBox.IsDropDownOpen = true;

        comboBox.X += 60;
        Tick();
        comboBox.IsDropDownOpen = false;

        comboBox.IsFocused.ShouldBeTrue();
    }

    #endregion

    #region Tree changes while open

    [Fact]
    public void Follow_RemovedFromTheTreeWhileOpenThenAddedAtAnotherPlace_PopupFollows()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);

        Gum.GumService.Default.Root.Children!.Remove(comboBox.Visual);
        Tick();
        comboBox.X += 140;
        Gum.GumService.Default.Root.Children.Add(comboBox.Visual);
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_AncestorHiddenThenShownAtANewPosition_PopupFollowsAfterShown()
    {
        (ContainerRuntime container, ComboBox comboBox) = CreateInContainer();
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        container.Visible = false;
        Tick();
        container.X += 100;
        container.UpdateLayout();

        container.Visible = true;
        Tick();

        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Follow_VisualReplacedWhileOpen_TickingDoesNotThrow()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;

        comboBox.Visual = null!;

        Should.NotThrow(() => Tick());
    }

    [Fact]
    public void Follow_VisualReplacedWhileOpen_PushingElsewhereDoesNotThrow()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;

        comboBox.Visual = null!;

        Should.NotThrow(() => PushOn(null, frames: 2));
    }

    #endregion

    #region Idle gating

    [Fact]
    public void Idle_TwoComboBoxesOneOpen_OnlyTheOpenOnesContainerStaysAwake()
    {
        (ContainerRuntime closedContainer, ComboBox closedBox) = CreateInContainer();
        (ContainerRuntime openContainer, ComboBox openBox) = CreateInContainer();
        openBox.IsDropDownOpen = true;

        Tick();
        Tick();
        Tick();

        closedContainer.IsIdle.ShouldBeTrue();
        openContainer.IsIdle.ShouldBeFalse();
        closedBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void Idle_ClosedComboBoxAncestorHiddenAndShown_StillGoesIdle()
    {
        (ContainerRuntime container, ComboBox comboBox) = CreateInContainer();
        Tick();
        Tick();
        container.Visible = false;
        Tick();
        container.Visible = true;

        Tick();
        Tick();
        Tick();

        container.IsIdle.ShouldBeTrue();
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void Idle_OpenComboBoxAncestorHiddenAndShown_StaysAwakeAndKeepsFollowing()
    {
        (ContainerRuntime container, ComboBox comboBox) = CreateInContainer();
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        Tick();
        container.Visible = false;
        Tick();
        container.Visible = true;

        comboBox.X += 45;
        Tick();
        Tick();

        container.IsIdle.ShouldBeFalse();
        ShouldKeepOffset(comboBox, offset);
    }

    [Fact]
    public void Idle_OpenedCloseAndOpenedAgainAfterIdle_FollowsEachTime()
    {
        (ContainerRuntime container, ComboBox comboBox) = CreateInContainer();
        Tick();
        Tick();
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        comboBox.X += 20;
        Tick();
        ShouldKeepOffset(comboBox, offset);
        comboBox.IsDropDownOpen = false;
        Tick();
        Tick();
        Tick();
        container.IsIdle.ShouldBeTrue();

        comboBox.IsDropDownOpen = true;
        comboBox.X += 20;
        Tick();

        ShouldKeepOffset(comboBox, offset);
        container.IsIdle.ShouldBeFalse();
    }

    [Fact]
    public void Idle_AnimationMovesAnAncestorOfTheOpenComboBox_PopupFollowsInTheSameFrame()
    {
        (ContainerRuntime container, ComboBox comboBox) = CreateInContainer();
        Tick();
        Tick();
        comboBox.IsDropDownOpen = true;
        (float X, float Y) offset = OffsetFromComboBox(comboBox);
        StateSaveCategory category = new() { Name = "Move" };
        StateSave from = new() { Name = "From" };
        from.Variables.Add(new VariableSave { Name = "X", Value = 0f });
        category.States.Add(from);
        StateSave to = new() { Name = "To" };
        to.Variables.Add(new VariableSave { Name = "X", Value = 100f });
        category.States.Add(to);
        container.Categories["Move"] = category;
        AnimationRuntime animation = new() { Name = "Move" };
        animation.Keyframes.Add(new KeyframeRuntime { Time = 0, StateName = "Move/From", InterpolationType = InterpolationType.Linear });
        animation.Keyframes.Add(new KeyframeRuntime { Time = 1, StateName = "Move/To" });

        container.PlayAnimation(animation);
        Tick(0.5);

        container.X.ShouldBe(50, 0.01f);
        ShouldKeepOffset(comboBox, offset);
    }

    #endregion
}
