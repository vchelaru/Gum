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

internal class Arc : RenderableShapeBase
{
    public Arc()
    {
        // Arc historically defaulted to a thicker stroke than the rest of the shapes (10 vs the
        // base's 2). Thickness used to be its own backing field; now that it routes through
        // base.StrokeWidth (matching Skia and unifying with the rest of the shape pipeline) the
        // ctor seeds the same default. ArcRuntime also seeds its runtime-level StrokeWidth to 10
        // so PreRender doesn't overwrite this.
        StrokeWidth = 10;
        IsFilled = false;
    }

    public float StartAngle
    {
        get;
        set;
    } = 0;

    public float SweepAngle
    {
        get;
        set;
    } = 90;

    /// <summary>
    /// Façade for <see cref="RenderableShapeBase.StrokeWidth"/>. Kept for back-compat with the
    /// older Apos-only Arc API and with .gumx default state which stores this variable as
    /// "Thickness". Matches Skia's Arc, so the two backends now agree on a single underlying
    /// field.
    /// </summary>
    public float Thickness
    {
        get => StrokeWidth;
        set => StrokeWidth = value;
    }

    bool _isEndRounded;
    public bool IsEndRounded
    {
        get => _isEndRounded;
        set
        {
            _isEndRounded = value;
        }
    }




    public override void Render(ISystemManagers managers)
    {
        if (SweepAngle == 0) return;

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

        // Issue #2925 — same rotation handling as Circle.Render: rotate the (Width/2, Width/2)
        // offset from the GUE's top-left origin to the arc center. The arc's StartAngle is also
        // offset by the absolute rotation in RenderInternal so the swept arc orients correctly
        // when the GUE is rotated. Preserves the original convention of using Width for both
        // axes (arcs are drawn in a square box keyed off Width).
        var rotationRadians = MathHelper.ToRadians(-this.GetAbsoluteRotation());
        var center = GetRotatedCenter(absoluteLeft, absoluteTop, Width, Width, rotationRadians);

        var radius = Width / 2 - StrokeWidth / 2;
        BeginAlphaBoundsMask(absoluteLeft, absoluteTop, Width, Width, rotationRadians);

        var cameraZoom = (managers as RenderingLibrary.SystemManagers)?.Renderer?.Camera?.Zoom ?? 1f;

        if(HasDropshadow)
        {
            var shadowLeft = absoluteLeft + DropshadowOffsetX + DropshadowBlurX / 2f;
            var shadowTop = absoluteTop + DropshadowOffsetY + DropshadowBlurX / 2f;

            var dropshadowCenter = center;
            dropshadowCenter.X += DropshadowOffsetX;
            dropshadowCenter.Y += DropshadowOffsetY;

            // Issue #2950 — stroke-only fade + world-anchored aaSize scaling (mirrors Circle).
            (float shadowLineThickness, Color shadowColor) =
                ComputeStrokeShadowDrawParameters(EffectiveDropshadowColor);
            int shadowAaSize = GetShadowAntiAliasSize(cameraZoom);

            RenderInternal(sb,
                absoluteLeft: shadowLeft,
                absoluteTop: shadowTop,
                center: dropshadowCenter,
                radius: radius,
                antiAliasSize: shadowAaSize,
                lineThickness: shadowLineThickness,
                cameraZoom: cameraZoom,
                forcedColor: shadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, center, radius, IsAntialiased ? 1 : 0, StrokeWidth, cameraZoom);
        EndAlphaBoundsMask();
    }

    private void RenderInternal(Apos.Shapes.ShapeBatch sb,
        float absoluteLeft,
        float absoluteTop,
        Vector2 center,
        float radius,
        int antiAliasSize,
        float lineThickness,
        float cameraZoom,
        Color? forcedColor = null)
    {
        // Issue #2925 — offset the swept arc by the GUE's absolute rotation so the arc tilts
        // with its bounding box. Matches the negate-and-add convention StartAngle already uses.
        var absoluteRotation = this.GetAbsoluteRotation();
        var startAngleRadians = MathHelper.ToRadians(-StartAngle - absoluteRotation);
        float endAngleRadians = 0;
        endAngleRadians = MathHelper.ToRadians(-StartAngle - SweepAngle - absoluteRotation);

        if (startAngleRadians > endAngleRadians)
        {
            // swap start and end:
            var temp = startAngleRadians;
            startAngleRadians = endAngleRadians;
            endAngleRadians = temp;
        }

        // The band is drawn the way Circle draws a stroke (#6254): the same anti-aliasing offset
        // comes off the half thickness so a 360 degree arc covers the same pixels as a stroked Circle.
        float halfThickness = GetRingHalfThickness(lineThickness, antiAliasSize, cameraZoom);

        // Issue #3972 — Apos.Shapes 0.7.5 added a native DashStyle parameter to both DrawArc and
        // DrawRing, so dashing flows through the same draw calls as a solid stroke instead of a
        // hand-rolled perimeter walk. This also lifts the old butt-cap-only restriction (#2892) —
        // rounded-end dashed arcs are now supported for free. DashSnap.Off preserves Gum's
        // historical look (see Circle.RenderInternal for the rationale). The shadow pass
        // (forcedColor != null) stays un-dashed on purpose: it renders as one continuous arc
        // behind the dashes, matching Skia.
        DashStyle dash = default;
        if (forcedColor == null && StrokeDashLength > 0 && StrokeGapLength > 0 && lineThickness > 0 && radius > 0)
        {
            dash = new DashStyle(StrokeDashLength, StrokeGapLength, cap: DashCap.Butt, snap: DashSnap.Off);
        }

        // A rounded end is a DrawArc, a butt end is a DrawRing; both take the same arguments.
        void DrawBand(Color color1, Color color2)
        {
            if (_isEndRounded)
            {
                sb.DrawArc(center, startAngleRadians, endAngleRadians, radius, halfThickness,
                    color1, color2, 1, aaSize: antiAliasSize, dash: dash);
            }
            else
            {
                sb.DrawRing(center, startAngleRadians, endAngleRadians, radius, halfThickness,
                    color1, color2, 1, aaSize: antiAliasSize, dash: dash);
            }
        }

        void DrawGradientBand(Gradient gradient)
        {
            if (_isEndRounded)
            {
                sb.DrawArc(center, startAngleRadians, endAngleRadians, radius, halfThickness,
                    gradient, gradient, 1, aaSize: antiAliasSize, dash: dash);
            }
            else
            {
                sb.DrawRing(center, startAngleRadians, endAngleRadians, radius, halfThickness,
                    gradient, gradient, 1, aaSize: antiAliasSize, dash: dash);
            }
        }

        if (ShouldPaintGradient(forcedColor))
        {
            DrawGradientBand(base.GetGradient(absoluteLeft, absoluteTop));
        }
        else
        {
            Color color = forcedColor ?? this.Color;
            DrawBand(color, color);
        }
    }
}
