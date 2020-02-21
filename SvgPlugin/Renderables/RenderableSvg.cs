using Gum.ToolStates;
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
        string sourceFile;
        public string SourceFile
        {
            get => sourceFile;
            set
            {
                if (sourceFile != value)
                {
                    sourceFile = value;
                    skiaSvg = GetSkSvg();
                    needsUpdate = true;
                }
            }
        }

        SkiaSharp.Extended.Svg.SKSvg skiaSvg;

        public float AspectRatio => skiaSvg == null ? 1 : skiaSvg.ViewBox.Width / (float)skiaSvg.ViewBox.Height;

        internal override void DrawToSurface(SKSurface surface)
        {
            if (skiaSvg != null)
            {
                var scaleX = this.Width / skiaSvg.ViewBox.Width;
                var scaleY = this.Height / skiaSvg.ViewBox.Height;

                SKMatrix scaleMatrix = SKMatrix.MakeScale(scaleX, scaleY);
                //SKMatrix rotationMatrix = SKMatrix.MakeRotationDegrees(-Rotation);
                //SKMatrix result = SKMatrix.MakeIdentity();
                //SKMatrix.Concat(
                //ref result, rotationMatrix, scaleMatrix);

                surface.Canvas.DrawPicture(skiaSvg.Picture, ref scaleMatrix);
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
        }

        private SkiaSharp.Extended.Svg.SKSvg GetSkSvg()
        {
            SkiaSharp.Extended.Svg.SKSvg skiaSvg = null;

            if (!string.IsNullOrWhiteSpace(sourceFile))
            {
                var sourceFileAbsolute =
                    FileManager.RemoveDotDotSlash(ProjectState.Self.ProjectDirectory + sourceFile);
                if (System.IO.File.Exists(sourceFileAbsolute))
                {
                    using (var fileStream = System.IO.File.OpenRead(sourceFileAbsolute))
                    {
                        skiaSvg = new SkiaSharp.Extended.Svg.SKSvg();
                        skiaSvg.Load(fileStream);
                    }
                }
            }

            return skiaSvg;
        }
    }
}
