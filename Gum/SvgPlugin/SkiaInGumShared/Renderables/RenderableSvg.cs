using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using SkiaSharp;
using ToolsUtilities;

namespace SkiaGum.Renderables
{
    public class RenderableSvg : RenderableSkiaObject, IAspectRatio
    {
        #region Fields/Properties

        string sourceFile;
        public string SourceFile
        {
            get => sourceFile;
            set
            {
                if (sourceFile != value)
                {
                    sourceFile = value;
#if INCLUDE_SVG
                    skiaSvg = GetSkSvg();
#endif
                    needsUpdate = true;
                }
            }
        }

        public new ColorOperation ColorOperation
        {
            get => base.ColorOperation;
            set => base.ColorOperation = value;
        }

        //public bool UseColorTextureAlpha
        //{
        //    get => ForceUseColor;
        //    set => ForceUseColor = value;
        //}


#if INCLUDE_SVG
        Svg.Skia.SKSvg skiaSvg;
        // old implementation:
        //public float AspectRatio => skiaSvg == null ? 1 : skiaSvg.ViewBox.Width / (float)skiaSvg.ViewBox.Height;
        public float AspectRatio => skiaSvg == null ? 1 : skiaSvg.Picture.CullRect.Width / (float)skiaSvg.Picture.CullRect.Height;
#else
        public float AspectRatio => 1;
#endif


        protected override bool ShouldApplyColorOnSpriteRender => true;

        #endregion

        public override void DrawToSurface(SKSurface surface)
        {
#if INCLUDE_SVG

            if (skiaSvg != null)
            {
                surface.Canvas.Clear(SKColors.Transparent);

                var scaleX = this.Width / skiaSvg.Picture.CullRect.Width;
                var scaleY = this.Height / skiaSvg.Picture.CullRect.Height;

                SKMatrix scaleMatrix = SKMatrix.CreateScale(scaleX, scaleY);

                {

                    surface.Canvas.DrawPicture(skiaSvg.Picture , scaleMatrix);
                }
            }
            else
            {
                surface.Canvas.Clear(SKColors.Red);

                using (var whitePaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true })
                {
                    var radius = Width / 2;
                    surface.Canvas.DrawCircle(new SKPoint(radius, radius), radius, whitePaint);
                }
            }
#endif
        }

#if INCLUDE_SVG
        private Svg.Skia.SKSvg GetSkSvg()
        {
            Svg.Skia.SKSvg skiaSvg = null;

            if (!string.IsNullOrWhiteSpace(sourceFile))
            {
                try
                {
                    var disposable = LoaderManager.Self.GetDisposable(sourceFile) as
                        Svg.Skia.SKSvg;

                    if(disposable != null)
                    {
                        skiaSvg = disposable;
                    }
                    else
                    {
                        var sourceFileAbsolute =
                            FileManager.MakeAbsolute(sourceFile);
                        if (System.IO.File.Exists(sourceFileAbsolute))
                        {
                            using (var fileStream = System.IO.File.OpenRead(sourceFileAbsolute))
                            {
                                skiaSvg = new Svg.Skia.SKSvg();
                                skiaSvg.Load(fileStream);

                                if(LoaderManager.Self.CacheTextures)
                                {
                                    LoaderManager.Self.AddDisposable(sourceFile, skiaSvg);
                                }
                            }
                        }
                    }

                }
                catch
                {
                    // do nothing? Report a problem?
                    skiaSvg = null;
                }
            }

            return skiaSvg;
        }
#endif
    }
}
