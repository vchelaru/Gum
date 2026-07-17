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

    /// <summary>
    /// Native Gaussian drop shadow. CSS spec is
    /// <c>box-shadow: 0 8px 28px rgba(180,80,120,.2)</c>; per the gum-theming
    /// skill, sRGB-space compositing + Apos's blur-kernel semantics mean the
    /// CSS-literal alpha (51) reads too faint. Bumped ~1.6× to alpha 80 to
    /// give the window a clear "floating" affordance against the page.
    /// </summary>
    private const float ShadowOffsetY = 8f;
    private const float ShadowBlur = 28f;
    private static readonly Color ShadowColor = new Color(180, 80, 120, 80);

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

        _fill = BubblegumShapes.FillWithDropshadow(
            color: BubblegumStyling.ActiveStyle.Colors.Surface1,
            cornerRadius: CornerRadius,
            shadowColor: ShadowColor,
            offsetX: 0f,
            offsetY: ShadowOffsetY,
            blur: ShadowBlur,
            name: "BubblegumWindowFill");
        AddChild(_fill);

        _border = BubblegumShapes.Border(
            color: BubblegumStyling.ActiveStyle.Colors.Border,
            cornerRadius: CornerRadius,
            thickness: BorderThickness,
            name: "BubblegumWindowBorder");
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

        // CSS uses a gradient (linear-gradient(135deg,#FF8FB8,var(--acc))); we
        // approximate with a flat Accent fill. A real gradient would need a
        // gradient-capable renderable that doesn't exist in the runtime yet.
        _titleBarFill = BubblegumShapes.Fill(
            color: BubblegumStyling.ActiveStyle.Colors.Accent,
            name: "BubblegumWindowTitleBarFill");
        _titleBarFill.Parent = TitleBarInstance.Visual;

        _titleBarSeparator = CreateTitleBarSeparator();
        _titleBarSeparator.Parent = TitleBarInstance.Visual;
    }

    private static RectangleRuntime CreateTitleBarSeparator()
    {
        RectangleRuntime separator = new RectangleRuntime();
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
        separator.IsFilled = true;
        separator.FillColor = BubblegumStyling.ActiveStyle.Colors.Border;
        separator.StrokeWidth = 0;
        return separator;
    }
}
