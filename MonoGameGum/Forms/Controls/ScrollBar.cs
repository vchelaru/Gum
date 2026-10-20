using Gum.Wireframe;
using System;



#if FRB
using Microsoft.Xna.Framework;
using FlatRedBall.Gui;
using FlatRedBall.Forms.GumExtensions;
using FlatRedBall.Forms.Controls.Primitives;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
using Gum.Forms.Controls.Primitives;
namespace Gum.Forms.Controls;
#endif

public class ScrollBar : RangeBase
{
    #region Fields/Properties

    Button? upButton;
    Button? downButton;

    // The buttons whose Push/Click we are subscribed to, so they can be unhooked when the Visual
    // changes even though upButton/downButton are re-resolved from the new Visual.
    Button? _subscribedUpButton;
    Button? _subscribedDownButton;

    public Button? UpButton => upButton;
    public Button? DownButton => downButton;
    public float MinimumThumbSize { get; set; } = 16;

    // The Track whose SizeChanged we are currently subscribed to. Stored as a field rather than
    // re-resolved through the Track property at unsubscribe time so that if Visual (and therefore
    // Track) is swapped out, we unhook from the original Track instance rather than leaking the
    // subscription. See issue #2781 — thumb size + position both read Track absolute dimensions,
    // so any Track resize (parent resize, sibling resize, layout reflow, direct manipulation)
    // must re-run UpdateThumbSize + UpdateThumbPositionAccordingToValue. The previous hook on
    // Visual.SizeChanged missed any case where Track changed without the ScrollBar's own Visual
    // also changing.
    InteractiveGue? _subscribedTrack;

    double viewportSize = .1;
    public double ViewportSize
    {
        get { return viewportSize; }
        set
        {
#if FULL_DIAGNOSTICS
            if (double.IsNaN(value))
            {
                throw new Exception("ScrollBar ViewportSize cannot be float.NaN");
            }
#endif
            viewportSize = value;

            UpdateThumbSize();
            UpdateThumbPositionAccordingToValue();

        }
    }


    float MinThumbPosition => 0;
    float MaxThumbPosition =>
        Track is not { } track || thumb == null ? 0
        : Orientation == Orientation.Vertical
        ? track.AbsoluteHeight - thumb.ActualHeight
        : track.AbsoluteWidth - thumb.ActualWidth;


    /// <inheritdoc/>
    public override bool IsEnabled
    {
        get => base.IsEnabled;
        set
        {
            base.IsEnabled = value;
            ApplyIsEnabledToButtons();
        }
    }

    public override Orientation Orientation 
    { 
        get => base.Orientation; 
        set
        {
            hasAssignedOrientation = true;

            if(value != Orientation)
            {
                base.Orientation = value;

                UpdateOrientationState();
            }
        }
    }

    #endregion

    #region Initialize

    public ScrollBar() : base() 
    {
        base.Orientation = Orientation.Vertical;
    }

    public ScrollBar(InteractiveGue visual) : base(visual) 
    {
        base.Orientation = Orientation.Vertical;
    }

    bool hasAssignedOrientation = false;
    protected override void ReactToVisualChanged()
    {
        if(!hasAssignedOrientation)
        {
            // default it!
            Orientation = Orientation.Vertical;
        }
        else
        {
            // The Orientation is kept across a Visual change, but the new Visual
            // has not had the matching state applied yet.
            ApplyOrientationStateToVisual();
        }
        RefreshInternalVisualReferences();
        ApplyIsEnabledToButtons();

        base.ReactToVisualChanged();


        var thumbHeight = thumb?.ActualHeight ?? 32;

        if(upButton != null)
        {
            // Push gives mouse "press and hold to keep scrolling" behavior.
            // Click is also wired specifically for keyboard activation
            // (Enter / Space when the arrow button is focused). The filter has
            // to discriminate keyboard from mouse — and the right test is
            // narrow: "InputEventArgs *and* the input device is a keyboard."
            // A bare `e is InputEventArgs` check looked correct on paper but
            // didn't reliably exclude the mouse path, producing a double
            // scroll per click. Pinning to the keyboard device type leaves
            // the mouse path untouched.
            _subscribedUpButton = upButton;
            upButton.Push += HandleUpButtonPush;
            upButton.Click += HandleUpButtonClick;
        }

        if(downButton != null)
        {
            _subscribedDownButton = downButton;
            downButton.Push += HandleDownButtonPush;
            downButton.Click += HandleDownButtonClick;
        }

        // Subscribe to Track.SizeChanged rather than Visual.SizeChanged because Track size is what
        // UpdateThumbSize / UpdateThumbPositionAccordingToValue actually read. If Track fills Visual
        // (the common case) a Visual resize propagates to Track and fires this anyway. If Track is
        // sized independently of Visual (e.g. by sibling changes or absolute sizing) we still react.
        // Hooking only Track is strictly more correct than hooking Visual. Field-tracked for clean
        // unsubscribe on visual change/removal.
        if (Track is { } track)
        {
            _subscribedTrack = track;
            track.SizeChanged += HandleTrackSizeChanged;
#if FRB
            track.Push += HandleScrollBarTrackPushFrb;
#else
            track.Push += HandleTrackPush;
#endif
        }



        var visibleTrackSpace = Track != null && upButton != null && downButton != null
            ? Track.AbsoluteHeight - upButton.ActualHeight - downButton.ActualHeight
            : 0;

        if (visibleTrackSpace != 0)
        {
            var thumbRatio = thumbHeight / visibleTrackSpace;

            ViewportSize = (Maximum - Minimum) * thumbRatio;
            if (ViewportSize <= 0)
            {
                LargeChange = 10;
            }
            else
            {
                LargeChange = ViewportSize;
            }

            Value = Minimum;
        }
        else
        {
            ViewportSize = 10;
            LargeChange = 10;
            SmallChange = 2;
        }
    }

    // NOTE: This overrides RangeBase's hiding `virtual`, NOT FrameworkElement's original `virtual`.
    // This means ScrollBar.RefreshInternalVisualReferences runs twice — once from ScrollBar.ReactToVisualChanged
    // and once from RangeBase.ReactToVisualChanged. The lookups are idempotent so this is harmless but wasteful.
    // RangeBase.RefreshInternalVisualReferences (AssignExplicitTrack) never runs for ScrollBar because this
    // override doesn't call base. ScrollBar.Track works via its fallback path (thumb.Visual.EffectiveParentGue).
    // See the comment on RangeBase.RefreshInternalVisualReferences for why this isn't changed to a proper chain.
    protected override void RefreshInternalVisualReferences()
    {
        upButton = null;
        downButton = null;

        var upButtonVisual = this.Visual.GetGraphicalUiElementByName("UpButtonInstance") as InteractiveGue;
#if FULL_DIAGNOSTICS
        //if (upButtonVisual == null)
        //{
        //    throw new Exception("The ScrollBar Gum object must have a button called UpButtonInstance");
        //}
#endif
        if (upButtonVisual != null)
        {
            if (upButtonVisual.FormsControlAsObject == null)
            {
                upButton = new Button(upButtonVisual);
            }
            else
            {
                upButton = upButtonVisual.FormsControlAsObject as Button;
            }
        }

        var downButtonVisual = this.Visual.GetGraphicalUiElementByName("DownButtonInstance") as InteractiveGue;
#if FULL_DIAGNOSTICS
        //if (downButtonVisual == null)
        //{
        //    throw new Exception("The ScrollBar Gum object must have a button called DownButtonInstance");
        //}
#endif
        if (downButtonVisual != null)
        {
            if (downButtonVisual.FormsControlAsObject == null)
            {
                downButton = new Button(downButtonVisual);
            }
            else
            {
                downButton = downButtonVisual.FormsControlAsObject as Button;
            }
        }
    }

#endregion

    #region Event Handlers
    protected override void HandleThumbPush(object? sender, EventArgs e)
    {
        if(Orientation == Orientation.Vertical)
        {
            var topOfThumb = this.thumb!.AbsoluteTop;
            var cursorScreen = MainCursor.YRespectingGumZoomAndBounds();

            cursorGrabOffsetRelativeToThumb = cursorScreen - topOfThumb;
        }
        else
        {
            var leftOfThumb = this.thumb!.AbsoluteLeft;
            var cursorScreen = MainCursor.XRespectingGumZoomAndBounds();

            cursorGrabOffsetRelativeToThumb = cursorScreen - leftOfThumb;
        }
    }

    private void HandleUpButtonPush(object? sender, EventArgs args) => this.Value -= this.SmallChange;

    private void HandleDownButtonPush(object? sender, EventArgs args) => this.Value += this.SmallChange;

    private void HandleUpButtonClick(object? sender, EventArgs e)
    {
#if !FRB
        if (e is InputEventArgs iea && iea.InputDevice is IInputReceiverKeyboard)
        {
            this.Value -= this.SmallChange;
        }
#endif
    }

    private void HandleDownButtonClick(object? sender, EventArgs e)
    {
#if !FRB
        if (e is InputEventArgs iea && iea.InputDevice is IInputReceiverKeyboard)
        {
            this.Value += this.SmallChange;
        }
#endif
    }

#if FRB
    // Named wrapper rather than a lambda so it can be unsubscribed.
    private void HandleScrollBarTrackPushFrb(IWindow _) => HandleTrackPush(this, EventArgs.Empty);
#endif

    private void HandleTrackPush(object? sender, EventArgs args)
    {
        // Early out, can't calulate without this
        if (thumb == null)
            return;

        if(Orientation == Orientation.Vertical)
        {
            if (MainCursor.YRespectingGumZoomAndBounds() < thumb.AbsoluteTop)
            {
                Value -= LargeChange;
            }
            else if (MainCursor.YRespectingGumZoomAndBounds() > thumb.AbsoluteTop + thumb.ActualHeight)
            {
                Value += LargeChange;
            }
        }
        else
        {
            if(MainCursor.XRespectingGumZoomAndBounds() < thumb.AbsoluteLeft)
            {
                Value -= LargeChange;
            }
            else if(MainCursor.XRespectingGumZoomAndBounds() > thumb.AbsoluteLeft + thumb.ActualWidth)
            {
                Value += LargeChange;
            }
        }
    }

    private void HandleTrackSizeChanged(object? sender, EventArgs e)
    {
        UpdateThumbSize();
        UpdateThumbPositionAccordingToValue();
    }

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();

        if (_subscribedUpButton != null)
        {
            _subscribedUpButton.Push -= HandleUpButtonPush;
            _subscribedUpButton.Click -= HandleUpButtonClick;
            _subscribedUpButton = null;
        }

        if (_subscribedDownButton != null)
        {
            _subscribedDownButton.Push -= HandleDownButtonPush;
            _subscribedDownButton.Click -= HandleDownButtonClick;
            _subscribedDownButton = null;
        }

        if (_subscribedTrack != null)
        {
            _subscribedTrack.SizeChanged -= HandleTrackSizeChanged;
#if FRB
            _subscribedTrack.Push -= HandleScrollBarTrackPushFrb;
#else
            _subscribedTrack.Push -= HandleTrackPush;
#endif
            _subscribedTrack = null;
        }
    }

    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);

        UpdateThumbSize();
        UpdateThumbPositionAccordingToValue();
    }

    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);

        UpdateThumbSize();
        UpdateThumbPositionAccordingToValue();
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);

        UpdateThumbPositionAccordingToValue();
    }

    #endregion

    #region UpdateTo Methods

    /// <inheritdoc/>
    public override void ApplyRuntimeProperties()
    {
        UpdateThumbPositionAccordingToValue();
        base.ApplyRuntimeProperties();
    }

    private void UpdateThumbPositionAccordingToValue()
    {
        if(thumb == null)
        {
            return;
        }
        var ratioDown = (Value - Minimum) / (Maximum - Minimum);
        ratioDown = System.Math.Max(0, ratioDown);
        ratioDown = System.Math.Min(1, ratioDown);
        if (Maximum <= Minimum)
        {
            ratioDown = 0;
        }

        var range = MaxThumbPosition - MinThumbPosition;

        if(Orientation == Orientation.Vertical)
        {
            thumb.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            thumb.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

            thumb.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;

            thumb.Y = MinThumbPosition + range * (float)ratioDown;
        }
        else
        {
            thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            thumb.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

            thumb.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;

            thumb.X = MinThumbPosition + range * (float)ratioDown;
        }

    }

#if FRB
    protected override void UpdateThumbPositionToCursorDrag(Cursor cursor)
#else
    protected override void UpdateThumbPositionToCursorDrag(ICursor cursor)
#endif
    {
        // Only called while the thumb is being dragged.
        if (thumb == null || Track is not { } track)
        {
            return;
        }

        if (Orientation == Orientation.Vertical)
        {
            var cursorScreenY = cursor.YRespectingGumZoomAndBounds();
            var cursorYRelativeToTrack = cursorScreenY - track.AbsoluteTop;

            thumb.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            thumb.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

            thumb.Y = cursorYRelativeToTrack - cursorGrabOffsetRelativeToThumb;
        }
        else
        {
            var cursorScreenX = cursor.XRespectingGumZoomAndBounds();
            var cursorXRelativeToTrack = cursorScreenX - track.AbsoluteLeft;

            thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            thumb.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

            thumb.X = cursorXRelativeToTrack - cursorGrabOffsetRelativeToThumb;
        }

        float range = MaxThumbPosition - MinThumbPosition;

        var valueBefore = Value;
        if (range != 0)
        {
            var ratio = Orientation == Orientation.Vertical
                ? (thumb.Y) / range
                : (thumb.X) / range;

            var ratioBefore = ratio;
            ratio = System.Math.Max(0, ratio);
            ratio = System.Math.Min(1, ratio);


            Value = Minimum + (Maximum - Minimum) * ratio;

            if (valueBefore != Value)
            {
                RaiseValueChangedByUi();
            }

            if (ratioBefore != ratio)
            {
                // we clamped it, so force the thumb:
                UpdateThumbPositionAccordingToValue();
            }
        }
        else
        {
            // In this case the user may have dragged the thumb outside of its bounds. We are resetting
            // the value back to the minimum, but the value may already be 0, so the if check will bypass
            // the updating of the value...
            var shouldForceUpdateThumb = Value == Minimum;

            Value = Minimum;

            if (valueBefore != Value)
            {
                RaiseValueChangedByUi();
            }

            if (shouldForceUpdateThumb)
            {
                UpdateThumbPositionAccordingToValue();
            }
        }
    }

    private void UpdateThumbSize()
    {
        var desiredHeight = MinimumThumbSize;
        // Track falls back to the thumb's parent, so a thumb exists whenever Track does.
        if (ViewportSize != 0 && Track is { } track && thumb != null)
        {

            var valueRange = (Maximum - Minimum) + ViewportSize;
            if (valueRange > 0)
            {
                var thumbRatio = ViewportSize / valueRange;

                if(Orientation == Orientation.Vertical)
                {
                    float trackSize =  track.AbsoluteHeight;
                    thumb.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
                    thumb.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

                    // The track length wins over MinimumThumbSize so the thumb always fits in the track.
                    thumb.Height = System.Math.Min(trackSize, System.Math.Max(MinimumThumbSize, (float)(trackSize * thumbRatio)));
                }
                else
                {
                    float trackSize =  track.AbsoluteWidth;
                    thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
                    thumb.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

                    thumb.Width = System.Math.Min(trackSize, System.Math.Max(MinimumThumbSize, (float)(trackSize * thumbRatio)));
                }
            }
        }
    }

#endregion

    private void ApplyIsEnabledToButtons()
    {
        if (upButton != null)
        {
            upButton.IsEnabled = IsEnabled;
        }

        if (downButton != null)
        {
            downButton.IsEnabled = IsEnabled;
        }
    }

    private void UpdateOrientationState()
    {
        ApplyOrientationStateToVisual();

        UpdateThumbSize();
        UpdateThumbPositionAccordingToValue();
    }

    private void ApplyOrientationStateToVisual()
    {
        var categoryName = "OrientationCategory";

        string state = string.Empty;
        if(Orientation == Orientation.Horizontal)
        {
            state = "Horizontal";
        }
        else
        {
            state = "Vertical";
        }

        Visual.SetProperty(categoryName + "State", state);
    }
}
