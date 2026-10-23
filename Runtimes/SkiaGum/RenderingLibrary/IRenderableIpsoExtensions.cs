using SkiaSharp;

namespace RenderingLibrary.Graphics
{
    public static class IRenderableIpsoExtensions
    {
        /// <summary>
        /// Returns the axis-aligned bounds of the clip that applies to this renderable: its own
        /// rectangle if it clips its children, otherwise the nearest clipping ancestor's. Null when
        /// nothing clips it. The clip itself turns with a rotated clipper (see <see cref="ClipTo"/>).
        /// </summary>
        public static SKRect? GetEffectiveClipRect(this IRenderableIpso renderableIpso)
        {
            IRenderableIpso? clipper = renderableIpso.GetEffectiveClipper();
            return clipper?.GetClipTransform(out SKRect localRect).MapRect(localRect);
        }

        /// <summary>
        /// The renderable whose rectangle clips this one: itself if it clips its children,
        /// otherwise the nearest ancestor that does.
        /// </summary>
        public static IRenderableIpso? GetEffectiveClipper(this IRenderableIpso renderableIpso)
        {
            IRenderableIpso? current = renderableIpso;
            while (current != null && !current.ClipsChildren)
            {
                current = current.Parent;
            }
            return current;
        }

        /// <summary>
        /// The transform from the clipper's local rectangle (returned in <paramref name="localRect"/>)
        /// to world space: placed at its absolute position and turned by its absolute rotation, the
        /// same transform shapes draw with.
        /// </summary>
        public static SKMatrix GetClipTransform(this IRenderableIpso clipper, out SKRect localRect)
        {
            localRect = new SKRect(0, 0, clipper.Width, clipper.Height);
            return SKMatrix.CreateTranslation(clipper.GetAbsoluteX(), clipper.GetAbsoluteY())
                .PreConcat(SKMatrix.CreateRotationDegrees(-clipper.GetAbsoluteRotation()));
        }

        /// <summary>
        /// Intersects the canvas clip with the clipper's rectangle, turned with the clipper and its
        /// parents (#6604). The canvas matrix is left unchanged; call inside a Save/Restore pair.
        /// </summary>
        public static void ClipTo(this SKCanvas canvas, IRenderableIpso clipper)
        {
            SKMatrix toWorld = clipper.GetClipTransform(out SKRect localRect);
            if (clipper.GetAbsoluteRotation() == 0)
            {
                canvas.ClipRect(toWorld.MapRect(localRect));
            }
            else
            {
                SKMatrix matrixBeforeClip = canvas.TotalMatrix;
                canvas.Concat(in toWorld);
                canvas.ClipRect(localRect, antialias: true);
                canvas.SetMatrix(matrixBeforeClip);
            }
        }
    }
}
