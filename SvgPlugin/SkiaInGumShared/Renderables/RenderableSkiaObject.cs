﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SkiaPlugin.Renderables
{
    public abstract class RenderableSkiaObject : IRenderableIpso, IVisible
    {
        #region Fields/Properties

        protected Microsoft.Xna.Framework.Vector2 Position;

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

        public bool ClipsChildren => false;

        public bool Wrap => false;

        protected Texture2D texture;

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

        public bool FlipHorizontal { get; set; }

        float width;
        public float Width
        {
            get => width;
            set
            {
                if(value != width)
                {
                    width = value;
                    needsUpdate = true;
                }
            }
        }

        float height;
        public float Height
        {
            get => height;
            set
            {
                if(value != height)
                {
                    height = value;
                    needsUpdate = true;
                }
            }
        }

        public string Name
        {
            get;
            set;
        }

        public object Tag { get; set; }

        public Color Color = Color.White;

        public int Alpha
        {
            get => Color.A;
            set
            {
                Color.A = (byte)value;
                needsUpdate = true;
            }
        }

        /// <summary>
        /// If this is false, then the DrawToSurface will handle applying the colors (like when creating a RoundedRectangle). If true,
        /// then this will multiply the rendering by the argument color (like when rendering an SVG).
        /// </summary>
        protected virtual bool ShouldApplyColorOnSpriteRender => false;

        public int Red
        {
            get => Color.R;
            set
            {
                Color.R = (byte)value;
                needsUpdate = true;
            }
        }

        public int Green
        {
            get => Color.G;
            set
            {
                Color.G = (byte)value;
                needsUpdate = true;
            }
        }

        public int Blue
        {
            get => Color.B;
            set
            {
                Color.B = (byte)value;
                needsUpdate = true;
            }
        }

        protected bool needsUpdate = true;

        protected virtual float XSizeSpillover => 0;
        protected virtual float YSizeSpillover => 0;

        protected bool ForceUseColor
        {
            get; set;
        }

        ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

        #endregion

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

        public RenderableSkiaObject()
        {
            this.Visible = true;
            mChildren = new ObservableCollection<IRenderableIpso>();
        }

        public void Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            if(AbsoluteVisible)
            {
                var oldX = this.X;
                var oldY = this.Y;
                var oldWidth = this.Width;
                var oldHeight = this.Height;

                this.X -= XSizeSpillover;
                this.Y -= YSizeSpillover;
                // use fields not props, to not trigger a needsUpdate = true
                this.width += XSizeSpillover * 2;
                this.height += YSizeSpillover * 2;

                var color = ShouldApplyColorOnSpriteRender ? Color : Color.White;

                Sprite.Render(managers, spriteRenderer, this, texture, color, rotationInDegrees: Rotation);

                this.X = oldX;
                this.Y = oldY;
                this.width = oldWidth;
                this.height = oldHeight;
            }
        }

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        public void PreRender()
        {
            if (needsUpdate && Width > 0 && Height > 0 && AbsoluteVisible)
            {
                if (texture != null)
                {
                    texture.Dispose();
                    texture = null;
                }

                var colorType = SKImageInfo.PlatformColorType;

                var widthToUse = Math.Min(2048, Width + XSizeSpillover * 2);
                var heightToUse = Math.Min(2048, Height + YSizeSpillover * 2);

                //var imageInfo = new SKImageInfo((int)widthToUse, (int)heightToUse, colorType, SKAlphaType.Unpremul);
                var imageInfo = new SKImageInfo((int)widthToUse, (int)heightToUse, colorType, SKAlphaType.Premul);
                using (var surface = SKSurface.Create(imageInfo))
                {
                    // It's possible this can fail
                    if(surface != null)
                    {
                        DrawToSurface(surface);

                        var skImage = surface.Snapshot();

                        Color? forcedColor = null;
                        if(ForceUseColor)
                        {
                            forcedColor = this.Color;
                        }

                        texture = RenderImageToTexture2D(skImage, SystemManagers.Default.Renderer.GraphicsDevice, colorType, forcedColor);
                        needsUpdate = false;
                    }
                }
            }
        }

        internal abstract void DrawToSurface(SKSurface surface);

        public static Texture2D RenderImageToTexture2D(SKImage image, GraphicsDevice graphicsDevice, SKColorType skiaColorType, Color? forcedColor = null)
        {
            var pixelMap = image.PeekPixels();
            var pointer = pixelMap.GetPixels();
            var originalPixels = new byte[image.Height * pixelMap.RowBytes];

            Marshal.Copy(pointer, originalPixels, 0, originalPixels.Length);

            var texture = new Texture2D(graphicsDevice, image.Width, image.Height);
            if (skiaColorType == SKColorType.Rgba8888)
            {
                texture.SetData(originalPixels);
            }
            else
            {
                // need a new byte[] to convert from BGRA to ARGB
                var convertedBytes = new byte[originalPixels.Length];

                var premult = false;
            
                if(premult)
                {
                    for (int i = 0; i < convertedBytes.Length; i += 4)
                    {
                        var b = originalPixels[i + 0];
                        var g = originalPixels[i + 1];
                        var r = originalPixels[i + 2];
                        var a = originalPixels[i + 3];

                        //var ratio = a / 255.0f;

                        //convertedBytes[i + 0] = (byte)(r * ratio + .5);
                        //convertedBytes[i + 1] = (byte)(g * ratio + .5);
                        //convertedBytes[i + 2] = (byte)(b * ratio + .5);
                        //convertedBytes[i + 3] = a;

                        if (forcedColor != null)
                        {
                            r = forcedColor.Value.R;
                            g = forcedColor.Value.G;
                            b = forcedColor.Value.B;
                        }

                        convertedBytes[i + 0] = r;
                        convertedBytes[i + 1] = g;
                        convertedBytes[i + 2] = b;
                        convertedBytes[i + 3] = a;
                    }
                }
                else
                {
                    for (int i = 0; i < convertedBytes.Length; i += 4)
                    {
                        var b = originalPixels[i + 0];
                        var g = originalPixels[i + 1];
                        var r = originalPixels[i + 2];
                        var a = originalPixels[i + 3];
                        var ratio = a / 255.0f;

                        if(forcedColor != null)
                        {
                            r = forcedColor.Value.R;
                            g = forcedColor.Value.G;
                            b = forcedColor.Value.B;
                        }

                        // output will always be premult so we need to unpremult
                        convertedBytes[i + 0] = (byte)(r / ratio + .5);
                        convertedBytes[i + 1] = (byte)(g / ratio + .5);
                        convertedBytes[i + 2] = (byte)(b / ratio + .5);
                        convertedBytes[i + 3] = a;
                    }
                }

                texture.SetData(convertedBytes);

            }
            return texture;
        }
    }
}
