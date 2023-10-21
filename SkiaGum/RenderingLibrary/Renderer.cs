using SkiaGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Graphics
{
    public class Renderer : IRenderer
    {
        /// <summary>
        /// Use the custom effect for rendering. This setting takes priority if
        /// both UseCustomEffectRendering and UseBasicEffectRendering are enabled.
        /// </summary>
        public static bool UseCustomEffectRendering { get; set; } = false;
        public static bool UseBasicEffectRendering { get; set; } = true;
        public static bool UsingEffect { get { return UseCustomEffectRendering || UseBasicEffectRendering; } }

        public static Renderer Self
        {
            get
            {
                // Why is this using a singleton instead of system managers default? This seems bad...

                //if (mSelf == null)
                //{
                //    mSelf = new Renderer();
                //}
                //return mSelf;
                if(SystemManagers.Default == null)
                {
                    throw new InvalidOperationException(
                        "The SystemManagers.Default is null. You must either specify the default SystemManagers, or use a custom SystemsManager if your app has multiple SystemManagers.");
                }
                return SystemManagers.Default.Renderer;

            }
        }

        public Camera Camera { get; private set; }
        public bool ClearsCanvas { get; set; } = true;

        public void Initialize(SystemManagers managers)
        {
            Camera = new Camera();
        }

        //public void Draw(SystemManagers systemManagers)
        //{
        //    var canvas = systemManagers.Canvas;
        //}

        // This syntax is a little different than standard Gum, but we're moving in that direction incrementally:
        public void Draw(IList<BindableGraphicalUiElement> whatToRender, SystemManagers managers)
        {
            if (ClearsCanvas)
            {
                managers.Canvas.Clear();
            }
            
            if (Camera.Zoom != 1)
            {
                managers.Canvas.Scale(Camera.Zoom);
            }
            if(Camera.X != 0 || Camera.Y != 0)
            {
                managers.Canvas.Translate(-Camera.X, -Camera.Y);
            }

            PreRender(whatToRender);

            foreach (var element in whatToRender)
            {
                if (element.Visible)
                {
                    ((IRenderable)element).Render(managers.Canvas);
                }
            }

            managers.Canvas.Restore();
        }

        private void PreRender(IList<BindableGraphicalUiElement> renderables)
        {
#if DEBUG
            if (renderables == null)
            {
                throw new ArgumentNullException("renderables");
            }
#endif

            var count = renderables.Count;
            for (int i = 0; i < count; i++)
            {
                var renderable = renderables[i];
                if (renderable.Visible)
                {
                    renderable.PreRender();

                    // Some Gum objects, like GraphicalUiElements, may not have children if the object hasn't
                    // yet been assigned a visual. Just skip over it...
                    if (renderable.Visible && renderable.Children != null)
                    {
                        PreRender(renderable.Children);
                    }
                }
            }
        }

        private void PreRender(ObservableCollection<IRenderableIpso> renderables)
        {
#if DEBUG
            if (renderables == null)
            {
                throw new ArgumentNullException("renderables");
            }
#endif

            var count = renderables.Count;
            for (int i = 0; i < count; i++)
            {
                var renderable = renderables[i];
                if (renderable.Visible)
                {
                    renderable.PreRender();

                    // Some Gum objects, like GraphicalUiElements, may not have children if the object hasn't
                    // yet been assigned a visual. Just skip over it...
                    if (renderable.Visible && renderable.Children != null)
                    {
                        PreRender(renderable.Children);
                    }
                }
            }
        }
    }
}
