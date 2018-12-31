using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics
{
    public class SolidRectangle : IRenderableIpso, IVisible
    {
        #region Fields
        
        Vector2 Position;
        IRenderableIpso mParent;

        ObservableCollection<IRenderableIpso> mChildren;
        private static Texture2D mTexture;
        public static Rectangle SinglePixelTextureSourceRectangle;

        public Color Color;

        #endregion

        #region Properties
        public static string AtlasedTextureName { get; set; }

        public static Texture2D Texture
        {
            get { return mTexture; }
        }

        public bool Wrap
        {
            get { return false; }
        }

        public string Name
        {
            get;
            set;
        }
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

        public float Width
        {
            get;
            set;
        }

        public float Height
        {
            get;
            set;
        }


        bool IRenderableIpso.ClipsChildren
        {
            get
            {
                return false;
            }
        }
        public IRenderableIpso Parent
        {
            get { return mParent; }
            set
            {
                if (mParent != value)
                {
                    if (mParent != null && mParent.Children != null)
                    {
                        mParent.Children.Remove(this);
                    }
                    mParent = value;
                    if (mParent != null && mParent.Children != null)
                    {
                        mParent.Children.Add(this);
                    }
                }
            }
        }

        public float Rotation { get; set; }

        public ObservableCollection<IRenderableIpso> Children
        {
            get { return mChildren; }
        }

        public object Tag { get; set; }

        public BlendState BlendState { get; set; }


        public int Alpha
        {
            get
            {
                return Color.A;
            }
            set
            {
                Color.A = (byte)value;
            }
        }

        public int Red
        {
            get
            {
                return Color.R;
            }
            set
            {
                Color.R = (byte)value;
            }
        }

        public int Green
        {
            get
            {
                return Color.G;
            }
            set
            {
                Color.G = (byte)value;
            }
        }

        public int Blue
        {
            get
            {
                return Color.B;
            }
            set
            {
                Color.B = (byte)value;
            }
        }


        #endregion

        public SolidRectangle()
        {
            mChildren = new ObservableCollection<IRenderableIpso>();
            Color = Color.White;
            Visible = true;

            if (mTexture == null && !string.IsNullOrEmpty(AtlasedTextureName)) mTexture = GetAtlasedTexture();
        }

        /// <summary>
        /// Checks if the Colored Rectangle texture is located in a loaded atlas.
        /// </summary>
        /// <returns>Returns atlased texture if it exists.</returns>
        private Texture2D GetAtlasedTexture()
        {
            Texture2D texture = null;

            if (ToolsUtilities.FileManager.IsRelative(AtlasedTextureName))
            {
                AtlasedTextureName = ToolsUtilities.FileManager.RelativeDirectory + AtlasedTextureName;

                AtlasedTextureName = ToolsUtilities.FileManager.RemoveDotDotSlash(AtlasedTextureName);
            }

            // see if an atlas exists:
            var atlasedTexture =
                global::RenderingLibrary.Content.LoaderManager.Self.TryLoadContent<AtlasedTexture>(AtlasedTextureName);

            if (atlasedTexture != null)
            {
                SinglePixelTextureSourceRectangle = new Rectangle(atlasedTexture.SourceRectangle.Left + 1,
                    atlasedTexture.SourceRectangle.Top + 1, 1, 1);

                texture = atlasedTexture.Texture;
            }

            return texture;
        }

        void IRenderable.Render(SpriteRenderer spriteRenderer, SystemManagers managers)
        {
            if (this.AbsoluteVisible && this.Width > 0 && this.Height > 0)
            {
                Renderer renderer = null;
                if (managers == null)
                {
                    renderer = Renderer.Self;
                }
                else
                {
                    renderer = managers.Renderer;
                }

                var texture = renderer.SinglePixelTexture;
                Rectangle? sourceRect = renderer.SinglePixelSourceRectangle;
                if (mTexture != null)
                {
                    texture = mTexture;
                    sourceRect = SinglePixelTextureSourceRectangle;
                }

                Sprite.Render(managers, spriteRenderer, this, texture, Color, sourceRect, false, false, Rotation);
            }
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

        void IRenderableIpso.SetParentDirect(IRenderableIpso parent)
        {
            mParent = parent;
        }

        void IRenderable.PreRender() { }

        public bool Visible
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Name + " (SolidRectangle)";
        }
    }
}
