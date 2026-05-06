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

    public float Thickness
    {
        get;
        set;
    } = 10;

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

        var sb = ShapeRenderer.ShapeBatch;

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();

        var center = new Microsoft.Xna.Framework.Vector2(
            absoluteLeft + Width / 2.0f,
            absoluteTop + Width / 2.0f);

        var radius = Width / 2 - Thickness / 2;

        if(HasDropshadow)
        {
            var shadowLeft = absoluteLeft + DropshadowOffsetX + DropshadowBlurX / 2f;
            var shadowTop = absoluteTop + DropshadowOffsetY + DropshadowBlurX / 2f;

            var dropshadowCenter = center;
            dropshadowCenter.X += DropshadowOffsetX;
            dropshadowCenter.Y += DropshadowOffsetY;

            RenderInternal(sb, 
                absoluteLeft: shadowLeft, 
                absoluteTop: shadowTop, 
                center: dropshadowCenter, 
                radius: radius,
                antiAliasSize: MathFunctions.RoundToInt(DropshadowBlurX),
                lineThickness: Thickness - DropshadowBlurX,
                forcedColor: DropshadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, center, radius, 1, Thickness);
    }

    private void RenderInternal(Apos.Shapes.ShapeBatch sb, 
        float absoluteLeft, 
        float absoluteTop, 
        Vector2 center, 
        float radius,
        int antiAliasSize, 
        float lineThickness,
        Color? forcedColor = null)
    {
        var startAngleRadians = MathHelper.ToRadians(-StartAngle);
        float endAngleRadians = 0;
        endAngleRadians = MathHelper.ToRadians(-StartAngle - SweepAngle);

        if (startAngleRadians > endAngleRadians)
        {
            // swap start and end:
            var temp = startAngleRadians;
            startAngleRadians = endAngleRadians;
            endAngleRadians = temp;
        }

        var endpointRadius = lineThickness / 2;

        if(_isEndRounded)
        {
            if (UseGradient && forcedColor == null)
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop);
                sb.DrawArc(center,
                    startAngleRadians,
                    endAngleRadians,
                    radius,
                    endpointRadius,
                    gradient,
                    gradient,
                    1,
                    aaSize: antiAliasSize);
            }
            else
            {
                var color = forcedColor ?? this.Color;
                sb.DrawArc(center,
                    startAngleRadians,
                    endAngleRadians,
                    radius,
                    endpointRadius,
                    color,
                    color,
                    1,
                    aaSize: antiAliasSize);
            }
        }
        else
        {
            // Apos.Shapes 0.6.x DrawRing's signature is (radius1, radius2) but the shader actually
            // uses them as (centerline, totalThickness): see RingSDF, abs(length(p) - r) - th * 0.5.
            // The shader also does radius1 -= 1f internally, so the rendered band sits one pixel
            // inside what the caller thinks it asked for. Without the +1f compensation here, an Arc
            // sized to fit a NxN bounding box renders with a visible 1-pixel gap at its outer edge -
            // confirmed visually by overlaying a thick Arc on a same-size filled Circle and seeing a
            // thin background-color ring around the outside of the Arc.
            var compensatedRadius = radius + 1f;
            if (UseGradient && forcedColor == null)
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop);
                sb.DrawRing(center,
                    startAngleRadians,
                    endAngleRadians,
                    compensatedRadius,
                    lineThickness,
                    gradient,
                    gradient,
                    1,
                    aaSize: antiAliasSize);
            }
            else
            {
                var color = forcedColor ?? this.Color;
                sb.DrawRing(center,
                    startAngleRadians,
                    endAngleRadians,
                    compensatedRadius,
                    lineThickness,
                    color,
                    color,
                    1,
                    aaSize: antiAliasSize);
            }
        }

    }
}
