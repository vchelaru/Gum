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
        // clip-inside-RT: deferred, see #3440 (fails under CI software GL) — cell removed so the
        // sample doesn't show a ClipsChildren descendant rendering unclipped inside a render target.
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
    // sibling. The nested composite toggles blend mid-bake; the sibling must still composite cleanly
    // (no dark fringe) — the whole point of re-establishing the premultiply pass after a blend toggle.
    static GraphicalUiElement BuildNestedWithSibling()
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

    // The render target is sized to the container's bounds, so a child larger than the container is
    // clipped to it (MonoGame-parity behavior). The green child is 220x220 but the target is 90x90.
    static GraphicalUiElement BuildOverflow()
    {
        ContainerRuntime holder = BuildFrame(150, 110);

        ContainerRuntime group = new();
        group.X = 30;
        group.Y = 10;
        group.Width = 90;
        group.Height = 90;
        group.IsRenderTarget = true;
        group.Children.Add(Rect(0, 0, 220, 220, new Color((byte)80, (byte)200, (byte)120, (byte)255)));
        holder.Children.Add(group);
        return holder;
    }

    // clip-inside-RT: deferred, see #3440. A "ClipsChildren descendant inside an RT" cell used to
    // live here, but that case is unsupported (fails under CI software GL), so it was removed rather
    // than show a descendant rendering unclipped.
}
