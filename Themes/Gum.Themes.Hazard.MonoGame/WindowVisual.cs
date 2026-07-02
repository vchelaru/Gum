using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using BaseWindowVisual = Gum.Forms.DefaultVisuals.V3.WindowVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled Window visual. Same Surface1 + 1 px border shell as the
/// other Hazard controls, plus a Surface2 title bar fill so users can see
/// where to drag, and a 1 px Border-colored separator between the title bar
/// and the content area.
/// <para>
/// Window has no state category in V3 (the only V3 styling property is a
/// single BackgroundColor), so no state callbacks here — the shell is static.
/// </para>
/// </summary>
public class WindowVisual : BaseWindowVisual
{
    private const float CornerRadius = 0f;
    private const float BorderThickness = 1f;
    private const float TitleBarSeparatorHeight = 1f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;
    private readonly RectangleRuntime _titleBarFill;
    private readonly RectangleRuntime _titleBarSeparator;

    public WindowVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // V3 attaches children in this order:
        //   [Background, InnerPanel, TitleBar, 8 resize borders]
        // The borders go LAST so they sit on top for resize hit testing, and
        // TitleBar goes AFTER InnerPanel so it sits on top for drag input.
        //
        // To insert the Hazard chrome (fill + border) UNDER everything else,
        // detach the whole tree, add chrome first, then re-attach the V3
        // children in the original order. Skipping the re-attach order is what
        // caused the original Window drag bug — InnerPanel ended up on top of
        // TitleBar and swallowed the drag input.
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

        _fill = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Surface1, CornerRadius, "HazardWindowFill");
        AddChild(_fill);

        _border = HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness, "HazardWindowBorder");
        AddChild(_border);

        // V3 order from here on.
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

        // Olive Band fill child of the title bar so the drag area is visibly
        // chrome (.sv-win-bar). GraphicalUiElement subclasses (RectangleRuntime)
        // don't intercept events, so this won't block the title bar's drag handling.
        _titleBarFill = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Band, cornerRadius: 0f, "HazardWindowTitleBarFill");
        _titleBarFill.Parent = TitleBarInstance.Visual;

        // 1 px Border-colored separator pinned to the bottom edge of the
        // title bar so the title bar visually separates from the content
        // area, matching the rest of Hazard's hairline borders.
        _titleBarSeparator = CreateTitleBarSeparator();
        _titleBarSeparator.Parent = TitleBarInstance.Visual;
    }

    private static RectangleRuntime CreateTitleBarSeparator()
    {
        RectangleRuntime separator = new RectangleRuntime();
        separator.Name = "HazardWindowTitleBarSeparator";
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
        separator.FillColor = HazardStyling.ActiveStyle.Colors.Border;
        separator.StrokeWidth = 0;
        return separator;
    }
}
