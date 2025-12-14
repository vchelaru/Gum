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
            if (UseGradient && forcedColor == null)
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop);
                sb.DrawRing(center,
                    startAngleRadians,
                    endAngleRadians,
                    radius,
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
                    radius,
                    lineThickness,
                    color,
                    color,
                    1,
                    aaSize: antiAliasSize);
            }
        }

    }
}
