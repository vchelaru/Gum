using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.Renderables;

public class RoundedRectangle : RenderableShapeBase
{
    public float CornerRadius { get; set; }

    public override void Render(ISystemManagers managers)
    {
        var sb = ShapeRenderer.ShapeBatch;

        var absoluteLeft = this.GetAbsoluteLeft();
        var absoluteTop = this.GetAbsoluteTop();

        var size = new Microsoft.Xna.Framework.Vector2(Width, Height);

        if(HasDropshadow)
        {
            var shadowLeft = absoluteLeft + DropshadowOffsetX + DropshadowBlurX/2;
            var shadowTop = absoluteTop + DropshadowOffsetY + DropshadowBlurY/2;

            // Currently apos shapes doesn't support different sizes for anti-aliasing on X and Y
            var dropshadowSize = size;
            dropshadowSize.X -= DropshadowBlurX;
            dropshadowSize.Y -= DropshadowBlurX;

            RenderInternal(sb, shadowLeft, shadowTop, dropshadowSize, 
                MathFunctions.RoundToInt(DropshadowBlurX), 
                StrokeWidth - DropshadowBlurX,
                forcedColor: DropshadowColor);
        }

        RenderInternal(sb, absoluteLeft, absoluteTop, size, 1, StrokeWidth);
    }

    private void RenderInternal(Apos.Shapes.ShapeBatch sb, 
        float absoluteLeft, 
        float absoluteTop, 
        Vector2 size, 
        int antiAliasSize, 
        float strokeWidth,
        Color? forcedColor = null)
    {
        //antiAliasSize = 0;
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

                var transparentGradient = gradient;
                transparentGradient.AC = new Color((int)gradient.AC.R, gradient.AC.G, gradient.AC.B, 0);
                transparentGradient.BC = new Color((int)gradient.BC.R, gradient.BC.G, gradient.BC.B, 0);

                sb.DrawRectangle(
                    position,
                    size,
                    transparentGradient,
                    gradient,
                    strokeWidth,
                    CornerRadius,
                    0,
                    antiAliasSize);
            }
            else
            {
                var color= forcedColor ?? this.Color;
                var transparentColor = color;
                transparentColor.A = 0;

                sb.DrawRectangle(
                    position,
                    size,
                    transparentColor,
                    color,
                    strokeWidth,
                    CornerRadius,
                    0,
                    antiAliasSize);
            }
        }
    }
}
