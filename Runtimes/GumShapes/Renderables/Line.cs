using Apos.Shapes;
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
        // Issue #2937 — re-open the shared ShapeBatch with this shape's blend if it differs.
        ShapeRenderer.EnsureBlend(this);

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
                forcedColor: EffectiveDropshadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, a, b, antiAliasSize: IsAntialiased ? 1 : 0);
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
        //
        // Issue #3972 — Apos.Shapes 0.7.5 added a native DashStyle parameter to DrawLine, which
        // dashes an open stroke along its centerline with independent per-dash cap control. This
        // replaces the manual per-segment RenderRounded/RenderButt loop below. Only the round-cap
        // path (RenderRounded's DrawLine call) can carry it — DrawRectangle's dash (RenderButt)
        // dashes the shape's own outline perimeter, not a straight cut along a line's length, so
        // it can't be reused for a butt-cap dashed line the same way. DashCap picks the per-dash
        // end shape independently of IsRounded, and DashSnap.Off preserves Gum's historical look
        // (see Circle.RenderInternal for the rationale).
        if (!IsFilled && StrokeDashLength > 0 && StrokeGapLength > 0)
        {
            var dash = new DashStyle(StrokeDashLength, StrokeGapLength,
                cap: IsRounded ? DashCap.Round : DashCap.Butt, snap: DashSnap.Off);
            RenderRounded(sb, absoluteLeft, absoluteTop, a, b, antiAliasSize, forcedColor, dash);
            return;
        }

        if (IsRounded)
        {
            RenderRounded(sb, absoluteLeft, absoluteTop, a, b, antiAliasSize, forcedColor, default);
        }
        else
        {
            RenderButt(sb, absoluteLeft, absoluteTop, a, b, antiAliasSize, forcedColor);
        }
    }

    private void RenderRounded(Apos.Shapes.ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Vector2 a,
        Vector2 b,
        float antiAliasSize,
        Color? forcedColor,
        DashStyle dash)
    {
        var lineRadius = StrokeWidth / 2.0f;

        if (UseGradient && forcedColor == null)
        {
            var gradient = base.GetGradient(absoluteLeft, absoluteTop);
            sb.DrawLine(a, b, lineRadius, gradient, gradient, aaSize: antiAliasSize, dash: dash);
        }
        else
        {
            var color = forcedColor ?? this.Color;
            sb.DrawLine(a, b, lineRadius, color, color, aaSize: antiAliasSize, dash: dash);
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
            sb.DrawRectangle(position, size, gradient, gradient, thickness: 0, cornerRadii: 0, rotation: rotation, aaSize: antiAliasSize);
        }
        else
        {
            var color = forcedColor ?? this.Color;
            sb.DrawRectangle(position, size, color, color, thickness: 0, cornerRadii: 0, rotation: rotation, aaSize: antiAliasSize);
        }
    }
}
