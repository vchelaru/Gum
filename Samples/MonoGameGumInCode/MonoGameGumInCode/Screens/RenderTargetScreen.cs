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
        root.AddChild(BuildCell("ClipsChildren inside RT", BuildClipsChildrenInside()));
        root.AddChild(BuildCell("Sprite shows RT texture", BuildSpriteFromRenderTarget()));
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
    // truncated to it. An OUTLINED circle bigger than the 90x90 target makes the truncation
    // unmistakable: the top-left arc curves inside the target while the right/bottom of the ring is
    // sliced dead flat at the container boundary, with no green beyond. A curve cut to a straight edge
    // reads as "clipped" in a way a rectangle (which just looks like a smaller rectangle) never can.
    // This is texture-size truncation, not the ClipsChildren scissor path (#3440).
    //
    // NOTE: use an OUTLINE circle (leave IsFilled off, set Color for the ring). It must render the
    // same on both backends: this sample's CircleRuntime is backed by the core SpriteBatch LineCircle,
    // which draws an outline only (no Apos.Shapes needed), and raylib's CircleRuntime defaults to a
    // stroke-only outline too. A FILLED circle would render filled on raylib but only as an outline
    // here — a mismatch that misleads in a comparison.
    // Set the ring via Color, NOT StrokeColor: this sample's core LineCircle exposes only Color, so
    // StrokeColor is raylib/Sokol-only and switching to it would break this build. Ignore the CS0618
    // deprecation on Color here — it does not apply to this outline-only cross-backend case.
    private static GraphicalUiElement BuildOverflow()
    {
        var holder = BuildFrame(150, 110);

        var group = new ContainerRuntime();
        group.X = 30;
        group.Y = 10;
        group.Width = 90;
        group.Height = 90;
        group.IsRenderTarget = true;

        var circle = new CircleRuntime();
        circle.X = 8;
        circle.Y = 8;
        circle.Width = 140;
        circle.Height = 140;
        circle.Color = new Color(80, 200, 120, 255);
        group.AddChild(circle);

        holder.AddChild(group);
        return holder;
    }

    // A ClipsChildren descendant inside the render target (#3440). The clipped child is an OUTLINED
    // circle far larger than the clip window: its right and bottom arcs are sliced dead flat at the
    // clip boundary, so the clip is unmistakable — a curve cut to a straight edge. (Clipping a
    // rectangle to a rectangle just yields a smaller rectangle and demonstrates nothing, which is why
    // the child must be a circle.)
    //
    // Same outline-circle rules as BuildOverflow: leave IsFilled off and set the ring via Color (NOT
    // StrokeColor) so it renders identically on MonoGame and raylib. The clip window is a sub-region
    // of the render target, so the empty area to its right is the RT extending past the clip. Kept
    // identical to the raylib sample's BuildClipsChildrenInside.
    private static GraphicalUiElement BuildClipsChildrenInside()
    {
        var holder = BuildFrame(150, 110);

        var group = new ContainerRuntime();
        group.Width = 150;
        group.Height = 110;
        group.IsRenderTarget = true;

        var clip = new ContainerRuntime();
        clip.X = 10;
        clip.Y = 10;
        clip.Width = 65;
        clip.Height = 90;
        clip.ClipsChildren = true;

        var circle = new CircleRuntime();
        circle.X = 5;
        circle.Y = 5;
        circle.Width = 120;
        circle.Height = 120;
        circle.Color = new Color(220, 60, 60, 255);
        clip.AddChild(circle);

        group.AddChild(clip);
        holder.AddChild(group);
        return holder;
    }

    // A Sprite whose RenderTargetTextureSource points at a DIFFERENT (sibling) container's baked
    // render target displays that texture like a regular one — scalable/rotatable/stackable. Kept
    // identical to the raylib sample's BuildSpriteFromRenderTarget (#3449) so the two apps compare
    // directly; the sprite is drawn larger and rotated, proving it's a live reference to the
    // source's bake rather than a duplicated draw of the same content.
    private static GraphicalUiElement BuildSpriteFromRenderTarget()
    {
        var row = new ContainerRuntime();
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 12;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;

        var source = new ContainerRuntime();
        source.Width = 70;
        source.Height = 70;
        source.IsRenderTarget = true;
        source.AddChild(Rect(0, 0, 70, 35, new Color(220, 60, 60, 255)));
        source.AddChild(Rect(0, 35, 70, 35, new Color(60, 120, 220, 255)));

        var sprite = new SpriteRuntime();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = 100;
        sprite.Height = 100;
        sprite.RenderTargetTextureSource = source;
        // Kept identical to the raylib sample's sign so the two backends match (see its comment:
        // the pivot is the sprite's top-left corner, and a positive value swings the box up and
        // out of its cell).
        sprite.Rotation = -20;

        row.AddChild(BuildSwatch("source", source));
        row.AddChild(BuildSwatch("sprite (scaled + rotated)", sprite));
        return row;
    }
}
