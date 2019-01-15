using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;

namespace RenderingLibrary
{
    public partial class SystemManagers
    {
        #region Fields

        int mPrimaryThreadId;

        #endregion

        #region Properties

        public static SystemManagers Default
        {
            get;
            set;
        }

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
#if WINDOWS_8 || UWP
                int threadId = Environment.CurrentManagedThreadId;
#else
                int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
                return threadId == mPrimaryThreadId;
            }
        }

        #endregion

        public void Activity(double currentTime)
        {
#if DEBUG
            if(SpriteManager == null)
            {
                throw new InvalidOperationException("The SpriteManager is null - did you remember to initialize the SystemManagers?");
            }
#endif

            SpriteManager.Activity(currentTime);
        }

        public void Draw()
        {
            Renderer.Draw(this);
        }

        public void Draw(Layer layer)
        {
            Renderer.Draw(this, layer);
        }

        public void Draw(IEnumerable<Layer> layers)
        {
            Renderer.Draw(this, layers);
        }


        public void Initialize(GraphicsDevice graphicsDevice)
        {
#if WINDOWS_8 || UWP
            mPrimaryThreadId = Environment.CurrentManagedThreadId;
#else
            mPrimaryThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif

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
