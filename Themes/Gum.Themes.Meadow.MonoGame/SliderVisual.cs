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

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled Slider visual. A 14 px peach pill track (matches
/// <c>.pp-sldr-trk</c>) with a coral fill bar from 0 to the thumb, and a white
/// circular <see cref="SliderThumbVisual"/> in place of the V3 Button thumb.
/// </summary>
public class SliderVisual : BaseSliderVisual
{
    private const float ThumbSize = 22f;
    private const float TrackHeight = 14f;
    private const float TrackCornerRadius = 7f;

    private readonly RectangleRuntime _track;
    private readonly RectangleRuntime _fill;
    private readonly SliderThumbVisual _thumb;

    public SliderVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        TrackInstance.Width = -ThumbSize;
        FocusedIndicator.Parent = null;

        TrackBackground.Parent = null;

        _track = CreateTrack();
        TrackInstance.AddChild(_track);

        _fill = CreateFill();
        TrackInstance.AddChild(_fill);

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

    /// <inheritdoc/>
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
        track.Name = "MeadowSliderTrack";
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
        track.FillColor = MeadowStyling.ActiveStyle.Colors.PeachDark;
        track.StrokeWidth = 0;
        return track;
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowSliderFill";
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
        fill.FillColor = MeadowStyling.ActiveStyle.Colors.Coral;
        fill.StrokeWidth = 0;
        return fill;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyTrack(MeadowStyling.ActiveStyle.Colors.PeachDark, MeadowStyling.ActiveStyle.Colors.Coral);
        States.Highlighted.Apply = () => ApplyTrack(MeadowStyling.ActiveStyle.Colors.PeachDark, MeadowStyling.ActiveStyle.Colors.Coral);
        States.HighlightedFocused.Apply = () => ApplyTrack(MeadowStyling.ActiveStyle.Colors.PeachDark, MeadowStyling.ActiveStyle.Colors.Coral);
        States.Focused.Apply = () => ApplyTrack(MeadowStyling.ActiveStyle.Colors.PeachDark, MeadowStyling.ActiveStyle.Colors.Coral);
        States.Pushed.Apply = () => ApplyTrack(MeadowStyling.ActiveStyle.Colors.PeachDark, MeadowStyling.ActiveStyle.Colors.Coral);
        States.Disabled.Apply = () => ApplyTrack(MeadowStyling.ActiveStyle.Colors.Disabled, MeadowStyling.ActiveStyle.Colors.DisabledSliderFill);
        States.DisabledFocused.Apply = () => ApplyTrack(MeadowStyling.ActiveStyle.Colors.Disabled, MeadowStyling.ActiveStyle.Colors.DisabledSliderFill);
    }

    private void ApplyTrack(Color trackFill, Color fillBar)
    {
        _track.FillColor = trackFill;
        _fill.FillColor = fillBar;
    }
}
