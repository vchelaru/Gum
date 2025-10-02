using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.Renderables;

public class RoundedRectangle : AposShapeBase
{
    public float CornerRadius { get; set; }

    public override void Render(ISystemManagers managers)
    {
        var sb = ShapeRenderer.ShapeBatch;

        sb.Begin();

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();

        var size = new Microsoft.Xna.Framework.Vector2(Width, Height);

        if(HasDropshadow)
        {
            var shadowLeft = absoluteLeft + DropshadowOffsetX;
            var shadowTop = absoluteTop + DropshadowOffsetY;

            // Currently apos shapes doesn't support different sizes for anti-aliasing on X and Y
            var dropshadowSize = size;
            //dropshadowSize.X -= DropshadowBlurX;
            //dropshadowSize.Y -= DropshadowBlurX;

            RenderInternal(sb, shadowLeft, shadowTop, dropshadowSize, 
                MathFunctions.RoundToInt(DropshadowBlurX), 
                forcedColor: DropshadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, size, 1);

        sb.End();
    }

    private void RenderInternal(Apos.Shapes.ShapeBatch sb, 
        float absoluteLeft, 
        float absoluteTop, 
        Vector2 size, 
        int antiAliasSize, 
        Color? forcedColor = null)
    {
        var position = new Microsoft.Xna.Framework.Vector2(absoluteLeft, absoluteTop);

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
                    0,
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
                    0,
                    antiAliasSize);
            }
        }
        else
        {
            if(UseGradient && forcedColor == null)
            {
                var gradient = base.GetGradient(absoluteLeft, absoluteTop);
                sb.DrawRectangle(
                    position,
                    size,
                    Microsoft.Xna.Framework.Color.Transparent,
                    gradient,
                    StrokeWidth,
                    CornerRadius,
                    0,
                    antiAliasSize);
            }
            else
            {
                sb.DrawRectangle(
                    position,
                    size,
                    Microsoft.Xna.Framework.Color.Transparent,
                    forcedColor ?? Color,
                    StrokeWidth,
                    CornerRadius,
                    0,
                    antiAliasSize);
            }
        }
    }
}
