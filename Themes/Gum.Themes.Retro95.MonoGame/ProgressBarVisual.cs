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
using BaseProgressBarVisual = Gum.Forms.DefaultVisuals.V3.ProgressBarVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled ProgressBar visual. An inset-bevel white well, the same chrome as the
/// Slider track, with a Selection-colored indicator inside the bevel.
/// </summary>
public class ProgressBarVisual : BaseProgressBarVisual
{
    // The bevel's two edge strips plus one pixel of padding between the bevel and the indicator.
    private const float WellInset = 3f;

    private readonly Retro95Bevel _bevel;
    private readonly RectangleRuntime _indicator;

    public ProgressBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        TrackBackground.Parent = null;
        // The ProgressBar finds its indicator by name, so the replacement takes the name
        // and the V3 NineSlice leaves the tree.
        IndicatorInstance.Parent = null;

        // The bevel goes on the root behind the track; the track shrinks to sit inside it.
        TrackInstance.Parent = null;
        _bevel = Retro95Bevel.AddTo(this, BevelMode.Inset, Retro95Styling.ActiveStyle.Colors.WhiteFill);

        TrackInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        TrackInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        TrackInstance.XOrigin = HorizontalAlignment.Center;
        TrackInstance.YOrigin = VerticalAlignment.Center;
        TrackInstance.Width = -WellInset * 2;
        TrackInstance.Height = -WellInset * 2;
        AddChild(TrackInstance);

        _indicator = new RectangleRuntime();
        _indicator.Name = "IndicatorInstance";
        _indicator.Dock(Gum.Wireframe.Dock.Left);
        _indicator.Width = 0;
        _indicator.WidthUnits = DimensionUnitType.PercentageOfParent;
        _indicator.IsFilled = true;
        _indicator.FillColor = Retro95Styling.ActiveStyle.Colors.Selection;
        _indicator.StrokeWidth = 0;
        TrackInstance.AddChild(_indicator);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Gum.Forms.Controls.ProgressBar(this);
        }
    }

    /// <summary>The color of the well behind the indicator. Overrides the base property, whose NineSlice is not drawn.</summary>
    public override Color TrackColor
    {
        get => _bevel.Fill.FillColor;
        set => _bevel.SetFill(value);
    }

    /// <summary>The color of the indicator. Overrides the base property, whose NineSlice is not drawn.</summary>
    public override Color IndicatorColor
    {
        get => _indicator.FillColor;
        set => _indicator.FillColor = value;
    }
}
