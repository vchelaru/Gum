using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseTooltipVisual = Gum.Forms.DefaultVisuals.V3.TooltipVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled Tooltip visual. Pale-yellow info-bubble fill with a 1 px black
/// outline — the canonical Win95 tooltip look. Passive overlay; no state callbacks.
/// </summary>
public class TooltipVisual : BaseTooltipVisual
{
    // Win95 tooltip yellow ≈ #FFFFE1.
    private static readonly Color TooltipFill = new Color(255, 255, 225);

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _outerTop;
    private readonly RectangleRuntime _outerBottom;
    private readonly RectangleRuntime _outerLeft;
    private readonly RectangleRuntime _outerRight;

    public TooltipVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        TextInstance.Parent = null;

        _fill = NewStretched("Retro95TooltipFill", TooltipFill);
        AddChild(_fill);

        _outerTop = NewEdge("Retro95TooltipOuterTop", horizontal: true, top: true);
        _outerBottom = NewEdge("Retro95TooltipOuterBottom", horizontal: true, top: false);
        _outerLeft = NewEdge("Retro95TooltipOuterLeft", horizontal: false, top: true);
        _outerRight = NewEdge("Retro95TooltipOuterRight", horizontal: false, top: false);
        AddChild(_outerTop);
        AddChild(_outerBottom);
        AddChild(_outerLeft);
        AddChild(_outerRight);

        AddChild(TextInstance);
        TextInstance.Color = Retro95Styling.ActiveStyle.Colors.Text;
    }

    private static RectangleRuntime NewStretched(string name, Color color)
    {
        RectangleRuntime r = new RectangleRuntime();
        r.Name = name;
        r.X = 0; r.Y = 0;
        r.XUnits = GeneralUnitType.PixelsFromMiddle;
        r.YUnits = GeneralUnitType.PixelsFromMiddle;
        r.XOrigin = HorizontalAlignment.Center;
        r.YOrigin = VerticalAlignment.Center;
        r.Width = 0; r.Height = 0;
        r.WidthUnits = DimensionUnitType.RelativeToParent;
        r.HeightUnits = DimensionUnitType.RelativeToParent;
        r.IsFilled = true;
        r.FillColor = color;
        r.StrokeWidth = 0;
        return r;
    }

    private static RectangleRuntime NewEdge(string name, bool horizontal, bool top)
    {
        RectangleRuntime r = new RectangleRuntime();
        r.Name = name;
        r.IsFilled = true;
        r.FillColor = Retro95Styling.ActiveStyle.Colors.Text;
        r.StrokeWidth = 0;
        if (horizontal)
        {
            r.X = 0;
            r.Y = 0;
            r.XUnits = GeneralUnitType.PixelsFromMiddle;
            r.YUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            r.XOrigin = HorizontalAlignment.Center;
            r.YOrigin = top ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            r.Width = 0;
            r.Height = 1f;
            r.WidthUnits = DimensionUnitType.RelativeToParent;
            r.HeightUnits = DimensionUnitType.Absolute;
        }
        else
        {
            r.X = 0;
            r.Y = 0;
            r.XUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            r.YUnits = GeneralUnitType.PixelsFromMiddle;
            r.XOrigin = top ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            r.YOrigin = VerticalAlignment.Center;
            r.Width = 1f;
            r.Height = 0;
            r.WidthUnits = DimensionUnitType.Absolute;
            r.HeightUnits = DimensionUnitType.RelativeToParent;
        }
        return r;
    }
}
