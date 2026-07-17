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
using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled ListBox visual. A cream panel with a 2.5 px <b>dashed</b> peach
/// outline at 16 px corner radius (matches <c>.pp-lb</c>); the focus state simply
/// deepens the dashed stroke to sage. The dashed stroke is rendered natively by
/// Apos.Shapes (<c>StrokeDashLength</c> / <c>StrokeGapLength</c>), not faked with
/// per-edge rectangles.
/// </summary>
public class ListBoxVisual : BaseListBoxVisual
{
    private const float CornerRadius = 16f;
    private const float BorderThickness = 2.5f;
    private const float DashLength = 5f;
    private const float GapLength = 4f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ClipAndScrollContainer.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        // ClipAndScrollContainer goes between fill and border so the rounded
        // dashed border paints on top of clipped row content (Gum clips to a
        // rectangle, so rows would otherwise poke past the rounded corners).
        AddChild(ClipAndScrollContainer);

        _border = CreateBorder();
        AddChild(_border);

        if (VerticalScrollBarInstance != null)
        {
            VerticalScrollBarInstance.X = -2f;
        }

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowListBoxFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.CornerRadius = CornerRadius;
        fill.IsFilled = true;
        fill.FillColor = MeadowStyling.ActiveStyle.Colors.Cream2;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "MeadowListBoxBorder";
        border.X = 0;
        border.Y = 0;
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.CornerRadius = CornerRadius;
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = MeadowStyling.ActiveStyle.Colors.PeachDark;
        border.StrokeDashLength = DashLength;
        border.StrokeGapLength = GapLength;
        return border;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => _border.StrokeColor = MeadowStyling.ActiveStyle.Colors.PeachDark;
        States.Highlighted.Apply = () => _border.StrokeColor = MeadowStyling.ActiveStyle.Colors.PeachDark;
        States.Focused.Apply = () => _border.StrokeColor = MeadowStyling.ActiveStyle.Colors.SageDark;
        States.HighlightedFocused.Apply = () => _border.StrokeColor = MeadowStyling.ActiveStyle.Colors.SageDark;
        States.Pushed.Apply = () => _border.StrokeColor = MeadowStyling.ActiveStyle.Colors.SageDark;
        States.Disabled.Apply = () => _border.StrokeColor = MeadowStyling.ActiveStyle.Colors.Disabled;
        States.DisabledFocused.Apply = () => _border.StrokeColor = MeadowStyling.ActiveStyle.Colors.Disabled;
    }
}
