using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Tab / Shift+Tab across the containers of a screen (issue #7418). A screen loaded from a file is a
/// plain <see cref="GraphicalUiElement"/> (not an InteractiveGue) whose instances have it as their
/// Parent, so the tab climb must work on GraphicalUiElement parents. Code-generated screens derive from
/// InteractiveGue; those tests are the control group.
/// </summary>
public class TabNavigationAcrossContainersTests : BaseTestClass
{
    private class Layout
    {
        public GraphicalUiElement Screen = null!;
        public List<StackPanel> Containers = new();
        public List<Button> Buttons = new();
        public List<TextBox> TextBoxes = new();
    }

    /// <summary>
    /// Builds a screen with one container per entry, holding that many buttons (0 = an empty container).
    /// </summary>
    private static Layout Build(GraphicalUiElement screen, params int[] buttonsPerContainer)
    {
        Layout layout = new() { Screen = screen };
        foreach (int count in buttonsPerContainer)
        {
            StackPanel container = new();
            screen.Children.Add(container.Visual);
            layout.Containers.Add(container);
            for (int i = 0; i < count; i++)
            {
                Button button = new();
                container.AddChild(button);
                layout.Buttons.Add(button);
            }
        }
        return layout;
    }

    private static GraphicalUiElement NewPlain() => new GraphicalUiElement(new RenderingLibrary.Graphics.InvisibleRenderable());

    private static Layout BuildPlain(params int[] buttonsPerContainer) =>
        Build(NewPlain(), buttonsPerContainer);

    private static Layout BuildInteractive(params int[] buttonsPerContainer) =>
        Build(new ContainerRuntime(), buttonsPerContainer);

    private static Button? Focused(IEnumerable<Button> buttons) => buttons.FirstOrDefault(b => b.IsFocused);

    private static void AssertTabVisits(List<Button> expectedInOrder, TabDirection direction, int steps)
    {
        List<Button> order = direction == TabDirection.Down ? expectedInOrder : Enumerable.Reverse(expectedInOrder).ToList();
        order[0].IsFocused = true;
        for (int i = 1; i <= steps; i++)
        {
            Button current = Focused(expectedInOrder)!;
            current.HandleTab(direction, loop: true);
            Focused(expectedInOrder).ShouldBeSameAs(order[i % order.Count], $"step {i}");
        }
    }

    #region Reported bug (#7418)

    [Fact]
    public void HandleTab_ShouldVisitEveryControlAcrossContainers_WhenScreenIsPlainGraphicalUiElement()
    {
        Layout layout = BuildPlain(2, 2, 2);
        AssertTabVisits(layout.Buttons, TabDirection.Down, layout.Buttons.Count * 2);
    }

    [Fact]
    public void HandleTab_ShouldVisitEveryControlAcrossContainers_WhenShiftTabbingOnPlainScreen()
    {
        Layout layout = BuildPlain(2, 2, 2);
        AssertTabVisits(layout.Buttons, TabDirection.Up, layout.Buttons.Count * 2);
    }

    #endregion

    #region Root kinds (control group and variants)

    [Fact]
    public void HandleTab_ShouldVisitEveryControl_WhenScreenIsInteractiveGue()
    {
        Layout layout = BuildInteractive(2, 2, 2);
        AssertTabVisits(layout.Buttons, TabDirection.Down, layout.Buttons.Count * 2);
    }

    [Fact]
    public void HandleTab_ShouldVisitEveryControlBackwards_WhenScreenIsInteractiveGue()
    {
        Layout layout = BuildInteractive(2, 2, 2);
        AssertTabVisits(layout.Buttons, TabDirection.Up, layout.Buttons.Count * 2);
    }

    [Fact]
    public void HandleTab_ShouldVisitEveryControl_WhenPlainScreenIsChildOfRoot()
    {
        Layout layout = BuildPlain(2, 2);
        GumService.Default.Root.Children.Add(layout.Screen);
        AssertTabVisits(layout.Buttons, TabDirection.Down, layout.Buttons.Count * 2);
    }

    [Fact]
    public void HandleTab_ShouldVisitEveryControl_WhenRootIsPanel()
    {
        Panel panel = new();
        Layout layout = Build(panel.Visual, 2, 2);
        AssertTabVisits(layout.Buttons, TabDirection.Down, layout.Buttons.Count * 2);
    }

    [Fact]
    public void HandleTab_ShouldVisitEveryControl_WhenPlainScreenHasPlainIntermediateGroup()
    {
        GraphicalUiElement screen = NewPlain();
        GraphicalUiElement group = NewPlain();
        screen.Children.Add(group);
        Layout layout = Build(group, 2, 2);
        AssertTabVisits(layout.Buttons, TabDirection.Down, layout.Buttons.Count * 2);
    }

    [Fact]
    public void HandleTab_ShouldMoveToSecondContainer_WhenPlainScreenAndNoLoop()
    {
        Layout layout = BuildPlain(1, 1);
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(TabDirection.Down);

        layout.Buttons[1].IsFocused.ShouldBeTrue();
        layout.Buttons[0].IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleTab_ShouldMoveToPreviousContainer_WhenPlainScreenAndNoLoop()
    {
        Layout layout = BuildPlain(1, 1);
        layout.Buttons[1].IsFocused = true;

        layout.Buttons[1].HandleTab(TabDirection.Up);

        layout.Buttons[0].IsFocused.ShouldBeTrue();
    }

    #endregion

    #region Wrap and boundaries

    [Fact]
    public void HandleTab_ShouldStayFocused_WhenLastControlOfPlainScreenAndNoLoop()
    {
        Layout layout = BuildPlain(1, 1);
        layout.Buttons[1].IsFocused = true;

        layout.Buttons[1].HandleTab(TabDirection.Down);

        layout.Buttons[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldStayFocused_WhenFirstControlOfPlainScreenAndNoLoopUp()
    {
        Layout layout = BuildPlain(1, 1);
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(TabDirection.Up);

        layout.Buttons[0].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldKeepFocus_WhenOnlyOneFocusableControlOnPlainScreen()
    {
        Layout layout = BuildPlain(1);
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);
        layout.Buttons[0].IsFocused.ShouldBeTrue();

        layout.Buttons[0].HandleTab(TabDirection.Up, loop: true);
        layout.Buttons[0].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldSkipEmptyContainerBetweenContainers_OnPlainScreen()
    {
        Layout layout = BuildPlain(1, 0, 1);
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);
        layout.Buttons[1].IsFocused.ShouldBeTrue();

        layout.Buttons[1].HandleTab(TabDirection.Up, loop: true);
        layout.Buttons[0].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldTabFromMiddleControlInBothDirections_OnPlainScreen()
    {
        Layout layout = BuildPlain(2, 2, 2);

        layout.Buttons[2].IsFocused = true;
        layout.Buttons[2].HandleTab(loop: true);
        layout.Buttons[3].IsFocused.ShouldBeTrue();

        layout.Buttons[3].HandleTab(TabDirection.Up, loop: true);
        layout.Buttons[2].IsFocused.ShouldBeTrue();
        layout.Buttons[2].HandleTab(TabDirection.Up, loop: true);
        layout.Buttons[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldWrapFromLastToFirst_AndFirstToLast_OnPlainScreen()
    {
        Layout layout = BuildPlain(2, 3);
        Button first = layout.Buttons[0];
        Button last = layout.Buttons[^1];

        last.IsFocused = true;
        last.HandleTab(loop: true);
        first.IsFocused.ShouldBeTrue();

        first.HandleTab(TabDirection.Up, loop: true);
        last.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldLeaveExactlyOneControlFocused_AfterEveryStep_OnPlainScreen()
    {
        Layout layout = BuildPlain(2, 1, 2);
        layout.Buttons[0].IsFocused = true;
        for (int i = 0; i < layout.Buttons.Count * 2; i++)
        {
            Focused(layout.Buttons)!.HandleTab(loop: true);
            layout.Buttons.Count(b => b.IsFocused).ShouldBe(1);
        }
    }

    [Fact]
    public void HandleTab_ShouldFocusFirstChildOfContainer_WhenStaticCalledWithNullRequester()
    {
        Layout layout = BuildPlain(2);
        InteractiveGue container = (InteractiveGue)layout.Containers[0].Visual;

        FrameworkElement.HandleTab(TabDirection.Down, null, container, shouldAskParent: true, loop: false);
        layout.Buttons[0].IsFocused.ShouldBeTrue();

        layout.Buttons[0].IsFocused = false;
        FrameworkElement.HandleTab(TabDirection.Up, null, container, shouldAskParent: true, loop: false);
        layout.Buttons[1].IsFocused.ShouldBeTrue();
    }

    #endregion

    #region Nesting

    [Fact]
    public void HandleTab_ShouldReachControlsThreeLevelsDeep_OnPlainScreen()
    {
        GraphicalUiElement screen = NewPlain();
        List<Button> buttons = new();

        // level 1 -> level 2 -> level 3 container, each also holding a button
        StackPanel level1 = new();
        screen.Children.Add(level1.Visual);
        Button b1 = new(); level1.AddChild(b1); buttons.Add(b1);
        StackPanel level2 = new();
        level1.AddChild(level2);
        Button b2 = new(); level2.AddChild(b2); buttons.Add(b2);
        StackPanel level3 = new();
        level2.AddChild(level3);
        Button b3 = new(); level3.AddChild(b3); buttons.Add(b3);
        StackPanel sibling = new();
        screen.Children.Add(sibling.Visual);
        Button b4 = new(); sibling.AddChild(b4); buttons.Add(b4);

        AssertTabVisits(buttons, TabDirection.Down, buttons.Count * 2);
        AssertTabVisits(buttons, TabDirection.Up, buttons.Count * 2);
    }

    [Fact]
    public void HandleTab_ShouldEnterNestedContainerInNextTopLevelContainer_OnPlainScreen()
    {
        GraphicalUiElement screen = NewPlain();
        StackPanel first = new();
        screen.Children.Add(first.Visual);
        Button firstButton = new(); first.AddChild(firstButton);
        StackPanel second = new();
        screen.Children.Add(second.Visual);
        StackPanel inner = new();
        second.AddChild(inner);
        Button deep = new(); inner.AddChild(deep);

        firstButton.IsFocused = true;
        firstButton.HandleTab(loop: true);

        deep.IsFocused.ShouldBeTrue();
    }

    #endregion

    #region Siblings that are not tab stops

    [Fact]
    public void HandleTab_ShouldSkipNonFocusableSiblings_OnPlainScreen()
    {
        GraphicalUiElement screen = NewPlain();
        StackPanel first = new();
        screen.Children.Add(first.Visual);
        Button a = new(); first.AddChild(a);
        screen.Children.Add(new ColoredRectangleRuntime());
        screen.Children.Add(new TextRuntime());
        screen.Children.Add(new ContainerRuntime());
        StackPanel second = new();
        screen.Children.Add(second.Visual);
        Button b = new(); second.AddChild(b);

        a.IsFocused = true;
        a.HandleTab(loop: true);
        b.IsFocused.ShouldBeTrue();

        b.HandleTab(TabDirection.Up, loop: true);
        a.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldSkipHiddenContainer_OnPlainScreen()
    {
        Layout layout = BuildPlain(1, 1, 1);
        layout.Containers[1].Visual.Visible = false;
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);
        layout.Buttons[2].IsFocused.ShouldBeTrue();

        layout.Buttons[2].HandleTab(TabDirection.Up, loop: true);
        layout.Buttons[0].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldSkipHiddenControl_OnPlainScreen()
    {
        Layout layout = BuildPlain(2, 2);
        layout.Buttons[2].Visual.Visible = false;
        layout.Buttons[1].IsFocused = true;

        layout.Buttons[1].HandleTab(loop: true);

        layout.Buttons[3].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldSkipDisabledControl_OnPlainScreen()
    {
        Layout layout = BuildPlain(2, 2);
        layout.Buttons[2].IsEnabled = false;
        layout.Buttons[1].IsFocused = true;

        layout.Buttons[1].HandleTab(loop: true);

        layout.Buttons[3].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldSkipDisabledContainer_OnPlainScreen()
    {
        Layout layout = BuildPlain(1, 1, 1);
        layout.Containers[1].IsEnabled = false;
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);

        layout.Buttons[2].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldFocusDisabledControlThatAllowsIt_OnPlainScreen()
    {
        Layout layout = BuildPlain(1, 1, 1);
        layout.Buttons[1].IsEnabled = false;
        layout.Buttons[1].AllowFocusWhenDisabled = true;
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);

        layout.Buttons[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldNotFocusAllowFocusWhenDisabledControl_WhenContainerIsDisabled_OnPlainScreen()
    {
        Layout layout = BuildPlain(1, 1, 1);
        layout.Containers[1].IsEnabled = false;
        layout.Buttons[1].AllowFocusWhenDisabled = true;
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);

        layout.Buttons[1].IsFocused.ShouldBeFalse();
        layout.Buttons[2].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldNotFocusAllowFocusWhenDisabledControl_WhenDisabledAncestorIsAbovePlainGroup()
    {
        StackPanel disabledContainer = new();
        disabledContainer.IsEnabled = false;
        GraphicalUiElement plainGroup = NewPlain();
        disabledContainer.Visual.Children.Add(plainGroup);
        Button a = new();
        plainGroup.Children.Add(a.Visual);
        Button b = new();
        plainGroup.Children.Add(b.Visual);
        b.IsEnabled = false;
        b.AllowFocusWhenDisabled = true;
        a.IsFocused = true;

        a.HandleTab();

        b.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleTab_ShouldSkipControlWithoutEvents_OnPlainScreen()
    {
        Layout layout = BuildPlain(1, 1, 1);
        layout.Buttons[1].Visual.HasEvents = false;
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);

        layout.Buttons[2].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldSkipControlWithSkipOnTab_OnPlainScreen()
    {
        Layout layout = BuildPlain(1, 1, 1);
        layout.Buttons[1].GamepadTabbingFocusBehavior = TabbingFocusBehavior.SkipOnTab;
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);

        layout.Buttons[2].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldReachContainerAgain_WhenVisibilityToggledBackOn_OnPlainScreen()
    {
        Layout layout = BuildPlain(1, 1, 1);
        layout.Containers[1].Visual.Visible = false;
        layout.Containers[1].Visual.Visible = true;
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);

        layout.Buttons[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldNotMove_WhenEveryOtherControlIsHidden_OnPlainScreen()
    {
        Layout layout = BuildPlain(1, 1, 1);
        layout.Containers[1].Visual.Visible = false;
        layout.Containers[2].Visual.Visible = false;
        layout.Buttons[0].IsFocused = true;

        layout.Buttons[0].HandleTab(loop: true);

        layout.Buttons[0].IsFocused.ShouldBeTrue();
    }

    #endregion

    #region Compound controls

    [Fact]
    public void HandleTab_ShouldTreatListBoxAsSingleStop_OnPlainScreen()
    {
        GraphicalUiElement screen = NewPlain();
        StackPanel first = new();
        screen.Children.Add(first.Visual);
        ListBox listBox = new();
        first.AddChild(listBox);
        for (int i = 0; i < 3; i++)
        {
            listBox.Items.Add("item" + i);
        }
        StackPanel second = new();
        screen.Children.Add(second.Visual);
        Button button = new(); second.AddChild(button);

        listBox.IsFocused = true;
        listBox.HandleTab(loop: true);

        button.IsFocused.ShouldBeTrue();
        listBox.IsFocused.ShouldBeFalse();

        button.HandleTab(loop: true);
        listBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldTreatComboBoxAsSingleStop_OnPlainScreen()
    {
        GraphicalUiElement screen = NewPlain();
        StackPanel first = new();
        screen.Children.Add(first.Visual);
        ComboBox combo = new();
        first.AddChild(combo);
        combo.Items.Add("a");
        combo.Items.Add("b");
        StackPanel second = new();
        screen.Children.Add(second.Visual);
        Button button = new(); second.AddChild(button);

        combo.IsFocused = true;
        combo.HandleTab(loop: true);

        button.IsFocused.ShouldBeTrue();
        button.HandleTab(loop: true);
        combo.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ShouldReachButtonsInsideScrollViewer_OnPlainScreen()
    {
        GraphicalUiElement screen = NewPlain();
        StackPanel first = new();
        screen.Children.Add(first.Visual);
        Button before = new(); first.AddChild(before);
        ScrollViewer viewer = new();
        // A ScrollViewer is itself an input receiver and so a tab stop; skip it to tab through its content.
        viewer.GamepadTabbingFocusBehavior = TabbingFocusBehavior.SkipOnTab;
        screen.Children.Add(viewer.Visual);
        Button inside1 = new(); viewer.AddChild(inside1);
        Button inside2 = new(); viewer.AddChild(inside2);
        StackPanel last = new();
        screen.Children.Add(last.Visual);
        Button after = new(); last.AddChild(after);

        List<Button> buttons = new() { before, inside1, inside2, after };
        AssertTabVisits(buttons, TabDirection.Down, buttons.Count * 2);
    }

    #endregion

    #region Modal trap

    [Fact]
    public void HandleTab_ShouldTrapFocusInTopMostModal_WhenModalIsPlainGraphicalUiElement()
    {
        StackPanel background = new();
        background.AddToRoot();
        Button behind = new();
        background.AddChild(behind);

        Layout modal = BuildPlain(1, 1);
        FrameworkElement.ModalRoot.Children.Add(modal.Screen);

        modal.Buttons[1].IsFocused = true;
        modal.Buttons[1].HandleTab(loop: true);
        modal.Buttons[0].IsFocused.ShouldBeTrue();
        behind.IsFocused.ShouldBeFalse();

        modal.Buttons[0].HandleTab(TabDirection.Up, loop: true);
        modal.Buttons[1].IsFocused.ShouldBeTrue();
        behind.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleTab_ShouldMoveBetweenModalContainers_WhenModalIsPlainGraphicalUiElement()
    {
        Layout modal = BuildPlain(1, 1, 1);
        FrameworkElement.ModalRoot.Children.Add(modal.Screen);

        AssertTabVisits(modal.Buttons, TabDirection.Down, modal.Buttons.Count * 2);
    }

    #endregion

    #region Keyboard Tab key

    private static void PressTab(bool shift)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.LeftShift)).Returns(shift);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
    }

    private static Layout BuildTextBoxes(params int[] perContainer)
    {
        GraphicalUiElement screen = NewPlain();
        Layout layout = new() { Screen = screen };
        foreach (int count in perContainer)
        {
            StackPanel container = new();
            screen.Children.Add(container.Visual);
            for (int i = 0; i < count; i++)
            {
                layout.TextBoxes.Add(AddTextBox(container));
            }
        }
        return layout;
    }

    private static TextBox AddTextBox(StackPanel container)
    {
        TextBox textBox = new();
        container.AddChild(textBox);
        return textBox;
    }

    [Fact]
    public void OnFocusUpdate_ShouldMoveFocusToNextContainer_WhenTabPressedInTextBox_OnPlainScreen()
    {
        Layout layout = BuildTextBoxes(1, 1);
        layout.TextBoxes[0].IsFocused = true;
        PressTab(shift: false);

        layout.TextBoxes[0].OnFocusUpdate();

        layout.TextBoxes[1].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OnFocusUpdate_ShouldMoveFocusToPreviousContainer_WhenShiftTabPressedInTextBox_OnPlainScreen()
    {
        Layout layout = BuildTextBoxes(1, 1);
        layout.TextBoxes[1].IsFocused = true;
        PressTab(shift: true);

        layout.TextBoxes[1].OnFocusUpdate();

        layout.TextBoxes[0].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OnFocusUpdate_ShouldWrapToFirstContainer_WhenTabPressedInLastTextBox_OnPlainScreen()
    {
        Layout layout = BuildTextBoxes(1, 1);
        layout.TextBoxes[1].IsFocused = true;
        PressTab(shift: false);

        layout.TextBoxes[1].OnFocusUpdate();

        layout.TextBoxes[0].IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void OnFocusUpdate_ShouldNotMoveFocus_WhenTextBoxAcceptsTab_OnPlainScreen()
    {
        Layout layout = BuildTextBoxes(1, 1);
        layout.TextBoxes[0].AcceptsTab = true;
        layout.TextBoxes[0].IsFocused = true;
        PressTab(shift: false);

        layout.TextBoxes[0].OnFocusUpdate();

        layout.TextBoxes[0].IsFocused.ShouldBeTrue();
        layout.TextBoxes[1].IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void OnFocusUpdate_ShouldMoveFocusAcrossContainers_WhenMultiLineTextBoxDoesNotAcceptTab_OnPlainScreen()
    {
        Layout layout = BuildTextBoxes(1, 1);
        layout.TextBoxes[0].TextWrapping = Gum.Forms.TextWrapping.Wrap;
        layout.TextBoxes[0].AcceptsReturn = true;
        layout.TextBoxes[0].IsFocused = true;
        PressTab(shift: false);

        layout.TextBoxes[0].OnFocusUpdate();

        layout.TextBoxes[1].IsFocused.ShouldBeTrue();
    }

    #endregion
}
