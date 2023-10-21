using RenderingLibrary.Graphics;

namespace RenderingLibrary
{
    public partial class SystemManagers : ISystemManagers
    {
        /// <summary>
        /// The font scale value. This can be used to scale all fonts globally, 
        /// generally in response to a font scaling value like the Android font scale setting.
        /// </summary>
        public static float GlobalFontScale { get; set; } = 1.0f;

        public static SystemManagers Default
        {
            get;
            set;
        }

        public SkiaSharp.SKCanvas Canvas { get; set; }


        public RenderingLibrary.Graphics.Renderer Renderer { get; private set; }
        public bool EnableTouchEvents { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        IRenderer ISystemManagers.Renderer => throw new System.NotImplementedException();

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
        }

        public void InvalidateSurface()
        {
            
        }
    }
}
