using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace SkiaPlugin.Renderables
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

        public ColorOperation ColorOperation
        {
            get => base.colorOperation;
            set => base.colorOperation = value;
        }

        //public bool UseColorTextureAlpha
        //{
        //    get => ForceUseColor;
        //    set => ForceUseColor = value;
        //}


#if INCLUDE_SVG
        global::SkiaSharp.Extended.Svg.SKSvg skiaSvg;
        public float AspectRatio => skiaSvg == null ? 1 : skiaSvg.ViewBox.Width / (float)skiaSvg.ViewBox.Height;
#else
        public float AspectRatio => 1;
#endif


        protected override bool ShouldApplyColorOnSpriteRender => true;

#endregion

        internal override void DrawToSurface(SKSurface surface)
        {
#if INCLUDE_SVG

            if (skiaSvg != null)
            {
                surface.Canvas.Clear(SKColors.Transparent);

                var scaleX = this.Width / skiaSvg.ViewBox.Width;
                var scaleY = this.Height / skiaSvg.ViewBox.Height;

                SKMatrix scaleMatrix = SKMatrix.MakeScale(scaleX, scaleY);

                {

                    surface.Canvas.DrawPicture(skiaSvg.Picture, ref scaleMatrix);
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
        private SkiaSharp.Extended.Svg.SKSvg GetSkSvg()
        {
            SkiaSharp.Extended.Svg.SKSvg skiaSvg = null;

            if (!string.IsNullOrWhiteSpace(sourceFile))
            {
                try
                {
                    var sourceFileAbsolute =
                        FileManager.RemoveDotDotSlash(Gum.ToolStates.ProjectState.Self.ProjectDirectory + sourceFile);
                    if (System.IO.File.Exists(sourceFileAbsolute))
                    {
                        using (var fileStream = System.IO.File.OpenRead(sourceFileAbsolute))
                        {
                            skiaSvg = new SkiaSharp.Extended.Svg.SKSvg();
                            skiaSvg.Load(fileStream);
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
