using Apos.Shapes;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.Renderables;

public class Circle : RenderableShapeBase,
    Gum.GueDeriving.IFilledCircleRenderable,
    Gum.GueDeriving.IStrokedCircleRenderable,
    Gum.GueDeriving.IGradientedRenderable,
    Gum.GueDeriving.IAntialiasedRenderable,
    Gum.GueDeriving.IDropshadowRenderable,
    Gum.GueDeriving.IDashedStrokeRenderable,
    System.ICloneable
{
    /// <summary>
    /// Issue #2790 — required by <see cref="Gum.Wireframe.GraphicalUiElement.Clone"/> so
    /// shape runtimes can be deep-copied. MemberwiseClone copies the property bag; the
    /// children collection, parent pointer, and the OnPreRender hook (which still points
    /// back at the source runtime) are reset so the clone is structurally independent.
    /// CircleRuntime.Clone is responsible for re-wiring OnPreRender against the new runtime.
    /// </summary>
    public object Clone()
    {
        Circle clone = (Circle)MemberwiseClone();
        clone._children = new();
        clone._parent = null;
        clone.OnPreRender = null;
        return clone;
    }

    // IGradientedRenderable, IAntialiasedRenderable, IDropshadowRenderable, and
    // IDashedStrokeRenderable are all satisfied entirely by the property bag inherited from
    // RenderableShapeBase — every member name and type lines up. The interface declarations
    // exist only so CircleRuntime can pattern-match on each slot without coupling to the
    // concrete Apos.Shapes Circle type.
    /// <inheritdoc/>
    /// <remarks>
    /// Issue #2852 — when Width and Height differ, the rendered radius is
    /// <c>min(Width, Height) / 2</c> so the circle fits inside its bounding box centered,
    /// matching SkiaGum's behavior (the Gum tool/viewport). Setting <see cref="Radius"/>
    /// keeps Width and Height in lockstep so the shape is square. Implemented to satisfy
    /// both <see cref="Gum.GueDeriving.IFilledCircleRenderable"/> and
    /// <see cref="Gum.GueDeriving.IStrokedCircleRenderable"/>; which slot any given Circle
    /// instance fills is determined by its <c>IsFilled</c> flag, set by the factory in
    /// <c>AposShapeRuntime.RegisterRuntimeTypes</c>.
    /// </remarks>
    public float Radius
    {
        get => System.Math.Min(Width, Height) / 2f;
        set
        {
            Width = value * 2;
            Height = value * 2;
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Issue #2834 — pushed by <see cref="Gum.GueDeriving.CircleRuntime.PreRender"/> on the
    /// fill slot only when a visible stroke is present. Applied as a radius subtraction at
    /// render time; Width/Height are left untouched so the layout system stays the sole
    /// source of size truth (mutating Width here would feed back into layout because the
    /// fill instance is the runtime's contained sizing object).
    /// </remarks>
    public float FillRadiusInset { get; set; }

    /// <summary>
    /// Issue #2958 — the body and the dropshadow share <see cref="RenderInternal"/>, but
    /// <see cref="FillRadiusInset"/> only applies to the body pass (it exists to hide the seam
    /// between fill and stroke per #2834). The shadow must draw at the full <paramref name="radius"/>
    /// so its outer edge lines up with the body's outer edge — otherwise the shadow comes up
    /// short by <c>FillRadiusInset</c> world units (1 px at zoom = 1, more when zoomed in).
    /// Clamped at 0 on the body pass so a runaway inset can't render an inverted disk.
    /// </summary>
    public float ComputeFillDrawRadius(float radius, bool isShadowPass)
    {
        if (isShadowPass)
        {
            return radius;
        }
        return System.Math.Max(0f, radius - FillRadiusInset);
    }

    /// <summary>
    /// Insets the draw circle by the pixel-center AA alignment offset
    /// (<see cref="RenderableShapeBase.GetAntiAliasWorldOffset"/>): the radius shrinks by the
    /// offset while the center stays FIXED, so every edge insets symmetrically. Shifting the
    /// center (the original <c>center += 0.5</c>) insets the top/left edge by twice the offset
    /// and leaves the bottom/right flush, biasing the whole disk down and right (visible
    /// spill-over when zoomed out). Scaled by <paramref name="cameraZoom"/> so the inset holds a
    /// constant on-screen size. Returns the circle unchanged when antialiasing is off.
    /// </summary>
    public (Vector2 center, float radius) ApplyAntiAliasInset(Vector2 center, float radius, int antiAliasSize, float cameraZoom)
    {
        var offset = GetAntiAliasWorldOffset(antiAliasSize, cameraZoom);
        if (offset == 0f)
        {
            return (center, radius);
        }
        return (center, radius - offset);
    }

    public override void Render(ISystemManagers managers)
    {
        // Issue #2950 follow-up — stroke-only Circle with StrokeWidth = 0 would otherwise draw
        // a hairline AA ring in the stroke color (Apos paints a 1 px AA fringe regardless of
        // strokeWidth). Skip the entire render — the shadow alpha would already be 0 via the
        // ComputeStrokeShadowDrawParameters fade, so no visual is lost.
        if (!HasVisibleOutput)
        {
            return;
        }

        // Issue #2937 — re-open the shared ShapeBatch with this shape's blend if it differs
        // from the one the batch is currently using (no-op when it matches).
        ShapeRenderer.EnsureBlend(this);

        var sb = ShapeRenderer.ShapeBatch;

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();

        // Issue #2925 — rotation is around the GUE's top-left origin (Gum convention), so the
        // (Width/2, Height/2) offset from top-left to circle center must be rotated by the
        // absolute rotation. DrawCircle has no rotation parameter (a true circle is rotation-
        // symmetric, so only the center position needs rotating). Issue #2852: also center on
        // the actual bounding box and use the smaller dimension as the radius so a non-square
        // Circle fits within its box (matches SkiaGum).
        var rotationRadians = MathHelper.ToRadians(-this.GetAbsoluteRotation());
        var center = GetRotatedCenter(absoluteLeft, absoluteTop, Width, Height, rotationRadians);

        var radius = System.Math.Min(Width, Height) / 2.0f;

        // Resolve camera zoom once: it scales the pixel-center AA inset (RenderInternal) and the
        // dropshadow halo/geometry so both hold a constant on-screen size as the tool zooms.
        var cameraZoom = (managers as RenderingLibrary.SystemManagers)?.Renderer?.Camera?.Zoom ?? 1f;

        if(HasDropshadow)
        {
            var shadowLeft = absoluteLeft + DropshadowOffsetX + DropshadowBlurX;
            var shadowTop = absoluteTop + DropshadowOffsetY + DropshadowBlurY;

            var dropshadowCenter = center;
            dropshadowCenter.X += DropshadowOffsetX;
            dropshadowCenter.Y += DropshadowOffsetY;

            // Issue #2950 — when stroke <= blur on a stroke-only Circle, fade the shadow's
            // starting alpha and clamp lineThickness positive so Apos still draws (otherwise
            // the shadow disappears entirely).
            (float shadowStrokeWidth, Color shadowColor) =
                ComputeStrokeShadowDrawParameters(EffectiveDropshadowColor);

            float shadowRadius;
            int shadowAaSize;
            if (IsFilled)
            {
                // Issue #2950 — filled-disk strict-anchor shadow geometry. Returns the (rDisk,
                // aaSize, alphaScale) triple that puts the smoothstep falloff's 50% line exactly
                // at the host radius. When blur exceeds 2R the inner ramp edge would be negative;
                // the helper truncates to rDisk=0 and reduces base alpha so the curve still passes
                // through (R, 0.5) and (R + B/2, 0) — center becomes translucent, which is correct
                // for big-blur cases. Camera zoom is folded into aaSize so the visible halo holds a
                // constant world extent under zoom.
                (float diskRadius, int diskAaSize, float shadowAlphaScale) =
                    ComputeShadowDrawGeometry(radius, cameraZoom);
                shadowRadius = diskRadius;
                shadowAaSize = diskAaSize;
                if (shadowAlphaScale < 1f)
                {
                    shadowColor = new Color(
                        shadowColor.R, shadowColor.G, shadowColor.B,
                        (byte)(shadowColor.A * shadowAlphaScale));
                }
            }
            else
            {
                // Issue #2977 — a stroke-only shadow is a ring, not a disk, so the filled-disk
                // anchor above (which pulls the radius inward by blur/2) would drag the ring
                // inward as blur grows past the stroke width, making the shape look like it
                // contracts. Anchor the ring centerline at the body stroke's centerline instead;
                // blur only widens the AA halo. ComputeStrokeShadowDrawParameters already faded
                // the alpha for the large-blur case, so no alphaScale is applied here.
                shadowRadius = ComputeStrokeShadowDrawRadius(radius, shadowStrokeWidth);
                shadowAaSize = GetShadowAntiAliasSize(cameraZoom);
            }

            RenderInternal(sb, shadowLeft, shadowTop, dropshadowCenter, shadowRadius,
                shadowAaSize,
                shadowStrokeWidth,
                rotationRadians,
                cameraZoom,
                shadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, center, radius, IsAntialiased ? 1 : 0, StrokeWidth, rotationRadians, cameraZoom);
    }

    private void RenderInternal(ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Microsoft.Xna.Framework.Vector2 center,
        float radius,
        int antiAliasSize,
        float strokeWidth,
        float rotationRadians,
        float cameraZoom,
        Color? forcedColor = null)
    {
        // Issue #3972 — Apos.Shapes 0.7.5 added a native DashStyle parameter to DrawCircle, so
        // dashing now flows through the same draw call as a solid stroke instead of a hand-rolled
        // perimeter walk. DashSnap.Off preserves Gum's historical look (fixed period from a fixed
        // start, last dash clipped wherever it lands) rather than upstream's new seamless-tiling
        // default, so existing saved projects render unchanged.
        DashStyle dash = default;
        if (!IsFilled && StrokeDashLength > 0 && StrokeGapLength > 0 && strokeWidth > 0 && radius > 0)
        {
            dash = new DashStyle(StrokeDashLength, StrokeGapLength, cap: DashCap.Butt, snap: DashSnap.Off);
        }

        // See RoundedRectangle for more info. The offset is half a SCREEN pixel, so it is divided
        // by cameraZoom (via ApplyAntiAliasInset) — otherwise the inset grows in world space as
        // the tool zooms in and the circle pulls visibly inward.
        (center, radius) = ApplyAntiAliasInset(center, radius, antiAliasSize, cameraZoom);

        if (IsFilled)
        {
            // as outlined here:
            // https://github.com/Apostolique/Apos.Shapes/issues/12
            // There is a strange issue with rendering. However, adding 1 antialias with 1 border results in teh correct size and no artifacts.
            //
            // NOTE FOR CALLERS: the Apos shader treats stroke thickness = 0 as "don't draw"
            // even when aaSize > 0 — the AA halo cannot render without a non-zero stroke to
            // attach to. Confirmed empirically while wiring CircleRuntime's AA-bloom
            // compensation (#2790). If you need a thin-as-possible AA-only stroke, push a
            // small positive epsilon (e.g. 0.01) instead of 0; the 1 px AA halo dominates and
            // the sub-pixel stroke is invisible.

            // Issue #2834 — pull the fill's outer edge inside the companion stroke slot's
            // opaque band so the two AA boundaries don't composite into a visible color
            // bleed. Only the fill branch consumes this; stroke ignores it. Issue #2958 —
            // the shadow pass shares this branch but must NOT inherit the inset, or its
            // outer edge falls short of the body's outer edge.
            float fillRadius = ComputeFillDrawRadius(radius, isShadowPass: forcedColor != null);

            if (ShouldPaintGradient(forcedColor))
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop, rotationRadians);

                sb.DrawCircle(
                    center,
                    fillRadius,
                    gradient,
                    gradient,
                    1,
                    antiAliasSize);
            }
            else
            {
                var color = forcedColor ?? this.Color;

                sb.DrawCircle(center,
                    fillRadius,
                    color,
                    color,
                    1,
                    aaSize: antiAliasSize);
            }
        }
        else
        {
            if(ShouldPaintGradient(forcedColor))
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop, rotationRadians);

                var transparentGradient = gradient;
                transparentGradient.AC = new Color((int)gradient.AC.R, gradient.AC.G, gradient.AC.B, 0);
                transparentGradient.BC = new Color((int)gradient.BC.R, gradient.BC.G, gradient.BC.B, 0);

                sb.DrawCircle(center,
                    radius,
                    transparentGradient,
                    gradient,
                    strokeWidth,
                    aaSize: antiAliasSize,
                    dash: dash);
            }
            else
            {
                var color = forcedColor ?? this.Color;

                var transparentColor = color;
                transparentColor.A = 0;

                sb.DrawCircle(center,
                    radius,
                    transparentColor,
                    color,
                    strokeWidth,
                    aaSize: antiAliasSize,
                    dash: dash);
            }
        }
    }
}
