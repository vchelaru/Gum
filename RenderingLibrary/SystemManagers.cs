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

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            Renderer = new Renderer();
            Renderer.Initialize(graphicsDevice, this);

            SpriteManager = new SpriteManager();

            ShapeManager = new ShapeManager();

            TextManager = new TextManager();

            SpriteManager.Managers = this;
            ShapeManager.Managers = this;
            TextManager.Managers = this;
        }


    }
}
