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

public class RoundedRectangle : RenderableShapeBase,
    Gum.GueDeriving.IFilledRectangleRenderable,
    Gum.GueDeriving.IStrokedRectangleRenderable,
    Gum.GueDeriving.IGradientedRenderable,
    Gum.GueDeriving.IAntialiasedRenderable,
    Gum.GueDeriving.IDropshadowRenderable,
    Gum.GueDeriving.IDashedStrokeRenderable,
    System.ICloneable
{
    // IGradientedRenderable, IAntialiasedRenderable, IDropshadowRenderable, and
    // IDashedStrokeRenderable are all satisfied entirely by the property bag inherited from
    // RenderableShapeBase — declared here so RectangleRuntime (#2818) can pattern-match
    // those surfaces the same way CircleRuntime does, without coupling to the concrete
    // Apos.Shapes RoundedRectangle type.

    /// <summary>
    /// Issue #2818 — required by <see cref="Gum.Wireframe.GraphicalUiElement.Clone"/> so
    /// shape runtimes can be deep-copied. Mirrors the Circle.Clone implementation: the
    /// children collection, parent pointer, and OnPreRender hook (which still points back at
    /// the source runtime) are reset so the clone is structurally independent.
    /// RectangleRuntime.Clone is responsible for re-wiring OnPreRender against the new runtime.
    /// </summary>
    public object Clone()
    {
        RoundedRectangle clone = (RoundedRectangle)MemberwiseClone();
        clone._children = new();
        clone._parent = null;
        clone.OnPreRender = null;
        return clone;
    }

    /// <summary>
    /// Rounded-corner radius in pixels. Apos.Shapes' <c>RoundedRectangle</c> honors this on
    /// both fill and stroke draws, so <c>RectangleRuntime</c> pushes the same value to both
    /// slots. Per-corner overrides via the <c>CustomRadius*</c> properties are unchanged.
    /// </summary>
    public float CornerRadius { get; set; }

    // Per-corner overrides. When any of these is non-null, the fill/border draws use the
    // CornerRadii overload added in Apos.Shapes 0.6.9 (PR #32) so each corner is rasterized
    // independently. Nulls fall back to CornerRadius. Mirrors the Skia renderable's API.
    // The dashed-stroke path below intentionally still uses a single radius — its arc/edge
    // segmentation assumes uniform corners.
    public float? CustomRadiusTopLeft { get; set; }
    public float? CustomRadiusTopRight { get; set; }
    public float? CustomRadiusBottomRight { get; set; }
    public float? CustomRadiusBottomLeft { get; set; }

    /// <inheritdoc/>
    /// <remarks>
    /// Rectangle analog of <see cref="Circle.FillRadiusInset"/> (#2834). Pushed by
    /// <see cref="Gum.GueDeriving.RectangleRuntime.PreRender"/> on the fill slot only when a
    /// visible stroke is present. Applied as a symmetric inset at render time via
    /// <see cref="ComputeFillDrawRect"/>; Width/Height are left untouched so the layout system
    /// stays the sole source of size truth (mutating Width here would feed back into layout
    /// because the fill instance is the runtime's contained sizing object).
    /// </remarks>
    public float FillInset { get; set; }

    /// <summary>
    /// Insets the fill draw rect by <see cref="FillInset"/> on every side, keeping the rect
    /// centered (so the shape doesn't drift) and clamping each dimension at 0 rather than
    /// inverting. Mirrors <see cref="Circle.ComputeFillDrawRadius"/>: only the body pass
    /// consumes the inset; the shadow pass (<paramref name="isShadowPass"/>) draws at full size
    /// so its outer edge lines up with the body's outer edge (#2958). Centering keeps it
    /// correct under rotation — Apos rotates the rect around <c>position + size/2</c>, which the
    /// symmetric inset leaves unchanged.
    /// </summary>
    public (Vector2 position, Vector2 size) ComputeFillDrawRect(Vector2 position, Vector2 size, bool isShadowPass)
    {
        if (isShadowPass || FillInset <= 0f)
        {
            return (position, size);
        }

        var newSize = new Vector2(
            System.Math.Max(0f, size.X - 2f * FillInset),
            System.Math.Max(0f, size.Y - 2f * FillInset));
        var newPosition = new Vector2(
            position.X + (size.X - newSize.X) / 2f,
            position.Y + (size.Y - newSize.Y) / 2f);
        return (newPosition, newSize);
    }

    /// <summary>
    /// Issue #3268 — corner-radius companion to <see cref="ComputeFillDrawRect"/>. Insetting the
    /// fill by <see cref="FillInset"/> on every side moves each corner's center inward by
    /// <c>(inset, inset)</c>; keeping the full <see cref="CornerRadius"/> there leaves the fill arc
    /// NON-concentric with the stroke's inner edge, opening a background gap that is ~0 along the
    /// straight edges and widest at the 45° point of each corner. Reducing the radius by the same
    /// inset (clamped at 0) keeps the corner center fixed — the classic "inner radius = outer
    /// radius − inset" rule — so the inset fill stays concentric with the stroke. Returns the full
    /// radius on the shadow pass (which draws the fill at full size, see
    /// <see cref="ComputeFillDrawRect"/>) or when there is no inset.
    /// </summary>
    public float ComputeFillCornerRadius(bool isShadowPass)
    {
        if (isShadowPass || FillInset <= 0f)
        {
            return CornerRadius;
        }
        return System.Math.Max(0f, CornerRadius - FillInset);
    }

    /// <summary>
    /// Per-corner analog of <see cref="ComputeFillCornerRadius"/> for the <c>CustomRadius*</c> path
    /// (#3268), in Apos.Shapes' <c>CornerRadii</c> order (top-left, top-right, bottom-right,
    /// bottom-left). Each corner resolves its null override against <see cref="CornerRadius"/>
    /// exactly as <see cref="HasCustomCorners"/> does, then is reduced by <see cref="FillInset"/>
    /// (clamped at 0) on the inset body pass so all four corners stay concentric with the stroke.
    /// Returns the full radii on the shadow pass or when there is no inset.
    /// </summary>
    public (float topLeft, float topRight, float bottomRight, float bottomLeft) ComputeFillCornerRadii(bool isShadowPass)
    {
        float inset = isShadowPass || FillInset <= 0f ? 0f : FillInset;
        return (
            System.Math.Max(0f, (CustomRadiusTopLeft ?? CornerRadius) - inset),
            System.Math.Max(0f, (CustomRadiusTopRight ?? CornerRadius) - inset),
            System.Math.Max(0f, (CustomRadiusBottomRight ?? CornerRadius) - inset),
            System.Math.Max(0f, (CustomRadiusBottomLeft ?? CornerRadius) - inset));
    }

    /// <summary>
    /// Insets the draw rect by the pixel-center AA alignment offset
    /// (<see cref="RenderableShapeBase.GetAntiAliasWorldOffset"/>): the top-left moves in by the
    /// offset and each dimension shrinks by twice it, keeping the rect centered. Scaled by
    /// <paramref name="cameraZoom"/> so the inset stays a constant on-screen size at any zoom.
    /// Returns the rect unchanged when antialiasing is off.
    /// </summary>
    public (Vector2 position, Vector2 size) ApplyAntiAliasInset(Vector2 position, Vector2 size, int antiAliasSize, float cameraZoom)
    {
        var offset = GetAntiAliasWorldOffset(antiAliasSize, cameraZoom);
        if (offset == 0f)
        {
            return (position, size);
        }
        return (new Vector2(position.X + offset, position.Y + offset),
                new Vector2(size.X - 2f * offset, size.Y - 2f * offset));
    }

    /// <summary>
    /// Issue #2979 — dropshadow draw size, AA halo, and alpha scale for a rectangle of nominal
    /// <paramref name="hostSize"/>, mirroring <see cref="Circle"/>'s split (#2950/#2977).
    /// Apos.Shapes' <c>DrawRectangle</c> anchors the stroke at the outer edge (like
    /// <c>DrawCircle</c>), so a naive <c>size -= blur</c> shadow pass marches the outline inward
    /// as blur grows — a stroke-only rectangle visibly contracts.
    /// <list type="bullet">
    /// <item><description><b>Filled:</b> the disk strict-anchor (<see cref="RenderableShapeBase.ComputeShadowDrawGeometry"/>),
    /// keyed off the smaller half-dimension so the same inset applies to both axes; the 50%
    /// alpha line lands on the original edge. Beyond <c>blur &gt; min(Width,Height)</c> the helper
    /// truncates the inner ramp (the smaller dimension collapses to 0) and returns an alpha
    /// scale &lt; 1 so the center fades instead of inverting.</description></item>
    /// <item><description><b>Stroke-only:</b> anchor the band centerline at the body stroke
    /// centerline regardless of blur — each side pulls in <c>(StrokeWidth - effectiveShadowStrokeWidth)/2</c>,
    /// so once blur exceeds the stroke width (effective shadow stroke clamped to a ~0 epsilon by
    /// <see cref="RenderableShapeBase.ComputeStrokeShadowDrawParameters"/>) the box stops shrinking
    /// at <c>hostSize - StrokeWidth</c> and only the AA halo (aaSize) keeps growing.</description></item>
    /// </list>
    /// </summary>
    public (Vector2 size, int aaSize, float alphaScale) ComputeShadowDrawParameters(
        Vector2 hostSize, float effectiveShadowStrokeWidth, float cameraZoom)
    {
        float insetPerSide;
        int aaSize;
        float alphaScale;

        if (IsFilled)
        {
            float minHalf = System.Math.Min(hostSize.X, hostSize.Y) / 2f;
            (float effectiveRadius, int effAaSize, float effAlphaScale) =
                ComputeShadowDrawGeometry(minHalf, cameraZoom);
            insetPerSide = minHalf - effectiveRadius;
            aaSize = effAaSize;
            alphaScale = effAlphaScale;
        }
        else
        {
            insetPerSide = (StrokeWidth - effectiveShadowStrokeWidth) / 2f;
            aaSize = GetShadowAntiAliasSize(cameraZoom);
            alphaScale = 1f;
        }

        Vector2 size = new(
            System.Math.Max(0f, hostSize.X - 2f * insetPerSide),
            System.Math.Max(0f, hostSize.Y - 2f * insetPerSide));
        return (size, aaSize, alphaScale);
    }

    public override void Render(ISystemManagers managers)
    {
        // Issue #2950 follow-up — see Circle.Render for the rationale on this gate.
        if (!HasVisibleOutput)
        {
            return;
        }

        // Issue #2937 — re-open the shared ShapeBatch with this shape's blend if it differs.
        ShapeRenderer.EnsureBlend(this);

        var sb = ShapeRenderer.ShapeBatch;

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();
        var rotationRadians = MathHelper.ToRadians(-this.GetAbsoluteRotation());

        var size = new Microsoft.Xna.Framework.Vector2(Width, Height);

        // Resolve camera zoom once: it scales the pixel-center AA inset (RenderInternal) and the
        // dropshadow halo/geometry so both hold a constant on-screen size as the tool zooms.
        var cameraZoom = (managers as RenderingLibrary.SystemManagers)?.Renderer?.Camera?.Zoom ?? 1f;

        if(HasDropshadow)
        {
            // Issue #2950 — when stroke <= blur on a stroke-only RoundedRectangle, fade the
            // shadow's starting alpha and clamp lineThickness positive so Apos still draws.
            (float shadowStrokeWidth, Color shadowColor) =
                ComputeStrokeShadowDrawParameters(EffectiveDropshadowColor);

            // Issue #2979 — strict-anchor shadow geometry (filled disk anchor / stroke centerline
            // anchor), mirroring Circle.Render. Replaces the old naive `size -= blur` that drove
            // the outline inward as blur grew. aaSize is world-anchored (scaled by camera zoom).
            (Vector2 dropshadowSize, int shadowAaSize, float shadowAlphaScale) =
                ComputeShadowDrawParameters(size, shadowStrokeWidth, cameraZoom);
            if (shadowAlphaScale < 1f)
            {
                shadowColor = new Color(
                    shadowColor.R, shadowColor.G, shadowColor.B,
                    (byte)(shadowColor.A * shadowAlphaScale));
            }

            // Re-center the (shrunken) shadow box on the body so only the halo extends outward;
            // the per-side inset equals (size - dropshadowSize)/2 in each axis.
            var shadowLeft = absoluteLeft + DropshadowOffsetX + (size.X - dropshadowSize.X) / 2f;
            var shadowTop = absoluteTop + DropshadowOffsetY + (size.Y - dropshadowSize.Y) / 2f;

            RenderInternal(sb, shadowLeft, shadowTop, dropshadowSize,
                shadowAaSize,
                shadowStrokeWidth,
                rotationRadians,
                cameraZoom,
                forcedColor: shadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, size, IsAntialiased ? 1 : 0, StrokeWidth, rotationRadians, cameraZoom);
    }

    private void RenderInternal(Apos.Shapes.ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Vector2 size,
        int antiAliasSize,
        float strokeWidth,
        float rotationRadians,
        float cameraZoom,
        Color? forcedColor = null)
    {
        if (!IsFilled && StrokeDashLength > 0 && StrokeGapLength > 0 && strokeWidth > 0
            && size.X > 0 && size.Y > 0)
        {
            RenderDashed(sb, absoluteLeft, absoluteTop, size, antiAliasSize, strokeWidth, rotationRadians, forcedColor);
            return;
        }

        var position = AdjustPositionForCenterRotation(
            new Vector2(absoluteLeft, absoluteTop), size, rotationRadians);

        int thickness = 1;

        if(antiAliasSize > 1)
        {
            thickness = 0;
        }

        bool hasCustomCorners = HasCustomCorners(out var corners);

        // We do this so that shapes draw starting at the center of the pixel. This is most important
        // when shapes have a stroke thickness of 1 at the Gum level with anti aliasing. This renders wiht
        // an epsilon value of 0.01, with an anti-alias size of 1. By offsetting, the sampling of the antialias
        // gradient in the shader happens right at the start of the gradient, making for solid beautiful lines.
        // The offset is half a SCREEN pixel, so it is divided by cameraZoom (via ApplyAntiAliasInset) —
        // otherwise the inset grows in world space as the tool zooms in and the shape pulls visibly
        // inward from an equally-sized NineSlice.
        (position, size) = ApplyAntiAliasInset(position, size, antiAliasSize, cameraZoom);

        if (IsFilled)
        {
            // Pull the fill's outer edge inside the companion stroke band (#2834 rectangle
            // analog) so a semi-transparent stroke shows the background through it, not the
            // fill. The shadow pass (forcedColor != null) must NOT inherit the inset, or its
            // outer edge falls short of the body's outer edge (#2958).
            bool isShadowPass = forcedColor != null;
            var (fillPosition, fillSize) = ComputeFillDrawRect(position, size, isShadowPass);

            // Issue #3268 — shrink the corner radius by the same inset so the inset fill stays
            // concentric with the stroke's inner edge (no gap opens at the rounded corners). Gated
            // on isShadowPass identically to the rect inset above.
            float fillRadius = ComputeFillCornerRadius(isShadowPass);
            Apos.Shapes.CornerRadii fillCorners = default;
            if (hasCustomCorners)
            {
                var (tl, tr, br, bl) = ComputeFillCornerRadii(isShadowPass);
                fillCorners = new Apos.Shapes.CornerRadii(tl, tr, br, bl);
            }

            if (ShouldPaintGradient(forcedColor))
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop, rotationRadians);

                if (hasCustomCorners)
                    sb.DrawRectangle(fillPosition, fillSize, gradient, gradient, thickness, fillCorners, rotationRadians, antiAliasSize);
                else
                    sb.DrawRectangle(fillPosition, fillSize, gradient, gradient, thickness, fillRadius, rotationRadians, antiAliasSize);
            }
            else
            {
                var color = forcedColor ?? Color;
                if (hasCustomCorners)
                    sb.DrawRectangle(fillPosition, fillSize, color, color, thickness, fillCorners, rotationRadians, antiAliasSize);
                else
                    sb.DrawRectangle(fillPosition, fillSize, color, color, thickness, fillRadius, rotationRadians, antiAliasSize);
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

                if (hasCustomCorners)
                    sb.DrawRectangle(position, size, transparentGradient, gradient, strokeWidth, corners, rotationRadians, antiAliasSize);
                else
                    sb.DrawRectangle(position, size, transparentGradient, gradient, strokeWidth, CornerRadius, rotationRadians, antiAliasSize);
            }
            else
            {
                var color= forcedColor ?? this.Color;
                var transparentColor = color;
                transparentColor.A = 0;

                if (hasCustomCorners)
                    sb.DrawRectangle(position, size, transparentColor, color, strokeWidth, corners, rotationRadians, antiAliasSize);
                else
                    sb.DrawRectangle(position, size, transparentColor, color, strokeWidth, CornerRadius, rotationRadians, antiAliasSize);

            }
        }
    }

    private bool HasCustomCorners(out Apos.Shapes.CornerRadii corners)
    {
        if (CustomRadiusTopLeft is null && CustomRadiusTopRight is null
            && CustomRadiusBottomLeft is null && CustomRadiusBottomRight is null)
        {
            corners = default;
            return false;
        }
        corners = new Apos.Shapes.CornerRadii(
            CustomRadiusTopLeft ?? CornerRadius,
            CustomRadiusTopRight ?? CornerRadius,
            CustomRadiusBottomRight ?? CornerRadius,
            CustomRadiusBottomLeft ?? CornerRadius);
        return true;
    }

    // Ported from the upstream Apos.Shapes dashed-line PR
    // (https://github.com/Apostolique/Apos.Shapes/pull/31, DrawDashedRectangle + EmitStripDash + EmitArcDash).
    // Walks the perimeter (4 straight sides + 4 corner arcs) parameterized by t, clipping each
    // dash to whichever segment it lands on. We follow Skia's "exact" dashing (last dash clipped
    // wherever it lands) rather than the upstream PR's optional FitToPath rescaling, to keep the
    // pattern visually identical to the Skia runtime.
    private void RenderDashed(Apos.Shapes.ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Vector2 size,
        int antiAliasSize,
        float strokeWidth,
        float rotationRadians,
        Color? forcedColor)
    {
        // Build paint once, world-space. Pass the same Gradient to every dash so the gradient looks
        // continuous across the dashed border (each dash samples the same global gradient rather
        // than restarting per-segment). Falls back to a solid Color when no gradient is configured.
        // Apos.Shapes overloads accept either, so we forward whichever fits.
        var fallbackColor = forcedColor ?? Color;
        Gradient strokeGradient = ShouldPaintGradient(forcedColor)
            ? base.GetGradient(absoluteLeft, absoluteTop, rotationRadians)
            : new Gradient(Vector2.Zero, fallbackColor, Vector2.Zero, fallbackColor, Gradient.Shape.None);

        var halfT = strokeWidth / 2f;
        var rounded = MathHelper.Min(MathHelper.Min(CornerRadius, size.X / 2f), size.Y / 2f);
        // Apos.Shapes 0.6.x DrawRing's (radius1, radius2) params are really (centerline, totalThickness)
        // per the shader's RingSDF (abs(length(p) - r) - th * 0.5). +1f cancels the internal
        // radius1 -= 1f so the outer edge of each corner arc lines up with the rect's outer edge.
        var ringRadius = MathHelper.Max(rounded - halfT, 0f);

        var straightX = size.X - 2f * rounded;
        var straightY = size.Y - 2f * rounded;
        var cornerArc = rounded * MathHelper.PiOver2;
        var perimeter = 2f * (straightX + straightY) + 4f * cornerArc;
        if (perimeter <= 0) return;

        // Issue #2818 (mirror of Circle.RenderDashed #2790): when AA is on, each dash's
        // tangential end-cap halo leaks into the neighboring gap and smears dotted patterns
        // into near-continuous borders. Shift 1.5 * aaSize into the gap and trim 0.5 * aaSize
        // off each dash so the gap opens up noticeably while dashes shrink slightly to
        // compensate. Dash length floored at a small epsilon so a tight 1-px-dash pattern
        // doesn't push 0 (Apos won't render a zero-length dash).
        var effectiveGapLen = StrokeGapLength + 1.5f * antiAliasSize;
        var dashLen = MathHelper.Max(0.01f, StrokeDashLength - 0.5f * antiAliasSize);
        var period = dashLen + effectiveGapLen;
        if (period <= 0) return;

        var topY = absoluteTop;
        var bottomY = absoluteTop + size.Y;
        var leftX = absoluteLeft;
        var rightX = absoluteLeft + size.X;

        var cornerTL = new Vector2(leftX + rounded, topY + rounded);
        var cornerTR = new Vector2(rightX - rounded, topY + rounded);
        var cornerBR = new Vector2(rightX - rounded, bottomY - rounded);
        var cornerBL = new Vector2(leftX + rounded, bottomY - rounded);

        // Rotation pivot matches the solid render: AdjustPositionForCenterRotation in the
        // non-dashed path treats absolute(top, left) as the pivot the user "rotates around".
        // We rotate every emitted dash's center around the same pivot, then pass rotationRadians
        // to DrawRectangle/DrawRing so each primitive is also oriented to match.
        var pivot = new Vector2(absoluteLeft, absoluteTop);
        var cos = MathF.Cos(rotationRadians);
        var sin = MathF.Sin(rotationRadians);

        for (float t = 0; t < perimeter; t += period)
        {
            var dashStart = t;
            var dashEnd = MathHelper.Min(t + dashLen, perimeter);
            if (dashEnd <= dashStart) continue;

            float segOff = 0f;

            // Top edge (LtR), inward normal +y.
            EmitStraightDash(sb, dashStart, dashEnd, segOff, straightX,
                edgeStart: new Vector2(leftX + rounded, topY), edgeDir: new Vector2(1, 0), inwardNormal: new Vector2(0, 1),
                strokeWidth, strokeGradient, antiAliasSize, pivot, cos, sin, rotationRadians);
            segOff += straightX;

            // TR corner: from -π/2 (top tangent) to 0 (right tangent), CCW.
            if (rounded > 0)
            {
                EmitCornerDash(sb, dashStart, dashEnd, segOff, cornerArc,
                    cornerTR, startAngle: -MathHelper.PiOver2, endAngle: 0f,
                    ringRadius, strokeWidth, strokeGradient, antiAliasSize, pivot, cos, sin, rotationRadians);
            }
            segOff += cornerArc;

            // Right edge (TtB), inward normal -x.
            EmitStraightDash(sb, dashStart, dashEnd, segOff, straightY,
                edgeStart: new Vector2(rightX, topY + rounded), edgeDir: new Vector2(0, 1), inwardNormal: new Vector2(-1, 0),
                strokeWidth, strokeGradient, antiAliasSize, pivot, cos, sin, rotationRadians);
            segOff += straightY;

            // BR corner: 0 to π/2.
            if (rounded > 0)
            {
                EmitCornerDash(sb, dashStart, dashEnd, segOff, cornerArc,
                    cornerBR, startAngle: 0f, endAngle: MathHelper.PiOver2,
                    ringRadius, strokeWidth, strokeGradient, antiAliasSize, pivot, cos, sin, rotationRadians);
            }
            segOff += cornerArc;

            // Bottom edge (RtL), inward normal -y.
            EmitStraightDash(sb, dashStart, dashEnd, segOff, straightX,
                edgeStart: new Vector2(rightX - rounded, bottomY), edgeDir: new Vector2(-1, 0), inwardNormal: new Vector2(0, -1),
                strokeWidth, strokeGradient, antiAliasSize, pivot, cos, sin, rotationRadians);
            segOff += straightX;

            // BL corner: π/2 to π.
            if (rounded > 0)
            {
                EmitCornerDash(sb, dashStart, dashEnd, segOff, cornerArc,
                    cornerBL, startAngle: MathHelper.PiOver2, endAngle: MathHelper.Pi,
                    ringRadius, strokeWidth, strokeGradient, antiAliasSize, pivot, cos, sin, rotationRadians);
            }
            segOff += cornerArc;

            // Left edge (BtT), inward normal +x.
            EmitStraightDash(sb, dashStart, dashEnd, segOff, straightY,
                edgeStart: new Vector2(leftX, bottomY - rounded), edgeDir: new Vector2(0, -1), inwardNormal: new Vector2(1, 0),
                strokeWidth, strokeGradient, antiAliasSize, pivot, cos, sin, rotationRadians);
            segOff += straightY;

            // TL corner: π to 3π/2.
            if (rounded > 0)
            {
                EmitCornerDash(sb, dashStart, dashEnd, segOff, cornerArc,
                    cornerTL, startAngle: MathHelper.Pi, endAngle: 1.5f * MathHelper.Pi,
                    ringRadius, strokeWidth, strokeGradient, antiAliasSize, pivot, cos, sin, rotationRadians);
            }
        }
    }

    private static Vector2 RotateAround(Vector2 point, Vector2 pivot, float cos, float sin)
    {
        var dx = point.X - pivot.X;
        var dy = point.Y - pivot.Y;
        return new Vector2(
            pivot.X + dx * cos - dy * sin,
            pivot.Y + dx * sin + dy * cos);
    }

    private static void EmitStraightDash(Apos.Shapes.ShapeBatch sb,
        float dashStart, float dashEnd, float segStart, float segLen,
        Vector2 edgeStart, Vector2 edgeDir, Vector2 inwardNormal,
        float strokeWidth, Gradient gradient, int aaSize,
        Vector2 pivot, float cos, float sin, float rotationRadians)
    {
        if (segLen <= 0) return;
        var overlapStart = MathHelper.Max(dashStart, segStart);
        var overlapEnd = MathHelper.Min(dashEnd, segStart + segLen);
        if (overlapEnd <= overlapStart) return;

        var localStart = overlapStart - segStart;
        var localEnd = overlapEnd - segStart;
        var subDashLen = localEnd - localStart;

        var p1 = edgeStart + edgeDir * localStart;
        var p2 = edgeStart + edgeDir * localEnd;
        var midpoint = (p1 + p2) * 0.5f;
        var center = midpoint + inwardNormal * (strokeWidth * 0.5f);

        // Axis-aligned: dashes on top/bottom are size (subDashLen, strokeWidth), on left/right
        // are (strokeWidth, subDashLen). MathF.Abs distinguishes the two via edgeDir.
        Vector2 dashSize = MathF.Abs(edgeDir.X) > 0.5f
            ? new Vector2(subDashLen, strokeWidth)
            : new Vector2(strokeWidth, subDashLen);

        // Rotate the dash's visual center around the rect pivot, then back out the unrotated xy
        // that Apos needs (Apos rotates each primitive around xy + size/2; if we want the visual
        // center at rotatedCenter, set xy = rotatedCenter - size/2).
        var rotatedCenter = RotateAround(center, pivot, cos, sin);
        var xy = rotatedCenter - dashSize * 0.5f;
        sb.DrawRectangle(xy, dashSize, gradient, gradient, thickness: 1f, rounded: 0f, rotation: rotationRadians, aaSize: aaSize);
    }

    private static void EmitCornerDash(Apos.Shapes.ShapeBatch sb,
        float dashStart, float dashEnd, float segStart, float segLen,
        Vector2 cornerCenter, float startAngle, float endAngle,
        float ringRadius, float strokeWidth, Gradient gradient, int aaSize,
        Vector2 pivot, float cos, float sin, float rotationRadians)
    {
        if (segLen <= 0) return;
        var overlapStart = MathHelper.Max(dashStart, segStart);
        var overlapEnd = MathHelper.Min(dashEnd, segStart + segLen);
        if (overlapEnd <= overlapStart) return;

        var t1 = (overlapStart - segStart) / segLen;
        var t2 = (overlapEnd - segStart) / segLen;
        // Add rotationRadians to both angles so the arc segment is positioned around the (rotated)
        // corner center the same way as in the unrotated case. The corner center itself is also
        // rotated around the rect pivot.
        var a1 = MathHelper.Lerp(startAngle, endAngle, t1) + rotationRadians;
        var a2 = MathHelper.Lerp(startAngle, endAngle, t2) + rotationRadians;
        var rotatedCornerCenter = RotateAround(cornerCenter, pivot, cos, sin);

        // +1f cancels Apos.Shapes' internal radius1 -= 1f in DrawRing so the corner arc's outer
        // edge lines up with the rect's outer bounding edge (where EmitStraightDash sits).
        sb.DrawRing(rotatedCornerCenter, a1, a2, ringRadius + 1f, strokeWidth, gradient, gradient, 1, aaSize: aaSize);
    }
}
