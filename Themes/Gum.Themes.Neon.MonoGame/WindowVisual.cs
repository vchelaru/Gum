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

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled Window visual. Surface1 body with a 1 px cyan accent border at
/// near-square corners (CSS <c>--r: 1px</c>), Surface2 title bar with a cyan
/// separator line beneath it. The body glow approximates CSS
/// <c>box-shadow: 0 0 20px rgba(0,229,255,.2), 0 8px 28px rgba(0,0,0,.8)</c> —
/// rendered as a single Gaussian halo via the native Apos shape dropshadow.
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
    private const float CornerRadius = 1f;
    private const float BorderThickness = 1f;
    private const float TitleBarSeparatorHeight = 1f;

    /// <summary>
    /// Native Gaussian cyan halo standing in for the layered CSS spec
    /// (<c>0 0 20px rgba(0,229,255,.2)</c>). Bumped per the gum-theming skill's
    /// sRGB-vs-linear note so the window reads as glowing rather than flat.
    /// </summary>
    private const float ShadowOffsetY = 0f;
    private const float ShadowBlur = 56f;

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
        fill.Name = "NeonWindowFill";
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
        fill.FillColor = NeonStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        // Native Gaussian drop shadow — replaces the prior three-layer stack.
        fill.HasDropshadow = true;
        fill.DropshadowColor = NeonStyling.ActiveStyle.Colors.WindowShadow;
        fill.DropshadowOffsetX = 0f;
        fill.DropshadowOffsetY = ShadowOffsetY;
        fill.DropshadowBlur = ShadowBlur;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "NeonWindowBorder";
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
        border.StrokeColor = NeonStyling.ActiveStyle.Colors.Accent;
        return border;
    }

    private static RectangleRuntime CreateTitleBarFill()
    {
        // CSS: title bar is flat Surface2 with a cyan separator beneath it
        // (border-bottom:1px solid var(--acc)). No gradient.
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "NeonWindowTitleBarFill";
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
        // CSS spec is var(--s2), but Surface2 (#131330) reads almost identical
        // to the Surface1 body against the dark page — the title bar visually
        // dissolved into the body. Tinting with the translucent cyan accent
        // (AccentDim) gives the bar a recognizable header weight without
        // departing from the palette.
        fill.IsFilled = true;
        fill.FillColor = NeonStyling.ActiveStyle.Colors.AccentDim;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateTitleBarSeparator()
    {
        RectangleRuntime separator = new RectangleRuntime();
        separator.Name = "NeonWindowTitleBarSeparator";
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
        separator.FillColor = NeonStyling.ActiveStyle.Colors.Accent;
        separator.StrokeWidth = 0;
        return separator;
    }
}
