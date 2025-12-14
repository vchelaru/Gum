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
        //antiAliasSize = 0;

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
}
