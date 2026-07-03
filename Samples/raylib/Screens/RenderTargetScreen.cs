using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;

namespace Examples.Shapes;

// Render-to-texture gallery for issue #3434. Each cell wraps its content in a ContainerRuntime with
// IsRenderTarget = true, so the subtree bakes into an offscreen texture and composites back in place.
// The cases are chosen to exercise the bugs the feature's fixes address:
//   - Group alpha / additive: the whole group flattens, then composites with one blend (item 2 / fix 4).
//   - Nested RT + sibling: a nested render target composites first (toggling blend); the following
//     semi-transparent sibling must still composite cleanly with no dark fringe (fix 2).
//   - Overflow: the render target is sized to the container's bounds, so an over-large child is
//     clipped to the container — matching the MonoGame behavior.
//   - ClipsChildren inside the RT: a clipping descendant clips correctly within the target (fix 5).
internal class RenderTargetScreen : FrameworkElement
{
    public RenderTargetScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        root.WidthUnits = DimensionUnitType.RelativeToChildren;
        root.HeightUnits = DimensionUnitType.RelativeToChildren;
        root.Width = 0;
        root.Height = 0;
        root.StackSpacing = 28;
        root.X = 12;
        root.Y = 12;
        this.AddChild(root);

        root.Children.Add(BuildCell("Render to target", BuildBaseline()));
        root.Children.Add(BuildCell("Group alpha 50%", BuildGroupAlpha()));
        root.Children.Add(BuildCell("Additive group", BuildAdditiveGroup()));
        root.Children.Add(BuildCell("Nested RT + sibling", BuildNestedWithSibling()));
        root.Children.Add(BuildCell("Overflow clipped", BuildOverflow()));
        root.Children.Add(BuildCell("ClipsChildren inside RT", BuildClipsChildrenInside()));
        root.Children.Add(BuildCell("Sprite shows RT texture", BuildSpriteFromRenderTarget()));
    }

    static ContainerRuntime BuildCell(string caption, GraphicalUiElement body)
    {
        ContainerRuntime cell = new();
        cell.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        cell.StackSpacing = 6;
        cell.WidthUnits = DimensionUnitType.RelativeToChildren;
        cell.HeightUnits = DimensionUnitType.RelativeToChildren;
        cell.Width = 0;
        cell.Height = 0;

        TextRuntime header = new();
        header.Text = caption;
        header.Red = 220;
        header.Green = 220;
        header.Blue = 220;
        cell.Children.Add(header);
        cell.Children.Add(body);
        return cell;
    }

    // A mid-gray frame behind each demo so group transparency and clipping are visible against a
    // consistent backdrop rather than the page color.
    static ContainerRuntime BuildFrame(float width, float height)
    {
        ColoredRectangleRuntime frame = new();
        frame.Width = width;
        frame.Height = height;
        frame.Color = new Color((byte)70, (byte)70, (byte)90, (byte)255);

        ContainerRuntime holder = new();
        holder.Width = width;
        holder.Height = height;
        holder.Children.Add(frame);
        return holder;
    }

    static ColoredRectangleRuntime Rect(float x, float y, float width, float height, Color color)
    {
        ColoredRectangleRuntime rect = new();
        rect.X = x;
        rect.Y = y;
        rect.Width = width;
        rect.Height = height;
        rect.Color = color;
        return rect;
    }

    // Two overlapping opaque rects inside a render target — visually identical to drawing them
    // directly, proving the bake + composite round-trips content correctly.
    static GraphicalUiElement BuildBaseline()
    {
        ContainerRuntime holder = BuildFrame(150, 110);

        ContainerRuntime group = new();
        group.Width = 150;
        group.Height = 110;
        group.IsRenderTarget = true;
        group.Children.Add(Rect(10, 10, 90, 70, new Color((byte)220, (byte)60, (byte)60, (byte)255)));
        group.Children.Add(Rect(55, 35, 90, 70, new Color((byte)60, (byte)120, (byte)220, (byte)255)));
        holder.Children.Add(group);
        return holder;
    }

    // Same content as the baseline, but the container carries a 50% group alpha. Because the group
    // flattens to a texture first, the whole thing (overlap included) fades uniformly — the page
    // and frame show through evenly, which per-child alpha could not produce.
    static GraphicalUiElement BuildGroupAlpha()
    {
        ContainerRuntime holder = BuildFrame(150, 110);

        ContainerRuntime group = new();
        group.Width = 150;
        group.Height = 110;
        group.IsRenderTarget = true;
        group.Alpha = 128;
        group.Children.Add(Rect(10, 10, 90, 70, new Color((byte)220, (byte)60, (byte)60, (byte)255)));
        group.Children.Add(Rect(55, 35, 90, 70, new Color((byte)60, (byte)120, (byte)220, (byte)255)));
        holder.Children.Add(group);
        return holder;
    }

    // Additive blend on the render target: the flattened group adds its (premultiplied) color to the
    // background, so overlapping the gray frame brightens it rather than replacing it.
    static GraphicalUiElement BuildAdditiveGroup()
    {
        ContainerRuntime holder = BuildFrame(150, 110);

        ContainerRuntime group = new();
        group.Width = 150;
        group.Height = 110;
        group.IsRenderTarget = true;
        group.Blend = Blend.Additive;
        group.Children.Add(Rect(10, 10, 90, 70, new Color((byte)120, (byte)40, (byte)40, (byte)255)));
        group.Children.Add(Rect(55, 35, 90, 70, new Color((byte)40, (byte)70, (byte)120, (byte)255)));
        holder.Children.Add(group);
        return holder;
    }

    // Outer render target containing a nested render-target group followed by a semi-transparent
    // sibling, shown beside a direct-draw reference of the same semi-transparent rect over the same
    // frame. The invariant: the in-RT sibling (right block of the "in RT" swatch) must look identical
    // to the "direct ref" swatch. The nested composite toggles blend mid-bake; re-establishing the
    // premultiply pass is what keeps the sibling matching the reference (no dark fringe / no double
    // alpha).
    static GraphicalUiElement BuildNestedWithSibling()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 12;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;

        row.Children.Add(BuildSwatch("in RT", BuildNestedInRenderTarget()));
        row.Children.Add(BuildSwatch("direct ref", BuildDirectReference()));
        return row;
    }

    static ContainerRuntime BuildSwatch(string label, GraphicalUiElement body)
    {
        ContainerRuntime swatch = new();
        swatch.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        swatch.StackSpacing = 3;
        swatch.WidthUnits = DimensionUnitType.RelativeToChildren;
        swatch.HeightUnits = DimensionUnitType.RelativeToChildren;
        swatch.Width = 0;
        swatch.Height = 0;

        TextRuntime caption = new();
        caption.Text = label;
        caption.Red = 180;
        caption.Green = 180;
        caption.Blue = 180;
        swatch.Children.Add(caption);
        swatch.Children.Add(body);
        return swatch;
    }

    static GraphicalUiElement BuildNestedInRenderTarget()
    {
        ContainerRuntime holder = BuildFrame(150, 110);

        ContainerRuntime outer = new();
        outer.Width = 150;
        outer.Height = 110;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.X = 8;
        inner.Y = 8;
        inner.Width = 64;
        inner.Height = 94;
        inner.IsRenderTarget = true;
        inner.Alpha = 200;
        inner.Children.Add(Rect(0, 0, 64, 94, new Color((byte)230, (byte)120, (byte)40, (byte)255)));

        ColoredRectangleRuntime sibling =
            Rect(80, 8, 62, 94, new Color((byte)255, (byte)255, (byte)255, (byte)128));

        outer.Children.Add(inner);
        outer.Children.Add(sibling);
        holder.Children.Add(outer);
        return holder;
    }

    // The same semi-transparent white rect over the same frame color, drawn directly (no render
    // target). This is the ground truth the in-RT sibling must match.
    static GraphicalUiElement BuildDirectReference()
    {
        ContainerRuntime holder = BuildFrame(70, 110);
        holder.Children.Add(Rect(8, 8, 62, 94, new Color((byte)255, (byte)255, (byte)255, (byte)128)));
        return holder;
    }

    // The render target is sized to the container's bounds, so a child larger than the container is
    // truncated to it. An OUTLINED circle bigger than the 90x90 target makes the truncation
    // unmistakable: the top-left arc curves inside the target while the right/bottom of the ring is
    // sliced dead flat at the container boundary, with no green beyond. A curve cut to a straight edge
    // reads as "clipped" in a way a rectangle (which just looks like a smaller rectangle) never can.
    // This is texture-size truncation, not the ClipsChildren scissor path (#3440).
    //
    // NOTE: use an OUTLINE circle (leave IsFilled off, set Color for the ring). It must render the
    // same on both backends: MonoGame's CircleRuntime is backed by the core SpriteBatch LineCircle,
    // which draws an outline only (no Apos.Shapes needed), and raylib's CircleRuntime defaults to a
    // stroke-only outline too. A FILLED circle would render filled on raylib but only as an outline on
    // MonoGame — a mismatch that misleads in a comparison.
    // Set the ring via Color, NOT StrokeColor: MonoGame's core LineCircle exposes only Color, so
    // StrokeColor is raylib/Sokol-only and switching to it would break the MonoGame build. Ignore the
    // CS0618 deprecation on Color here — it does not apply to this outline-only cross-backend case.
    static GraphicalUiElement BuildOverflow()
    {
        ContainerRuntime holder = BuildFrame(150, 110);

        ContainerRuntime group = new();
        group.X = 30;
        group.Y = 10;
        group.Width = 90;
        group.Height = 90;
        group.IsRenderTarget = true;

        CircleRuntime circle = new();
        circle.X = 8;
        circle.Y = 8;
        circle.Width = 140;
        circle.Height = 140;
        circle.Color = new Color((byte)80, (byte)200, (byte)120, (byte)255);
        group.Children.Add(circle);

        holder.Children.Add(group);
        return holder;
    }

    // A ClipsChildren descendant inside the render target (#3440). The clip rect is rebased into
    // RT-local space during the bake, so it clips correctly on both hardware GL and software GL
    // (Mesa llvmpipe). The clipped child is an OUTLINED circle far larger than the clip window: its
    // right and bottom arcs are sliced dead flat at the clip boundary, so the clip is unmistakable —
    // a curve cut to a straight edge. (Clipping a rectangle to a rectangle just yields a smaller
    // rectangle and demonstrates nothing, which is why the child must be a circle.)
    //
    // Same outline-circle rules as BuildOverflow: leave IsFilled off and set the ring via Color (NOT
    // StrokeColor) so it renders identically on MonoGame and raylib. The clip window is a sub-region
    // of the render target, so the empty area to its right is the RT extending past the clip. Kept
    // identical to the MonoGame sample's BuildClipsChildrenInside.
    static GraphicalUiElement BuildClipsChildrenInside()
    {
        ContainerRuntime holder = BuildFrame(150, 110);

        ContainerRuntime group = new();
        group.Width = 150;
        group.Height = 110;
        group.IsRenderTarget = true;

        ContainerRuntime clip = new();
        clip.X = 10;
        clip.Y = 10;
        clip.Width = 65;
        clip.Height = 90;
        clip.ClipsChildren = true;

        CircleRuntime circle = new();
        circle.X = 5;
        circle.Y = 5;
        circle.Width = 120;
        circle.Height = 120;
        circle.Color = new Color((byte)220, (byte)60, (byte)60, (byte)255);
        clip.Children.Add(circle);

        group.Children.Add(clip);
        holder.Children.Add(group);
        return holder;
    }

    // A Sprite whose RenderTargetTextureSource points at a DIFFERENT (sibling) container's baked
    // render target displays that texture like a regular one — scalable/rotatable/stackable
    // (#3449, item 3 of #3434, deferred out of #3436). The sprite is drawn larger and rotated,
    // proving it's a live reference to the source's bake rather than a duplicated draw of the
    // same content.
    static GraphicalUiElement BuildSpriteFromRenderTarget()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 12;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;

        ContainerRuntime source = new();
        source.Width = 70;
        source.Height = 70;
        source.IsRenderTarget = true;
        source.Children.Add(Rect(0, 0, 70, 35, new Color((byte)220, (byte)60, (byte)60, (byte)255)));
        source.Children.Add(Rect(0, 35, 70, 35, new Color((byte)60, (byte)120, (byte)220, (byte)255)));

        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = 100;
        sprite.Height = 100;
        sprite.RenderTargetTextureSource = source;
        sprite.Rotation = 20;

        row.Children.Add(BuildSwatch("source", source));
        row.Children.Add(BuildSwatch("sprite (scaled + rotated)", sprite));
        return row;
    }
}
