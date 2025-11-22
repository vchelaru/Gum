using SkiaSharp;

namespace SkiaGum.Renderables
{
    public class RenderableCircle : RenderableSkiaObject
    {

        public override void DrawToSurface(SKSurface surface)
        {
            surface.Canvas.Clear(SKColors.Transparent);

            using (var paint = CreatePaint())
            {
                var radius = Width / 2;
                // subtract 1 on the radius to allow antialiasing to work
                var extraToSubtract = 1.0f;
                if(this.IsFilled == false)
                {
                    extraToSubtract = StrokeWidth/2.0f;
                }
                surface.Canvas.DrawCircle(new SKPoint(XSizeSpillover + radius, YSizeSpillover + radius), 
                    radius-extraToSubtract, paint);
            }
        }
    }
}
