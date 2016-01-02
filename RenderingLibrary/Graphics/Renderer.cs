using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework;
using System.IO;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Graphics
{
    public class RenderStateVariables
    {
        public BlendState BlendState;
        public bool Filtering;
        public bool Wrap;
    }

    public class Renderer
    {
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
        }

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
                if (mSelf == null)
                {
                    mSelf = new Renderer();
                }
                return mSelf;

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


        public void AddLayer(SortableLayer sortableLayer, Layer masterLayer)
        {
            if (masterLayer == null)
            {
                masterLayer = LayersWritable[0];
            }

            masterLayer.Add(sortableLayer);
        }

        public void Draw(SystemManagers managers)
        {
            ClearPerformanceRecordingVariables();

            if (managers == null)
            {
                managers = SystemManagers.Default;
            }
            // Before we draw, make sure all Text objects have their text updated
            managers.TextManager.RenderTextTextures();

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

                mRenderStateVariables.BlendState = BlendState.NonPremultiplied;
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


                mRenderStateVariables.BlendState = BlendState.NonPremultiplied;
                mRenderStateVariables.Wrap = false;


                foreach (Layer layer in layers)
                {
                    RenderLayer(managers, layer);
                }
            }
        }

        internal void RenderLayer(SystemManagers managers, Layer layer)
        {
            //////////////////Early Out////////////////////////////////
            if (layer.Renderables.Count == 0)
            {
                return;
            }
            ///////////////End Early Out///////////////////////////////

            // If the Layer's clip region has no width or height, then let's
            // skip over rendering it, otherwise XNA crashes:
            var clipRegion = layer.GetScissorRectangleFor(managers.Renderer.Camera);

            if (clipRegion.Width != 0 && clipRegion.Height != 0)
            {
                spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Push, mCamera);

                layer.SortRenderables();

                foreach (IRenderable renderable in layer.RenderablesWriteable)
                {
                    AdjustRenderStates(mRenderStateVariables, layer, renderable);
                    renderable.Render(spriteRenderer, managers);
                }

                spriteRenderer.EndSpriteBatch();
            }
        }

        private void AdjustRenderStates(RenderStateVariables renderState, Layer layer, IRenderable renderable)
        {
            BlendState renderBlendState = renderable.BlendState;
            bool wrap = renderable.Wrap;
            bool shouldResetStates = false;

            if (renderBlendState == null)
            {
                renderBlendState = BlendState.NonPremultiplied;
            }
            if (renderState.BlendState != renderBlendState)
            {
                renderState.BlendState = renderable.BlendState;
                shouldResetStates = true;

            }

            if (renderState.Wrap != wrap)
            {
                renderState.Wrap = wrap;
                shouldResetStates = true;
            }


            if (shouldResetStates)
            {
                spriteRenderer.BeginSpriteBatch(renderState, layer, BeginType.Begin, mCamera);
            }
        }




        internal void RemoveRenderable(IRenderable renderable)
        {
            foreach (Layer layer in this.Layers)
            {
                if (layer.Renderables.Contains(renderable))
                {
                    layer.RenderablesWriteable.Remove(renderable);
                }
            }
        }

        public void RemoveLayer(SortableLayer sortableLayer)
        {
            RemoveRenderable(sortableLayer);
        }

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
