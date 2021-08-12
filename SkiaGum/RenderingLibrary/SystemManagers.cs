using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace RenderingLibrary
{
    public partial class SystemManagers
    {

        public static SystemManagers Default
        {
            get;
            set;
        }

        public RenderingLibrary.Graphics.Renderer Renderer { get; private set; }

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
    }
}
