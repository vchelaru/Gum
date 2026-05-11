using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum;
using RenderingLibrary.Graphics;
using BaseWindowVisual = Gum.Forms.DefaultVisuals.V3.WindowVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled Window visual. Same Surface1 + 1 px border shell as the
/// other Dark Pro controls, plus a Surface2 title bar fill so users can see
/// where to drag. The 8 resize-border panels and the InnerPanel are left
/// untouched — they're hit-test areas, not visible chrome.
/// <para>
/// Window has no state category in V3 (the only V3 styling property is a
/// single BackgroundColor), so no state callbacks here; the shell is static.
/// </para>
/// </summary>
public class WindowVisual : BaseWindowVisual
{
    private const float CornerRadius = 2f;
    private const float BorderThickness = 1f;

    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;
    private readonly ColoredRectangleRuntime _titleBarFill;

    public WindowVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the V3 NineSlice background. The InnerPanel, title bar, and
        // 8 resize-border panels all stay attached — they're functional zones
        // the Forms Window relies on for drag/resize hit testing.
        Background.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        // Reattach interior structure on top of the new shape stack so user
        // content, the title bar, and the resize-border hit zones render
        // above the background. Panel is a Forms control, so the visual-tree
        // reparenting goes through Panel.Visual.Parent.
        InnerPanelInstance.Visual.Parent = null;
        TitleBarInstance.Visual.Parent = null;

        AddChild(TitleBarInstance.Visual);
        AddChild(InnerPanelInstance.Visual);

        // Give the title bar a visible Surface2 fill so the drag area reads
        // as chrome. The 2 px corner rounding on the window background is
        // small enough that the title bar overlapping it visually squares
        // the top corners by ~2 px — an acceptable tradeoff for not having
        // to special-case per-corner radii (Apos.Shapes is uniform-radius).
        _titleBarFill = CreateTitleBarFill();
        _titleBarFill.Parent = TitleBarInstance.Visual;
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "DarkProWindowFill";
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
        fill.Color = DarkProColors.Surface1;
        // Pure chrome — must not swallow input destined for the title bar
        // (drag) or the resize border zones beneath it.
        fill.HasEvents = false;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "DarkProWindowBorder";
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
        border.Color = DarkProColors.Border;
        border.HasEvents = false;
        return border;
    }

    private static ColoredRectangleRuntime CreateTitleBarFill()
    {
        ColoredRectangleRuntime fill = new ColoredRectangleRuntime();
        fill.Name = "DarkProWindowTitleBarFill";
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
        fill.Color = DarkProColors.Surface2;
        // Critical: the title bar Panel itself is the drag handle. A child
        // with HasEvents=true (the default for GraphicalUiElement) sits on
        // top and swallows the drag input. Leave the panel's events to do
        // their job.
        fill.HasEvents = false;
        return fill;
    }
}
