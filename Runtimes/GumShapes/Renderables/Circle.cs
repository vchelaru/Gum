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

public class Circle : RenderableShapeBase
{
    public override void Render(ISystemManagers managers)
    {
        var sb = ShapeRenderer.ShapeBatch;

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();

        var center = new Microsoft.Xna.Framework.Vector2(
            absoluteLeft + Width / 2.0f,
            absoluteTop + Width / 2.0f);

        var radius = Width / 2.0f;

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
                DropshadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, center, radius, 1, StrokeWidth);
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

        if (IsFilled)
        {
            // as outlined here:
            // https://github.com/Apostolique/Apos.Shapes/issues/12
            // There is a strange issue with rendering. However, adding 1 antialias with 1 border results in teh correct size and no artifacts:

            if (UseGradient && forcedColor == null)
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop);

                sb.DrawCircle(
                    center,
                    radius,
                    gradient,
                    gradient,
                    1,
                    antiAliasSize);
            }
            else
            {
                var color = forcedColor ?? this.Color;

                sb.DrawCircle(center,
                    radius,
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

        var dashLen = StrokeDashLength;
        var period = dashLen + StrokeGapLength;
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
