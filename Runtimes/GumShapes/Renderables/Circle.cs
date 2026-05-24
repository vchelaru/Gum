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

    public override void Render(ISystemManagers managers)
    {
        var sb = ShapeRenderer.ShapeBatch;

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();

        // Issue #2852: center on the actual bounding box and use the smaller dimension as
        // the radius so a non-square Circle fits within its box (matches SkiaGum).
        var center = new Microsoft.Xna.Framework.Vector2(
            absoluteLeft + Width / 2.0f,
            absoluteTop + Height / 2.0f);

        var radius = System.Math.Min(Width, Height) / 2.0f;

        if(HasDropshadow)
        {
            var shadowLeft = absoluteLeft + DropshadowOffsetX + DropshadowBlurX;
            var shadowTop = absoluteTop + DropshadowOffsetY + DropshadowBlurY;

            var dropshadowCenter = center;
            dropshadowCenter.X += DropshadowOffsetX;
            dropshadowCenter.Y += DropshadowOffsetY;

            RenderInternal(sb, shadowLeft, shadowTop, dropshadowCenter, radius - DropshadowBlurX / 2f,
                MathFunctions.RoundToInt(DropshadowBlurX),
                StrokeWidth - DropshadowBlurX,
                EffectiveDropshadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, center, radius, IsAntialiased ? 1 : 0, StrokeWidth);
    }

    private void RenderInternal(ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Microsoft.Xna.Framework.Vector2 center,
        float radius,
        int antiAliasSize,
        float strokeWidth,
        Color? forcedColor = null)
    {
        if (!IsFilled && StrokeDashLength > 0 && StrokeGapLength > 0 && strokeWidth > 0 && radius > 0)
        {
            RenderDashed(sb, absoluteLeft, absoluteTop, center, radius, antiAliasSize, strokeWidth, forcedColor);
            return;
        }

        // See RoundedRectangle for more info
        if (antiAliasSize != 0)
        {
            center.X += .5f;
            center.Y += .5f;
            radius -= .5f;
        }

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
            // bleed. Clamped at 0 so a runaway inset (larger than the radius) can't render an
            // inverted disk. Only the fill branch consumes this; stroke ignores it.
            float fillRadius = System.Math.Max(0f, radius - FillRadiusInset);

            if (UseGradient && forcedColor == null)
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop);

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
            if(UseGradient && forcedColor == null)
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop);

                var transparentGradient = gradient;
                transparentGradient.AC = new Color((int)gradient.AC.R, gradient.AC.G, gradient.AC.B, 0);
                transparentGradient.BC = new Color((int)gradient.BC.R, gradient.BC.G, gradient.BC.B, 0);

                sb.DrawCircle(center,
                    radius,
                    transparentGradient,
                    gradient,
                    strokeWidth,
                    aaSize: antiAliasSize);
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
                    aaSize: antiAliasSize);
            }
        }
    }

    // Ported from the upstream Apos.Shapes dashed-line PR
    // (https://github.com/Apostolique/Apos.Shapes/pull/31, DrawDashedCircle).
    // Walks dash starts around the circle perimeter and emits a partial ring per dash via
    // ShapeBatch.DrawRing, which batches into the same draw call as the surrounding shapes.
    // Adapted to Apos.Shapes 0.6.8's DrawRing(center, a1, a2, radius, thickness, ...) signature
    // (the upstream PR is built against the unreleased master signature with radius1/radius2);
    // the inner-fill pass and FitToPath logic are dropped because we only invoke this branch when
    // IsFilled is false and we want Skia-style exact dashing.
    private void RenderDashed(ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Microsoft.Xna.Framework.Vector2 center,
        float radius,
        int antiAliasSize,
        float strokeWidth,
        Color? forcedColor)
    {
        // Apos.Shapes 0.6.x DrawRing parameter naming is misleading - despite being called
        // (radius1, radius2), the shader's RingSDF treats them as (centerline, totalThickness):
        //   abs(length(p) - r) - th * 0.5, meaning the band spans [r - th/2, r + th/2].
        // The shader also does `radius1 -= 1f` internally before sampling, so we pass
        // centerline + 1 to compensate so the outer edge of the band lines up with where a
        // solid BorderCircle's outline sits.
        var ringRadius = radius - strokeWidth / 2f;
        var circumference = 2f * MathHelper.Pi * radius;

        // Issue #2790: when AA is on, each dash's tangential end-cap halo leaks into the
        // neighboring gap from each side, smearing dotted patterns into near-continuous
        // rings. Shift 1.5 * aaSize into the gap and trim 0.5 * aaSize off each dash so the
        // gap opens up noticeably while the dashes shrink slightly to compensate. Total
        // period grows by aaSize so the perimeter still has one or two fewer dashes overall,
        // but each one reads as a discrete dot. Dash length floored at a small epsilon so a
        // tight 1-px-dash pattern doesn't push 0 (Apos won't render a zero-length dash).
        var effectiveGapLen = StrokeGapLength + 1.5f * antiAliasSize;
        var dashLen = MathHelper.Max(0.01f, StrokeDashLength - 0.5f * antiAliasSize);
        var period = dashLen + effectiveGapLen;
        if (period <= 0) return;

        // Build the stroke "paint" once and pass the same Gradient/Color to every dash so the
        // gradient looks continuous across the dashed border (each dash samples the same world-
        // space gradient rather than restarting per-segment). GetGradient already returns world
        // coords, mirroring how upstream's DrawDashedCircle calls GradientToWorld + IsLocal=false.
        Gradient? gradient = (UseGradient && forcedColor == null)
            ? base.GetGradient(absoluteLeft, absoluteTop)
            : null;
        var color = forcedColor ?? Color;

        for (float t = 0; t < circumference; t += period)
        {
            var dashEnd = MathHelper.Min(t + dashLen, circumference);
            // Arc length / radius = swept angle. Apos.Shapes DrawRing expects start < end in math
            // (CCW) radians; user-facing CW angles only enter via Arc.StartAngle, which is none of
            // our concern here since we're walking the perimeter from angle 0.
            var a1 = t / radius;
            var a2 = dashEnd / radius;

            if (gradient is Gradient g)
            {
                sb.DrawRing(center, a1, a2, ringRadius + 1f, strokeWidth, g, g, 1, aaSize: antiAliasSize);
            }
            else
            {
                sb.DrawRing(center, a1, a2, ringRadius + 1f, strokeWidth, color, color, 1, aaSize: antiAliasSize);
            }
        }
    }
}
