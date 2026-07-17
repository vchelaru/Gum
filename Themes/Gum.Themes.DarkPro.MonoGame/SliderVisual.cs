using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseSliderVisual = Gum.Forms.DefaultVisuals.V3.SliderVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled Slider visual. Replaces the V3 SliderVisual's NineSlice track
/// with an Apos rounded-rect track (Surface2 fill + Border outline + 3px radius)
/// and swaps the auto-created Button thumb for a circular
/// <see cref="SliderThumbVisual"/>.
///
/// Deliberately deferred for v1 (flagged by user during design):
/// - The "broken line" effect (a 2px background-colored ring around the thumb
///   that visually breaks the track at the thumb position). Tracked as follow-up.
/// - The accent-colored fill bar covering the track from 0 to the thumb position.
///   Tracked as follow-up - needs dynamic width binding tied to slider Value.
/// </summary>
public class SliderVisual : BaseSliderVisual
{
    private const float ThumbSize = 16f;
    private const float TrackHeight = 5f;
    private const float TrackCornerRadius = 3f;
    private const float BorderThickness = 1f;

    private readonly RectangleRuntime _track;
    private readonly RectangleRuntime _trackBorder;
    private readonly RectangleRuntime _fill;
    private readonly SliderThumbVisual _thumb;

    public SliderVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        // Defer Forms-object construction so RangeBase doesn't wire itself to the
        // V3 base's auto-created Button thumb before we've had a chance to swap
        // it for our circle thumb.
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        // Adjust the track-vs-thumb margin to the smaller (16px) thumb so the
        // thumb can travel the full visual span.
        TrackInstance.Width = -ThumbSize;
        FocusedIndicator.Parent = null;

        // Replace the V3 NineSlice track background with the Apos rounded rect.
        TrackBackground.Parent = null;

        _track = CreateTrack();
        TrackInstance.AddChild(_track);

        _trackBorder = CreateTrackBorder();
        TrackInstance.AddChild(_trackBorder);

        // Accent fill sits on top of the track, from the left edge to the
        // thumb's center. Width is updated dynamically via Slider.ValueChanged.
        _fill = CreateFill();
        TrackInstance.AddChild(_fill);

        // Drop the V3-created Button thumb from the track and replace with our
        // circle thumb (named "ThumbInstance" so RangeBase can find it).
        ThumbInstance!.Parent = null;
        _thumb = new SliderThumbVisual();
        _thumb.Name = "ThumbInstance";
        _thumb.XUnits = GeneralUnitType.Percentage;
        _thumb.YUnits = GeneralUnitType.PixelsFromMiddle;
        _thumb.XOrigin = HorizontalAlignment.Center;
        _thumb.YOrigin = VerticalAlignment.Center;
        TrackInstance.AddChild(_thumb);

        WireStates();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Gum.Forms.Controls.Slider(this);
        }
    }

    /// <summary>
    /// Overridden so the ValueChanged hook is attached regardless of whether
    /// the Forms Slider is created internally (tryCreateFormsObject=true path,
    /// from the ctor) or externally (FrameworkElement ctor assigns it after
    /// constructing the visual with tryCreateFormsObject=false - the path
    /// taken by <c>new Slider()</c>). Without the override, the external path
    /// never wires up the fill-bar update and the fill stays at width 0.
    ///
    /// RangeBase.Value raises ValueChanged but not OnPropertyChanged, so
    /// INotifyPropertyChanged isn't a viable hook. Minimum/Maximum changes
    /// don't fire any subscribable event externally; consumers typically set
    /// them once before assigning Value, so the initial <see cref="UpdateFillWidth"/>
    /// call here plus subsequent ValueChanged events cover the practical cases.
    /// </summary>
    public override object FormsControlAsObject
    {
        get => base.FormsControlAsObject;
        set
        {
            if (base.FormsControlAsObject is Gum.Forms.Controls.Slider previous)
            {
                previous.ValueChanged -= HandleValueChanged;
            }
            base.FormsControlAsObject = value;
            if (value is Gum.Forms.Controls.Slider current)
            {
                current.ValueChanged += HandleValueChanged;
                UpdateFillWidth();
            }
        }
    }

    private void HandleValueChanged(object? sender, System.EventArgs e) => UpdateFillWidth();

    private void UpdateFillWidth()
    {
        if (FormsControlAsObject is not Gum.Forms.Controls.Slider slider)
        {
            return;
        }
        double range = slider.Maximum - slider.Minimum;
        if (range <= 0)
        {
            _fill.Width = 0;
            return;
        }
        double pct = (slider.Value - slider.Minimum) / range;
        if (pct < 0) pct = 0;
        if (pct > 1) pct = 1;
        _fill.Width = (float)(pct * 100.0);
    }

    private static RectangleRuntime CreateTrack()
    {
        RectangleRuntime track = new RectangleRuntime();
        track.Name = "DarkProSliderTrack";
        track.X = 0;
        track.Y = 0;
        track.XUnits = GeneralUnitType.PixelsFromMiddle;
        track.YUnits = GeneralUnitType.PixelsFromMiddle;
        track.XOrigin = HorizontalAlignment.Center;
        track.YOrigin = VerticalAlignment.Center;
        track.Width = 0;
        track.Height = TrackHeight;
        track.WidthUnits = DimensionUnitType.RelativeToParent;
        track.HeightUnits = DimensionUnitType.Absolute;
        track.CornerRadius = TrackCornerRadius;
        track.IsFilled = true;
        track.FillColor = DarkProStyling.ActiveStyle.Colors.Surface2;
        track.StrokeWidth = 0;
        return track;
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "DarkProSliderFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromSmall;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Left;
        fill.YOrigin = VerticalAlignment.Center;
        // Width is set in percent and updated dynamically by UpdateFillWidth.
        fill.Width = 0;
        fill.Height = TrackHeight;
        fill.WidthUnits = DimensionUnitType.PercentageOfParent;
        fill.HeightUnits = DimensionUnitType.Absolute;
        fill.CornerRadius = TrackCornerRadius;
        fill.IsFilled = true;
        fill.FillColor = DarkProStyling.ActiveStyle.Colors.Accent;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateTrackBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "DarkProSliderTrackBorder";
        border.X = 0;
        border.Y = 0;
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = TrackHeight;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.Absolute;
        border.CornerRadius = TrackCornerRadius;
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = DarkProStyling.ActiveStyle.Colors.Border;
        return border;
    }

    private void WireStates()
    {
        // The base's SetValuesForState pokes ThumbInstance.IsEnabled, but
        // ThumbInstance now points at the detached V3 button (no longer in the
        // tree). Override the state callbacks to color the track and let the
        // thumb manage its own appearance via the Button wrapping it.
        States.Enabled.Apply = () => ApplyTrack(DarkProStyling.ActiveStyle.Colors.Surface2, DarkProStyling.ActiveStyle.Colors.Border, DarkProStyling.ActiveStyle.Colors.Accent);
        States.Highlighted.Apply = () => ApplyTrack(DarkProStyling.ActiveStyle.Colors.Surface2, DarkProStyling.ActiveStyle.Colors.Border, DarkProStyling.ActiveStyle.Colors.Accent);
        States.HighlightedFocused.Apply = () => ApplyTrack(DarkProStyling.ActiveStyle.Colors.Surface2, DarkProStyling.ActiveStyle.Colors.Border, DarkProStyling.ActiveStyle.Colors.Accent);
        States.Focused.Apply = () => ApplyTrack(DarkProStyling.ActiveStyle.Colors.Surface2, DarkProStyling.ActiveStyle.Colors.Border, DarkProStyling.ActiveStyle.Colors.Accent);
        States.Pushed.Apply = () => ApplyTrack(DarkProStyling.ActiveStyle.Colors.Surface2, DarkProStyling.ActiveStyle.Colors.Border, DarkProStyling.ActiveStyle.Colors.Accent);
        States.Disabled.Apply = () => ApplyTrack(DarkProStyling.ActiveStyle.Colors.DisabledFill, DarkProStyling.ActiveStyle.Colors.DisabledBorder, DarkProStyling.ActiveStyle.Colors.DisabledThumb);
        States.DisabledFocused.Apply = () => ApplyTrack(DarkProStyling.ActiveStyle.Colors.DisabledFill, DarkProStyling.ActiveStyle.Colors.DisabledBorder, DarkProStyling.ActiveStyle.Colors.DisabledThumb);
    }

    private void ApplyTrack(Color trackFill, Color border, Color fillBar)
    {
        _track.FillColor = trackFill;
        _trackBorder.StrokeColor = border;
        _fill.FillColor = fillBar;
    }
}
