using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseWindowVisual = Gum.Forms.DefaultVisuals.V3.WindowVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled Window visual. Surface (gray) body with a raised bevel and a
/// navy title bar (matches <c>.rc-titlebar</c>). Hard 2 px black drop shadow
/// approximation skipped — the runtime doesn't compose 2 px offset shadows on
/// non-shape primitives without a duplicated rectangle, and Win95 windows read
/// fine without it. Children attach in the same order V3 added them so the
/// title-bar drag area stays in front of InnerPanel.
/// </summary>
public class WindowVisual : BaseWindowVisual
{
    private const float TitleBarHeight = 18f;

    private readonly Retro95Bevel _bodyBevel;
    private readonly ColoredRectangleRuntime _titleBarFill;

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

        _bodyBevel = Retro95Bevel.AddTo(this, BevelMode.Raised);

        // V3 child order from here on so InnerPanel doesn't end up in front of
        // TitleBar (its invisible InteractiveGue would swallow drag input).
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

        TitleBarInstance.Visual.Height = TitleBarHeight;
        TitleBarInstance.Visual.HeightUnits = DimensionUnitType.Absolute;
    }

    private static ColoredRectangleRuntime CreateTitleBarFill()
    {
        ColoredRectangleRuntime fill = new ColoredRectangleRuntime();
        fill.Name = "Retro95WindowTitleBarFill";
        fill.X = 0; fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0; fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.Color = Retro95Colors.Selection;
        return fill;
    }
}
