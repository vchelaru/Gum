using Microsoft.Xna.Framework;
using RenderingLibrary;
using System;

namespace MonoGameAndGum.Renderables;

internal class Line : RenderableShapeBase
{
    public bool IsRounded
    {
        get;
        set;
    }

    public override void Render(ISystemManagers managers)
    {
        var sb = ShapeRenderer.ShapeBatch;

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();

        var a = new Vector2(absoluteLeft, absoluteTop);
        var b = new Vector2(absoluteLeft + Width, absoluteTop + Height);

        if (HasDropshadow)
        {
            var shadowA = a;
            shadowA.X += DropshadowOffsetX;
            shadowA.Y += DropshadowOffsetY;

            var shadowB = b;
            shadowB.X += DropshadowOffsetX;
            shadowB.Y += DropshadowOffsetY;

            RenderInternal(sb, absoluteLeft + DropshadowOffsetX, absoluteTop + DropshadowOffsetY,
                shadowA, shadowB,
                antiAliasSize: MathHelper.Max(1, DropshadowBlurX),
                forcedColor: DropshadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, a, b, antiAliasSize: 1);
    }

    private void RenderInternal(Apos.Shapes.ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Vector2 a,
        Vector2 b,
        float antiAliasSize,
        Color? forcedColor = null)
    {
        // Match Skia's semantics: dashing only kicks in when IsFilled is false. Skia lines also
        // require IsFilled = false to render as a stroke, so users authoring dashed strokes already
        // have to set this; keeping the trigger identical avoids cross-runtime surprises.
        if (!IsFilled && StrokeDashLength > 0 && StrokeGapLength > 0)
        {
            RenderDashed(sb, absoluteLeft, absoluteTop, a, b, antiAliasSize, forcedColor);
            return;
        }

        if (IsRounded)
        {
            RenderRounded(sb, absoluteLeft, absoluteTop, a, b, antiAliasSize, forcedColor);
        }
        else
        {
            RenderButt(sb, absoluteLeft, absoluteTop, a, b, antiAliasSize, forcedColor);
        }
    }

    private void RenderDashed(Apos.Shapes.ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Vector2 a,
        Vector2 b,
        float antiAliasSize,
        Color? forcedColor)
    {
        var delta = b - a;
        var length = delta.Length();
        if (length <= 0) return;

        var direction = delta / length;
        var period = StrokeDashLength + StrokeGapLength;

        // Skia's default phase is 0: the first dash starts at t=0 and the last dash is clipped wherever
        // it lands. We mirror that here rather than the "fit to path" mode from the upstream PR; the
        // exact pattern is more predictable for short lines (where rescaling would visibly change dash
        // sizes) and matches what users authoring against the Skia runtime already see.
        for (float t = 0; t < length; t += period)
        {
            var dashEnd = MathHelper.Min(t + StrokeDashLength, length);
            var segA = a + direction * t;
            var segB = a + direction * dashEnd;

            if (IsRounded)
            {
                RenderRounded(sb, absoluteLeft, absoluteTop, segA, segB, antiAliasSize, forcedColor);
            }
            else
            {
                RenderButt(sb, absoluteLeft, absoluteTop, segA, segB, antiAliasSize, forcedColor);
            }
        }
    }

    private void RenderRounded(Apos.Shapes.ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Vector2 a,
        Vector2 b,
        float antiAliasSize,
        Color? forcedColor)
    {
        var lineRadius = StrokeWidth / 2.0f;

        if (UseGradient && forcedColor == null)
        {
            var gradient = base.GetGradient(absoluteLeft, absoluteTop);
            sb.DrawLine(a, b, lineRadius, gradient, gradient, aaSize: antiAliasSize);
        }
        else
        {
            var color = forcedColor ?? this.Color;
            sb.DrawLine(a, b, lineRadius, color, color, aaSize: antiAliasSize);
        }
    }

    private void RenderButt(Apos.Shapes.ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Vector2 a,
        Vector2 b,
        float antiAliasSize,
        Color? forcedColor)
    {
        var delta = b - a;
        var length = delta.Length();

        if (length <= 0)
        {
            return;
        }

        var rotation = (float)Math.Atan2(delta.Y, delta.X);
        var size = new Vector2(length, StrokeWidth);

        // Apos rotates around the rectangle's center.
        // The center should be at the midpoint of A-B.
        // xy = midpoint - half_size
        var midpoint = (a + b) / 2.0f;
        var position = new Vector2(midpoint.X - length / 2.0f, midpoint.Y - StrokeWidth / 2.0f);

        if (UseGradient && forcedColor == null)
        {
            var gradient = base.GetGradient(absoluteLeft, absoluteTop);
            sb.DrawRectangle(position, size, gradient, gradient, thickness: 0, rounded: 0, rotation: rotation, aaSize: antiAliasSize);
        }
        else
        {
            var color = forcedColor ?? this.Color;
            sb.DrawRectangle(position, size, color, color, thickness: 0, rounded: 0, rotation: rotation, aaSize: antiAliasSize);
        }
    }
}
