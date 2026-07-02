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
    private readonly RectangleRuntime _titleBarFill;

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

        // Win95 windows have a thin dark outline wrapping the raised bevel — it's
        // what makes the corners "snap" instead of fading into the desktop
        // background. Four 1 px strips of pure black on the outermost edge.
        AddOuterOutline();

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

    private void AddOuterOutline()
    {
        // 4 × 1 px black strips at inset 0, attached after the body bevel so
        // they paint on top of the bevel's outermost white/dark ring. The
        // visible result reads as: black 1 px edge → light/dark bevel inner →
        // surface, which is the Win95 window-frame profile.
        AddChild(NewOutlineEdge(top: true, vertical: false));
        AddChild(NewOutlineEdge(top: false, vertical: false));
        AddChild(NewOutlineEdge(top: true, vertical: true));
        AddChild(NewOutlineEdge(top: false, vertical: true));
    }

    private static RectangleRuntime NewOutlineEdge(bool top, bool vertical)
    {
        RectangleRuntime r = new RectangleRuntime();
        r.Name = "Retro95WindowOutline";
        r.IsFilled = true;
        r.FillColor = Color.Black;
        r.StrokeWidth = 0;
        if (!vertical)
        {
            r.X = 0;
            r.Y = 0;
            r.XUnits = GeneralUnitType.PixelsFromMiddle;
            r.YUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            r.XOrigin = HorizontalAlignment.Center;
            r.YOrigin = top ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            r.Width = 0;
            r.Height = 1f;
            r.WidthUnits = DimensionUnitType.RelativeToParent;
            r.HeightUnits = DimensionUnitType.Absolute;
        }
        else
        {
            r.X = 0;
            r.Y = 0;
            r.XUnits = top ? GeneralUnitType.PixelsFromSmall : GeneralUnitType.PixelsFromLarge;
            r.YUnits = GeneralUnitType.PixelsFromMiddle;
            r.XOrigin = top ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            r.YOrigin = VerticalAlignment.Center;
            r.Width = 1f;
            r.Height = 0;
            r.WidthUnits = DimensionUnitType.Absolute;
            r.HeightUnits = DimensionUnitType.RelativeToParent;
        }
        return r;
    }

    private static RectangleRuntime CreateTitleBarFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "Retro95WindowTitleBarFill";
        fill.X = 0; fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0; fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.IsFilled = true;
        fill.FillColor = Retro95Styling.ActiveStyle.Colors.Selection;
        fill.StrokeWidth = 0;
        return fill;
    }
}
