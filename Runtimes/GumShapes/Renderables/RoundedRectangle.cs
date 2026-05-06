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

public class RoundedRectangle : RenderableShapeBase
{
    public float CornerRadius { get; set; }

    public override void Render(ISystemManagers managers)
    {
        var sb = ShapeRenderer.ShapeBatch;

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();
        var rotationRadians = MathHelper.ToRadians(-this.GetAbsoluteRotation());

        var size = new Microsoft.Xna.Framework.Vector2(Width, Height);

        if(HasDropshadow)
        {
            var shadowLeft = absoluteLeft + DropshadowOffsetX + DropshadowBlurX/2;
            var shadowTop = absoluteTop + DropshadowOffsetY + DropshadowBlurY/2;

            // Currently apos shapes doesn't support different sizes for anti-aliasing on X and Y
            var dropshadowSize = size;
            dropshadowSize.X -= DropshadowBlurX;
            dropshadowSize.Y -= DropshadowBlurX;

            RenderInternal(sb, shadowLeft, shadowTop, dropshadowSize,
                MathFunctions.RoundToInt(DropshadowBlurX),
                StrokeWidth - DropshadowBlurX,
                rotationRadians,
                forcedColor: DropshadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, size, 1, StrokeWidth, rotationRadians);
    }

    private void RenderInternal(Apos.Shapes.ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Vector2 size,
        int antiAliasSize,
        float strokeWidth,
        float rotationRadians,
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

        if (IsFilled)
        {
            if (UseGradient && forcedColor == null)
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop);

                sb.DrawRectangle(
                    position,
                    size,
                    gradient,
                    gradient,
                    thickness,
                    CornerRadius,
                    rotationRadians,
                    antiAliasSize);
            }
            else
            {
                sb.DrawRectangle(
                    position,
                    size,
                    forcedColor ?? Color,
                    forcedColor ?? Color,
                    thickness,
                    CornerRadius,
                    rotationRadians,
                    antiAliasSize);
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

                sb.DrawRectangle(
                    position,
                    size,
                    transparentGradient,
                    gradient,
                    strokeWidth,
                    CornerRadius,
                    rotationRadians,
                    antiAliasSize);
            }
            else
            {
                var color= forcedColor ?? this.Color;
                var transparentColor = color;
                transparentColor.A = 0;

                sb.DrawRectangle(
                    position,
                    size,
                    transparentColor,
                    color,
                    strokeWidth,
                    CornerRadius,
                    rotationRadians,
                    antiAliasSize);
            }
        }
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
        Gradient strokeGradient = (UseGradient && forcedColor == null)
            ? base.GetGradient(absoluteLeft, absoluteTop)
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

        var period = StrokeDashLength + StrokeGapLength;
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
            var dashEnd = MathHelper.Min(t + StrokeDashLength, perimeter);
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
