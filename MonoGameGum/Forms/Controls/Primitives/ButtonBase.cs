using Gum.Wireframe;
using System;
using System.Collections.Generic;




#if FRB
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms.Controls;
using FlatRedBall.Forms.Input;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using GamepadButton = FlatRedBall.Input.Xbox360GamePad.Button;
using static FlatRedBall.Input.Xbox360GamePad;
namespace FlatRedBall.Forms.Controls.Primitives;
#elif RAYLIB
using RaylibGum.Input;
using Keys = Raylib_cs.KeyboardKey;
#else
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Input;
using GamepadButton = Microsoft.Xna.Framework.Input.Buttons;
#endif


#if !FRB
namespace Gum.Forms.Controls.Primitives;

#endif

public class ButtonBase :
#if FRB
    FrameworkElement,
#else
    Gum.Forms.Controls.FrameworkElement, 
#endif
    IInputReceiver
{
    #region Fields / Properties

    public List<Keys> IgnoredKeys => throw new NotImplementedException();

    public bool TakingInput => throw new NotImplementedException();

    public IInputReceiver NextInTabSequence { get; set; }

    public override bool IsFocused
    { 
        get => base.IsFocused;
        set
        {
            if(value == false)
            {
                wasPushedOnCurrentHold = false;
            }
            base.IsFocused = value;

        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the user pushes, then releases the control.
    /// This means the cursor is over the button, the button was originally pushed,
    /// the primary button was pressed last frame, but is no longer pressed this frame.
    /// The "click" terminology comes from the Cursor's PrimaryClick property.
    /// </summary>
    public event EventHandler Click;

    /// <summary>
    /// Event raised when the user pushes on the control. 
    /// This means the cursor is over the button and the primary button was not pressed last frame, but is pressed this frame.
    /// The "push" terminology comes from the Cursor's PrimaryPush property.
    /// </summary>
    public event EventHandler Push;
    public event Action<IInputReceiver> FocusUpdate;

    /// <summary>
    /// Event raised when any button is pressed on an Xbox360GamePad which is being used by the 
    /// GuiManager.GamePadsForUiControl.
    /// </summary>
    public event Action<GamepadButton> ControllerButtonPushed;


#if FRB
    public event Action<int> GenericGamepadButtonPushed;

    public event Action<FlatRedBall.Input.Mouse.MouseButtons> MouseButtonPushed;
#endif

#endregion

    #region Initialize

    public ButtonBase() : base() { }

    public ButtonBase(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
#if FRB
        Visual.Click += _=>this.HandleClick(this, EventArgs.Empty);
        Visual.Push += _ => this.HandlePush (this, EventArgs.Empty);
        Visual.LosePush += _ => this.HandleLosePush (this, EventArgs.Empty);
        Visual.RollOn += _ => this.HandleRollOn (this, EventArgs.Empty);
        Visual.RollOff += _ => this.HandleRollOff(this, EventArgs.Empty);
#else
        Visual.Click += this.HandleClick;
        Visual.Push += this.HandlePush;
        Visual.LosePush += this.HandleLosePush;
        Visual.RollOn += this.HandleRollOn;
        Visual.RollOff += this.HandleRollOff;
#endif

        base.ReactToVisualChanged();

        UpdateState();
    }

    #endregion

    #region Event Handler Methods

    private void HandleClick(object sender, EventArgs args)
    {
        UpdateState();

        OnClick();

        var innerArgs = args as InputEventArgs ?? new InputEventArgs();

        Click?.Invoke(this, args);
#if FRB
        MouseButtonPushed?.Invoke(FlatRedBall.Input.Mouse.MouseButtons.LeftButton);
#endif
    }

    private void HandlePush(object sender, EventArgs args)
    {
        UpdateState();

        Push?.Invoke(this, null);
    }

    private void HandleLosePush(object sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandleRollOn(object sender, EventArgs args)
    {
        UpdateState();
    }

    private void HandleRollOff(object sender, EventArgs args)
    {
        UpdateState();
    }

    #endregion

    protected virtual void OnClick() { }

    public void PerformClick(object? inputDevice = null)
    {
        var args = inputDevice != null 
            ? new InputEventArgs() { InputDevice = inputDevice } 
            : EventArgs.Empty;
        HandleClick(this, args);
    }

    #region IInputReceiver Methods
    public IInputReceiver? ParentInputReceiver =>
    this.GetParentInputReceiver();
    public void OnFocusUpdatePreview(RoutedEventArgs args)
    {
    }
    public GamepadButton ClickGamepadButton { get; set; } = GamepadButton.A;

    bool wasPushedOnCurrentHold = false;

    public void OnFocusUpdate()
    {
        if(!GetIfPushInputIsHeld())
        {
            wasPushedOnCurrentHold = false;
        }
        var gamepads = GamePadsForUiControl;

        for (int i = 0; i < gamepads.Count; i++)
        {
            var gamepad = gamepads[i];

            HandleGamepadNavigation(gamepad);

            if (gamepad.ButtonPushed(ClickGamepadButton) &&
                // A button may be focused, then through the action of clicking the button
                // (like buying items) it may lose its enabled state,but
                // remain focused as to not focus a new item.
                IsEnabled)
            {
                wasPushedOnCurrentHold = true;
                //this.HandlePush(null);
                this.HandleClick(this, new InputEventArgs() { InputDevice = gamepad });

            }
            else if(gamepad.ButtonReleased(ClickGamepadButton))
            {
                UpdateState();
            }

            void RaiseIfPushedAndEnabled(GamepadButton button)
            {
                if (IsEnabled && gamepad.ButtonPushed(button))
                {
                    ControllerButtonPushed?.Invoke(button);
                }
            }

            RaiseIfPushedAndEnabled(GamepadButton.A);
            RaiseIfPushedAndEnabled(GamepadButton.B);
            RaiseIfPushedAndEnabled(GamepadButton.X);
            RaiseIfPushedAndEnabled(GamepadButton.Y);
            RaiseIfPushedAndEnabled(GamepadButton.Start);
            RaiseIfPushedAndEnabled(GamepadButton.Back);

            RaiseIfPushedAndEnabled(GamepadButton.DPadLeft);
            RaiseIfPushedAndEnabled(GamepadButton.DPadRight);

            if (IsEnabled && gamepad.LeftStick.AsDPadPushed(DPadDirection.Left))
            {
                ControllerButtonPushed?.Invoke(GamepadButton.DPadLeft);
            }
            if (IsEnabled && gamepad.LeftStick.AsDPadPushed(DPadDirection.Right))
            {
                ControllerButtonPushed?.Invoke(GamepadButton.DPadRight);
            }
        }

#if FRB
        for (int i = 0; i < GuiManager.GenericGamePadsForUiControl.Count; i++)
        {
            var gamepad = GuiManager.GenericGamePadsForUiControl[i];

            HandleGamepadNavigation(gamepad);

            if ((gamepad as IInputDevice).DefaultConfirmInput.WasJustPressed && IsEnabled)
            {
                //this.HandlePush(null);
                wasPushedOnCurrentHold = true;
                this.HandleClick(this, EventArgs.Empty);
            }

            if (IsEnabled)
            {
                for (int buttonIndex = 0; buttonIndex < gamepad.NumberOfButtons; i++)
                {
                    if (gamepad.ButtonPushed(buttonIndex))
                    {
                        GenericGamepadButtonPushed?.Invoke(buttonIndex);
                    }
                }
            }
        }
        for (int i = 0; i < GuiManager.InputDevicesForUiControl.Count; i++)
        {
            var inputDevice = GuiManager.InputDevicesForUiControl[i];

            HandleInputDeviceNavigation(inputDevice);

            if (inputDevice.DefaultConfirmInput.WasJustPressed && IsEnabled)
            {
                //this.HandlePush(null);
                wasPushedOnCurrentHold = true;
                this.HandleClick(this, EventArgs.Empty);
            }
        }
#endif

#if (MONOGAME || KNI) && !FRB

        foreach (var keyboard in KeyboardsForUiControl)
        {
            bool wasPushed = false;
            bool wasReleased = false;

            foreach (var combo in FrameworkElement.ClickCombos)
            {
                if (combo.IsComboPushed())
                {
                    wasPushed = true;
                    break;
                }
                else if (combo.IsComboReleased())
                {
                    wasReleased = true;
                    break;
                }
            }

            if(wasPushed)
            {
                InteractiveGue.LastVisualPushed = this.Visual;

                wasPushedOnCurrentHold = true;

                this.HandleClick(this, new InputEventArgs() { InputDevice = keyboard });

                UpdateState();

            }
            if(wasReleased)
            {
                UpdateState();
            }
        }

        base.HandleKeyboardFocusUpdate();


#endif
        FocusUpdate?.Invoke(this);
    }

    protected override bool GetIfPushInputIsHeld()
    {
        return wasPushedOnCurrentHold && base.GetIfPushInputIsHeld();
    }

    public void OnGainFocus()
    {
        IsFocused = true;
    }

    [Obsolete("Use OnLoseFocus instead")]
    public void LoseFocus() => OnLoseFocus();

    public void OnLoseFocus()
    {
        IsFocused = false;
    }

#if !FRB
    public void DoKeyboardAction(IInputReceiverKeyboard keyboard)
    {
#if RAYLIB
#else
        var asKeyboard = keyboard as IInputReceiverKeyboardMonoGame;
        if(asKeyboard != null)
        {
            foreach(var key in asKeyboard.KeysTyped)
            {
                HandleKeyDown(key, 
                    keyboard.IsShiftDown,
                    keyboard.IsAltDown,
                    keyboard.IsCtrlDown);
            }
        }
#endif
    }
#endif

    public void ReceiveInput()
    {
    }

    public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {
        var args = new KeyEventArgs();
        args.Key = key;
        base.RaiseKeyDown(args);
    }

    public void HandleCharEntered(char character)
    {
    }

#endregion


}

