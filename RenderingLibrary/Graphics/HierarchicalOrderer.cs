using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Default <see cref="IRenderableOrderer"/>. Emits a depth-first pre-order walk that mirrors
/// the legacy recursive main-pass walk in <c>Renderer</c>: visit each visible renderable,
/// emit a <see cref="DrawCommandKind.DrawRenderable"/>, and recurse into children when
/// <see cref="Renderer.RenderUsingHierarchy"/> is true. A node with
/// <see cref="IRenderableIpso.ClipsChildren"/> is bracketed by <see cref="DrawCommandKind.BeginClip"/>
/// / <see cref="DrawCommandKind.EndClip"/>. <see cref="IRenderableIpso.IsRenderTarget"/> nodes
/// short-circuit child recursion — their children were already baked into the cached texture
/// by the prerender phase and re-walking them would draw their pixels twice.
/// </summary>
public sealed class HierarchicalOrderer : IRenderableOrderer
{
    /// <summary>Shared stateless instance.</summary>
    public static readonly HierarchicalOrderer Instance = new HierarchicalOrderer();

    /// <inheritdoc/>
    public void BuildDrawList(Layer layer, List<DrawCommand> destination, Camera? camera = null)
    {
        destination.Clear();

        ClipBoundsSource clipBounds = camera != null
            ? new ClipBoundsSource(camera, layer)
            : default;

        AppendRenderables(layer.Renderables, clipBounds, null, destination);
    }

    /// <inheritdoc/>
    public void BuildDrawList(
        IList<IRenderableIpso> roots,
        List<DrawCommand> destination,
        ClipBoundsSource clipBounds = default)
    {
        destination.Clear();
        AppendRenderables(roots, clipBounds, null, destination);
    }

    private static void AppendRenderables(
        IList<IRenderableIpso> renderables,
        ClipBoundsSource clipBounds,
        System.Drawing.Rectangle? activeClip,
        List<DrawCommand> destination)
    {
        int count = renderables.Count;
        for (int i = 0; i < count; i++)
        {
            IRenderableIpso renderable = renderables[i];
            if (!renderable.Visible)
            {
                continue;
            }

            // #2998 off-screen cull: when a clip is active, skip this renderable and its whole
            // subtree if its bounds fall entirely outside the clip. Gated on a resolvable mapping
            // (the render path) so the mapping-less order-only unit tests are unaffected.
            if (clipBounds.CanResolveCullTestBounds
                && activeClip.HasValue
                && CameraScissorExtensions.CullOffscreenWhenClipped
                && CameraScissorExtensions.IsFullyOutside(
                    clipBounds.GetCullTestBounds(renderable),
                    activeClip.Value,
                    CameraScissorExtensions.OffscreenCullMarginInPixels))
            {
                continue;
            }

            bool clips = renderable.ClipsChildren;
            if (clips)
            {
                destination.Add(new DrawCommand(DrawCommandKind.BeginClip, renderable));
            }

            destination.Add(new DrawCommand(DrawCommandKind.DrawRenderable, renderable));

            if (Renderer.RenderUsingHierarchy && !renderable.IsRenderTarget)
            {
                ObservableCollection<IRenderableIpso> children = renderable.Children;
                if (children != null && children.Count > 0)
                {
                    // Entering a clipper narrows the active clip for descendants (intersect, mirroring
                    // Renderer.AdjustRenderStates). Without a mapping we can't compute it, so leave it null.
                    System.Drawing.Rectangle? childClip = activeClip;
                    if (clips && clipBounds.CanResolveScissorRectangle)
                    {
                        System.Drawing.Rectangle thisClip = clipBounds.GetScissorRectangle(renderable);
                        childClip = activeClip.HasValue
                            ? System.Drawing.Rectangle.Intersect(activeClip.Value, thisClip)
                            : thisClip;
                    }
                    AppendRenderables(children, clipBounds, childClip, destination);
                }
            }

            if (clips)
            {
                destination.Add(new DrawCommand(DrawCommandKind.EndClip, renderable));
            }
        }
    }
}
