using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.ObjectModel;
using BlendState = Gum.BlendState;
using Color = System.Drawing.Color;

namespace SkiaGum.Renderables
{
    /// <summary>
    /// MonoGame rendering wrapper for an <see cref="ISkiaSurfaceDrawable"/>.
    /// Owns the <see cref="SpriteBatchRenderableBase"/>, <see cref="IRenderableIpso"/>,
    /// and <see cref="IManagedObject"/> concerns so that shape classes can remain pure
    /// Skia drawing objects.
    /// </summary>
    public class SkiaTexturedRenderable : SpriteBatchRenderableBase, IRenderableIpso, IVisible, IManagedObject
    {
        #region Fields

        readonly ISkiaSurfaceDrawable _drawable;
        readonly SkiaObjectTextureRenderer _textureRenderer;

        Microsoft.Xna.Framework.Vector2 _position;
        IRenderableIpso? _mParent;
        ObservableCollection<IRenderableIpso> _mChildren;

        // Separate backing fields so Render() can temporarily adjust size
        // without triggering needsUpdate on the drawable.
        float _width;
        float _height;

        #endregion

        #region Constructor

        public SkiaTexturedRenderable(ISkiaSurfaceDrawable drawable)
        {
            _drawable = drawable;
            _textureRenderer = new SkiaObjectTextureRenderer(_drawable);
            _mChildren = new ObservableCollection<IRenderableIpso>();
            Visible = true;
        }

        #endregion

        #region Drawable Access

        /// <summary>
        /// The underlying shape/content object that draws to a Skia surface.
        /// </summary>
        public ISkiaSurfaceDrawable Drawable => _drawable;

        #endregion

        #region IRenderableIpso — positioning and scene-graph

        bool IRenderableIpso.IsRenderTarget => false;

        public float X
        {
            get => _position.X;
            set => _position.X = value;
        }

        public float Y
        {
            get => _position.Y;
            set => _position.Y = value;
        }

        public float Z { get; set; }

        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                _drawable.Width = value;
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                _drawable.Height = value;
            }
        }

        public float Rotation { get; set; }

        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }

        public string? Name { get; set; }

        public object? Tag { get; set; }

        ColorOperation IRenderableIpso.ColorOperation => _drawable.ColorOperation;

        public IRenderableIpso? Parent
        {
            get => _mParent;
            set
            {
                if (_mParent != value)
                {
                    if (_mParent != null)
                    {
                        _mParent.Children.Remove(this);
                    }
                    _mParent = value;
                    if (_mParent != null)
                    {
                        _mParent.Children.Add(this);
                    }
                }
            }
        }

        public ObservableCollection<IRenderableIpso> Children => _mChildren;

        public bool ClipsChildren => false;

        public bool Wrap => false;

        int IRenderableIpso.Alpha => _drawable.Color.A;

        void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
        {
            _mParent = parent;
        }

        #endregion

        #region IVisible

        public bool Visible { get; set; }

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

        IVisible? IVisible.Parent => _mParent as IVisible;

        #endregion

        #region IManagedObject

        public void AddToManagers()
        {
            _textureRenderer.AddToManagers();
        }

        public void RemoveFromManagers()
        {
            _textureRenderer.RemoveFromManagers();
        }

        #endregion

        #region Pre-render and Render

        public void PreRender()
        {
            _drawable.PreRender();
            _textureRenderer.NeedsUpdate = _drawable.NeedsUpdate;
            _textureRenderer.PreRender();
            if (!_textureRenderer.NeedsUpdate)
            {
                _drawable.NeedsUpdate = false;
            }
        }

        public override void Render(ISystemManagers managers)
        {
            if (AbsoluteVisible)
            {
                var xSpillover = _drawable.XSizeSpillover;
                var ySpillover = _drawable.YSizeSpillover;

                var oldX = this.X;
                var oldY = this.Y;

                this.X -= xSpillover;
                this.Y -= ySpillover;
                // Use backing fields directly to avoid triggering needsUpdate on the drawable.
                _width += xSpillover * 2;
                _height += ySpillover * 2;

                var color = _drawable.ShouldApplyColorOnSpriteRender ? _drawable.Color : Color.White;

                var systemManagers = managers as SystemManagers;

                Sprite.Render(systemManagers, systemManagers!.Renderer.SpriteRenderer, this, _textureRenderer.Texture, color, rotationInDegrees: Rotation);

                this.X = oldX;
                this.Y = oldY;
                _width -= xSpillover * 2;
                _height -= ySpillover * 2;
            }
        }

        #endregion
    }
}
