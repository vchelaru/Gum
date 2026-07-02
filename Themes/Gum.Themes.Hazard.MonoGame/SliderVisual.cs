using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseSliderVisual = Gum.Forms.DefaultVisuals.V3.SliderVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled Slider visual. Replaces the V3 NineSlice track with an Apos
/// rounded-rect track (built via <see cref="HazardShapes"/>) plus an accent fill
/// bar that tracks the value, and swaps the auto-created Button thumb for a circular
/// <see cref="SliderThumbVisual"/>.
/// </summary>
public class SliderVisual : BaseSliderVisual
{
    private const float ThumbSize = 16f;
    private const float TrackHeight = 5f;
    private const float TrackCornerRadius = 0f;
    private const float BorderThickness = 1f;

    private readonly RectangleRuntime _track;
    private readonly RectangleRuntime _trackBorder;
    private readonly RectangleRuntime _fill;
    private readonly SliderThumbVisual _thumb;

    public SliderVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        // Defer Forms-object construction so RangeBase doesn't wire itself to the V3
        // base's auto-created Button thumb before we swap in our circle thumb.
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        // Shrink the track-vs-thumb margin to the smaller (16px) thumb so the thumb
        // can travel the full visual span.
        TrackInstance.Width = -ThumbSize;
        FocusedIndicator.Parent = null;

        // Replace the V3 NineSlice track background. Track and border are full-width
        // shapes with a fixed height, so they come from HazardShapes with Height
        // overridden; the fill bar is left-anchored percentage geometry, built inline.
        TrackBackground.Parent = null;

        _track = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Surface2, TrackCornerRadius, "SliderTrack");
        _track.Height = TrackHeight;
        _track.HeightUnits = DimensionUnitType.Absolute;
        TrackInstance.AddChild(_track);

        _fill = CreateFillBar();
        TrackInstance.AddChild(_fill);

        _trackBorder = HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, TrackCornerRadius, BorderThickness, "SliderTrackBorder");
        _trackBorder.Height = TrackHeight;
        _trackBorder.HeightUnits = DimensionUnitType.Absolute;
        TrackInstance.AddChild(_trackBorder);

        // Drop the V3-created Button thumb and replace with our circle thumb (named
        // "ThumbInstance" so RangeBase can find it).
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
    /// Overridden so the ValueChanged hook is attached whether the Forms Slider is
    /// created here (tryCreateFormsObject=true) or externally (the FrameworkElement
    /// ctor assigns it after constructing the visual - the <c>new Slider()</c> path).
    /// RangeBase.Value raises ValueChanged but not OnPropertyChanged, so
    /// INotifyPropertyChanged isn't a viable hook; this setter is the one place that
    /// works for both construction paths.
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

    private static RectangleRuntime CreateFillBar()
    {
        // Left-anchored, fixed-height, percentage-width: doesn't fit the centered
        // full-parent shape HazardShapes builds, so it's constructed directly. Width
        // is driven by UpdateFillWidth from the slider value.
        RectangleRuntime fill = new RectangleRuntime { Name = "SliderFill" };
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromSmall;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Left;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = TrackHeight;
        fill.WidthUnits = DimensionUnitType.PercentageOfParent;
        fill.HeightUnits = DimensionUnitType.Absolute;
        fill.CornerRadius = TrackCornerRadius;
        fill.IsFilled = true;
        fill.FillColor = HazardStyling.ActiveStyle.Colors.Accent;
        fill.StrokeWidth = 0;
        return fill;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyTrack(HazardStyling.ActiveStyle.Colors.Surface2, HazardStyling.ActiveStyle.Colors.Border, HazardStyling.ActiveStyle.Colors.Accent);
        States.Highlighted.Apply = () => ApplyTrack(HazardStyling.ActiveStyle.Colors.Surface2, HazardStyling.ActiveStyle.Colors.Border, HazardStyling.ActiveStyle.Colors.Accent);
        States.HighlightedFocused.Apply = () => ApplyTrack(HazardStyling.ActiveStyle.Colors.Surface2, HazardStyling.ActiveStyle.Colors.Border, HazardStyling.ActiveStyle.Colors.Accent);
        States.Focused.Apply = () => ApplyTrack(HazardStyling.ActiveStyle.Colors.Surface2, HazardStyling.ActiveStyle.Colors.Border, HazardStyling.ActiveStyle.Colors.Accent);
        States.Pushed.Apply = () => ApplyTrack(HazardStyling.ActiveStyle.Colors.Surface2, HazardStyling.ActiveStyle.Colors.Border, HazardStyling.ActiveStyle.Colors.Accent);
        States.Disabled.Apply = () => ApplyTrack(HazardStyling.ActiveStyle.Colors.DisabledFill, HazardStyling.ActiveStyle.Colors.DisabledBorder, HazardStyling.ActiveStyle.Colors.DisabledAccent);
        States.DisabledFocused.Apply = () => ApplyTrack(HazardStyling.ActiveStyle.Colors.DisabledFill, HazardStyling.ActiveStyle.Colors.DisabledBorder, HazardStyling.ActiveStyle.Colors.DisabledAccent);
    }

    private void ApplyTrack(Color trackFill, Color border, Color fillBar)
    {
        _track.FillColor = trackFill;
        _trackBorder.StrokeColor = border;
        _fill.FillColor = fillBar;
    }
}
