using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaGum;
using System;

namespace RenderingLibrary
{
    public partial class SystemManagers : ISystemManagers
    {
        /// <summary>
        /// The font scale value. This can be used to scale all fonts globally, 
        /// generally in response to a font scaling value like the Android font scale setting.
        /// </summary>
        public static float GlobalFontScale { get; set; } = 1.0f;

        static SystemManagers _default;
        public static SystemManagers Default
        {
            get => _default;
            set
            {
                _default = value;
                ISystemManagers.Default = value;
            }
        }

        public SkiaSharp.SKCanvas Canvas { get; set; }


        public RenderingLibrary.Graphics.Renderer Renderer { get; private set; }
        public bool EnableTouchEvents { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        IRenderer ISystemManagers.Renderer => Renderer;

        public void Initialize()
        {
            //mPrimaryThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            Renderer = new Renderer();
            Renderer.Initialize(this);

            //SpriteManager = new SpriteManager();

            //ShapeManager = new ShapeManager();

            //TextManager = new TextManager();

            //SpriteManager.Managers = this;
            //ShapeManager.Managers = this;
            //Tex

            GraphicalUiElement.AddRenderableToManagers = AddRenderableToManagers;

        }

        private void AddRenderableToManagers(IRenderableIpso renderable, ISystemManagers managers, Layer layer)
        {
            if (layer == null)
            {
                managers.Renderer.Layers[0].Add(renderable);
            }
            else
            {
                layer.Add(renderable);
            }
        }

        public void InvalidateSurface()
        {

        }

        public void Draw()
        {
            Renderer.Draw(this);
        }
    }
}
