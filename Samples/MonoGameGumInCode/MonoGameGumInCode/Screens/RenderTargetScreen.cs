using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace MonoGameGumInCode.Screens;

/// <summary>
/// Render-to-texture gallery (issue #3434), the MonoGame mirror of the raylib RenderTargetScreen.
/// Each cell wraps its content in a <see cref="ContainerRuntime"/> with
/// <see cref="ContainerRuntime.IsRenderTarget"/> = true, so the subtree bakes into an offscreen
/// texture and composites back in place. Unlike <c>RenderTargetEffectScreen</c>, no shader is
/// involved — this exercises the render-target path itself: group alpha/blend, nested targets,
/// overflow clipping to the container bounds, and a ClipsChildren descendant inside the target.
/// </summary>
internal class RenderTargetScreen : FrameworkElement
{
    public RenderTargetScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var root = new ContainerRuntime();
        root.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        root.WidthUnits = DimensionUnitType.RelativeToChildren;
        root.HeightUnits = DimensionUnitType.RelativeToChildren;
        root.Width = 0;
        root.Height = 0;
        root.StackSpacing = 28;
        root.X = 12;
        root.Y = 12;
        this.AddChild(root);

        root.AddChild(BuildCell("Render to target", BuildBaseline()));
        root.AddChild(BuildCell("Group alpha 50%", BuildGroupAlpha()));
        root.AddChild(BuildCell("Additive group", BuildAdditiveGroup()));
        root.AddChild(BuildCell("Nested RT + sibling", BuildNestedWithSibling()));
        root.AddChild(BuildCell("Overflow clipped", BuildOverflow()));
        // clip-inside-RT: deferred, see #3440 — cell kept out to stay parallel with the raylib
        // sample, where a ClipsChildren descendant inside a render target renders unclipped.
    }

    private static ContainerRuntime BuildCell(string caption, GraphicalUiElement body)
    {
        var cell = new ContainerRuntime();
        cell.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        cell.StackSpacing = 6;
        cell.WidthUnits = DimensionUnitType.RelativeToChildren;
        cell.HeightUnits = DimensionUnitType.RelativeToChildren;
        cell.Width = 0;
        cell.Height = 0;

        var header = new TextRuntime();
        header.Text = caption;
        header.Color = Color.White;
        cell.AddChild(header);
        cell.AddChild(body);
        return cell;
    }

    // A mid-gray frame behind each demo so group transparency and clipping read against a consistent
    // backdrop rather than the page color.
    private static ContainerRuntime BuildFrame(float width, float height)
    {
        var frame = new ColoredRectangleRuntime();
        frame.Width = width;
        frame.Height = height;
        frame.Color = new Color(70, 70, 90, 255);

        var holder = new ContainerRuntime();
        holder.Width = width;
        holder.Height = height;
        holder.AddChild(frame);
        return holder;
    }

    private static ColoredRectangleRuntime Rect(float x, float y, float width, float height, Color color)
    {
        var rect = new ColoredRectangleRuntime();
        rect.X = x;
        rect.Y = y;
        rect.Width = width;
        rect.Height = height;
        rect.Color = color;
        return rect;
    }

    // Two overlapping opaque rects inside a render target — visually identical to drawing them
    // directly, proving the bake + composite round-trips content correctly.
    private static GraphicalUiElement BuildBaseline()
    {
        var holder = BuildFrame(150, 110);

        var group = new ContainerRuntime();
        group.Width = 150;
        group.Height = 110;
        group.IsRenderTarget = true;
        group.AddChild(Rect(10, 10, 90, 70, new Color(220, 60, 60, 255)));
        group.AddChild(Rect(55, 35, 90, 70, new Color(60, 120, 220, 255)));
        holder.AddChild(group);
        return holder;
    }

    // Same content as the baseline, but the container carries a 50% group alpha. Because the group
    // flattens to a texture first, the whole thing (overlap included) fades uniformly — the page and
    // frame show through evenly, which per-child alpha could not produce.
    private static GraphicalUiElement BuildGroupAlpha()
    {
        var holder = BuildFrame(150, 110);

        var group = new ContainerRuntime();
        group.Width = 150;
        group.Height = 110;
        group.IsRenderTarget = true;
        group.Alpha = 128;
        group.AddChild(Rect(10, 10, 90, 70, new Color(220, 60, 60, 255)));
        group.AddChild(Rect(55, 35, 90, 70, new Color(60, 120, 220, 255)));
        holder.AddChild(group);
        return holder;
    }

    // Additive blend on the render target: the flattened group adds its color to the background, so
    // overlapping the gray frame brightens it rather than replacing it.
    private static GraphicalUiElement BuildAdditiveGroup()
    {
        var holder = BuildFrame(150, 110);

        var group = new ContainerRuntime();
        group.Width = 150;
        group.Height = 110;
        group.IsRenderTarget = true;
        group.Blend = Blend.Additive;
        group.AddChild(Rect(10, 10, 90, 70, new Color(120, 40, 40, 255)));
        group.AddChild(Rect(55, 35, 90, 70, new Color(40, 70, 120, 255)));
        holder.AddChild(group);
        return holder;
    }

    // Outer render target containing a nested render-target group followed by a semi-transparent
    // sibling, shown beside a direct-draw reference of the same semi-transparent rect over the same
    // frame. The invariant: the in-RT sibling (right block of the "in RT" swatch) must look identical
    // to the "direct ref" swatch. (raylib matches; MonoGame's RT path renders the sibling darker —
    // a pre-existing #816 straight-alpha render-target quirk, tracked separately.)
    private static GraphicalUiElement BuildNestedWithSibling()
    {
        var row = new ContainerRuntime();
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 12;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;

        row.AddChild(BuildSwatch("in RT", BuildNestedInRenderTarget()));
        row.AddChild(BuildSwatch("direct ref", BuildDirectReference()));
        return row;
    }

    private static ContainerRuntime BuildSwatch(string label, GraphicalUiElement body)
    {
        var swatch = new ContainerRuntime();
        swatch.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        swatch.StackSpacing = 3;
        swatch.WidthUnits = DimensionUnitType.RelativeToChildren;
        swatch.HeightUnits = DimensionUnitType.RelativeToChildren;
        swatch.Width = 0;
        swatch.Height = 0;

        var caption = new TextRuntime();
        caption.Text = label;
        caption.Color = new Color(180, 180, 180, 255);
        swatch.AddChild(caption);
        swatch.AddChild(body);
        return swatch;
    }

    private static GraphicalUiElement BuildNestedInRenderTarget()
    {
        var holder = BuildFrame(150, 110);

        var outer = new ContainerRuntime();
        outer.Width = 150;
        outer.Height = 110;
        outer.IsRenderTarget = true;

        var inner = new ContainerRuntime();
        inner.X = 8;
        inner.Y = 8;
        inner.Width = 64;
        inner.Height = 94;
        inner.IsRenderTarget = true;
        inner.Alpha = 200;
        inner.AddChild(Rect(0, 0, 64, 94, new Color(230, 120, 40, 255)));

        var sibling = Rect(80, 8, 62, 94, new Color(255, 255, 255, 128));

        outer.AddChild(inner);
        outer.AddChild(sibling);
        holder.AddChild(outer);
        return holder;
    }

    // The same semi-transparent white rect over the same frame color, drawn directly (no render
    // target). This is the ground truth the in-RT sibling must match.
    private static GraphicalUiElement BuildDirectReference()
    {
        var holder = BuildFrame(70, 110);
        holder.AddChild(Rect(8, 8, 62, 94, new Color(255, 255, 255, 128)));
        return holder;
    }

    // The render target is sized to the container's bounds, so a child larger than the container is
    // truncated to it. A checkerboard larger than the 90x90 target, offset so its cells are cut
    // mid-cell at the boundary, makes the truncation unmistakable: full cells inside, partial cells
    // sliced at the target edge, and clean frame margin beyond (the pattern simply stops at the
    // boundary). A solid rect would just look like a smaller rect. This is texture-size truncation,
    // not the ClipsChildren scissor path (#3440).
    //
    // NOTE: these comparison cells must use only primitives that render identically on both backends
    // with no extra dependencies — i.e. solid ColoredRectangleRuntime. Do NOT use CircleRuntime /
    // RoundedRectangleRuntime here: they route through Apos.Shapes, which this sample does not
    // reference, so they'd render on raylib but not here — misleading in a comparison.
    private static GraphicalUiElement BuildOverflow()
    {
        var holder = BuildFrame(150, 110);

        var group = new ContainerRuntime();
        group.X = 30;
        group.Y = 10;
        group.Width = 90;
        group.Height = 90;
        group.IsRenderTarget = true;

        var board = BuildCheckerboard(8, 8, 22, new Color(80, 200, 120, 255), new Color(230, 150, 40, 255));
        board.X = -18;
        board.Y = -18;
        group.AddChild(board);

        holder.AddChild(group);
        return holder;
    }

    // A grid of alternating solid-color cells, larger than the render target it goes in so the
    // pattern is visibly sliced at the target boundary.
    private static ContainerRuntime BuildCheckerboard(int columns, int rows, float cellSize, Color a, Color b)
    {
        var board = new ContainerRuntime();
        board.Width = columns * cellSize;
        board.Height = rows * cellSize;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Color color = (row + column) % 2 == 0 ? a : b;
                board.AddChild(Rect(column * cellSize, row * cellSize, cellSize, cellSize, color));
            }
        }
        return board;
    }

    // clip-inside-RT: deferred, see #3440. A "ClipsChildren descendant inside an RT" cell used to
    // live here; kept out to stay parallel with the raylib sample, where that case is unsupported.
}
