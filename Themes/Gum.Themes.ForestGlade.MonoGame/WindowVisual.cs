using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
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

    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;
    private readonly ColoredRectangleRuntime _titleBarFill;
    private readonly ColoredRectangleRuntime _titleBarSeparator;

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

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
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
        fill.Color = ForestGladePalette.WindowBody;
        fill.HasDropshadow = true;
        fill.DropshadowColor = ForestGladePalette.GlowMedium;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = 0f;
        fill.DropshadowBlurX = ShadowBlur;
        fill.DropshadowBlurY = ShadowBlur;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
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
        border.Color = ForestGladeColors.Bark;
        return border;
    }

    private static ColoredRectangleRuntime CreateTitleBarFill()
    {
        // CSS spec is a wood-grain repeating gradient — represented here by
        // a single bark-soft fill so the title bar reads as warm wood
        // against the canopy body.
        ColoredRectangleRuntime fill = new ColoredRectangleRuntime();
        fill.Name = "ForestGladeWindowTitleBarFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.Color = ForestGladePalette.WindowTitleBar;
        return fill;
    }

    private static ColoredRectangleRuntime CreateTitleBarSeparator()
    {
        ColoredRectangleRuntime separator = new ColoredRectangleRuntime();
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
        separator.Color = new Color(232, 255, 117, 31);
        return separator;
    }
}
