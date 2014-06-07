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
                int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                return threadId == mPrimaryThreadId;
            }
        }

        #endregion

        public void Activity(double currentTime)
        {
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

        public static SystemManagers CreateFromSingletons()
        {
            SystemManagers systemManagers = new SystemManagers();

            systemManagers.mPrimaryThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;


            systemManagers.Renderer = Renderer.Self;
            systemManagers.SpriteManager = SpriteManager.Self;
            systemManagers.ShapeManager = ShapeManager.Self;
            systemManagers.TextManager = TextManager.Self;

            systemManagers.SpriteManager.Managers = systemManagers;
            systemManagers.ShapeManager.Managers = systemManagers;
            systemManagers.TextManager.Managers = systemManagers;

            return systemManagers;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
