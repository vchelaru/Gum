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

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled ProgressBar visual. A PeachDark pill track with a Coral indicator, the same look as the Slider track and fill.
/// </summary>
public class ProgressBarVisual : BaseProgressBarVisual
{
    private const float CornerRadius = 8f;

    private readonly RectangleRuntime _track;
    private readonly RectangleRuntime _indicator;

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
        _track.FillColor = MeadowStyling.ActiveStyle.Colors.PeachDark;
        _track.StrokeWidth = 0;
        TrackInstance.AddChild(_track);

        _indicator = new RectangleRuntime();
        _indicator.Name = "IndicatorInstance";
        _indicator.Dock(Gum.Wireframe.Dock.Left);
        _indicator.Width = 0;
        _indicator.WidthUnits = DimensionUnitType.PercentageOfParent;
        _indicator.CornerRadius = CornerRadius;
        _indicator.IsFilled = true;
        _indicator.FillColor = MeadowStyling.ActiveStyle.Colors.Coral;
        _indicator.StrokeWidth = 0;
        TrackInstance.AddChild(_indicator);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Gum.Forms.Controls.ProgressBar(this);
        }
    }

    /// <summary>The color of the track. Overrides the base property, whose NineSlice is not drawn.</summary>
    public override Color TrackColor
    {
        get => _track.FillColor;
        set => _track.FillColor = value;
    }

    /// <summary>The color of the indicator. Overrides the base property, whose NineSlice is not drawn.</summary>
    public override Color IndicatorColor
    {
        get => _indicator.FillColor;
        set => _indicator.FillColor = value;
    }
}
