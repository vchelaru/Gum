using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;

namespace RenderingLibrary.Graphics
{
    public class Renderer : IRenderer
    {

        List<Layer> _layers = new List<Layer>();
        ReadOnlyCollection<Layer> mLayersReadOnly;
        public ReadOnlyCollection<Layer> Layers
        {
            get
            {
                return mLayersReadOnly;
            }
        }

        public Layer MainLayer => 
            // Not sure if we have any layers in skia so do a FirstOrDefault
            _layers.FirstOrDefault();

        /// <summary>
        /// Whether renderable objects should call Render
        /// on contained children. This is true by default, 
        /// results in a hierarchical rendering order.
        /// </summary>
        public static bool RenderUsingHierarchy = true;

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

            mLayersReadOnly = new ReadOnlyCollection<Layer>(_layers);

            _layers.Add(new Layer());
            _layers[0].Name = "Main Layer";
        }

        public void Draw(SystemManagers managers)
        {
            //ClearPerformanceRecordingVariables();

            if (managers == null)
            {
                managers = SystemManagers.Default;
            }

            Draw(managers, _layers);

            //ForceEnd();
        }

        public void Draw(SystemManagers managers, List<Layer> layers)
        {
            foreach(var layer in layers)
            {
                Draw(layer.Renderables, managers, isTopLevelDraw: true);
            }
        }

        //public void Draw(SystemManagers systemManagers)
        //{
        //    var canvas = systemManagers.Canvas;
        //}


        // This syntax is a little different than standard Gum, but we're moving in that direction incrementally:
        public void Draw(IList<IRenderableIpso> whatToRender, SystemManagers managers)
        {
            Draw(whatToRender, managers, true);
        }

        public void Draw(ObservableCollection<IRenderableIpso> whatToRender, SystemManagers managers)
        {
            Draw(whatToRender, managers, true);
        }

        void Draw(IList<IRenderableIpso> whatToRender, SystemManagers managers, bool isTopLevelDraw = false)
        {
            if (ClearsCanvas && isTopLevelDraw)
            {
                managers.Canvas.Clear();
            }

            if(isTopLevelDraw)
            { 
                if (Camera.Zoom != 1)
                {
                    managers.Canvas.Scale(Camera.Zoom);
                }

                var translateX = -Camera.X;
                var translateY = -Camera.Y;

                if(Camera.CameraCenterOnScreen == CameraCenterOnScreen.Center)
                {
                    translateX += (Camera.ClientWidth / 2.0f);
                    translateY += (Camera.ClientHeight / 2.0f);
                }

                if(translateX != 0 || translateY != 0)
                {
                    managers.Canvas.Translate(translateX, translateY);
                }
            }

            PreRender(whatToRender);

            var count = whatToRender.Count;

            for (int i = 0; i < count; i++)
            {
                var renderable = whatToRender[i];
                if (renderable.Visible)
                {

                    var canvas = (managers as SystemManagers).Canvas;

                    var isOnScreen = true;

                    if (renderable.ClipsChildren)
                    {
                        var absoluteX = renderable.GetAbsoluteX();
                        var absoluteY = renderable.GetAbsoluteY();

                        var width = renderable.Width;
                        var height = renderable.Height;

                        var rect = new SKRect(absoluteX, absoluteY, absoluteX + width, absoluteY + height);

                        isOnScreen =
                            rect.Bottom > canvas.LocalClipBounds.Top &&
                            rect.Top < canvas.LocalClipBounds.Bottom &&
                            rect.Right > canvas.LocalClipBounds.Left &&
                            rect.Left < canvas.LocalClipBounds.Right;

                        if (isOnScreen)
                        {
                            canvas.Save();
                            canvas.ClipRect(rect);
                            renderable.Render(managers);
                        }
                    }
                    else
                    {
                        renderable.Render(managers);
                    }

                    if (isOnScreen)
                    {
                        if (RenderUsingHierarchy)
                        {
                            Draw(renderable.Children, managers, false);
                        }

                        if (renderable.ClipsChildren)
                        {
                            canvas.Restore();
                        }
                    }
                }
            }

            managers.Canvas.Restore();
        }

        private void PreRender(IList<IRenderableIpso> renderables)
        {
#if FULL_DIAGNOSTICS
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
#if FULL_DIAGNOSTICS
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

        public void RenderLayer(ISystemManagers managers, Layer layer, bool prerender = true)
        {
            throw new NotImplementedException();
        }
    }
}
