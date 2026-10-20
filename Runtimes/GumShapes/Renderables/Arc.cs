using Apos.Shapes;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Renderables;

/// <summary>
/// A stroked arc that fills its box on every runtime. In a square box it is a circle. In a box that is not
/// square it is an ellipse that touches all four sides: the circle is drawn at the shorter side and the
/// longer axis is stretched through the batch view (#6256), so the stroke is exact on the short axis and
/// thicker along the long axis (Skia's pen is uniform). Strokes under 2 world units draw as 2 in that case.
/// Dash and gap lengths, round caps and a radial gradient stretch with the arc.
/// </summary>
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




    /// <summary>
    /// Half the thinnest band a stretched arc is drawn with, in world units (one pixel wide).
    /// </summary>
    internal const float MinStretchedHalfThickness = 0.5f;

    /// <summary>
    /// Width and Height closer than this are one square box and take the plain circle path, so a square
    /// arc pays no batch flushes.
    /// </summary>
    internal const float SquareTolerance = 0.01f;

    /// <summary>
    /// The transform that stretches the circle Apos.Shapes draws at the box's shorter side along the longer
    /// axis until it fills the box, as a view matrix pre-multiplied onto the camera view. The stretch runs
    /// along the box's own long axis, so a rotated arc is not sheared.
    /// </summary>
    /// <remarks>
    /// <c>antiAliasWorldOffset</c> is the inset <see cref="RenderableShapeBase.GetAntiAliasWorldOffset"/> gives
    /// the circle's outer edge. The stretch is measured between the inset edges so the long ends land on the
    /// box like the short ones do, instead of being pulled in by the offset times the stretch.
    /// </remarks>
    internal static Matrix CreateStretchToBoxMatrix(float width, float height, Vector2 center, float rotationRadians,
        float antiAliasWorldOffset)
    {
        float shortSide = System.Math.Min(width, height);
        float longSide = System.Math.Max(width, height);

        float stretch = longSide / shortSide;
        if (shortSide / 2 - antiAliasWorldOffset > 0)
        {
            stretch = (longSide / 2 - antiAliasWorldOffset) / (shortSide / 2 - antiAliasWorldOffset);
        }

        float horizontalScale = width > height ? stretch : 1;
        float verticalScale = height > width ? stretch : 1;

        return Matrix.CreateTranslation(-center.X, -center.Y, 0)
            * Matrix.CreateRotationZ(-rotationRadians)
            * Matrix.CreateScale(horizontalScale, verticalScale, 1)
            * Matrix.CreateRotationZ(rotationRadians)
            * Matrix.CreateTranslation(center.X, center.Y, 0);
    }

    public override void Render(ISystemManagers managers)
    {
        if (SweepAngle == 0) return;

        // Issue #2950 follow-up — see Circle.Render for the rationale on this gate.
        if (!HasVisibleOutput)
        {
            return;
        }

        // An empty box has nothing to fill, and a zero side would make the stretch below degenerate.
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        // Issue #2937 — re-open the shared ShapeBatch with this shape's blend if it differs.
        ShapeRenderer.EnsureBlend(this);

        var sb = ShapeRenderer.ShapeBatch;

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();

        // Issue #2925 — same rotation handling as Circle.Render: rotate the (Width/2, Height/2)
        // offset from the GUE's top-left origin to the arc center. The arc's StartAngle is also
        // offset by the absolute rotation in RenderInternal so the swept arc orients correctly
        // when the GUE is rotated.
        var rotationRadians = MathHelper.ToRadians(-this.GetAbsoluteRotation());
        var center = GetRotatedCenter(absoluteLeft, absoluteTop, Width, Height, rotationRadians);

        // Issue #6256 — Apos.Shapes has no elliptical arc, so a box that is not square is filled by
        // drawing the circle at the shorter side (the stroke is exactly StrokeWidth on the short axis)
        // and stretching the longer axis through the batch view, as Svg does for a mismatched aspect
        // ratio. The stroke is thicker along the long axis by long / short. Square boxes skip all this.
        var shortSide = System.Math.Min(Width, Height);
        var stretchesToBox = System.Math.Abs(Width - Height) > SquareTolerance;

        var radius = shortSide / 2 - StrokeWidth / 2;
        BeginAlphaBoundsMask(absoluteLeft, absoluteTop, Width, Height, rotationRadians);

        var cameraZoom = (managers as global::RenderingLibrary.SystemManagers)?.Renderer?.Camera?.Zoom ?? 1f;

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

            // The shadow offset is a world-space shift, so the stretch is centered on the shadow.
            Matrix? shadowStretch = stretchesToBox
                ? CreateStretchToBoxMatrix(Width, Height, dropshadowCenter, rotationRadians,
                    GetAntiAliasWorldOffset(shadowAaSize, cameraZoom))
                : null;

            RenderInternal(sb,
                absoluteLeft: shadowLeft,
                absoluteTop: shadowTop,
                center: dropshadowCenter,
                radius: radius,
                antiAliasSize: shadowAaSize,
                lineThickness: shadowLineThickness,
                cameraZoom: cameraZoom,
                stretch: shadowStretch,
                forcedColor: shadowColor);
        }

        Matrix? stretch = stretchesToBox
            ? CreateStretchToBoxMatrix(Width, Height, center, rotationRadians,
                GetAntiAliasWorldOffset(IsAntialiased ? 1 : 0, cameraZoom))
            : null;
        RenderInternal(sb, absoluteLeft, absoluteTop, center, radius, IsAntialiased ? 1 : 0, StrokeWidth, cameraZoom, stretch);
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
        Matrix? stretch,
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
        if (stretch.HasValue && lineThickness > 0)
        {
            // A band thinner than the one pixel anti-aliasing ramp can fall between two pixel rows and
            // vanish where the stretch carries it off the pixel centers, so a stretched stroke is at least
            // one pixel wide. The radius moves in by what the band grew so the outer edge stays put.
            float minimum = System.Math.Max(halfThickness, MinStretchedHalfThickness);
            radius -= minimum - halfThickness;
            halfThickness = minimum;
        }

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

        // The stretch is a view push, which costs two batch flushes; it brackets only the draw call.
        if (stretch.HasValue)
        {
            ShapeRenderer.PushView(stretch.Value);
        }

        if (ShouldPaintGradient(forcedColor))
        {
            Gradient gradient = base.GetGradient(absoluteLeft, absoluteTop);
            if (stretch.HasValue)
            {
                // Gradient points are read in the space before the view, so map the box-space points back.
                Matrix inverse = Matrix.Invert(stretch.Value);
                gradient.AXY = Vector2.Transform(gradient.AXY, inverse);
                gradient.BXY = Vector2.Transform(gradient.BXY, inverse);
            }
            DrawGradientBand(gradient);
        }
        else
        {
            Color color = forcedColor ?? this.Color;
            DrawBand(color, color);
        }

        if (stretch.HasValue)
        {
            ShapeRenderer.PopView();
        }
    }
}
