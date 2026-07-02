using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseWindowVisual = Gum.Forms.DefaultVisuals.V3.WindowVisual;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled Window visual. Cream body with a 2.5 px peach border at
/// CornerRadius=16 (matches <c>.pp-win</c>) and a teal title bar. A soft
/// warm-brown Gaussian drop shadow approximates the CSS
/// <c>box-shadow: 0 14px 30px rgba(150,110,70,.22)</c>.
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
    private const float CornerRadius = 16f;
    private const float BorderThickness = 2.5f;
    private const float TitleBarSeparatorHeight = 2f;

    /// <summary>
    /// Native Gaussian drop shadow. CSS spec is
    /// <c>box-shadow: 0 14px 30px rgba(150,110,70,.22)</c>; per the gum-theming
    /// skill, sRGB-space compositing + Apos's blur-kernel semantics mean the
    /// CSS-literal alpha reads too faint, so the alpha is bumped in
    /// <see cref="MeadowColors.WindowShadow"/>. Read live from
    /// <see cref="MeadowStyling.ActiveStyle"/> at construction (not cached in a static field)
    /// so a restyle before construction is picked up.
    /// </summary>
    private const float ShadowOffsetY = 14f;
    private const float ShadowBlur = 30f;

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

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowWindowFill";
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
        // Native Gaussian drop shadow — replaces the prior three-layer stack.
        fill.HasDropshadow = true;
        fill.DropshadowColor = MeadowStyling.ActiveStyle.Colors.WindowShadow;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = ShadowOffsetY;
        fill.DropshadowBlur = ShadowBlur;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "MeadowWindowBorder";
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
        return border;
    }

    private static RectangleRuntime CreateTitleBarFill()
    {
        // Flat teal title bar (CSS .pp-win-bar background: var(--teal)).
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowWindowTitleBarFill";
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
        fill.IsFilled = true;
        fill.FillColor = MeadowStyling.ActiveStyle.Colors.Teal;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateTitleBarSeparator()
    {
        RectangleRuntime separator = new RectangleRuntime();
        separator.Name = "MeadowWindowTitleBarSeparator";
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
        separator.IsFilled = true;
        separator.FillColor = MeadowStyling.ActiveStyle.Colors.TealDark;
        separator.StrokeWidth = 0;
        return separator;
    }
}
