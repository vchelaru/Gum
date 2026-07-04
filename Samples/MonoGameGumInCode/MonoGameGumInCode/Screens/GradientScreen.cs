using Gum.Converters;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;

// Deep-dive on RoundedRectangleRuntime's gradient surface. Three rows of three cells -
// every cell uses the same rectangle size so the configuration is what changes, not the
// canvas. Each cell shows a rectangle on top and a small label beneath naming the
// configuration. Together the nine variants cover:
//   - GradientType: Linear vs Radial
//   - GeneralUnitType: PixelsFromSmall / PixelsFromMiddle / PixelsFromLarge / Percentage
//     on all four gradient endpoint axes
//   - Radial-specific: GradientInnerRadius and GradientOuterRadius in absolute pixels and
//     in PercentageOfParent (DimensionUnitType, not GeneralUnitType)
// The Percentage-units cells double as a visual regression check for issue #2723.
internal class GradientScreen : FrameworkElement
{
    private const float RectWidth = 280;
    private const float RectHeight = 120;

    private static readonly Color StartColor = new Color(15, 196, 72);
    private static readonly Color EndColor = new Color(0, 70, 22);

    public GradientScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime container = new ContainerRuntime();
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 32;
        container.X = 8;
        container.Y = 8;
        this.AddChild(container);

        ContainerRuntime row1 = AddRow(container);
        AddCell(row1, "Linear horizontal\nPixelsFromSmall → PixelsFromLarge X",
            ConfigureLinearHorizontal);
        AddCell(row1, "Linear vertical\nPixelsFromSmall → PixelsFromLarge Y",
            ConfigureLinearVertical);
        AddCell(row1, "Linear diagonal\nSmall/Small → Large/Large",
            ConfigureLinearDiagonal);

        ContainerRuntime row2 = AddRow(container);
        AddCell(row2, "Linear from middle\nPixelsFromMiddle X (0 → 0)",
            ConfigureLinearFromMiddle);
        AddCell(row2, "Linear Percentage\n0% → 100% on Y",
            ConfigureLinearPercentageFull);
        AddCell(row2, "Linear Percentage band\n25% → 75% on X",
            ConfigureLinearPercentagePartial);

        ContainerRuntime row3 = AddRow(container);
        AddCell(row3, "Radial centered\nPixelsFromMiddle (0,0), outer = 100px",
            ConfigureRadialCentered);
        AddCell(row3, "Radial with inner radius\nInner 30px, outer 100px",
            ConfigureRadialDonut);
        AddCell(row3, "Radial Percentage outer\nPercentageOfParent radius = 50%",
            ConfigureRadialPercentageOuter);
    }

    private static ContainerRuntime AddRow(ContainerRuntime parent)
    {
        ContainerRuntime row = new ContainerRuntime();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 38;
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.Height = 0;
        parent.AddChild(row);
        return row;
    }

    private static void AddCell(ContainerRuntime row, string labelText,
        System.Action<RoundedRectangleRuntime> configure)
    {
        ContainerRuntime cell = new ContainerRuntime();
        cell.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        cell.StackSpacing = 4;
        cell.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        cell.Width = RectWidth;
        cell.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        cell.Height = 0;
        row.AddChild(cell);

        RoundedRectangleRuntime rect = new RoundedRectangleRuntime();
        rect.Width = RectWidth;
        rect.Height = RectHeight;
        rect.CornerRadius = 6;
        rect.UseGradient = true;
        rect.Red1 = StartColor.R; rect.Green1 = StartColor.G; rect.Blue1 = StartColor.B; rect.Alpha1 = 255;
        rect.Red2 = EndColor.R;   rect.Green2 = EndColor.G;   rect.Blue2 = EndColor.B;   rect.Alpha2 = 255;
        configure(rect);
        cell.AddChild(rect);

        TextRuntime label = new TextRuntime();
        label.Text = labelText;
        label.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        label.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        cell.AddChild(label);
    }

    private static void ConfigureLinearHorizontal(RoundedRectangleRuntime rect)
    {
        rect.GradientType = GradientType.Linear;
        rect.GradientX1Units = GeneralUnitType.PixelsFromSmall; rect.GradientX1 = 0;
        rect.GradientY1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientY1 = 0;
        rect.GradientX2Units = GeneralUnitType.PixelsFromLarge; rect.GradientX2 = 0;
        rect.GradientY2Units = GeneralUnitType.PixelsFromMiddle; rect.GradientY2 = 0;
    }

    private static void ConfigureLinearVertical(RoundedRectangleRuntime rect)
    {
        rect.GradientType = GradientType.Linear;
        rect.GradientX1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientX1 = 0;
        rect.GradientY1Units = GeneralUnitType.PixelsFromSmall; rect.GradientY1 = 0;
        rect.GradientX2Units = GeneralUnitType.PixelsFromMiddle; rect.GradientX2 = 0;
        rect.GradientY2Units = GeneralUnitType.PixelsFromLarge; rect.GradientY2 = 0;
    }

    private static void ConfigureLinearDiagonal(RoundedRectangleRuntime rect)
    {
        rect.GradientType = GradientType.Linear;
        rect.GradientX1Units = GeneralUnitType.PixelsFromSmall; rect.GradientX1 = 0;
        rect.GradientY1Units = GeneralUnitType.PixelsFromSmall; rect.GradientY1 = 0;
        rect.GradientX2Units = GeneralUnitType.PixelsFromLarge; rect.GradientX2 = 0;
        rect.GradientY2Units = GeneralUnitType.PixelsFromLarge; rect.GradientY2 = 0;
    }

    // Both endpoints at PixelsFromMiddle with 0 offset collapse into a single point, but
    // the renderer treats coincident endpoints by sweeping the second color across most of
    // the area - useful for showing what NOT to do, and that the units math handles the
    // degenerate case without crashing. Offset slightly so it's not literally zero-length.
    private static void ConfigureLinearFromMiddle(RoundedRectangleRuntime rect)
    {
        rect.GradientType = GradientType.Linear;
        rect.GradientX1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientX1 = -60;
        rect.GradientY1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientY1 = 0;
        rect.GradientX2Units = GeneralUnitType.PixelsFromMiddle; rect.GradientX2 = 60;
        rect.GradientY2Units = GeneralUnitType.PixelsFromMiddle; rect.GradientY2 = 0;
    }

    // 0% → 100% on Y is the canonical Percentage case that issue #2723 broke: before
    // the fix, the Percentage branches in RenderableShapeBase.GetGradient overwrote the
    // world offset and the rectangle rendered flat (end color everywhere). A visible
    // top-to-bottom gradient here means the fix is intact.
    private static void ConfigureLinearPercentageFull(RoundedRectangleRuntime rect)
    {
        rect.GradientType = GradientType.Linear;
        rect.GradientX1Units = GeneralUnitType.Percentage; rect.GradientX1 = 50;
        rect.GradientY1Units = GeneralUnitType.Percentage; rect.GradientY1 = 0;
        rect.GradientX2Units = GeneralUnitType.Percentage; rect.GradientX2 = 50;
        rect.GradientY2Units = GeneralUnitType.Percentage; rect.GradientY2 = 100;
    }

    // Percentage band: 25%->75% horizontally. Outside the band, the colors should clamp,
    // so the leftmost 25% is solid start color and the rightmost 25% is solid end color -
    // a quick visual check that Percentage endpoints really map into the rectangle's
    // local space, not somewhere far outside it.
    private static void ConfigureLinearPercentagePartial(RoundedRectangleRuntime rect)
    {
        rect.GradientType = GradientType.Linear;
        rect.GradientX1Units = GeneralUnitType.Percentage; rect.GradientX1 = 25;
        rect.GradientY1Units = GeneralUnitType.Percentage; rect.GradientY1 = 50;
        rect.GradientX2Units = GeneralUnitType.Percentage; rect.GradientX2 = 75;
        rect.GradientY2Units = GeneralUnitType.Percentage; rect.GradientY2 = 50;
    }

    private static void ConfigureRadialCentered(RoundedRectangleRuntime rect)
    {
        rect.GradientType = GradientType.Radial;
        rect.GradientX1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientX1 = 0;
        rect.GradientY1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientY1 = 0;
        rect.GradientOuterRadiusUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        rect.GradientOuterRadius = 100;
    }

    private static void ConfigureRadialDonut(RoundedRectangleRuntime rect)
    {
        rect.GradientType = GradientType.Radial;
        rect.GradientX1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientX1 = 0;
        rect.GradientY1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientY1 = 0;
        rect.GradientInnerRadiusUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        rect.GradientInnerRadius = 30;
        rect.GradientOuterRadiusUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        rect.GradientOuterRadius = 100;
    }

    private static void ConfigureRadialPercentageOuter(RoundedRectangleRuntime rect)
    {
        rect.GradientType = GradientType.Radial;
        rect.GradientX1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientX1 = 0;
        rect.GradientY1Units = GeneralUnitType.PixelsFromMiddle; rect.GradientY1 = 0;
        rect.GradientOuterRadiusUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        rect.GradientOuterRadius = 50;
    }
}
