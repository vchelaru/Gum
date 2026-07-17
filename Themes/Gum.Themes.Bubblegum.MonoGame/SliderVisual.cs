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

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled Slider visual. Replaces the V3 NineSlice track with an
/// 8 px AccentLight pill (matches <c>.bb-sldr-trk</c>), an Accent fill bar from
/// 0 to the thumb, and swaps the auto-created Button thumb for a
/// <see cref="SliderThumbVisual"/>.
/// </summary>
public class SliderVisual : BaseSliderVisual
{
    private const float ThumbSize = 20f;
    private const float TrackHeight = 8f;
    private const float TrackCornerRadius = 4f;

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
        track.Name = "BubblegumSliderTrack";
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
        track.FillColor = BubblegumStyling.ActiveStyle.Colors.AccentLight;
        track.StrokeWidth = 0;
        return track;
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "BubblegumSliderFill";
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
        fill.FillColor = BubblegumStyling.ActiveStyle.Colors.Accent;
        fill.StrokeWidth = 0;
        return fill;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyTrack(BubblegumStyling.ActiveStyle.Colors.AccentLight, BubblegumStyling.ActiveStyle.Colors.Accent);
        States.Highlighted.Apply = () => ApplyTrack(BubblegumStyling.ActiveStyle.Colors.AccentLight, BubblegumStyling.ActiveStyle.Colors.Accent);
        States.HighlightedFocused.Apply = () => ApplyTrack(BubblegumStyling.ActiveStyle.Colors.AccentLight, BubblegumStyling.ActiveStyle.Colors.Accent);
        States.Focused.Apply = () => ApplyTrack(BubblegumStyling.ActiveStyle.Colors.AccentLight, BubblegumStyling.ActiveStyle.Colors.Accent);
        States.Pushed.Apply = () => ApplyTrack(BubblegumStyling.ActiveStyle.Colors.AccentLight, BubblegumStyling.ActiveStyle.Colors.Accent);
        States.Disabled.Apply = () => ApplyTrack(BubblegumStyling.ActiveStyle.Colors.DisabledFill, BubblegumStyling.ActiveStyle.Colors.Disabled);
        States.DisabledFocused.Apply = () => ApplyTrack(BubblegumStyling.ActiveStyle.Colors.DisabledFill, BubblegumStyling.ActiveStyle.Colors.Disabled);
    }

    private void ApplyTrack(Color trackFill, Color fillBar)
    {
        _track.FillColor = trackFill;
        _fill.FillColor = fillBar;
    }
}
