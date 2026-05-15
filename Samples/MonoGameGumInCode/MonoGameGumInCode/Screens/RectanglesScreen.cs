using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;

// Visual smoke test for RectangleRuntime's two-slot model (issue #2768). Unlike circles,
// core MonoGameGum ships defaults for BOTH slots — DefaultFilledRectangleRenderable wraps
// SolidRectangle, DefaultStrokedRectangleRenderable wraps LineRectangle — so fill, stroke,
// and fill+stroke all render correctly here without MonoGameGumShapes installed. CornerRadius
// is stored but NOT rendered (the core defaults are hard-cornered): see the same screen in
// MonoGameGumShapesGallery for visually rounded corners.
internal class RectanglesScreen : FrameworkElement
{
    public RectanglesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 12;
        root.X = 8;
        root.Y = 8;
        AddChild(root);

        root.AddChild(BuildSection("Modes: FillColor only, StrokeColor only, Fill+Stroke, default", BuildModeRow()));
        root.AddChild(BuildSection("StrokeWidth (1, 2, 4, 8 px on a filled card)", BuildStrokeWidthRow()));
        root.AddChild(BuildSection("CornerRadius (0, 4, 12, 24 — stored but flat on core)", BuildCornerRadiusRow()));
    }

    static ContainerRuntime BuildSection(string label, GraphicalUiElement body)
    {
        ContainerRuntime section = new();
        section.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        section.StackSpacing = 4;
        section.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        section.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        section.Width = 0;
        section.Height = 0;

        TextRuntime header = new();
        header.Text = label;
        section.AddChild(header);
        section.AddChild(body);
        return section;
    }

    static ContainerRuntime BuildHorizontalRow()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 16;
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime filled = new();
        filled.Width = 80; filled.Height = 60;
        filled.FillColor = Color.Crimson;
        row.AddChild(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 80; stroked.Height = 60;
        stroked.StrokeColor = Color.Cyan;
        row.AddChild(stroked);

        RectangleRuntime both = new();
        both.Width = 80; both.Height = 60;
        both.FillColor = new Color(40, 40, 80);
        both.StrokeColor = Color.Yellow;
        row.AddChild(both);

        RectangleRuntime defaultRect = new();
        defaultRect.Width = 80; defaultRect.Height = 60;
        row.AddChild(defaultRect);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        // LineRectangle (core stroke default) IS LinePixelWidth-aware, so this row varies
        // visually even without MonoGameGumShapes. On Apos the visual is identical except
        // the line is anti-aliased.
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            RectangleRuntime rect = new();
            rect.Width = 80; rect.Height = 60;
            rect.FillColor = new Color(30, 30, 50);
            rect.StrokeColor = Color.LightGreen;
            rect.StrokeWidth = strokeWidth;
            row.AddChild(rect);
        }
        return row;
    }

    static ContainerRuntime BuildCornerRadiusRow()
    {
        // CornerRadius is stored but ignored visually on the core defaults — SolidRectangle
        // and LineRectangle are hard-cornered. The shapes-package version of this screen
        // looks visibly different at every value > 0.
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float cornerRadius in new[] { 0f, 4f, 12f, 24f })
        {
            RectangleRuntime rect = new();
            rect.Width = 80; rect.Height = 60;
            rect.FillColor = new Color(40, 40, 80);
            rect.StrokeColor = Color.Orange;
            rect.StrokeWidth = 2;
            rect.CornerRadius = cornerRadius;
            row.AddChild(rect);
        }
        return row;
    }
}
