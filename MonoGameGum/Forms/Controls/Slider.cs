using System;
using System.Collections.Generic;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Math;




#if FRB
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms.Controls;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Forms.Controls.Primitives;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using static FlatRedBall.Input.Xbox360GamePad;
using GamepadButton = FlatRedBall.Input.Xbox360GamePad.Button;
namespace FlatRedBall.Forms.Controls;
#elif RAYLIB
using RaylibGum.Input;
using Gum.Forms.Controls.Primitives;
using Keys = Raylib_cs.KeyboardKey;

namespace Gum.Forms.Controls;

#else
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Input;
using MonoGameGum.Forms.Controls.Primitives;
using GamepadButton = Microsoft.Xna.Framework.Input.Buttons;
namespace MonoGameGum.Forms.Controls;
#endif

/// <summary>
/// A control enabling changing a value between the specified Minimum and Maximum
/// </summary>
public class Slider : RangeBase, IInputReceiver
{
    #region Fields/Properties


    /// <summary>
    /// The frequency to snap the Slider value when IsSnapToTickEnabled is true. If IsSnapToTickEnabled
    /// is false, then this value is ignored.
    /// </summary>
    public double TicksFrequency { get; set; } = 1;

    /// <summary>
    /// Controls whether the TickFrequency is applied when the slider is moved throuh the UI. If false, then any
    /// value between Minimum and Maximum is possible. If true, then values snap to the TickFrequency.
    /// </summary>
    public bool IsSnapToTickEnabled { get; set; } = false;

    public bool IsThumbGrabbed => MainCursor.WindowPushed == this.thumb?.Visual;

    public List<Keys> IgnoredKeys => throw new NotImplementedException();

    public bool TakingInput => throw new NotImplementedException();

    public IInputReceiver NextInTabSequence { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    double ValueOnThumbOrTrackPush;

    public const string SliderCategoryName = "SliderCategory";

    #endregion

    #region Events

    public event Action<IInputReceiver> FocusUpdate;

    public event Action<GamepadButton> ControllerButtonPushed;

    public event Action<int> GenericGamepadButtonPushed;

    #endregion

    #region Initialize

    public Slider() : base()
    {
        Initialize();
    }

    public Slider(InteractiveGue visual) : base(visual)
    {
        Initialize();
    }

    private void Initialize()
    {
        Minimum = 0;
        Maximum = 100;
        LargeChange = 25;
        SmallChange = 5;

        // by default sliders use left/right to change the slider value
        this.IsUsingLeftAndRightGamepadDirectionsForNavigation = false;
    }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        Track.Push += HandleTrackPush;

#if FRB
        Track.RemovedAsPushedWindow += _ => HandleTrackRemovedAsPushedWindow(this, EventArgs.Empty);
#else
        Track.RemovedAsPushed += HandleTrackRemovedAsPushedWindow;
#endif

        if (thumb != null)
        {
#if FRB
            base.thumb.Visual.RemovedAsPushedWindow += _ => HandleThumbRemovedAsPushedWindow(this, EventArgs.Empty);
#else
            base.thumb.Visual.RemovedAsPushed += HandleThumbRemovedAsPushedWindow;
#endif
        }
        UpdateState();

        RaiseValueChangeCompleted();
    }

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();

        Track.Push -= HandleTrackPush;

    }

#endregion

    #region Event Handlers
    protected override void HandleThumbPush(object sender, EventArgs e)
    {
        var leftOfThumb = this.thumb.AbsoluteLeft;

        if (this.thumb.Visual.XOrigin == global::RenderingLibrary.Graphics.HorizontalAlignment.Center)
        {
            leftOfThumb += this.thumb.ActualWidth / 2.0f;
        }
        else if (this.thumb.Visual.XOrigin == global::RenderingLibrary.Graphics.HorizontalAlignment.Right)
        {
            leftOfThumb += this.thumb.ActualWidth;
        }
        var cursorScreen = MainCursor.XRespectingGumZoomAndBounds();

        cursorGrabOffsetRelativeToThumb = cursorScreen - leftOfThumb;

        ValueOnThumbOrTrackPush = Value;
    }

    private void HandleThumbRemovedAsPushedWindow(object sender, EventArgs args)
    {
        if (ValueOnThumbOrTrackPush != Value)
        {
            RaiseValueChangeCompleted();
        }
    }

    private void HandleTrackRemovedAsPushedWindow(object sender, EventArgs args)
    {
        if (ValueOnThumbOrTrackPush != Value && IsMoveToPointEnabled)
        {
            RaiseValueChangeCompleted();
        }
    }

    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);

        if (Visual != null)
        {
            UpdateThumbPositionAccordingToValue();
        }
    }

    // Normally we'd just make this use the standard EventHandler signature
    // and handle it with a lambda wherever this is used. But for this case we
    // need to -= the event, so we can't wrap the call in a lambda to fix for the
    // different signatures in FRB vs MonoGame Gum. therefore, we have to #if the signature
    // here on the method:
#if FRB
    private void HandleTrackPush(IWindow window)
#else
    private void HandleTrackPush(object sender, EventArgs args)
#endif
    {
        //////////////////////////Early Out//////////////////////////
        if(!IsEnabled)
        {
            return;
        }
        /////////////////////////////////////////////////////////////

        var valueBefore = Value;

        ValueOnThumbOrTrackPush = Value;

        if (IsMoveToPointEnabled)
        {
            var left = Track.GetAbsoluteX();
            var right = Track.GetAbsoluteX() + Track.GetAbsoluteWidth();

            var screenX = MainCursor.XRespectingGumZoomAndBounds();

            var ratio = (screenX - left) / (right - left);

            ratio = System.Math.Max(0, ratio);
            ratio = System.Math.Min(1, ratio);

            var value = Minimum + (Maximum - Minimum) * ratio;

            ApplyValueConsideringSnapping(value);
        }
        else
        {
            double newValue;

            var sign = GetCurrentSignRelativeToValue();

            if (sign < 0)
            {
                newValue = Value - LargeChange;
                ApplyValueConsideringSnapping(newValue);
            }
            else if (sign > 0)
            {
                newValue = Value + LargeChange;

                ApplyValueConsideringSnapping(newValue);
            }
        }

        if (valueBefore != Value)
        {
            RaiseValueChangedByUi();
        }
    }

    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);

        if (Visual != null)
        {
            UpdateThumbPositionAccordingToValue();
        }
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);

        if (Visual != null)
        {
            UpdateThumbPositionAccordingToValue();
        }
    }

    #endregion

    private double ApplyValueConsideringSnapping(double newValue)
    {
        var originalValue = newValue;

        if (IsSnapToTickEnabled)
        {
            newValue = MathFunctions.RoundDouble(newValue, TicksFrequency, Minimum);

            var range = Maximum - Minimum;
            var lastTick = ((int)((Maximum - Minimum) / TicksFrequency)) * TicksFrequency;

            if (originalValue > lastTick)
            {
                // see if we snap to end or not...
                var distanceFromLastTick = System.Math.Abs(originalValue - lastTick);
                var distanceFromMax = System.Math.Abs(Maximum - originalValue);

                if (distanceFromMax < distanceFromLastTick)
                {
                    newValue = Maximum;
                }
            }

        }

        if (Value != newValue)
        {
            Value = newValue;
        }
        else
        {
            // cursor drag will set the position to the cursor, we may need to snap it back
            UpdateThumbPositionAccordingToValue();
        }
        return newValue;
    }

    #region UpdateTo Methods

    public override void UpdateState()
    {
        if (Visual == null) //don't try to update the UI when the UI is not set yet, mmmmkay?
            return;

        var state = GetDesiredState();
        Visual.SetProperty(SliderCategoryName + "State", state);
    }

    private void UpdateThumbPositionAccordingToValue()
    {
        var ratioOver = (Value - Minimum) / (Maximum - Minimum);
        if (Maximum <= Minimum)
        {
            ratioOver = 0;
        }


        // Update December 26, 2022
        // If the thumb uses XUnits of
        // absolute, then if the slider
        // changes, the thumb will be in
        // the old position. By using an X
        // value of percentage, then changes
        // in width won't cause thumb positioning
        // problems:

        //thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        //thumb.X = Microsoft.Xna.Framework.MathHelper.Lerp(0, Track.GetAbsoluteWidth(),
        //    (float)ratioOver);

        if(thumb != null)
        {
            thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.Percentage;
            thumb.X = 100 * (float)ratioOver;
        }
    }

#if FRB
    protected override void UpdateThumbPositionToCursorDrag(Cursor cursor)
#else
    protected override void UpdateThumbPositionToCursorDrag(ICursor cursor)
#endif
    {
        var valueBefore = Value;

        var cursorScreenX = cursor.XRespectingGumZoomAndBounds();

        var cursorXRelativeToTrack = cursorScreenX - Track.AbsoluteLeft;

        // See UpdateThumbPositionAccordingToValue for an explanation of why we use
        // Percentage rather than PixelsFromSmall:
        //thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        //thumb.X = cursorXRelativeToTrack - cursorGrabOffsetRelativeToThumb;

        thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.Percentage;

        var pixelOffset = cursorXRelativeToTrack - cursorGrabOffsetRelativeToThumb;
        var width = Track.GetAbsoluteWidth();
        if (width == 0)
        {
            // prevent divide by 0's
            width = 1;
        }

        thumb.X = 100 * pixelOffset / width;

        float range = Track.GetAbsoluteWidth();


        if (range != 0)
        {
            var ratio = (thumb.X) / 100;
            ratio = System.Math.Max(0, ratio);
            ratio = System.Math.Min(1, ratio);

            var valueToSet = Minimum + (Maximum - Minimum) * ratio;

            ApplyValueConsideringSnapping(valueToSet);
        }
        else
        {
            Value = Minimum;
        }
        if (valueBefore != Value)
        {
            RaiseValueChangedByUi();
        }
    }

    #endregion

    #region IInputReceiver Methods

    public IInputReceiver? ParentInputReceiver =>
    this.GetParentInputReceiver();
    public void OnFocusUpdatePreview(RoutedEventArgs args)
    {
    }

    public void OnFocusUpdate()
    {
        var gamepads = FrameworkElement.GamePadsForUiControl;

        for (int i = 0; i < gamepads.Count; i++)
        {
            var gamepad = gamepads[i];

            HandleGamepadNavigation(gamepad);


            if (gamepad.ButtonRepeatRate(GamepadButton.DPadLeft) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Left))
            {
                this.Value -= this.SmallChange;
            }
            else if (gamepad.ButtonRepeatRate(GamepadButton.DPadRight) ||
                gamepad.LeftStick.AsDPadPushedRepeatRate(DPadDirection.Right))
            {
                this.Value += this.SmallChange;
            }


            void RaiseIfPushedAndEnabled(GamepadButton button)
            {
                if (IsEnabled && gamepad.ButtonPushed(button))
                {
                    ControllerButtonPushed?.Invoke(button);
                }
            }

            RaiseIfPushedAndEnabled(GamepadButton.B);
            RaiseIfPushedAndEnabled(GamepadButton.X);
            RaiseIfPushedAndEnabled(GamepadButton.Y);
            RaiseIfPushedAndEnabled(GamepadButton.Start);
            RaiseIfPushedAndEnabled(GamepadButton.Back);
        }

#if FRB
        var genericGamepads = GuiManager.GenericGamePadsForUiControl;
        for (int i = 0; i < genericGamepads.Count; i++)
        {
            var gamepad = genericGamepads[i];

            HandleGamepadNavigation(gamepad);

            var leftStick = gamepad.AnalogSticks.Length > 0
                ? gamepad.AnalogSticks[0]
                : null;

            if (gamepad.DPadRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Left) ||
                leftStick?.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Left) == true)
            {
                this.Value -= this.SmallChange;
            }
            else if (gamepad.DPadRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Right) ||
                leftStick?.AsDPadPushedRepeatRate(FlatRedBall.Input.Xbox360GamePad.DPadDirection.Right) == true)
            {
                this.Value += this.SmallChange;
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
#endif

#if (MONOGAME || KNI) && !FRB

        foreach (var keyboard in KeyboardsForUiControl)
        {
            if(keyboard.KeyTyped(Keys.Right) == true)
            {
                this.Value += SmallChange;
            }
            if(keyboard.KeyTyped(Keys.Left) == true)
            {
                this.Value -= SmallChange;
            }
        }

        base.HandleKeyboardFocusUpdate();
#endif
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

    public void ReceiveInput()
    {
    }

#if !FRB
    public void DoKeyboardAction(IInputReceiverKeyboard keyboard)
    {
        ReceiveInput();

        var shift = keyboard.IsShiftDown;
        var ctrl = keyboard.IsCtrlDown;
        var alt = keyboard.IsAltDown;




        // This allocates. We could potentially make this return 
        // an IList or List. That's a breaking change for a tiny amount
        // of allocation....what to do....

#if !RAYLIB
        var asMonoGameKeyboard = (IInputReceiverKeyboardMonoGame)keyboard;

        foreach (var key in asMonoGameKeyboard.KeysTyped)
        {
            HandleKeyDown(key, shift, alt, ctrl);
        }
#endif

        var stringTyped = keyboard.GetStringTyped();

        if (stringTyped != null)
        {
            for (int i = 0; i < stringTyped.Length; i++)
            {
                // receiver could get nulled out by itself when something like enter is pressed
                HandleCharEntered(stringTyped[i]);
            }
        }
    }
#endif

    public void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown)
    {
#if MONOGAME && !FRB
        var args = new KeyEventArgs();
        args.Key = key;
        base.RaiseKeyDown(args);
#endif
    }

    public void HandleCharEntered(char character)
    {
    }


    #endregion
}
