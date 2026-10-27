using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum.Input;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Issue #6937: an explicit SpatialNavigationLeft/Right override on a focused Slider wins over the
/// Slider's own use of Left/Right, and a Slider with no override changes its Value as before.
/// </summary>
public class SliderSpatialOverrideTests : BaseTestClass
{
    private readonly Panel _panel;
    private readonly Button _leftNeighbor;
    private readonly Slider _slider;
    private readonly Button _rightNeighbor;
    private readonly GamePad _gamepad = new GamePad();
    private double _time;

    public SliderSpatialOverrideTests()
    {
        _panel = new Panel();
        _panel.AddToRoot();
        _panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;

        _leftNeighbor = new Button();
        _panel.AddChild(_leftNeighbor);
        _leftNeighbor.X = 0;

        _slider = new Slider();
        _panel.AddChild(_slider);
        _slider.X = 300;
        _slider.Width = 200;
        _slider.Minimum = 0;
        _slider.Maximum = 100;
        _slider.SmallChange = 5;
        _slider.Value = 50;

        _rightNeighbor = new Button();
        _panel.AddChild(_rightNeighbor);
        _rightNeighbor.X = 700;

        FrameworkElement.GamePadsForUiControl.Add(_gamepad);
    }

    // One frame: advance the clock, read the gamepad, run the game's own code, then give the
    // focused control its per-frame update (what GumService.Update does for the focused control).
    private void Frame(System.Action? gameCode = null)
    {
        _time += 1.0 / 60.0;
        _gamepad.Activity(_time);
        gameCode?.Invoke();
        foreach (FrameworkElement control in new FrameworkElement[] { _leftNeighbor, _slider, _rightNeighbor })
        {
            if (control.IsFocused)
            {
                ((IInputReceiver)control).OnFocusUpdate();
                break;
            }
        }
    }

    private void Tap(GamepadButton button, System.Action? gameCode = null)
    {
        _gamepad.SetButtonState(button, true);
        Frame(gameCode);
        _gamepad.SetButtonState(button, false);
        Frame();
    }

    [Fact]
    public void DPad_NoOverrides_LeftAndRightChangeValueAndKeepFocus()
    {
        _slider.IsFocused = true;

        Tap(GamepadButton.DPadLeft);
        _slider.Value.ShouldBe(45);
        Tap(GamepadButton.DPadRight);
        Tap(GamepadButton.DPadRight);
        _slider.Value.ShouldBe(55);

        _slider.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void LeftStick_NoOverrides_LeftChangesValueAndKeepsFocus()
    {
        _slider.IsFocused = true;

        _gamepad.SetLeftStickPosition(-1f, 0f);
        Frame();

        _slider.Value.ShouldBe(45);
        _slider.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void DPad_OverridesSet_LeftMovesFocusAndLeavesValue()
    {
        _slider.SpatialNavigationLeft = _leftNeighbor;
        _slider.SpatialNavigationRight = _rightNeighbor;
        _slider.IsFocused = true;

        Tap(GamepadButton.DPadLeft);

        _leftNeighbor.IsFocused.ShouldBeTrue();
        _slider.IsFocused.ShouldBeFalse();
        _slider.Value.ShouldBe(50);
    }

    [Fact]
    public void DPad_OverridesSet_RightMovesFocusAndLeavesValue()
    {
        _slider.SpatialNavigationLeft = _leftNeighbor;
        _slider.SpatialNavigationRight = _rightNeighbor;
        _slider.IsFocused = true;

        Tap(GamepadButton.DPadRight);

        _rightNeighbor.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(50);
    }

    [Fact]
    public void LeftStick_OverrideSet_MovesFocusAndLeavesValue()
    {
        _slider.SpatialNavigationLeft = _leftNeighbor;
        _slider.IsFocused = true;

        _gamepad.SetLeftStickPosition(-1f, 0f);
        Frame();

        _leftNeighbor.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(50);
    }

    [Fact]
    public void DPad_OverrideSetOnOneSideOnly_OtherSideStillChangesValue()
    {
        _slider.SpatialNavigationLeft = _leftNeighbor;
        _slider.IsFocused = true;

        Tap(GamepadButton.DPadRight);

        _slider.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(55);
    }

    [Fact]
    public void DPad_OverrideSetAndLeftRightNavigationEnabled_MovesFocusWithoutChangingValue()
    {
        // The issue's step 6: the flag is true and an override is set; focus used to move AND the value changed.
        _slider.IsUsingLeftAndRightGamepadDirectionsForNavigation = true;
        _slider.SpatialNavigationLeft = _leftNeighbor;
        _slider.IsFocused = true;

        Tap(GamepadButton.DPadLeft);

        _leftNeighbor.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(50);
    }

    [Fact]
    public void DPad_OverrideSet_HeldPress_DoesNotChangeValueOnRepeat()
    {
        _slider.SpatialNavigationLeft = _leftNeighbor;
        _slider.IsFocused = true;

        _gamepad.SetButtonState(GamepadButton.DPadLeft, true);
        Frame();
        // Hold well past the repeat delay while the slider is still the control being updated.
        for (int i = 0; i < 60; i++)
        {
            _slider.IsFocused = true;
            _leftNeighbor.IsFocused = false;
            Frame();
        }

        _slider.Value.ShouldBe(50);
    }

    [Fact]
    public void DPad_SelfReferenceOverride_SuppressesBothNavigationAndValueChange()
    {
        _slider.SpatialNavigationLeft = _slider;
        _slider.IsFocused = true;

        Tap(GamepadButton.DPadLeft);

        _slider.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(50);
    }

    [Fact]
    public void DPad_OverrideSet_DownStillNavigatesAsBefore()
    {
        Button below = new Button();
        _panel.AddChild(below);
        below.X = 300;
        below.Y = 200;
        _slider.SpatialNavigationLeft = _leftNeighbor;
        _slider.IsFocused = true;

        Tap(GamepadButton.DPadDown);

        below.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(50);
    }

    [Fact]
    public void Button_RightOverride_StillMovesFocusToOverride()
    {
        // Controls that leave Left/Right on for navigation are unaffected by the change.
        Button other = new Button();
        _panel.AddChild(other);
        other.X = 900;
        _leftNeighbor.SpatialNavigationRight = other;
        _leftNeighbor.IsFocused = true;

        Tap(GamepadButton.DPadRight);

        other.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Keyboard_OverrideSet_RightComboMovesFocusAndLeavesValue()
    {
        FrameworkElement.RightKeyCombos.Add(new KeyCombo { PushedKey = Keys.Right });
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.Right)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Right)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(Keys.Right)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        _slider.SpatialNavigationRight = _rightNeighbor;
        _slider.IsFocused = true;

        ((IInputReceiver)_slider).OnFocusUpdate();

        _rightNeighbor.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(50);
    }

    // --- Docs recipe: console-style "lock in" (A enters edit mode, A or B leaves it) ---

    private bool _isEditing;

    private void SetEditing(bool isEditing)
    {
        _isEditing = isEditing;
        _slider.SpatialNavigationLeft = isEditing ? null : _leftNeighbor;
        _slider.SpatialNavigationRight = isEditing ? null : _rightNeighbor;
    }

    private void LockInGameCode()
    {
        if (_slider.IsFocused == false)
        {
            return;
        }
        if (_isEditing && (_gamepad.ButtonPushed(GamepadButton.A) || _gamepad.ButtonPushed(GamepadButton.B)))
        {
            SetEditing(false);
        }
        else if (!_isEditing && _gamepad.ButtonPushed(GamepadButton.A))
        {
            SetEditing(true);
        }
    }

    [Fact]
    public void LockInRecipe_AToEdit_AOrBToLeave_TogglesBetweenValueAndNavigation()
    {
        SetEditing(false);
        _slider.IsFocused = true;

        // Not editing: Left navigates and leaves the value.
        Tap(GamepadButton.DPadLeft, LockInGameCode);
        _leftNeighbor.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(50);

        // Back on the slider, A enters edit mode; A is not consumed by the Slider.
        _leftNeighbor.IsFocused = false;
        _slider.IsFocused = true;
        Tap(GamepadButton.A, LockInGameCode);
        _isEditing.ShouldBeTrue();
        _slider.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(50);

        // Editing: Left and Right change the value and keep focus.
        Tap(GamepadButton.DPadLeft, LockInGameCode);
        Tap(GamepadButton.DPadRight, LockInGameCode);
        Tap(GamepadButton.DPadRight, LockInGameCode);
        _slider.Value.ShouldBe(55);
        _slider.IsFocused.ShouldBeTrue();

        // B leaves edit mode; Right then navigates and leaves the value.
        Tap(GamepadButton.B, LockInGameCode);
        _isEditing.ShouldBeFalse();
        Tap(GamepadButton.DPadRight, LockInGameCode);
        _rightNeighbor.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(55);

        // A also leaves edit mode.
        _rightNeighbor.IsFocused = false;
        _slider.IsFocused = true;
        Tap(GamepadButton.A, LockInGameCode);
        _isEditing.ShouldBeTrue();
        Tap(GamepadButton.A, LockInGameCode);
        _isEditing.ShouldBeFalse();
        Tap(GamepadButton.DPadLeft, LockInGameCode);
        _leftNeighbor.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(55);
    }

    [Fact]
    public void LockInRecipe_AAndLeftOnSameFrame_ChangesValueOnceWithoutNavigating()
    {
        SetEditing(false);
        _slider.IsFocused = true;

        // The game code runs before the focused control's update, so clearing the overrides on this
        // frame already lets the same frame's Left change the value, once.
        _gamepad.SetButtonState(GamepadButton.A, true);
        _gamepad.SetButtonState(GamepadButton.DPadLeft, true);
        Frame(LockInGameCode);

        _isEditing.ShouldBeTrue();
        _slider.IsFocused.ShouldBeTrue();
        _slider.Value.ShouldBe(45);
    }
}
