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
using BaseWindowVisual = Gum.Forms.DefaultVisuals.V3.WindowVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade Window visual — a wooden plank with hewn-bark border. Deep
/// canopy body at leaf-xl per-corner radii, bark-colored title bar with a
/// sun-pale hairline underneath, leaf-bright Gaussian halo replacing the
/// CSS triple drop-shadow.
/// </summary>
public class WindowVisual : BaseWindowVisual
{
    private const float BorderThickness = 1f;
    private const float TitleBarSeparatorHeight = 1f;

    private const float ShadowBlur = 40f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;
    private readonly RectangleRuntime _titleBarFill;
    private readonly RectangleRuntime _titleBarSeparator;

    public WindowVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        InnerPanelInstance.Visual.Parent = null;
        TitleBarInstance.Visual.Parent = null;
        BorderTopLeftInstance.Visual.Parent = null;
        BorderTopRightInstance.Visual.Parent = null;
        BorderBottomLeftInstance.Visual.Parent = null;
        BorderBottomRightInstance.Visual.Parent = null;
        BorderTopInstance.Visual.Parent = null;
        BorderBottomInstance.Visual.Parent = null;
        BorderLeftInstance.Visual.Parent = null;
        BorderRightInstance.Visual.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        // V3 child order from here on — keep parity with Neon for the same
        // drag-event reasons (InnerPanel must NOT end up on top of the title
        // bar or it eats drag input).
        AddChild(InnerPanelInstance.Visual);
        AddChild(TitleBarInstance.Visual);
        AddChild(BorderTopLeftInstance.Visual);
        AddChild(BorderTopRightInstance.Visual);
        AddChild(BorderBottomLeftInstance.Visual);
        AddChild(BorderBottomRightInstance.Visual);
        AddChild(BorderTopInstance.Visual);
        AddChild(BorderBottomInstance.Visual);
        AddChild(BorderLeftInstance.Visual);
        AddChild(BorderRightInstance.Visual);

        _titleBarFill = CreateTitleBarFill();
        _titleBarFill.Parent = TitleBarInstance.Visual;

        _titleBarSeparator = CreateTitleBarSeparator();
        _titleBarSeparator.Parent = TitleBarInstance.Visual;
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "ForestGladeWindowFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyExtraLarge(fill);
        fill.IsFilled = true;
        fill.FillColor = ForestGladeStyling.ActiveStyle.Colors.WindowBody;
        fill.StrokeWidth = 0;
        fill.HasDropshadow = true;
        fill.DropshadowColor = ForestGladeStyling.ActiveStyle.Colors.GlowMedium;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = 0f;
        fill.DropshadowBlur = ShadowBlur;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "ForestGladeWindowBorder";
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyExtraLarge(border);
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.Bark;
        return border;
    }

    private static RectangleRuntime CreateTitleBarFill()
    {
        // CSS spec is a wood-grain repeating gradient — represented here by
        // a single bark-soft fill so the title bar reads as warm wood
        // against the canopy body.
        //
        // Per-corner radii match the window body's TOP corners so the bark
        // fill follows the leaf-xl curve at the top-right (and the sharp
        // 6 px at top-left). The bottom of the title bar joins the body
        // interior, so its bottom corners stay square. Without this, the
        // rectangular fill would spill past the body's rounded top-right
        // corner — the 1 px body border isn't thick enough to mask 24 px
        // of spill on its own.
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "ForestGladeWindowTitleBarFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.CornerRadius = 0f;
        fill.CustomRadiusTopLeft = 6f;
        fill.CustomRadiusTopRight = 24f;
        fill.CustomRadiusBottomRight = 0f;
        fill.CustomRadiusBottomLeft = 0f;
        fill.IsFilled = true;
        fill.FillColor = ForestGladeStyling.ActiveStyle.Colors.WindowTitleBar;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateTitleBarSeparator()
    {
        RectangleRuntime separator = new RectangleRuntime();
        separator.Name = "ForestGladeWindowTitleBarSeparator";
        separator.XUnits = GeneralUnitType.PixelsFromMiddle;
        separator.YUnits = GeneralUnitType.PixelsFromLarge;
        separator.XOrigin = HorizontalAlignment.Center;
        separator.YOrigin = VerticalAlignment.Bottom;
        separator.Width = 0;
        separator.Height = TitleBarSeparatorHeight;
        separator.WidthUnits = DimensionUnitType.RelativeToParent;
        separator.HeightUnits = DimensionUnitType.Absolute;
        // CSS box-shadow: inset 0 -1px 0 rgba(232,255,117,.12)
        separator.IsFilled = true;
        separator.FillColor = new Color(232, 255, 117, 31);
        separator.StrokeWidth = 0;
        return separator;
    }
}
