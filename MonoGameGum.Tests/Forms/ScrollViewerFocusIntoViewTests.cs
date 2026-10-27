using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Issue #6939: focus moving to a descendant of a ScrollViewer by gamepad, keyboard, tab or code scrolls the
/// viewer by the least amount that shows it; mouse-initiated focus does not.
/// </summary>
public class ScrollViewerFocusIntoViewTests : BaseTestClass
{
    private Mock<ICursor> _cursor = new();

    public ScrollViewerFocusIntoViewTests()
    {
        SetCursor(push: false);
    }

    private void SetCursor(bool push)
    {
        _cursor = new Mock<ICursor>();
        _cursor.SetupProperty(x => x.VisualOver);
        _cursor.SetupProperty(x => x.WindowPushed);
        _cursor.Setup(x => x.LastInputDevice).Returns(InputDevice.Mouse);
        _cursor.Setup(x => x.PrimaryPush).Returns(push);
        _cursor.Setup(x => x.PrimaryDown).Returns(push);
        FormsUtilities.SetCursor(_cursor.Object);
    }

    private static (StackPanel container, ScrollViewer viewer, List<Button> buttons) Build(
        GamepadNavigationMode mode, bool skipOnTab = false, int count = 10)
    {
        StackPanel container = new();
        container.AddToRoot();
        container.GamepadNavigationMode = mode;

        ScrollViewer viewer = new();
        container.AddChild(viewer);
        viewer.Visual.Width = 200;
        viewer.Visual.Height = 110;
        // Spatial navigation skips touching rects (distance 0), so keep a gap between rows.
        viewer.InnerPanel.StackSpacing = 4;
        if (skipOnTab)
        {
            viewer.GamepadTabbingFocusBehavior = TabbingFocusBehavior.SkipOnTab;
        }

        List<Button> buttons = new();
        for (int i = 0; i < count; i++)
        {
            Button button = new();
            button.Text = "Button " + i;
            viewer.AddChild(button);
            buttons.Add(button);
        }
        container.Visual.UpdateLayout();
        if (count >= 10)
        {
            viewer.VerticalScrollBarMaximum.ShouldBeGreaterThan(0, "the test needs scrollable content");
        }
        return (container, viewer, buttons);
    }

    private static void Press(GamepadButton button)
    {
        GamePad gamepad = new();
        gamepad.SetButtonState(button, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Add(gamepad);
        InteractiveGue.CurrentInputReceiver!.OnFocusUpdate();
        FrameworkElement.GamePadsForUiControl.Clear();
    }

    private static bool IsInView(ScrollViewer viewer, GraphicalUiElement visual)
    {
        GraphicalUiElement clip = viewer.ClipContainer;
        return visual.AbsoluteTop >= clip.AbsoluteTop - 0.5f &&
            visual.AbsoluteBottom <= clip.AbsoluteBottom + 0.5f;
    }

    #region Pinned behavior

    [Fact]
    public void ListBox_DownThroughItems_ScrollsToKeepSelectionInView()
    {
        StackPanel container = new();
        container.AddToRoot();
        container.GamepadNavigationMode = GamepadNavigationMode.Spatial;
        ListBox listBox = new();
        container.AddChild(listBox);
        listBox.Visual.Width = 200;
        listBox.Visual.Height = 110;
        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.Add("Item " + i);
        }
        container.Visual.UpdateLayout();

        listBox.IsFocused = true;
        Press(GamepadButton.A);
        for (int i = 0; i < 6; i++)
        {
            Press(GamepadButton.DPadDown);
        }

        listBox.SelectedIndex.ShouldBe(5);
        listBox.VerticalScrollBarValue.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ScrollViewer_ScrollBarValueSet_Scrolls()
    {
        (_, ScrollViewer viewer, _) = Build(GamepadNavigationMode.TabOrder);

        viewer.VerticalScrollBarValue = 30;

        viewer.VerticalScrollBarValue.ShouldBe(30);
    }

    #endregion

    #region Scenarios from the issue

    [Fact]
    public void SpatialSkipOnTab_DownThroughButtons_KeepsFocusedButtonInView()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.Spatial, skipOnTab: true);

        buttons[0].IsFocused = true;
        for (int i = 0; i < 6; i++)
        {
            Press(GamepadButton.DPadDown);
            IsInView(viewer, buttons[i + 1].Visual).ShouldBeTrue("Button " + (i + 1) + " focused=" + buttons.FindIndex(b => b.IsFocused) + " scroll=" + viewer.VerticalScrollBarValue + " top=" + buttons[i + 1].Visual.AbsoluteTop + " clip=" + viewer.ClipContainer.AbsoluteTop + "-" + viewer.ClipContainer.AbsoluteBottom);
        }

        buttons[6].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void SpatialViewerFocusable_EnterThenDown_MovesFocusAndScrolls()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.Spatial);

        viewer.IsFocused = true;
        Press(GamepadButton.A);
        buttons[0].IsFocused.ShouldBeTrue();
        for (int i = 0; i < 6; i++)
        {
            Press(GamepadButton.DPadDown);
        }

        buttons[6].IsFocused.ShouldBeTrue();
        IsInView(viewer, buttons[6].Visual).ShouldBeTrue();
    }

    [Fact]
    public void TabOrder_DownThroughButtons_KeepsFocusedButtonInView()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);

        viewer.IsFocused = true;
        Press(GamepadButton.A);
        for (int i = 0; i < 6; i++)
        {
            Press(GamepadButton.DPadDown);
        }

        buttons[6].IsFocused.ShouldBeTrue();
        IsInView(viewer, buttons[6].Visual).ShouldBeTrue();
    }

    #endregion

    #region Scroll amount

    [Fact]
    public void IsFocusedSetFromCode_ChildBelowView_ScrollsByLeastAmount()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);
        GraphicalUiElement clip = viewer.ClipContainer;
        float overflow = buttons[5].Visual.AbsoluteBottom - clip.AbsoluteBottom;
        overflow.ShouldBeGreaterThan(0);

        buttons[5].IsFocused = true;

        viewer.VerticalScrollBarValue.ShouldBe(overflow, 0.5);
        buttons[5].Visual.AbsoluteBottom.ShouldBe(clip.AbsoluteBottom, 0.5f);
    }

    [Fact]
    public void FocusMovingUp_ChildAboveView_ScrollsBackByLeastAmount()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);
        buttons[9].IsFocused = true;
        viewer.VerticalScrollBarValue.ShouldBeGreaterThan(0);

        buttons[2].IsFocused = true;

        buttons[2].Visual.AbsoluteTop.ShouldBe(viewer.ClipContainer.AbsoluteTop, 0.5f);
    }

    [Fact]
    public void IsFocusedSetFromCode_ChildFullyVisible_DoesNotScroll()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);

        buttons[1].IsFocused = true;

        viewer.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void IsFocusedSetFromCode_ChildTallerThanViewport_AlignsLeadingEdgeWithoutOscillating()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);
        buttons[5].Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        buttons[5].Visual.Height = 300;
        viewer.Visual.UpdateLayout();

        buttons[5].IsFocused = true;
        double first = viewer.VerticalScrollBarValue;
        buttons[5].IsFocused = false;
        buttons[5].IsFocused = true;

        buttons[5].Visual.AbsoluteTop.ShouldBe(viewer.ClipContainer.AbsoluteTop, 0.5f);
        viewer.VerticalScrollBarValue.ShouldBe(first, 0.01);
    }

    [Fact]
    public void IsFocusedSetFromCode_ChildAddedSameFrame_ScrollsAgainstCurrentLayout()
    {
        (_, ScrollViewer viewer, _) = Build(GamepadNavigationMode.TabOrder, count: 3);
        for (int i = 0; i < 8; i++)
        {
            viewer.AddChild(new Button());
        }
        Button last = new();
        viewer.AddChild(last);

        last.IsFocused = true;

        IsInView(viewer, last.Visual).ShouldBeTrue();
    }

    #endregion

    #region Nested, horizontal

    [Fact]
    public void NestedViewers_FocusInInnerHiddenInBoth_ScrollsInnerThenOuter()
    {
        StackPanel container = new();
        container.AddToRoot();
        ScrollViewer outer = new();
        container.AddChild(outer);
        outer.Visual.Width = 200;
        outer.Visual.Height = 110;
        for (int i = 0; i < 6; i++)
        {
            outer.AddChild(new Button());
        }
        ScrollViewer inner = new();
        outer.AddChild(inner);
        inner.Visual.Width = 150;
        inner.Visual.Height = 80;
        List<Button> innerButtons = new();
        for (int i = 0; i < 8; i++)
        {
            Button b = new();
            inner.AddChild(b);
            innerButtons.Add(b);
        }
        container.Visual.UpdateLayout();

        innerButtons[6].IsFocused = true;

        IsInView(inner, innerButtons[6].Visual).ShouldBeTrue("inner");
        IsInView(outer, innerButtons[6].Visual).ShouldBeTrue("outer");
        outer.VerticalScrollBarValue.ShouldBeGreaterThan(0);
        inner.VerticalScrollBarValue.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Horizontal_ChildRightOfView_ScrollsHorizontallyByLeastAmount()
    {
        ScrollViewer viewer = new();
        viewer.AddToRoot();
        viewer.Visual.Width = 200;
        viewer.Visual.Height = 110;
        viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
        viewer.HorizontalSizeMode = SizeMode.Fixed;
        viewer.InnerPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        viewer.InnerPanel.Width = 0;
        ContainerRuntime wideContent = new();
        wideContent.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        wideContent.Width = 800;
        wideContent.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        wideContent.Height = 40;
        viewer.AddChild(wideContent);
        Button far = new();
        wideContent.AddChild(far);
        far.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        far.X = 600;
        viewer.Visual.UpdateLayout();
        viewer.HorizontalScrollBarMaximum.ShouldBeGreaterThan(0, "the test needs horizontal range");

        far.IsFocused = true;

        far.Visual.AbsoluteRight.ShouldBe(viewer.ClipContainer.AbsoluteRight, 0.5f);
    }

    #endregion

    #region No scroll cases

    [Fact]
    public void MouseInitiatedFocus_PartiallyVisibleButton_DoesNotScroll()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);
        SetCursor(push: true);
        _cursor.Object.VisualOver = buttons[3].Visual;

        buttons[3].IsFocused = true;

        viewer.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void MouseClickOnLabelInsideFocusedControl_DoesNotScroll()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);
        SetCursor(push: true);
        _cursor.Object.VisualOver = buttons[3].Visual.Children[0] as InteractiveGue ?? buttons[3].Visual;

        buttons[3].IsFocused = true;

        viewer.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void MouseClickHandlerFocusingOtherOffscreenButton_Scrolls()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);
        SetCursor(push: true);
        _cursor.Object.VisualOver = buttons[0].Visual;

        buttons[7].IsFocused = true;

        IsInView(viewer, buttons[7].Visual).ShouldBeTrue();
    }

    [Fact]
    public void TabWhileMouseHeld_Scrolls()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);
        SetCursor(push: true);
        _cursor.Object.VisualOver = buttons[0].Visual;
        buttons[0].IsFocused = true;

        for (int i = 0; i < 6; i++)
        {
            buttons[i].HandleTab(TabDirection.Down);
        }

        buttons[6].IsFocused.ShouldBeTrue();
        IsInView(viewer, buttons[6].Visual).ShouldBeTrue();
    }

    [Fact]
    public void FocusLost_DoesNotScroll()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);
        buttons[1].IsFocused = true;

        buttons[1].IsFocused = false;

        viewer.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void HiddenViewer_FocusChild_DoesNotScrollOrThrow()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder);
        viewer.Visual.Visible = false;

        Should.NotThrow(() => buttons[8].IsFocused = true);

        viewer.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void ViewerWithoutScrollRange_FocusChild_DoesNothing()
    {
        (_, ScrollViewer viewer, List<Button> buttons) = Build(GamepadNavigationMode.TabOrder, count: 2);

        Should.NotThrow(() => buttons[1].IsFocused = true);

        viewer.VerticalScrollBarValue.ShouldBe(0);
    }

    [Fact]
    public void FocusingTheViewerItself_DoesNotScroll()
    {
        (_, ScrollViewer viewer, _) = Build(GamepadNavigationMode.TabOrder);
        viewer.VerticalScrollBarValue = 40;

        viewer.IsFocused = true;

        viewer.VerticalScrollBarValue.ShouldBe(40);
    }

    #endregion
}
