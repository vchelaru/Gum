using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace MonoGameGumInCode.Screens;

// Visual demo for ClipsChildren. Each scenario places oversized content inside a
// fixed-size frame so the difference between clipped and unclipped is obvious. The
// frame around each scenario is itself a RectangleRuntime sibling (not the clipping
// parent), so the frame outline remains visible even when its neighbor clips.
internal class ClipScreen : FrameworkElement
{
    public ClipScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 16;
        root.X = 8;
        root.Y = 8;
        AddChild(root);

        root.AddChild(BuildSection(
            "ClipsChildren off vs on (same oversized content)",
            BuildClipToggleRow()));

        root.AddChild(BuildSection(
            "Overflow on all four sides (single clipped frame)",
            BuildAllSidesOverflow()));

        root.AddChild(BuildSection(
            "Nested clips (inner frame clips, outer frame also clips)",
            BuildNestedClips()));

        root.AddChild(BuildSection(
            "Mixed content clipped (rectangle, text, circle)",
            BuildMixedContent()));
    }

    static ContainerRuntime BuildSection(string label, GraphicalUiElement body)
    {
        ContainerRuntime section = new();
        section.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        section.StackSpacing = 4;
        section.WidthUnits = DimensionUnitType.RelativeToChildren;
        section.HeightUnits = DimensionUnitType.RelativeToChildren;
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
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        return row;
    }

    static ContainerRuntime BuildClipToggleRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        row.AddChild(BuildOverflowFrame(clipsChildren: false));
        row.AddChild(BuildOverflowFrame(clipsChildren: true));
        return row;
    }

    // 160x100 frame holding a 220x140 child that extends past the bottom-right corner.
    // With ClipsChildren=false the child paints across siblings; with =true it is cropped
    // to the frame's bounds.
    static ContainerRuntime BuildOverflowFrame(bool clipsChildren)
    {
        ContainerRuntime frame = new();
        frame.Width = 160;
        frame.Height = 100;
        frame.ClipsChildren = clipsChildren;

        RectangleRuntime border = new();
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.Width = 0;
        border.Height = 0;
        border.StrokeColor = Color.White;
        border.StrokeWidth = 1;
        frame.AddChild(border);

        RectangleRuntime oversized = new();
        oversized.Width = 220;
        oversized.Height = 140;
        oversized.FillColor = clipsChildren ? new Color(60, 120, 60) : new Color(120, 60, 60);
        frame.AddChild(oversized);

        TextRuntime label = new();
        label.Text = clipsChildren ? "ClipsChildren = true" : "ClipsChildren = false";
        label.X = 4;
        label.Y = 4;
        frame.AddChild(label);

        return frame;
    }

    static ContainerRuntime BuildAllSidesOverflow()
    {
        ContainerRuntime frame = new();
        frame.Width = 240;
        frame.Height = 160;
        frame.ClipsChildren = true;

        RectangleRuntime border = new();
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.Width = 0;
        border.Height = 0;
        border.StrokeColor = Color.White;
        border.StrokeWidth = 1;
        frame.AddChild(border);

        // Four rectangles, one poking out each edge.
        AddPokingRect(frame, x: -40, y: 60, w: 100, h: 40, Color.Crimson);   // left edge
        AddPokingRect(frame, x: 180, y: 60, w: 100, h: 40, Color.Goldenrod); // right edge
        AddPokingRect(frame, x: 90, y: -30, w: 60, h: 80, Color.SeaGreen);   // top edge
        AddPokingRect(frame, x: 90, y: 120, w: 60, h: 80, Color.MediumPurple); // bottom edge

        return frame;
    }

    static void AddPokingRect(ContainerRuntime parent, float x, float y, float w, float h, Color color)
    {
        RectangleRuntime rect = new();
        rect.X = x;
        rect.Y = y;
        rect.Width = w;
        rect.Height = h;
        rect.FillColor = color;
        parent.AddChild(rect);
    }

    static ContainerRuntime BuildNestedClips()
    {
        ContainerRuntime outer = new();
        outer.Width = 280;
        outer.Height = 180;
        outer.ClipsChildren = true;

        RectangleRuntime outerBorder = new();
        outerBorder.WidthUnits = DimensionUnitType.RelativeToParent;
        outerBorder.HeightUnits = DimensionUnitType.RelativeToParent;
        outerBorder.Width = 0;
        outerBorder.Height = 0;
        outerBorder.StrokeColor = Color.White;
        outerBorder.StrokeWidth = 1;
        outer.AddChild(outerBorder);

        // Inner clip is positioned so part of it sits outside the outer clip — confirms
        // the outer scissor still trims the inner frame's border + contents.
        ContainerRuntime inner = new();
        inner.X = 180;
        inner.Y = 40;
        inner.Width = 160;
        inner.Height = 120;
        inner.ClipsChildren = true;
        outer.AddChild(inner);

        RectangleRuntime innerBorder = new();
        innerBorder.WidthUnits = DimensionUnitType.RelativeToParent;
        innerBorder.HeightUnits = DimensionUnitType.RelativeToParent;
        innerBorder.Width = 0;
        innerBorder.Height = 0;
        innerBorder.StrokeColor = Color.Yellow;
        innerBorder.StrokeWidth = 1;
        inner.AddChild(innerBorder);

        // Child inside inner clip that overflows the inner — gets clipped by the inner
        // scissor. The portion of `inner` outside `outer` is then clipped by the outer
        // scissor, so even an "overflowing" child can only ever paint into the outer
        // clip's intersection with the inner clip.
        RectangleRuntime nestedOverflow = new();
        nestedOverflow.X = -20;
        nestedOverflow.Y = -20;
        nestedOverflow.Width = 220;
        nestedOverflow.Height = 180;
        nestedOverflow.FillColor = new Color(60, 90, 140);
        inner.AddChild(nestedOverflow);

        return outer;
    }

    static ContainerRuntime BuildMixedContent()
    {
        ContainerRuntime frame = new();
        frame.Width = 260;
        frame.Height = 140;
        frame.ClipsChildren = true;

        RectangleRuntime border = new();
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.Width = 0;
        border.Height = 0;
        border.StrokeColor = Color.White;
        border.StrokeWidth = 1;
        frame.AddChild(border);

        RectangleRuntime tile = new();
        tile.X = -30;
        tile.Y = 20;
        tile.Width = 140;
        tile.Height = 100;
        tile.FillColor = new Color(80, 80, 140);
        frame.AddChild(tile);

        // Text wider than the frame — clipping crops the right side of the glyphs.
        TextRuntime text = new();
        text.X = 8;
        text.Y = 4;
        text.Text = "Long text that runs past the right edge of the frame";
        frame.AddChild(text);

        // Circle hanging off the lower-right corner.
        CircleRuntime circle = new();
        circle.X = 220;
        circle.Y = 100;
        circle.Radius = 40;
        circle.StrokeColor = Color.Goldenrod;
        frame.AddChild(circle);

        return frame;
    }
}
