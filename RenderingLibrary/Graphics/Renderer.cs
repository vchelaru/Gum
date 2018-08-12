using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework;
using System.IO;
using System.Collections.ObjectModel;
using RenderingLibrary.Math;
using RenderingLibrary;

namespace RenderingLibrary.Graphics
{
    public class RenderStateVariables
    {
        public BlendState BlendState;
        public bool Filtering;
        public bool Wrap;

        public Rectangle? ClipRectangle;
    }

    public class Renderer
    {
        public static bool RenderUsingHierarchy = true;

        #region Fields


        List<Layer> mLayers = new List<Layer>();
        ReadOnlyCollection<Layer> mLayersReadOnly;

        SpriteRenderer spriteRenderer = new SpriteRenderer();

        RenderStateVariables mRenderStateVariables = new RenderStateVariables();

        GraphicsDevice mGraphicsDevice;

        static Renderer mSelf;

        Camera mCamera;

        Texture2D mSinglePixelTexture;
        Texture2D mDottedLineTexture;

        public static object LockObject = new object();

        #endregion

        #region Properties

        internal float CurrentZoom
        {
            get
            {
                return spriteRenderer.CurrentZoom;
            }
            //private set;
        }

        public Layer MainLayer
        {
            get { return mLayers[0]; }
        }

        internal List<Layer> LayersWritable
        {
            get
            {
                return mLayers;
            }
        }

        public ReadOnlyCollection<Layer> Layers
        {
            get
            {
                return mLayersReadOnly;
            }
        }

        public Texture2D SinglePixelTexture
        {
            get
            {
#if DEBUG && !TEST
                // This should always be available
                if (mSinglePixelTexture == null)
                {
                    throw new InvalidOperationException("The single pixel texture is not set yet.  You must call Renderer.Initialize before accessing this property." +
                        "If running unit tests, be sure to run in UnitTest configuration");
                }
#endif
                return mSinglePixelTexture;
            }
            set
            {
                // Setter added to support rendering from sprite sheet.
                mSinglePixelTexture = value;
            }
        }

        /// <summary>
        /// The rectangle to use when rendering single-pixel texture objects, such as ColoredRectangles.
        /// By default this is null, indicating the entire texture is used.
        /// </summary>
        public Rectangle? SinglePixelSourceRectangle = null;

        public Texture2D DottedLineTexture
        {
            get
            {
#if DEBUG && !TEST
                // This should always be available
                if (mDottedLineTexture == null)
                {
                    throw new InvalidOperationException("The dotted line texture is not set yet.  You must call Renderer.Initialize before accessing this property." +
                        "If running unit tests, be sure to run in UnitTest configuration");
                }
#endif
                return mDottedLineTexture;
            }
        }

        public GraphicsDevice GraphicsDevice
        {
            get
            {
                return mGraphicsDevice;
            }
        }

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
                return SystemManagers.Default.Renderer;

            }
        }

        public Camera Camera
        {
            get
            {
                return mCamera;
            }
        }

        public SamplerState SamplerState
        {
            get;
            set;
        }

        internal SpriteRenderer SpriteRenderer
        {
            get
            {
                return spriteRenderer;
            }
        }

        /// <summary>
        /// Controls which XNA BlendState is used for the Rendering Library's Blend.Normal value.
        /// </summary>
        /// <remarks>
        /// This should be either NonPremultiplied (if textures do not use premultiplied alpha), or
        /// AlphaBlend if using premultiplied alpha textures.
        /// </remarks>
        public static BlendState NormalBlendState
        {
            get;
            set;
        } = BlendState.NonPremultiplied;

        #endregion

        #region Methods

        public void Initialize(GraphicsDevice graphicsDevice, SystemManagers managers)
        {
            SamplerState = SamplerState.LinearClamp;
            mCamera = new RenderingLibrary.Camera(managers);
            mLayersReadOnly = new ReadOnlyCollection<Layer>(mLayers);

            mLayers.Add(new Layer());
            mLayers[0].Name = "Main Layer";

            mGraphicsDevice = graphicsDevice;

            spriteRenderer.Initialize(graphicsDevice);

            mSinglePixelTexture = new Texture2D(mGraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Color[] pixels = new Color[1];
            pixels[0] = Color.White;
            mSinglePixelTexture.Name = "Rendering Library Single Pixel Texture";
            mSinglePixelTexture.SetData<Color>(pixels);

            mDottedLineTexture = new Texture2D(mGraphicsDevice, 2, 1, false, SurfaceFormat.Color);
            pixels = new Color[2];
            pixels[0] = Color.White;
            pixels[1] = Color.Transparent;
            mDottedLineTexture.SetData<Color>(pixels);

            mCamera.UpdateClient();
        }



        public Layer AddLayer()
        {
            Layer layer = new Layer();
            mLayers.Add(layer);
            return layer;
        }


        //public void AddLayer(SortableLayer sortableLayer, Layer masterLayer)
        //{
        //    if (masterLayer == null)
        //    {
        //        masterLayer = LayersWritable[0];
        //    }

        //    masterLayer.Add(sortableLayer);
        //}

        public void Draw(SystemManagers managers)
        {
            ClearPerformanceRecordingVariables();

            if (managers == null)
            {
                managers = SystemManagers.Default;
            }

            Draw(managers, mLayers);

            ForceEnd();
        }

        public void Draw(SystemManagers managers, Layer layer)
        {
            // So that 2 controls don't render at the same time.
            lock (LockObject)
            {
                mCamera.UpdateClient();

                var oldSampler = GraphicsDevice.SamplerStates[0];

                mRenderStateVariables.BlendState = Renderer.NormalBlendState;
                mRenderStateVariables.Wrap = false;

                RenderLayer(managers, layer);

                if (oldSampler != null)
                {
                    GraphicsDevice.SamplerStates[0] = oldSampler;
                }
            }
        }

        public void Draw(SystemManagers managers, IEnumerable<Layer> layers)
        {
            // So that 2 controls don't render at the same time.
            lock (LockObject)
            {
                mCamera.UpdateClient();


                mRenderStateVariables.BlendState = Renderer.NormalBlendState;
                mRenderStateVariables.Wrap = false;

                foreach (Layer layer in layers)
                {
                    PreRender(layer.Renderables);
                }

                foreach (Layer layer in layers)
                {
                    RenderLayer(managers, layer, prerender:false);
                }
            }
        }

        internal void RenderLayer(SystemManagers managers, Layer layer, bool prerender = true)
        {
            //////////////////Early Out////////////////////////////////
            if (layer.Renderables.Count == 0)
            {
                return;
            }
            ///////////////End Early Out///////////////////////////////

            if (prerender)
            {
                PreRender(layer.Renderables);
            }

            SpriteBatchStack.PerformStartOfLayerRenderingLogic();

            spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Push, mCamera);

            layer.SortRenderables();

            Render(layer.Renderables, managers, layer);

            spriteRenderer.EndSpriteBatch();
        }

        private void PreRender(IList<IRenderableIpso> renderables)
        {
#if DEBUG
            if(renderables == null)
            {
                throw new ArgumentNullException("renderables");
            }
#endif

            var count = renderables.Count;
            for(int i = 0; i < count; i++)
            {
                var renderable = renderables[i];
                renderable.PreRender();

                // Some Gum objects, like GraphicalUiElements, may not have children if the object hasn't
                // yet been assigned a visual. Just skip over it...
                if(renderable.Children != null)
                {
                    PreRender(renderable.Children);
                }
            }
        }

        private void Render(IList<IRenderableIpso> whatToRender, SystemManagers managers, Layer layer)
        {
            var count = whatToRender.Count;
            for (int i = 0; i < count; i++)
            {
                var renderable = whatToRender[i];
                var oldClip = mRenderStateVariables.ClipRectangle;
                AdjustRenderStates(mRenderStateVariables, layer, renderable);
                bool didClipChange = oldClip != mRenderStateVariables.ClipRectangle;

                renderable.Render(spriteRenderer, managers);


                if (RenderUsingHierarchy)
                {
                    Render(renderable.Children, managers, layer);
                }

                if (didClipChange)
                {
                    mRenderStateVariables.ClipRectangle = oldClip;
                    spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Begin, mCamera);
                }
            }
        }

        internal Microsoft.Xna.Framework.Rectangle GetScissorRectangleFor(Camera camera, IRenderableIpso ipso)
        {
            if (ipso == null)
            {
                return new Microsoft.Xna.Framework.Rectangle(
                    0, 0,
                    camera.ClientWidth,
                    camera.ClientHeight

                    );
            }
            else
            {

                float worldX = ipso.GetAbsoluteLeft();
                float worldY = ipso.GetAbsoluteTop();

                float screenX;
                float screenY;
                camera.WorldToScreen(worldX, worldY, out screenX, out screenY);

                int left = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenX);
                int top = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenY);

                worldX = ipso.GetAbsoluteRight();
                worldY = ipso.GetAbsoluteBottom();
                camera.WorldToScreen(worldX, worldY, out screenX, out screenY);

                int right = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenX);
                int bottom = global::RenderingLibrary.Math.MathFunctions.RoundToInt(screenY);



                left = System.Math.Max(0, left);
                top = System.Math.Max(0, top);
                right = System.Math.Max(0, right);
                bottom = System.Math.Max(0, bottom);

                left = System.Math.Min(left, camera.ClientWidth);
                right = System.Math.Min(right, camera.ClientWidth);

                top = System.Math.Min(top, camera.ClientHeight);
                bottom = System.Math.Min(bottom, camera.ClientHeight);


                int width = System.Math.Max(0, right - left);
                int height = System.Math.Max(0, bottom - top);

                // ScissorRectangles are relative to the viewport in Gum, so we need to adjust for that:
                left += this.GraphicsDevice.Viewport.X;
                right += this.GraphicsDevice.Viewport.X;

                top += this.GraphicsDevice.Viewport.Y;
                bottom += this.GraphicsDevice.Viewport.Y;

                Microsoft.Xna.Framework.Rectangle thisRectangle = new Microsoft.Xna.Framework.Rectangle(
                    left,
                    top,
                    width,
                    height);

                return thisRectangle;
            }

        }


        private void AdjustRenderStates(RenderStateVariables renderState, Layer layer, IRenderableIpso renderable)
        {
            BlendState renderBlendState = renderable.BlendState;
            bool wrap = renderable.Wrap;
            bool shouldResetStates = false;

            if (renderBlendState == null)
            {
                renderBlendState = Renderer.NormalBlendState;
            }
            if (renderState.BlendState != renderBlendState)
            {
                // This used to set this, but not sure why...I think it should set the renderBlendState:
                //renderState.BlendState = renderable.BlendState;
                renderState.BlendState = renderBlendState;

                shouldResetStates = true;

            }

            if (renderState.Wrap != wrap)
            {
                renderState.Wrap = wrap;
                shouldResetStates = true;
            }

            if (renderable.ClipsChildren)
            {
                Rectangle clipRectangle = GetScissorRectangleFor(Camera, renderable);

                if (renderState.ClipRectangle == null || clipRectangle != renderState.ClipRectangle.Value)
                {
                    //todo: Don't just overwrite it, constrain this rect to the existing one, if it's not null: 

                    var adjustedRectangle = clipRectangle;
                    if (renderState.ClipRectangle != null)
                    {
                        adjustedRectangle = ConstrainRectangle(clipRectangle, renderState.ClipRectangle.Value);
                    }


                    renderState.ClipRectangle = adjustedRectangle;
                    shouldResetStates = true;
                }

            }


            if (shouldResetStates)
            {
                spriteRenderer.BeginSpriteBatch(renderState, layer, BeginType.Begin, mCamera);
            }
        }

        private Rectangle ConstrainRectangle(Rectangle childRectangle, Rectangle parentRectangle)
        {
            int x = System.Math.Max(childRectangle.X, parentRectangle.X);
            int y = System.Math.Max(childRectangle.Y, parentRectangle.Y);

            int right = System.Math.Min(childRectangle.Right, parentRectangle.Right);
            int bottom = System.Math.Min(childRectangle.Bottom, parentRectangle.Bottom);

            return new Rectangle(x, y, right - x, bottom - y);
        }

        internal void RemoveRenderable(IRenderableIpso renderable)
        {
            foreach (Layer layer in this.Layers)
            {
                if (layer.Renderables.Contains(renderable))
                {
                    layer.Remove(renderable);
                }
            }
        }

        //public void RemoveLayer(SortableLayer sortableLayer)
        //{
        //    RemoveRenderable(sortableLayer);
        //}

        public void RemoveLayer(Layer layer)
        {
            mLayers.Remove(layer);
        }

        public void ClearPerformanceRecordingVariables()
        {
            spriteRenderer.ClearPerformanceRecordingVariables();
        }

        /// <summary>
        /// Ends the current SpriteBatchif it hasn't yet been ended. This is needed for projects which may need the
        /// rendering to end itself so that they can start sprite batch.
        /// </summary>
        public void ForceEnd()
        {
            this.spriteRenderer.End();

        }

        #endregion


    }
}
