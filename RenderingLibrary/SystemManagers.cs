using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;

namespace RenderingLibrary
{
    public class SystemManagers
    {
        int mPrimaryThreadId;

        public Renderer Renderer
        {
            get;
            private set;
        }

        public SpriteManager SpriteManager
        {
            get;
            private set;
        }

        public ShapeManager ShapeManager
        {
            get;
            private set;
        }

        public TextManager TextManager
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            set;
        }

        public bool IsCurrentThreadPrimary
        {
            get
            {
                int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                return threadId == mPrimaryThreadId;
            }
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {

            mPrimaryThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            Renderer = new Renderer();
            Renderer.Initialize(graphicsDevice, this);

            SpriteManager = new SpriteManager();

            ShapeManager = new ShapeManager();

            TextManager = new TextManager();

            SpriteManager.Managers = this;
            ShapeManager.Managers = this;
            TextManager.Managers = this;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
