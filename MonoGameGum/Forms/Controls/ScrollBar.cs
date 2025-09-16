using Gum.Wireframe;
using System;



#if FRB
using Microsoft.Xna.Framework;
using FlatRedBall.Gui;
using FlatRedBall.Forms.GumExtensions;
using FlatRedBall.Forms.Controls.Primitives;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#elif RAYLIB
#else
using Microsoft.Xna.Framework;
using MonoGameGum.Input;
#endif

#if !FRB
using Gum.Forms.Controls.Primitives;
namespace Gum.Forms.Controls;
#endif

public class ScrollBar : RangeBase
{
    #region Fields/Properties

    Button upButton;
    Button downButton;

    public Button? UpButton => upButton;
    public Button? DownButton => downButton;
    public float MinimumThumbSize { get; set; } = 16;

    double viewportSize = .1;
    public double ViewportSize
    {
        get { return viewportSize; }
        set
        {
#if DEBUG
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
        Orientation == Orientation.Vertical
        ? Track.GetAbsoluteHeight() - thumb.ActualHeight
        : Track.GetAbsoluteWidth() - thumb.ActualWidth;


    public override Orientation Orientation 
    { 
        get => base.Orientation; 
        set
        {
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
        RefreshInternalVisualReferences();

        base.ReactToVisualChanged();


        var thumbHeight = thumb?.ActualHeight ?? 32;

        if(upButton != null)
        {
            upButton.Push += (not, used) => this.Value -= this.SmallChange;
        }

        if(downButton != null)
        {
            downButton.Push += (not, used) => this.Value += this.SmallChange;
        }

        if(Track != null)
        {
    #if FRB
            Track.Push += _ => HandleTrackPush(this, EventArgs.Empty);
    #else
            Track.Push += HandleTrackPush;
    #endif
        }
        Visual.SizeChanged += HandleVisualSizeChange;



        var visibleTrackSpace = upButton != null && downButton != null
            ? Track.GetAbsoluteHeight() - upButton.ActualHeight - downButton.ActualHeight
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

    protected override void RefreshInternalVisualReferences()
    {
        var upButtonVisual = this.Visual.GetGraphicalUiElementByName("UpButtonInstance") as InteractiveGue;
#if DEBUG
        //if (upButtonVisual == null)
        //{
        //    throw new Exception("The ScrollBar Gum object must have a button called UpButtonInstance");
        //}
#endif
        if(upButtonVisual != null)
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
#if DEBUG
        //if (downButtonVisual == null)
        //{
        //    throw new Exception("The ScrollBar Gum object must have a button called DownButtonInstance");
        //}
#endif
        if(downButtonVisual != null)
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
    protected override void HandleThumbPush(object sender, EventArgs e)
    {
        if(Orientation == Orientation.Vertical)
        {
            var topOfThumb = this.thumb.AbsoluteTop;
            var cursorScreen = MainCursor.YRespectingGumZoomAndBounds();

            cursorGrabOffsetRelativeToThumb = cursorScreen - topOfThumb;
        }
        else
        {
            var leftOfThumb = this.thumb.AbsoluteLeft;
            var cursorScreen = MainCursor.XRespectingGumZoomAndBounds();

            cursorGrabOffsetRelativeToThumb = cursorScreen - leftOfThumb;
        }
    }

    private void HandleTrackPush(object sender, EventArgs args)
    {
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
            else if(MainCursor.XRespectingGumZoomAndBounds() > thumb.AbsoluteLeft + thumb.ActualHeight)
            {
                Value += LargeChange;
            }
        }
    }

    private void HandleVisualSizeChange(object sender, EventArgs e)
    {
        UpdateThumbPositionAccordingToValue();
        UpdateThumbSize();
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
        if (Orientation == Orientation.Vertical)
        {
            var cursorScreenY = cursor.YRespectingGumZoomAndBounds();
            var cursorYRelativeToTrack = cursorScreenY - Track.AbsoluteTop;

            thumb.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            thumb.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

            thumb.Y = cursorYRelativeToTrack - cursorGrabOffsetRelativeToThumb;
        }
        else
        {
            var cursorScreenX = cursor.XRespectingGumZoomAndBounds();
            var cursorXRelativeToTrack = cursorScreenX - Track.AbsoluteLeft;

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
        if (ViewportSize != 0 && Track != null)
        {

            var valueRange = (Maximum - Minimum) + ViewportSize;
            if (valueRange > 0)
            {
                var thumbRatio = ViewportSize / valueRange;

                if(Orientation == Orientation.Vertical)
                {
                    float trackSize =  Track.GetAbsoluteHeight();
                    thumb.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
                    thumb.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

                    thumb.Height = System.Math.Max(MinimumThumbSize, (float)(trackSize * thumbRatio));
                }
                else
                {
                    float trackSize =  Track.GetAbsoluteWidth();
                    thumb.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
                    thumb.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

                    thumb.Width = System.Math.Max(MinimumThumbSize, (float)(trackSize * thumbRatio));
                }
            }
        }
    }

#endregion

    private void UpdateOrientationState()
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


        UpdateThumbSize();
        UpdateThumbPositionAccordingToValue();
    }
}
