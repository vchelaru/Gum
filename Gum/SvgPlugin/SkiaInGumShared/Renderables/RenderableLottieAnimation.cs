using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using ToolsUtilities;

namespace SkiaGum.Renderables
{
    public class RenderableLottieAnimation : RenderableSkiaObject
    {
        string sourceFile;
        public string SourceFile
        {
            get => sourceFile;
            set
            {
                if (sourceFile != value)
                {
                    sourceFile = value;
                    animation = GetAnimation();
                    needsUpdate = true;
                }
            }
        }

        public bool IsAnimating { get; set; } = true;

        // This alias exists to match the other interfaces, but should not be used in code:
        public bool Animate
        {
            get => IsAnimating;
            set => IsAnimating = value;
        }

        DateTime lastUpdate;

        const double SecondsBetweenUpdates = .1;

        SkiaSharp.Skottie.Animation animation;

        public override void DrawToSurface(SKSurface surface)
        {
            if(animation != null)
            {
                surface.Canvas.Clear(SKColors.Transparent);

                //var scaleX = this.Width / skiaSvg.ViewBox.Width;
                //var scaleY = this.Height / skiaSvg.ViewBox.Height;

                //SKMatrix scaleMatrix = SKMatrix.MakeScale(scaleX, scaleY);

                //{

                //    surface.Canvas.DrawPicture(skiaSvg.Picture, ref scaleMatrix);
                //}
                var duration = animation.Duration.TotalSeconds;
                animation.SeekFrameTime(DateTime.Now.TimeOfDay.TotalSeconds % duration);
                animation.Render(surface.Canvas, new SKRect(0, 0, Width, Height));
            }
            else
            {
                surface.Canvas.Clear(SKColors.Blue);

                using (var whitePaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true })
                {
                    var radius = Width / 2;
                    surface.Canvas.DrawCircle(new SKPoint(radius, radius), radius, whitePaint);
                }
            }
        }

        public override void PreRender()
        {
            if((DateTime.Now - lastUpdate).TotalSeconds > SecondsBetweenUpdates)
            {
                needsUpdate = true;
            }

            base.PreRender();
        }

        private SkiaSharp.Skottie.Animation GetAnimation()
        {
            SkiaSharp.Skottie.Animation animation = null;

            if (!string.IsNullOrWhiteSpace(sourceFile))
            {
                try
                {
                    // todo - support MonoGame loads here using the normal content loader...
#if GUM
                    var sourceFileAbsolute =
                        FileManager.RemoveDotDotSlash(Gum.ToolStates.ProjectState.Self.ProjectDirectory + sourceFile);
                    if(System.IO.File.Exists(sourceFileAbsolute))
                    {
                        animation = SkiaSharp.Skottie.Animation.Create(sourceFileAbsolute);
                    }
#endif
                }
                catch
                {
                    // do nothing?
                    animation = null;
                }
            }
            return animation;
        }
    }
}
