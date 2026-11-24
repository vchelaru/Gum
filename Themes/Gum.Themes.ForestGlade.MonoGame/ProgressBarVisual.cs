using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using BaseProgressBarVisual = Gum.Forms.DefaultVisuals.V3.ProgressBarVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// ForestGlade-styled ProgressBar visual. A SliderTrack pill with a CanopyLit-to-SunPale gradient indicator, the same look as the Slider track and fill.
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
        _track.FillColor = ForestGladeStyling.ActiveStyle.Colors.SliderTrack;
        _track.StrokeWidth = 0;
        TrackInstance.AddChild(_track);

        _indicator = new RectangleRuntime();
        _indicator.Name = "IndicatorInstance";
        _indicator.Dock(Gum.Wireframe.Dock.Left);
        _indicator.Width = 0;
        _indicator.WidthUnits = DimensionUnitType.PercentageOfParent;
        _indicator.CornerRadius = CornerRadius;
        _indicator.IsFilled = true;
        _indicator.FillColor = ForestGladeStyling.ActiveStyle.Colors.CanopyLit;
        _indicator.StrokeWidth = 0;
        // Same left-to-right gradient as the Slider fill.
        _indicator.UseGradient = true;
        _indicator.GradientType = GradientType.Linear;
        _indicator.GradientX1Units = GeneralUnitType.PixelsFromSmall;
        _indicator.GradientY1Units = GeneralUnitType.PixelsFromMiddle;
        _indicator.GradientX1 = 0f;
        _indicator.GradientY1 = 0f;
        _indicator.GradientX2Units = GeneralUnitType.PixelsFromLarge;
        _indicator.GradientY2Units = GeneralUnitType.PixelsFromMiddle;
        _indicator.GradientX2 = 0f;
        _indicator.GradientY2 = 0f;
        _indicator.Color2 = ForestGladeStyling.ActiveStyle.Colors.SunPale;
        TrackInstance.AddChild(_indicator);

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
}
