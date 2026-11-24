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
using BaseProgressBarVisual = Gum.Forms.DefaultVisuals.V3.ProgressBarVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// DarkPro-styled ProgressBar visual. A Surface2 rounded track with a Border outline and an Accent indicator, the same look as the Slider track and fill.
/// </summary>
public class ProgressBarVisual : BaseProgressBarVisual
{
    private const float CornerRadius = 3f;
    private const float BorderThickness = 1f;

    private readonly RectangleRuntime _track;
    private readonly RectangleRuntime _indicator;
    private readonly RectangleRuntime _border;

    public ProgressBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        TrackBackground.Parent = null;
        // The ProgressBar finds its indicator by name, so the replacement takes the name
        // and the V3 NineSlice leaves the tree.
        IndicatorInstance.Parent = null;

        _track = new RectangleRuntime();
        _track.Name = "ProgressBarTrack";
        _track.Width = 0;
        _track.WidthUnits = DimensionUnitType.RelativeToParent;
        _track.Height = 0;
        _track.HeightUnits = DimensionUnitType.RelativeToParent;
        _track.CornerRadius = CornerRadius;
        _track.IsFilled = true;
        _track.FillColor = DarkProStyling.ActiveStyle.Colors.Surface2;
        _track.StrokeWidth = 0;
        TrackInstance.AddChild(_track);

        _indicator = new RectangleRuntime();
        _indicator.Name = "IndicatorInstance";
        _indicator.Dock(Gum.Wireframe.Dock.Left);
        _indicator.Width = 0;
        _indicator.WidthUnits = DimensionUnitType.PercentageOfParent;
        _indicator.CornerRadius = CornerRadius;
        _indicator.IsFilled = true;
        _indicator.FillColor = DarkProStyling.ActiveStyle.Colors.Accent;
        _indicator.StrokeWidth = 0;
        TrackInstance.AddChild(_indicator);

        // The border paints over the indicator so the indicator's ends sit inside the outline.
        _border = new RectangleRuntime();
        _border.Name = "ProgressBarBorder";
        _border.Width = 0;
        _border.WidthUnits = DimensionUnitType.RelativeToParent;
        _border.Height = 0;
        _border.HeightUnits = DimensionUnitType.RelativeToParent;
        _border.CornerRadius = CornerRadius;
        _border.IsFilled = false;
        _border.StrokeWidth = BorderThickness;
        _border.StrokeWidthUnits = DimensionUnitType.Absolute;
        _border.StrokeColor = DarkProStyling.ActiveStyle.Colors.Border;
        AddChild(_border);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Gum.Forms.Controls.ProgressBar(this);
        }
    }

    /// <summary>The color of the track. Replaces the base property, whose NineSlice is not drawn.</summary>
    public new Color TrackColor
    {
        get => _track.FillColor;
        set => _track.FillColor = value;
    }

    /// <summary>The color of the indicator. Replaces the base property, whose NineSlice is not drawn.</summary>
    public new Color IndicatorColor
    {
        get => _indicator.FillColor;
        set => _indicator.FillColor = value;
    }

    /// <summary>The color of the outline drawn over the track and indicator.</summary>
    public Color BorderColor
    {
        get => _border.StrokeColor;
        set => _border.StrokeColor = value;
    }
}
