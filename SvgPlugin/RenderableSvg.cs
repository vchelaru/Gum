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

namespace SvgPlugin
{
    public class RenderableSvg : IRenderableIpso, IVisible, IAspectRatio
    {
        public bool ClipsChildren => false;

        Vector2 Position;


        IRenderableIpso mParent;
        public IRenderableIpso Parent
        {
            get { return mParent; }
            set
            {
                if (mParent != value)
                {
                    if (mParent != null)
                    {
                        mParent.Children.Remove(this);
                    }
                    mParent = value;
                    if (mParent != null)
                    {
                        mParent.Children.Add(this);
                    }
                }
            }
        }

        ObservableCollection<IRenderableIpso> mChildren;
        public ObservableCollection<IRenderableIpso> Children
        {
            get { return mChildren; }
        }

        public BlendState BlendState
        {
            get;
            set;
        }

        public bool Wrap => false;

        public float X
        {
            get { return Position.X; }
            set { Position.X = value; }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position.Y = value; }
        }

        public float Z
        {
            get;
            set;
        }

        public float Rotation { get; set; }

        bool IPositionedSizedObject.FlipHorizontal { get; set; }

        float width;
        public float Width
        {
            get => width;
            set
            {
                width = value;
                needsUpdate = true;
            }
        }

        float height;
        public float Height
        {
            get => height;
            set
            {
                height = value;
                needsUpdate = true;
            }
        }

        public string Name
        {
            get;
            set;
        }

        public object Tag { get; set; }

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

        bool needsUpdate = true;

        Texture2D texture;

        public float AspectRatio => skiaSvg == null ? 1 : skiaSvg.ViewBox.Width / (float)skiaSvg.ViewBox.Height;

        #region IVisible Implementation

        public bool Visible
        {
            get;
            set;
        }

        public bool AbsoluteVisible
        {
            get
            {
                if (((IVisible)this).Parent == null)
                {
                    return Visible;
                }
                else
                {
                    return Visible && ((IVisible)this).Parent.AbsoluteVisible;
                }
            }
        }

        IVisible IVisible.Parent
        {
            get
            {
                return ((IRenderableIpso)this).Parent as IVisible;
            }
        }


        #endregion

        public RenderableSvg()
        {
            this.Visible = true;
            mChildren = new ObservableCollection<IRenderableIpso>();
        }

        public void PreRender()
        {
            if(needsUpdate && Width > 0 && Height > 0)
            {
                if(texture != null)
                {
                    texture.Dispose();
                    texture = null;
                }

                var colorType = SKImageInfo.PlatformColorType;

                var imageInfo = new SKImageInfo((int)Width, (int)Height, colorType, SKAlphaType.Unpremul);
                using (var surface = SKSurface.Create(imageInfo))
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

                        using (var whitePaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias=true })
                        {
                            var radius = Width / 2;
                            surface.Canvas.DrawCircle(new SKPoint(radius, radius), radius, whitePaint);
                        }
                    }

                    var skImage = surface.Snapshot();
                    texture = RenderImageToTexture2D(skImage, SystemManagers.Default.Renderer.GraphicsDevice, colorType);
                }
                needsUpdate = false;
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

        public void Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            
            Sprite.Render(managers, spriteRenderer, this, texture, Color.White, rotationInDegrees:Rotation);
            //throw new NotImplementedException();
        }

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        public static Texture2D RenderImageToTexture2D(SKImage image, GraphicsDevice graphicsDevice, SKColorType skiaColorType)
        {
            var pixelMap = image.PeekPixels();
            var pointer = pixelMap.GetPixels();
            var originalPixels = new byte[image.Height * pixelMap.RowBytes];

            Marshal.Copy(pointer, originalPixels, 0, originalPixels.Length);

            var texture = new Texture2D(graphicsDevice, image.Width, image.Height);
            if(skiaColorType == SKColorType.Rgba8888)
            {
                texture.SetData(originalPixels);
            }
            else
            {
                // need a new byte[] to convert from BGRA to ARGB
                var convertedBytes = new byte[originalPixels.Length];

                for(int i = 0; i < convertedBytes.Length; i+=4)
                {
                    var b = originalPixels[i + 0];
                    var g = originalPixels[i + 1];
                    var r = originalPixels[i + 2];
                    var a = originalPixels[i + 3];

                    convertedBytes[i + 0] = r;
                    convertedBytes[i + 1] = g;
                    convertedBytes[i + 2] = b;
                    convertedBytes[i + 3] = a;
                }

                texture.SetData(convertedBytes);

            }
            return texture;
        }

    }
}
