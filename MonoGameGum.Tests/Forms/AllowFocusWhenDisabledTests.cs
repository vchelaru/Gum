using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Issue #6935: <see cref="FrameworkElement.AllowFocusWhenDisabled"/> lets a disabled control take
/// focus from code, tab order, and gamepad/keyboard spatial navigation. Also pins that an explicit
/// SpatialNavigation override pointing at a control that refuses focus leaves the origin focused.
/// </summary>
public class AllowFocusWhenDisabledTests : BaseTestClass
{
    private static (Panel panel, Button left, Button middle, Button right) CreateRow(bool middleAllowsFocus)
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Button left = new Button();
        panel.AddChild(left);
        left.X = 0;
        left.Y = 0;

        Button middle = new Button();
        panel.AddChild(middle);
        middle.X = 200;
        middle.Y = 0;
        middle.IsEnabled = false;
        middle.AllowFocusWhenDisabled = middleAllowsFocus;

        Button right = new Button();
        panel.AddChild(right);
        right.X = 400;
        right.Y = 0;

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

    #region Default behavior (pinned)

    [Fact]
    public void AllowFocusWhenDisabled_DefaultsToFalse()
    {
        new Button().AllowFocusWhenDisabled.ShouldBeFalse();
    }

    [Fact]
    public void IsFocused_DisabledWithoutFlag_StaysFalse()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: false);
        left.IsFocused = true;

        middle.IsFocused = true;

        middle.IsFocused.ShouldBeFalse();
        left.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void GamepadSpatial_DisabledWithoutFlag_IsSkipped()
    {
        (_, Button left, Button middle, Button right) = CreateRow(middleAllowsFocus: false);
        left.IsFocused = true;
        PressGamepad(GamepadButton.DPadRight);

        left.OnFocusUpdate();

        right.IsFocused.ShouldBeTrue();
        middle.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleTab_DisabledWithoutFlag_IsSkipped()
    {
        (_, Button left, Button middle, Button right) = CreateRow(middleAllowsFocus: false);
        left.IsFocused = true;

        left.HandleTab(TabDirection.Down);

        right.IsFocused.ShouldBeTrue();
        middle.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region Opt-in behavior

    [Fact]
    public void IsFocused_DisabledWithFlag_TakesFocus()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: true);
        left.IsFocused = true;

        middle.IsFocused = true;

        middle.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleTab_DisabledWithFlag_IsIncluded()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: true);
        left.IsFocused = true;

        left.HandleTab(TabDirection.Down);

        middle.IsFocused.ShouldBeTrue();
        left.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void GamepadSpatial_DisabledWithFlag_IsIncluded()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: true);
        left.IsFocused = true;
        PressGamepad(GamepadButton.DPadRight);

        left.OnFocusUpdate();

        middle.IsFocused.ShouldBeTrue();
        left.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void KeyboardSpatial_DisabledWithFlag_IsIncluded()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: true);
        left.IsFocused = true;
        FrameworkElement.RightKeyCombos.Add(new KeyCombo { PushedKey = Keys.Right });
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Right).Object);

        left.OnFocusUpdate();

        middle.IsFocused.ShouldBeTrue();
        left.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void GamepadA_DisabledFocusedWithFlag_DoesNotClick()
    {
        (_, _, Button middle, _) = CreateRow(middleAllowsFocus: true);
        middle.IsFocused = true;
        int clicks = 0;
        int controllerPushes = 0;
        middle.Click += (_, _) => clicks++;
        middle.ControllerButtonPushed += _ => controllerPushes++;
        PressGamepad(GamepadButton.A);

        middle.OnFocusUpdate();

        clicks.ShouldBe(0);
        controllerPushes.ShouldBe(0);
        middle.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void GamepadDPad_DisabledFocusedWithFlag_NavigatesAway()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: true);
        middle.IsFocused = true;
        PressGamepad(GamepadButton.DPadLeft);

        middle.OnFocusUpdate();

        left.IsFocused.ShouldBeTrue();
        middle.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void AllowFocusWhenDisabled_TurnedOffWhileFocused_KeepsFocusAndCanNavigateAway()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: true);
        middle.IsFocused = true;

        middle.AllowFocusWhenDisabled = false;

        middle.IsFocused.ShouldBeTrue();

        PressGamepad(GamepadButton.DPadLeft);
        middle.OnFocusUpdate();

        left.IsFocused.ShouldBeTrue();
        middle.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void AllowFocusWhenDisabled_EnabledControl_IsUnaffected()
    {
        Button button = new Button();
        button.AddToRoot();
        button.AllowFocusWhenDisabled = true;

        button.IsFocused = true;

        button.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void DisabledContainer_ChildWithFlag_IsSkippedByTabAndSpatial()
    {
        Panel root = new Panel();
        root.AddToRoot();
        root.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Button origin = new Button();
        root.AddChild(origin);
        origin.X = 0;

        Panel container = new Panel();
        root.AddChild(container);
        container.X = 200;
        container.IsEnabled = false;

        Button child = new Button();
        container.AddChild(child);
        child.IsEnabled = false;
        child.AllowFocusWhenDisabled = true;

        Button far = new Button();
        root.AddChild(far);
        far.X = 600;

        origin.IsFocused = true;
        origin.HandleTab(TabDirection.Down);
        child.IsFocused.ShouldBeFalse();

        origin.IsFocused = true;
        PressGamepad(GamepadButton.DPadRight);
        origin.OnFocusUpdate();
        child.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region Explicit override to a control that refuses focus

    [Fact]
    public void GamepadExplicitOverride_TargetRefusesFocus_OriginKeepsFocus()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: false);
        left.SpatialNavigationRight = middle;
        left.IsFocused = true;
        PressGamepad(GamepadButton.DPadRight);

        left.OnFocusUpdate();

        left.IsFocused.ShouldBeTrue();
        middle.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void KeyboardExplicitOverride_TargetRefusesFocus_OriginKeepsFocus()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: false);
        left.SpatialNavigationRight = middle;
        left.IsFocused = true;
        FrameworkElement.RightKeyCombos.Add(new KeyCombo { PushedKey = Keys.Right });
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Right).Object);

        left.OnFocusUpdate();

        left.IsFocused.ShouldBeTrue();
        middle.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void GamepadExplicitOverride_TargetAcceptsFocus_MovesFocus()
    {
        (_, Button left, Button middle, _) = CreateRow(middleAllowsFocus: true);
        left.SpatialNavigationRight = middle;
        left.IsFocused = true;
        PressGamepad(GamepadButton.DPadRight);

        left.OnFocusUpdate();

        middle.IsFocused.ShouldBeTrue();
        left.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region A disabled, focused control does not act on input

    [Fact]
    public void KeyboardEnter_DisabledFocusedWithFlag_DoesNotClick()
    {
        (_, _, Button middle, _) = CreateRow(middleAllowsFocus: true);
        middle.IsFocused = true;
        int clicks = 0;
        middle.Click += (_, _) => clicks++;
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Enter).Object);

        middle.OnFocusUpdate();

        clicks.ShouldBe(0);
    }

    [Fact]
    public void Slider_DisabledFocused_DPadRightDoesNotChangeValue()
    {
        Slider slider = new Slider();
        slider.AddToRoot();
        slider.Value = 50;
        slider.IsEnabled = false;
        slider.AllowFocusWhenDisabled = true;
        slider.IsFocused = true;
        PressGamepad(GamepadButton.DPadRight);

        slider.OnFocusUpdate();

        slider.Value.ShouldBe(50);
    }

    [Fact]
    public void TextBox_DisabledFocused_TypingDoesNotChangeText()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        textBox.IsEnabled = false;
        textBox.AllowFocusWhenDisabled = true;
        textBox.IsFocused = true;
        Mock<IInputReceiverKeyboard> keyboard = new Mock<IInputReceiverKeyboard>();
        keyboard.Setup(k => k.KeysTyped).Returns(new List<Keys>());
        keyboard.Setup(k => k.GetStringTyped()).Returns("a");

        textBox.DoKeyboardAction(keyboard.Object);

        textBox.Text.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void TextBox_DisabledFocused_GamepadADoesNotRaiseControllerButtonPushed()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        textBox.IsEnabled = false;
        textBox.AllowFocusWhenDisabled = true;
        textBox.IsFocused = true;
        int pushes = 0;
        textBox.ControllerButtonPushed += _ => pushes++;
        PressGamepad(GamepadButton.A);

        textBox.OnFocusUpdate();

        pushes.ShouldBe(0);
    }

    [Fact]
    public void ComboBox_DisabledFocused_GamepadADoesNotOpenDropDown()
    {
        ComboBox comboBox = new ComboBox();
        comboBox.AddToRoot();
        comboBox.Items!.Add("a");
        comboBox.IsEnabled = false;
        comboBox.AllowFocusWhenDisabled = true;
        comboBox.IsFocused = true;
        PressGamepad(GamepadButton.A);

        comboBox.OnFocusUpdate();

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void ListBox_DisabledFocused_GamepadADoesNotGiveFocusToItems()
    {
        ListBox listBox = new ListBox();
        listBox.AddToRoot();
        listBox.Items!.Add("a");
        listBox.IsEnabled = false;
        listBox.AllowFocusWhenDisabled = true;
        listBox.IsFocused = true;
        PressGamepad(GamepadButton.A);

        listBox.OnFocusUpdate();

        listBox.DoListItemsHaveFocus.ShouldBeFalse();
    }

    [Fact]
    public void ScrollViewer_DisabledFocused_GamepadADoesNotGiveFocusToItems()
    {
        ScrollViewer scrollViewer = new ScrollViewer();
        scrollViewer.AddToRoot();
        scrollViewer.IsEnabled = false;
        scrollViewer.AllowFocusWhenDisabled = true;
        scrollViewer.IsFocused = true;
        PressGamepad(GamepadButton.A);

        scrollViewer.OnFocusUpdate();

        scrollViewer.DoItemsHaveFocus.ShouldBeFalse();
    }

    #endregion
}
