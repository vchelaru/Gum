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
/// Tests for issue #4272: keyboard arrow-key focus navigation reusing the same
/// <see cref="FrameworkElement.GamepadNavigationMode"/> resolution (TabOrder or Spatial, see
/// <see cref="Gum.Forms.SpatialNavigationService"/>) and <see cref="FrameworkElement.SpatialNavigationUp"/>-style
/// overrides that gamepad D-pad/stick navigation already has. Driven by
/// <see cref="FrameworkElement.UpKeyCombos"/>/<see cref="FrameworkElement.DownKeyCombos"/>/
/// <see cref="FrameworkElement.LeftKeyCombos"/>/<see cref="FrameworkElement.RightKeyCombos"/>, which are
/// empty by default (opt-in) since several stock controls already claim arrow keys for their own
/// value/selection/caret behavior. Mirrors <see cref="GamepadNavigationModeTests"/>.
/// </summary>
public class KeyboardDirectionalNavigationTests : BaseTestClass
{
    private static Mock<IInputReceiverKeyboardMonoGame> CreateKeyboardMock(Keys key)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys>());
        return keyboard;
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_DownKeyComboConfigured_TabOrderMode_MovesFocusToNextFocusableSibling()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        // GamepadNavigationMode left unset -- defaults to TabOrder, same as the gamepad counterpart.

        Button first = new Button();
        panel.AddChild(first);
        Button second = new Button();
        panel.AddChild(second);

        first.IsFocused = true;

        FrameworkElement.DownKeyCombos.Add(new KeyCombo { PushedKey = Keys.Down });
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Down).Object);

        first.OnFocusUpdate();

        first.IsFocused.ShouldBeFalse();
        second.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ExplicitSpatialNavigationRightOverride_TakesPrecedenceOverScoring()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Button origin = new Button();
        panel.AddChild(origin);
        origin.X = 0;
        origin.Y = 0;

        Button nearestByScore = new Button();
        panel.AddChild(nearestByScore);
        nearestByScore.X = 50;
        nearestByScore.Y = 0;

        Button overrideTarget = new Button();
        panel.AddChild(overrideTarget);
        overrideTarget.X = 500;
        overrideTarget.Y = 0;

        origin.SpatialNavigationRight = overrideTarget;
        origin.IsFocused = true;

        FrameworkElement.RightKeyCombos.Add(new KeyCombo { PushedKey = Keys.Right });
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Right).Object);

        origin.OnFocusUpdate();

        overrideTarget.IsFocused.ShouldBeTrue();
        nearestByScore.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_NoKeyCombosConfigured_DownKeyPressDoesNotNavigate()
    {
        // Pins the opt-in default: UpKeyCombos/DownKeyCombos/LeftKeyCombos/RightKeyCombos start
        // empty, so a raw Down key press does nothing until a project configures a combo for it.
        Panel panel = new Panel();
        panel.AddToRoot();

        Button origin = new Button();
        panel.AddChild(origin);
        Button below = new Button();
        panel.AddChild(below);

        origin.IsFocused = true;

        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Down).Object);

        origin.OnFocusUpdate();

        origin.IsFocused.ShouldBeTrue();
        below.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_RightKeyComboConfigured_SpatialMode_MovesFocusToCandidateOnRight()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Button origin = new Button();
        panel.AddChild(origin);
        origin.X = 0;
        origin.Y = 0;

        Button right = new Button();
        panel.AddChild(right);
        right.X = 150;
        right.Y = 0;

        Button above = new Button();
        panel.AddChild(above);
        above.X = 0;
        above.Y = -150;

        origin.IsFocused = true;

        FrameworkElement.RightKeyCombos.Add(new KeyCombo { PushedKey = Keys.Right });
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Right).Object);

        origin.OnFocusUpdate();

        origin.IsFocused.ShouldBeFalse();
        right.IsFocused.ShouldBeTrue();
        above.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_SliderRightKeyComboConfigured_SpatialMode_AdjustsValueAndDoesNotNavigate()
    {
        // Slider disables Left/Right-as-navigation in its own constructor
        // (IsUsingLeftAndRightGamepadDirectionsForNavigation = false) so it can use Left/Right for
        // its own value instead -- the new keyboard path reuses that same flag rather than adding a
        // parallel one, so this guard should protect keyboard Right exactly like it already
        // protects gamepad D-pad/stick Right (see GamepadNavigationModeTests's companion test).
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        Slider slider = new Slider();
        panel.AddChild(slider);
        slider.X = 0;
        slider.Y = 0;
        slider.Value = 50;

        Button right = new Button();
        panel.AddChild(right);
        right.X = 150;
        right.Y = 0;

        slider.IsFocused = true;

        FrameworkElement.RightKeyCombos.Add(new KeyCombo { PushedKey = Keys.Right });
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Right).Object);

        slider.OnFocusUpdate();

        slider.Value.ShouldBeGreaterThan(50);
        slider.IsFocused.ShouldBeTrue();
        right.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_UpKeyComboConfigured_TabOrderMode_MovesFocusToPreviousFocusableSibling()
    {
        Panel panel = new Panel();
        panel.AddToRoot();

        Button first = new Button();
        panel.AddChild(first);
        Button second = new Button();
        panel.AddChild(second);

        second.IsFocused = true;

        FrameworkElement.UpKeyCombos.Add(new KeyCombo { PushedKey = Keys.Up });
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardMock(Keys.Up).Object);

        second.OnFocusUpdate();

        second.IsFocused.ShouldBeFalse();
        first.IsFocused.ShouldBeTrue();
    }
}
