using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Issue #6956: a control that is not visible, itself or through any ancestor, refuses focus like a
/// disabled control does. Complements HideClearsFocusTests (#6934), which clears focus on hide.
/// </summary>
public class RefuseFocusWhenHiddenTests : BaseTestClass
{
    private static (Panel panel, Button left, Button middle, Button right) CreateRow()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Button left = new Button();
        panel.AddChild(left);
        left.X = 0;
        Button middle = new Button();
        panel.AddChild(middle);
        middle.X = 200;
        Button right = new Button();
        panel.AddChild(right);
        right.X = 400;
        return (panel, left, middle, right);
    }

    private static GamePad PressGamepad(GamepadButton button)
    {
        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(button, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);
        return gamepad;
    }

    private static Mock<IInputReceiverKeyboardMonoGame> CreateKeyboardMock(Keys key)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys>());
        return keyboard;
    }

    private static void PushOn(InteractiveGue visualOver)
    {
        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.PrimaryPush).Returns(true);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.WindowPushed).Returns(visualOver);
        cursor.Setup(c => c.VisualOver).Returns(visualOver);
        cursor.Setup(c => c.X).Returns(5);
        cursor.Setup(c => c.Y).Returns(5);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(5);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(5);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);

        Gum.GumService.Default.Update(new GameTime());
    }

    #region Pinned behavior

    [Fact]
    public void IsFocused_VisibleControl_TakesFocus()
    {
        Button button = new Button();
        button.AddToRoot();

        button.IsFocused = true;

        button.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(button);
    }

    [Fact]
    public void IsFocused_DisabledControl_StaysFalse()
    {
        Button button = new Button();
        button.AddToRoot();
        button.IsEnabled = false;

        button.IsFocused = true;

        button.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void IsFocused_ShownThenFocused_TakesFocus()
    {
        StackPanel panel = new StackPanel();
        panel.AddToRoot();
        panel.IsVisible = false;
        Button button = new Button();
        panel.AddChild(button);

        panel.IsVisible = true;
        button.IsFocused = true;

        button.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(button);
    }

    #endregion

    #region Refusing focus

    [Fact]
    public void IsFocused_HiddenSelf_StaysFalse()
    {
        Button button = new Button();
        button.AddToRoot();
        button.IsVisible = false;

        button.IsFocused = true;

        button.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void IsFocused_HiddenVisual_StaysFalse()
    {
        Button button = new Button();
        button.AddToRoot();
        button.Visual.Visible = false;

        button.IsFocused = true;

        button.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void IsFocused_HiddenSelf_RaisesNoFocusEvents()
    {
        Button button = new Button();
        button.AddToRoot();
        button.IsVisible = false;
        int gotFocus = 0;
        int lostFocus = 0;
        button.GotFocus += (_, _) => gotFocus++;
        button.LostFocus += (_, _) => lostFocus++;

        button.IsFocused = true;

        gotFocus.ShouldBe(0);
        lostFocus.ShouldBe(0);
    }

    [Fact]
    public void IsFocused_HiddenSelf_LeavesOtherFocusUnchanged()
    {
        Button other = new Button();
        other.AddToRoot();
        other.IsFocused = true;
        Button hidden = new Button();
        hidden.AddToRoot();
        hidden.IsVisible = false;

        hidden.IsFocused = true;

        hidden.IsFocused.ShouldBeFalse();
        other.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(other);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void IsFocused_HiddenAncestorAtDepth_StaysFalse(int depth)
    {
        StackPanel top = new StackPanel();
        top.AddToRoot();
        StackPanel[] chain = new StackPanel[depth];
        StackPanel parent = top;
        chain[0] = top;
        for (int i = 1; i < depth; i++)
        {
            chain[i] = new StackPanel();
            parent.AddChild(chain[i]);
            parent = chain[i];
        }
        Button button = new Button();
        parent.AddChild(button);
        chain[depth / 2].IsVisible = false;

        button.IsFocused = true;

        button.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void IsFocused_ReparentedIntoHiddenParent_StaysFalse()
    {
        Button button = new Button();
        button.AddToRoot();
        StackPanel hidden = new StackPanel();
        hidden.AddToRoot();
        hidden.IsVisible = false;
        hidden.AddChild(button);

        button.IsFocused = true;

        button.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void IsFocused_FocusThenShow_IsRefused()
    {
        StackPanel panel = new StackPanel();
        panel.AddToRoot();
        panel.IsVisible = false;
        Button button = new Button();
        panel.AddChild(button);

        button.IsFocused = true;
        panel.IsVisible = true;

        button.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void IsFocused_HiddenTextBox_StaysFalse()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        textBox.IsVisible = false;

        textBox.IsFocused = true;

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void IsFocused_HiddenListBox_StaysFalse()
    {
        ListBox listBox = new ListBox();
        listBox.AddToRoot();
        listBox.Items.Add("a");
        listBox.IsVisible = false;

        listBox.IsFocused = true;

        listBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void IsFocused_HiddenWithAllowFocusWhenDisabled_StaysFalse()
    {
        Button button = new Button();
        button.AddToRoot();
        button.IsEnabled = false;
        button.AllowFocusWhenDisabled = true;
        button.IsVisible = false;

        button.IsFocused = true;

        button.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void IsFocused_False_OnHiddenControl_Works()
    {
        Button button = new Button();
        button.AddToRoot();
        button.IsVisible = false;

        Should.NotThrow(() => button.IsFocused = false);

        button.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region Navigation skips hidden controls

    [Fact]
    public void HandleTab_HiddenMiddle_IsSkipped()
    {
        (_, Button left, Button middle, Button right) = CreateRow();
        middle.IsVisible = false;
        left.IsFocused = true;

        left.HandleTab(TabDirection.Down);

        right.IsFocused.ShouldBeTrue();
        middle.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleTab_HiddenDescendantOfVisibleContainer_IsSkipped()
    {
        Panel root = new Panel();
        root.AddToRoot();
        Button first = new Button();
        root.AddChild(first);
        StackPanel container = new StackPanel();
        root.AddChild(container);
        Button hiddenChild = new Button();
        container.AddChild(hiddenChild);
        hiddenChild.IsVisible = false;
        Button last = new Button();
        root.AddChild(last);
        first.IsFocused = true;

        first.HandleTab(TabDirection.Down);

        hiddenChild.IsFocused.ShouldBeFalse();
        last.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_ChildOfHiddenContainer_IsSkipped()
    {
        Panel root = new Panel();
        root.AddToRoot();
        Button first = new Button();
        root.AddChild(first);
        StackPanel container = new StackPanel();
        root.AddChild(container);
        Button inHidden = new Button();
        container.AddChild(inHidden);
        container.IsVisible = false;
        Button last = new Button();
        root.AddChild(last);
        first.IsFocused = true;

        first.HandleTab(TabDirection.Down);

        inHidden.IsFocused.ShouldBeFalse();
        last.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void GamepadSpatial_HiddenMiddle_IsSkipped()
    {
        (_, Button left, Button middle, Button right) = CreateRow();
        middle.IsVisible = false;
        left.IsFocused = true;
        PressGamepad(GamepadButton.DPadRight);

        left.OnFocusUpdate();

        middle.IsFocused.ShouldBeFalse();
        right.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void GamepadSpatial_ChildOfHiddenContainer_IsSkipped()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;
        Button left = new Button();
        panel.AddChild(left);
        StackPanel container = new StackPanel();
        panel.AddChild(container);
        container.X = 200;
        Button inHidden = new Button();
        container.AddChild(inHidden);
        container.IsVisible = false;
        Button right = new Button();
        panel.AddChild(right);
        right.X = 400;
        left.IsFocused = true;
        PressGamepad(GamepadButton.DPadRight);

        left.OnFocusUpdate();

        inHidden.IsFocused.ShouldBeFalse();
        right.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void KeyboardSpatial_HiddenMiddle_IsSkipped()
    {
        (_, Button left, Button middle, Button right) = CreateRow();
        middle.IsVisible = false;
        left.IsFocused = true;
        FrameworkElement.RightKeyCombos.Add(new KeyCombo { PushedKey = Keys.Right });
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Right).Object);

        left.OnFocusUpdate();

        middle.IsFocused.ShouldBeFalse();
        right.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_HiddenWithAllowFocusWhenDisabled_IsSkipped()
    {
        (_, Button left, Button middle, Button right) = CreateRow();
        middle.IsEnabled = false;
        middle.AllowFocusWhenDisabled = true;
        middle.IsVisible = false;
        left.IsFocused = true;

        left.HandleTab(TabDirection.Down);

        middle.IsFocused.ShouldBeFalse();
        right.IsFocused.ShouldBeTrue();
    }

    #endregion

    #region Other focus paths

    [Fact]
    public void ScrollViewer_ItemsFocus_HiddenChildReceiverIsSkipped()
    {
        ScrollViewer scrollViewer = new ScrollViewer();
        scrollViewer.AddToRoot();
        Button hidden = new Button();
        scrollViewer.AddChild(hidden);
        hidden.IsVisible = false;
        Button shown = new Button();
        scrollViewer.AddChild(shown);

        scrollViewer.DoItemsHaveFocus = true;

        InteractiveGue.CurrentInputReceiver.ShouldNotBe(hidden);
    }

    [Fact]
    public void MousePush_HiddenButton_DoesNotFocus()
    {
        Button button = new Button();
        button.AddToRoot();
        button.IsVisible = false;

        PushOn(button.Visual);

        button.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void MousePush_HiddenTextBox_DoesNotFocus()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        textBox.IsVisible = false;

        PushOn(textBox.Visual);

        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    #endregion
}
