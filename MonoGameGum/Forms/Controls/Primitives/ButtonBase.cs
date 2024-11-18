using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
namespace FlatRedBall.Forms.Controls.Primitives;
#else
namespace MonoGameGum.Forms.Controls.Primitives;
#endif

public class ButtonBase : FrameworkElement, IInputReceiver
{
    #region Fields / Properties

    public List<Keys> IgnoredKeys => throw new NotImplementedException();

    public bool TakingInput => throw new NotImplementedException();

    public IInputReceiver NextInTabSequence { get; set; }

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

#if FRB
    /// <summary>
    /// Event raised when any button is pressed on an Xbox360GamePad which is being used by the 
    /// GuiManager.GamePadsForUiControl.
    /// </summary>
    public event Action<Xbox360GamePad.Button> ControllerButtonPushed;
    public event Action<int> GenericGamepadButtonPushed;

    public event Action<FlatRedBall.Input.Mouse.MouseButtons> MouseButtonPushed;
#endif

    #endregion

    public ButtonBase() : base() { }

    public ButtonBase(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        Visual.Click += this.HandleClick;
        Visual.Push += this.HandlePush;
        Visual.LosePush += this.HandleLosePush;
        Visual.RollOn += this.HandleRollOn;
        Visual.RollOff += this.HandleRollOff;

        base.ReactToVisualChanged();

        UpdateState();
    }


    private void HandleClick(object sender, EventArgs args)
    {
        UpdateState();

        OnClick();

        Click?.Invoke(this, null);
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

    protected virtual void OnClick() { }

    public void PerformClick()
    {
        HandleClick(this, EventArgs.Empty);
    }

    #region IInputReceiver Methods

    public void OnFocusUpdate()
    {
#if FRB
        var gamepads = GuiManager.GamePadsForUiControl;
        for (int i = 0; i < gamepads.Count; i++)
        {
            var gamepad = gamepads[i];

            HandleGamepadNavigation(gamepad);

            if (gamepad.ButtonPushed(FlatRedBall.Input.Xbox360GamePad.Button.A) &&
                // A button may be focused, then through the action of clicking the button
                // (like buying items) it may lose its enabled state,but
                // remain focused as to not focus a new item.
                IsEnabled)
            {
                //this.HandlePush(null);
                this.HandleClick(null);

                ControllerButtonPushed?.Invoke(Xbox360GamePad.Button.A);
            }

            void RaiseIfPushedAndEnabled(FlatRedBall.Input.Xbox360GamePad.Button button)
            {
                if (IsEnabled && gamepad.ButtonPushed(button))
                {
                    ControllerButtonPushed?.Invoke(button);
                }
            }

            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.B);
            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.X);
            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Y);
            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Start);
            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.Back);

            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.DPadLeft);
            RaiseIfPushedAndEnabled(Xbox360GamePad.Button.DPadRight);

            if (IsEnabled && gamepad.LeftStick.AsDPadPushed(Xbox360GamePad.DPadDirection.Left))
            {
                ControllerButtonPushed?.Invoke(Xbox360GamePad.Button.DPadLeft);
            }
            if (IsEnabled && gamepad.LeftStick.AsDPadPushed(Xbox360GamePad.DPadDirection.Right))
            {
                ControllerButtonPushed?.Invoke(Xbox360GamePad.Button.DPadRight);
            }


            if (gamepad.ButtonReleased(FlatRedBall.Input.Xbox360GamePad.Button.A))
            {
            }
        }

        for (int i = 0; i < GuiManager.GenericGamePadsForUiControl.Count; i++)
        {
            var gamepad = GuiManager.GenericGamePadsForUiControl[i];

            HandleGamepadNavigation(gamepad);

            if ((gamepad as IInputDevice).DefaultConfirmInput.WasJustPressed && IsEnabled)
            {
                //this.HandlePush(null);
                this.HandleClick(null);
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
                this.HandleClick(null);
            }
        }
#endif
        FocusUpdate?.Invoke(this);
    }

    public void OnGainFocus()
    {
    }

    public void OnLoseFocus()
    {
        IsFocused = false;
    }

    public void DoKeyboardAction(IInputReceiverKeyboard keyboard)
    {

    }

    public void ReceiveInput()
    {
    }

    public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {
#if FRB
        var args = new Input.KeyEventArgs();
        args.Key = key;
        base.RaiseKeyDown(args);
#endif
    }

    public void HandleCharEntered(char character)
    {
    }

    #endregion


}
