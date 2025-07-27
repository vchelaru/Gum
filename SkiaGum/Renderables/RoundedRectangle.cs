using Gum.Wireframe;
using RenderingLibrary;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SkiaGum.Renderables;

class RoundedRectangle : RenderableBase, IClipPath, ICloneable
{
    public float CornerRadius { get; set; }

    public float? CustomRadiusTopLeft { get; set; } = null;
    public float? CustomRadiusTopRight { get; set; } = null;
    public float? CustomRadiusBottomRight { get; set; } = null;
    public float? CustomRadiusBottomLeft { get; set; } = null;

    public RoundedRectangle()
    {
        CornerRadius = 5;
        Color = SKColors.White;
    }

    public SKPath GetClipPath()
    {
        SKPath path = new SKPath();

        var absoluteX = this.GetAbsoluteX();
        var absoluteY = this.GetAbsoluteY();
        var boundingRect = new SKRect(absoluteX, absoluteY, absoluteX + this.Width, absoluteY + this.Height);

        path.AddRoundRect(boundingRect, CornerRadius, CornerRadius);

        return path;
    }

    object ICloneable.Clone() => Clone();

    public RoundedRectangle Clone()
    {
        var newInstance = (RoundedRectangle)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.mChildren = new ();
        newInstance.ClearCachedPaint();

        return newInstance;
    }

    public override void DrawBound(SKRect boundingRect, SKCanvas canvas, float absoluteRotation)
    {
        var paint = GetCachedPaint(boundingRect, absoluteRotation);
        SKPath path = new SKPath();
        if(CustomRadiusBottomLeft == null && CustomRadiusBottomRight == null && CustomRadiusTopLeft == null && CustomRadiusTopRight == null)
        {
            canvas.DrawRoundRect(boundingRect, CornerRadius, CornerRadius, paint);
        }
        else
        {

            float cornerRadius = CornerRadius;

            float topLeft = CustomRadiusTopLeft ?? CornerRadius;
            float topRight = CustomRadiusTopRight ?? CornerRadius;
            float bottomLeft = CustomRadiusBottomLeft ?? CornerRadius;
            float bottomRight = CustomRadiusBottomRight ?? CornerRadius;

            path.MoveTo(boundingRect.Left + topLeft, boundingRect.Top);
            path.LineTo(boundingRect.Right - topRight, boundingRect.Top);
            path.ArcTo(SKRect.Create(boundingRect.Right - topRight*2, boundingRect.Top, topRight*2, topRight* 2), 270, 90, false);

            path.LineTo(boundingRect.Right, boundingRect.Bottom - bottomRight);
            path.ArcTo(SKRect.Create(boundingRect.Right - bottomRight * 2, boundingRect.Bottom - bottomRight * 2, bottomRight * 2, bottomRight * 2), 0, 90, false);

            path.LineTo(boundingRect.Left + bottomLeft, boundingRect.Bottom);
            path.ArcTo(SKRect.Create(boundingRect.Left, boundingRect.Bottom - bottomLeft * 2, bottomLeft * 2, bottomLeft * 2), 90, 90, false);

            path.LineTo(boundingRect.Left, boundingRect.Top + topLeft);
            path.ArcTo(SKRect.Create(boundingRect.Left, boundingRect.Top, topLeft * 2, topLeft * 2), 180, 90, false);



        }
        path.Close();

        canvas.DrawPath(path, paint);

    }
}
