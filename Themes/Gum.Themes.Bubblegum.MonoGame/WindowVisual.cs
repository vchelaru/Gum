using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseWindowVisual = Gum.Forms.DefaultVisuals.V3.WindowVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled Window visual. Surface1 body with a 2 px pink border at
/// CornerRadius=14 (matches <c>.bb-win</c>) and an Accent title bar with white
/// text. Three-layer pink drop shadow approximates the CSS
/// <c>box-shadow:0 8px 28px rgba(180,80,120,.2)</c>.
/// <para>
/// Render order matters: V3 attaches children as
/// <c>[Background, InnerPanel, TitleBar, 8 resize borders]</c>. Re-attaching in
/// that exact order is required so the InnerPanel doesn't end up on top of the
/// title bar and swallow drag input. The shadow + chrome go in first, then the
/// V3 children in their canonical sequence.
/// </para>
/// </summary>
public class WindowVisual : BaseWindowVisual
{
    private const float CornerRadius = 14f;
    private const float BorderThickness = 2f;
    private const float TitleBarSeparatorHeight = 2f;

    private const float ShadowOffsetY = 6f;
    private const float ShadowSpread1 = 2f;
    private const float ShadowSpread2 = 6f;
    private const float ShadowSpread3 = 12f;
    private static readonly Color ShadowColor1 = new Color(180, 80, 120, 70);
    private static readonly Color ShadowColor2 = new Color(180, 80, 120, 40);
    private static readonly Color ShadowColor3 = new Color(180, 80, 120, 20);

    private readonly RoundedRectangleRuntime _shadow1;
    private readonly RoundedRectangleRuntime _shadow2;
    private readonly RoundedRectangleRuntime _shadow3;
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

        // Stack shadow outermost-first (faintest) so it renders behind the brighter layers.
        _shadow3 = CreateShadow(ShadowSpread3, ShadowColor3);
        AddChild(_shadow3);
        _shadow2 = CreateShadow(ShadowSpread2, ShadowColor2);
        AddChild(_shadow2);
        _shadow1 = CreateShadow(ShadowSpread1, ShadowColor1);
        AddChild(_shadow1);

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        // V3 child order from here on.
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
        fill.Name = "BubblegumWindowFill";
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
        fill.Color = BubblegumColors.Surface1;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "BubblegumWindowBorder";
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
        border.Color = BubblegumColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateShadow(float spread, Color color)
    {
        RoundedRectangleRuntime shadow = new RoundedRectangleRuntime();
        shadow.Name = "BubblegumWindowShadow";
        shadow.X = 0;
        shadow.Y = ShadowOffsetY;
        shadow.XUnits = GeneralUnitType.PixelsFromMiddle;
        shadow.YUnits = GeneralUnitType.PixelsFromMiddle;
        shadow.XOrigin = HorizontalAlignment.Center;
        shadow.YOrigin = VerticalAlignment.Center;
        shadow.Width = spread * 2f;
        shadow.Height = spread * 2f;
        shadow.WidthUnits = DimensionUnitType.RelativeToParent;
        shadow.HeightUnits = DimensionUnitType.RelativeToParent;
        shadow.CornerRadius = CornerRadius + spread;
        shadow.IsFilled = true;
        shadow.Color = color;
        return shadow;
    }

    private static ColoredRectangleRuntime CreateTitleBarFill()
    {
        // CSS uses a gradient (linear-gradient(135deg,#FF8FB8,var(--acc))); we
        // approximate with a flat Accent fill. A real gradient would need a
        // gradient-capable renderable that doesn't exist in the runtime yet.
        ColoredRectangleRuntime fill = new ColoredRectangleRuntime();
        fill.Name = "BubblegumWindowTitleBarFill";
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
        fill.Color = BubblegumColors.Accent;
        return fill;
    }

    private static ColoredRectangleRuntime CreateTitleBarSeparator()
    {
        ColoredRectangleRuntime separator = new ColoredRectangleRuntime();
        separator.Name = "BubblegumWindowTitleBarSeparator";
        separator.X = 0;
        separator.Y = 0;
        separator.XUnits = GeneralUnitType.PixelsFromMiddle;
        separator.YUnits = GeneralUnitType.PixelsFromLarge;
        separator.XOrigin = HorizontalAlignment.Center;
        separator.YOrigin = VerticalAlignment.Bottom;
        separator.Width = 0;
        separator.Height = TitleBarSeparatorHeight;
        separator.WidthUnits = DimensionUnitType.RelativeToParent;
        separator.HeightUnits = DimensionUnitType.Absolute;
        separator.Color = BubblegumColors.Border;
        return separator;
    }
}
