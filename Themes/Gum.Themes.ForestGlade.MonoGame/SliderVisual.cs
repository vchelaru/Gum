using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseSliderVisual = Gum.Forms.DefaultVisuals.V3.SliderVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade Slider visual. 6 px deep canopy track with sun-pale border,
/// leaf-bright fill from 0 to the thumb position, and a swapped-in dewdrop
/// <see cref="SliderThumbVisual"/>.
/// </summary>
public class SliderVisual : BaseSliderVisual
{
    private const float ThumbSize = 18f;
    private const float TrackHeight = 6f;
    private const float TrackCornerRadius = 3f;

    private readonly RoundedRectangleRuntime _track;
    private readonly RoundedRectangleRuntime _fill;
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

    private static RoundedRectangleRuntime CreateTrack()
    {
        RoundedRectangleRuntime track = new RoundedRectangleRuntime();
        track.Name = "ForestGladeSliderTrack";
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
        track.Color = ForestGladePalette.SliderTrack;
        return track;
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "ForestGladeSliderFill";
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
        fill.Color = ForestGladeColors.LeafBright;
        return fill;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyTrack(ForestGladePalette.SliderTrack, ForestGladeColors.LeafBright);
        States.Highlighted.Apply = () => ApplyTrack(ForestGladePalette.SliderTrack, ForestGladeColors.LeafBright);
        States.HighlightedFocused.Apply = () => ApplyTrack(ForestGladePalette.SliderTrack, ForestGladeColors.LeafBright);
        States.Focused.Apply = () => ApplyTrack(ForestGladePalette.SliderTrack, ForestGladeColors.LeafBright);
        States.Pushed.Apply = () => ApplyTrack(ForestGladePalette.SliderTrack, ForestGladeColors.LeafBright);
        States.Disabled.Apply = () => ApplyTrack(ForestGladePalette.SliderDisabled, ForestGladePalette.SliderDisabled);
        States.DisabledFocused.Apply = () => ApplyTrack(ForestGladePalette.SliderDisabled, ForestGladePalette.SliderDisabled);
    }

    private void ApplyTrack(Color trackFill, Color fillBar)
    {
        _track.Color = trackFill;
        _fill.Color = fillBar;
    }
}
