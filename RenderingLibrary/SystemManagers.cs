using System;
using System.Collections.Generic;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework;
using RenderingLibrary.Content;


#if USE_GUMCOMMON
using Gum.Managers;
using GumRuntime;

using MonoGameGum.Renderables;
using Gum.Wireframe;
#endif

namespace RenderingLibrary
{
    public partial class SystemManagers : ISystemManagers
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

        IRenderer ISystemManagers.Renderer => Renderer;

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

        /// <summary>
        /// The font scale value. This can be used to scale all fonts globally, 
        /// generally in response to a font scaling value like the Android font scale setting.
        /// </summary>
        public static float GlobalFontScale { get; set; } = 1.0f;
        public bool EnableTouchEvents { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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

        public void Draw(List<Layer> layers)
        {
            Renderer.Draw(this, layers);
        }

        public void Initialize(GraphicsDevice graphicsDevice, bool fullInstantiation = false)
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

            if(fullInstantiation)
            {
#if USE_GUMCOMMON
                LoaderManager.Self.ContentLoader = new ContentLoader();

                var assembly = typeof(SystemManagers).Assembly;
                var bitmapPattern = ToolsUtilities.FileManager.GetStringFromEmbeddedResource(assembly, "MonoGameGum.Content.Font18Arial.fnt");
                using var stream = ToolsUtilities.FileManager.GetStreamFromEmbeddedResource(assembly, "MonoGameGum.Content.Font18Arial_0.png");
                var defaultFontTexture = Texture2D.FromStream(graphicsDevice, stream);
                Text.DefaultBitmapFont = new BitmapFont(defaultFontTexture, bitmapPattern);

                GraphicalUiElement.CanvasWidth = graphicsDevice.Viewport.Width;
                GraphicalUiElement.CanvasHeight = graphicsDevice.Viewport.Height;
                GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
                GraphicalUiElement.UpdateFontFromProperties = CustomSetPropertyOnRenderable.UpdateToFontValues;
                GraphicalUiElement.ThrowExceptionsForMissingFiles = CustomSetPropertyOnRenderable.ThrowExceptionsForMissingFiles;

                GraphicalUiElement.AddRenderableToManagers = CustomSetPropertyOnRenderable.AddRenderableToManagers;
                GraphicalUiElement.RemoveRenderableFromManagers = CustomSetPropertyOnRenderable.RemoveRenderableFromManagers;
                Renderer.ApplyCameraZoomOnWorldTranslation = true;

                ElementSaveExtensions.CustomCreateGraphicalComponentFunc = RenderableCreator.HandleCreateGraphicalComponent;

                StandardElementsManager.Self.Initialize();

                Text.RenderBoundaryDefault = false;

                ToolsUtilities.FileManager.RelativeDirectory += "Content/";

#endif
            }
        }


        public override string ToString()
        {
            return Name;
        }

        public void InvalidateSurface()
        {
            throw new NotImplementedException();
        }
    }
}
