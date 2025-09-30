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

public class Circle : AposShapeBase
{
    public override void Render(ISystemManagers managers)
    {
        var sb = ShapeRenderer.ShapeBatch;

        sb.Begin();

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();

        var center = new Microsoft.Xna.Framework.Vector2(
            absoluteLeft + Width / 2.0f,
            absoluteTop + Width / 2.0f);

        var radius = Width / 2.0f;

        if(HasDropshadow)
        {
            var shadowLeft = absoluteLeft + DropshadowOffsetX + DropshadowBlurX / 2f;
            var shadowTop = absoluteTop + DropshadowOffsetY + DropshadowBlurX / 2f;

            var dropshadowCenter = center;
            dropshadowCenter.X += DropshadowOffsetX;
            dropshadowCenter.Y += DropshadowOffsetY;

            RenderInternal(sb, shadowLeft, shadowTop, dropshadowCenter, radius - DropshadowBlurX/2f, 
                MathFunctions.RoundToInt(DropshadowBlurX),
                DropshadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, center, radius, 1);

        sb.End();
    }

    private void RenderInternal(ShapeBatch sb, 
        float absoluteLeft, 
        float absoluteTop, 
        Microsoft.Xna.Framework.Vector2 center, 
        float radius,
        int antiAliasSize,
        Color? forcedColor = null)
    {
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
            sb.DrawCircle(center,
                radius,
                Color.Transparent,
                this.Color,
                StrokeWidth,
                aaSize: antiAliasSize);
        }
    }
}
