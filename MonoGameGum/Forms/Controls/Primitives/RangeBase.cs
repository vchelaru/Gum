﻿using Gum.Wireframe;
using System;
using RenderingLibrary;



#if FRB
using FlatRedBall.Gui;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls.Primitives;
#else
using MonoGameGum.Input;
namespace MonoGameGum.Forms.Controls.Primitives;
#endif

public abstract class RangeBase : FrameworkElement
{
    #region Fields/Properties

    static TimeSpan InitialRepeatRate = TimeSpan.FromSeconds(.33);
    static TimeSpan SubsequentRepeatRate = TimeSpan.FromSeconds(.12);

    protected Button thumb;

    // version 1 of this would use the thumb's parent. But this is problematic if the thumb
    // parent is re-assigned after the Slider is created. Instead we should look for an explicit
    // track:
    InteractiveGue explicitTrack;
    protected InteractiveGue Track => explicitTrack ?? thumb.Visual.EffectiveParentGue as InteractiveGue;

    /// <summary>
    /// Represents the X or Y offset of the cursor relative to the thumb when the thumb was grabbed.
    /// If the element is horizontal, this is an X value. If the element is vertical, this is a Y value.
    /// </summary>
    protected float cursorGrabOffsetRelativeToThumb = 0;

    /// <summary>
    /// The amount to change Value when the user clicks on the track. 
    /// Only applies if IsMoveToPointEnabled is false.
    /// </summary>
    public double LargeChange { get; set; }

    /// <summary>
    /// The amount to change Value when the user presses the up or down buttons on a scrollbar, 
    /// per mouse wheel tick on a scrollbar, and the amount to change the value in response
    /// to left/right presses on gamepad or keyboard on a Slider.
    /// </summary>
    public double SmallChange { get; set; }

    double minimum = 0;
    /// <summary>
    /// The minimum inclusive value which can be set through the UI.
    /// </summary>
    public double Minimum
    {
        get => minimum;
        set
        {
            var oldValue = minimum;
            minimum = value;

            OnMinimumChanged(oldValue, minimum);
        }
    }

    double maximum = 1;
    /// <summary>
    /// The maximum inclusive value which can be set through the UI.
    /// </summary>
    public double Maximum
    {
        get => maximum;
        set
        {
            var oldValue = maximum;
            maximum = value;

            OnMaximumChanged(oldValue, maximum);
        }
    }

    double value;
    private double TrackPushedTime;
    private float TrackPushedSignRelativeToValue;
    private double LastRepeatRate;

    /// <summary>
    /// The current value of the RangeBase. This value is clamped to be between Minimum and Maximum.
    /// </summary>
    public double Value
    {
        get => value;
        set
        {
#if DEBUG
            if (double.IsNaN(value))
            {
                throw new InvalidOperationException("Can't set the ScrollBar Value to NaN");
            }
#endif
            var oldValue = this.value;
            var newValue = value;


            // Cap the values first so the comparison is done against
            // the capped value
            newValue = System.Math.Min(newValue, Maximum);
            newValue = System.Math.Max(newValue, Minimum);

            if (oldValue != newValue)
            {
                this.value = newValue;

                OnValueChanged(oldValue, this.value);

                ValueChanged?.Invoke(this, EventArgs.Empty);

                var shouldRaiseChangeCompleted = true;

                if(thumb != null && MainCursor.WindowPushed == thumb.Visual)
                {
                    shouldRaiseChangeCompleted = false;
                }

                if(MainCursor.WindowPushed == Track && IsMoveToPointEnabled)
                {
                    shouldRaiseChangeCompleted = false;
                }

                if (shouldRaiseChangeCompleted)
                {
                    // Make sure the user isn't currently grabbing the thumb
                    ValueChangeCompleted?.Invoke(this, EventArgs.Empty);
                }

                PushValueToViewModel();
            }
        }
    }

    public override bool IsEnabled 
    { 
        get => base.IsEnabled;
        set
        {
            base.IsEnabled = value;
            if (thumb != null)
            {
                thumb.IsEnabled = value;
            }
        }
    }

    /// <summary>
    /// Controls whether clicking on the track sets the value to the clicked position.
    /// If true, the value moves to the clicked position. If false, the value increases or decreases
    /// according to the LargeChange value.
    /// </summary>
    public bool IsMoveToPointEnabled { get; set; }

    #endregion

    #region Events

    /// <summary>
    /// Event raised whenever the Value property changes regardless
    /// of source. This event may be raised multiple times if the user
    /// pushes+drags on the track or thumb.
    /// </summary>
    public event EventHandler ValueChanged;

    /// <summary>
    /// Event raised whenever a Value change has completed. This event is raised for discrete
    /// changes such as pressing left/right on a keyboard, or pushes on the track when
    /// IsMoveToPointEnabled is set to false.
    /// </summary>
    /// <remarks>
    /// This event can be used to prevent responding to changes every frame as a user
    /// is dragging a thumb or clicking on a track with IsMoveToPointEnabled set to true.
    /// Examples of when to use ValueChangeCompleted would be to play an audio cue when changing
    /// volume, or committing changes which are too expensive to perform every frame such as saving
    /// settings to disk.
    /// </remarks>
    public event EventHandler ValueChangeCompleted;

    /// <summary>
    /// Event raised whenever a Value change is initiated by the UI. This event is not raised
    /// when Value changes are performed in code, such as by assigning the Value property.
    /// </summary>
    public event EventHandler ValueChangedByUi;

    #endregion

    #region Initialize

    public RangeBase() : base() { }

    public RangeBase(InteractiveGue visual) : base(visual) { }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        var thumbVisual = this.Visual.GetGraphicalUiElementByName("ThumbInstance") as InteractiveGue;
        RefreshInternalVisualReferences();

        if (thumbVisual != null)
        {
            if (thumbVisual.FormsControlAsObject == null)
            {
                thumb = new Button(thumbVisual);
            }
            else
            {
                thumb = thumbVisual.FormsControlAsObject as Button;
            }
            thumb.Push += HandleThumbPush;
        }
#if FRB
        if(thumb != null)
        {
            thumb.Visual.DragOver += HandleThumbRollOverFrb;
        }
        Visual.RollOver += HandleThisRollOverFrb;
        Track.Push += HandleTrackPushFrb;
        Track.RollOver += HandleTrackHoverFrb;

#else
        if (thumb != null)
        {
            thumb.Visual.Dragging += HandleThumbRollOver;
        }
        Visual.RollOver += HandleThisRollOver;
        Track.Push += HandleTrackPush;
        Track.HoverOver += HandleTrackHover;
#endif

        // The attachments may not yet be set up, so set the explicitTrack's RaiseChildrenEventsOutsideOfBounds
        //var thumbParent = thumb.Visual.Parent as GraphicalUiElement;
        //if(thumbParent != null)
        //{
        //    thumbParent.RaiseChildrenEventsOutsideOfBounds = true;
        //}

        Minimum = 0;
        Maximum = 100;
        SmallChange = 10;
        Value = 0;

    }

    protected virtual void RefreshInternalVisualReferences()
    {
        // assign explicit track before adding events
        AssignExplicitTrack();
    }

    private void AssignExplicitTrack()
    {
        // Vic says
        // It seems FRB
        // tolerates a missing
        // track, but MonoGame requires
        // it. Not sure why...perhaps to
        // not break old FRB projects?
        var trackLocal = this.Visual.GetGraphicalUiElementByName("TrackInstance");
#if MONOGAME && !FRB

#if DEBUG
        if (trackLocal == null)
        {
            throw new Exception($"Could not find a child named TrackInstance when creating a {this.GetType()}");
        }
        else if (!(trackLocal is InteractiveGue))
        {
            throw new Exception("Found a TrackInstance, but it is not an InteractiveGue");
        }
#endif

#endif
        explicitTrack = (InteractiveGue)trackLocal;
        if (trackLocal is InteractiveGue trackAsInteractive)
        {
            trackAsInteractive.RaiseChildrenEventsOutsideOfBounds = true;
        }
    }

#if FRB
    // these wrappers exist at class level rather than lambdas so they can be unsubscribed
    void HandleThumbRollOverFrb(IWindow _) => HandleThumbRollOver(this, EventArgs.Empty);
    void HandleThisRollOverFrb(IWindow _) => HandleThisRollOver(this, EventArgs.Empty);
    void HandleTrackPushFrb(IWindow _) => HandleTrackPush(this, EventArgs.Empty);
    void HandleTrackHoverFrb(IWindow _) => HandleTrackHover(this, EventArgs.Empty);

#endif

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();

        thumb.Push -= HandleThumbPush;
#if FRB
        thumb.Visual.DragOver -= HandleThumbRollOverFrb;
        Visual.RollOver -= HandleThisRollOverFrb;
        Track.Push -= HandleTrackPushFrb;
        Track.RollOver -= HandleTrackHoverFrb;

#else
        thumb.Visual.Dragging -= HandleThumbRollOver;
        Visual.RollOver -= HandleThisRollOver;
        Track.Push -= HandleTrackPush;
        Track.HoverOver -= HandleTrackHover;
#endif
    }

    #endregion

    #region Track Events

    private void HandleTrackPush(object? sender, EventArgs e)
    {
        TrackPushedTime = MainCursor.LastPrimaryPushTime;

        TrackPushedSignRelativeToValue = GetCurrentSignRelativeToValue();
    }

    private void HandleTrackHover(object? sender, EventArgs e)
    {
        var cursor = MainCursor;
        if (cursor.WindowPushed == Track && cursor.WindowOver != thumb?.Visual)
        {
            // Should we be respecting MoveToPoint?

            if(IsMoveToPointEnabled)
            {
                // do this immediately!
                ApplyTrackDownRepeatRate();
            }
            else
            {
                var shouldRepeat = InteractiveGue.CurrentGameTime - TrackPushedTime > InitialRepeatRate.TotalSeconds &&
                    InteractiveGue.CurrentGameTime - LastRepeatRate > SubsequentRepeatRate.TotalSeconds;

                if (shouldRepeat)
                {
                    ApplyTrackDownRepeatRate();
                    // act as if the thumb was pushed:
                    LastRepeatRate = InteractiveGue.CurrentGameTime;
                }
            }
        }
    }

    private void ApplyTrackDownRepeatRate()
    {
        var valueBefore = Value;
        double newValue;
        int currentSignRelativeToThumb = GetCurrentSignRelativeToValue();

        if(IsMoveToPointEnabled)
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
            // This prevents the Thumb from hopping back and forth around the cursor's position
            if(currentSignRelativeToThumb == TrackPushedSignRelativeToValue)
            {
                if (currentSignRelativeToThumb == -1)
                {
                    newValue = Value - LargeChange;
                    ApplyValueConsideringSnapping(newValue);
                }
                else if (currentSignRelativeToThumb == 1)
                {
                    newValue = Value + LargeChange;

                    ApplyValueConsideringSnapping(newValue);
                }
            }
        }

        if (valueBefore != Value)
        {
            RaiseValueChangedByUi();
        }
    }

    protected int GetCurrentSignRelativeToValue()
    {
        var cursorX = MainCursor.XRespectingGumZoomAndBounds();

        //var currentSignRelativeToThumb = cursorX < thumb.AbsoluteLeft
        //    ? -1
        //    : cursorX > thumb.AbsoluteLeft + thumb.ActualWidth ? 1 : 0;
        //return currentSignRelativeToThumb;

        var currentPercentageOver = (Value - Minimum) / (Maximum - Minimum);
        var trackAbsoluteLeft = Track.AbsoluteLeft;
        var trackAbsoluteRight = Track.AbsoluteRight;
        var trackWidth = Track.GetAbsoluteWidth();
        var clickedPercentageOver = (cursorX - AbsoluteLeft) / trackWidth;

        if(clickedPercentageOver < currentPercentageOver)
        {
            return -1;
        }
        else if (clickedPercentageOver > currentPercentageOver)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    #endregion

    #region Thumb Events

    protected abstract void HandleThumbPush(object sender, EventArgs e);

    private void HandleThumbRollOver(object sender, EventArgs args)
    {
        var cursor = MainCursor;

        if (cursor.WindowPushed == thumb.Visual && IsEnabled)
        {
            UpdateThumbPositionToCursorDrag(cursor);
        }
    }

    // This is handling ThisRollOver, but it only does anything if the user pushed on the thumb,
    // so moving it to the ThumbEvents region
    private void HandleThisRollOver(object sender, EventArgs args)
    {
        var cursor = MainCursor;
        if (cursor.WindowPushed == thumb.Visual && IsEnabled)
        {
            UpdateThumbPositionToCursorDrag(cursor);
        }
    }
    #endregion


    protected virtual double ApplyValueConsideringSnapping(double newValue)
    {
        Value = newValue;
        return newValue;
    }

    protected virtual void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        if (Value > Maximum && Maximum >= Minimum)
        {
            Value = Maximum;
        }
    }
    protected virtual void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        if (Value < Minimum && Minimum <= Maximum)
        {
            Value = Minimum;
        }
    }

    protected virtual void OnValueChanged(double oldValue, double newValue) { }

    protected void RaiseValueChangeCompleted() => ValueChangeCompleted?.Invoke(this, EventArgs.Empty);

    protected void RaiseValueChangedByUi() => ValueChangedByUi?.Invoke(this, EventArgs.Empty);

#if FRB
    protected abstract void UpdateThumbPositionToCursorDrag(Cursor cursor);
#else
    protected abstract void UpdateThumbPositionToCursorDrag(ICursor cursor);
#endif
}