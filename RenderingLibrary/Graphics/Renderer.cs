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
    class RenderStateVariables
    {
        public BlendState BlendState;
        public bool Filtering;
        public bool Wrap;
    }

    public class Renderer
    {
        #region Fields

        RasterizerState scissorTestEnabled;
        RasterizerState scissorTestDisabled;

        int mDrawCallsPerFrame = 0;

        List<Layer> mLayers = new List<Layer>();
        ReadOnlyCollection<Layer> mLayersReadOnly;

        protected SpriteBatchStack mSpriteBatch;
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
            get;
            private set;
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

        public int DrawCalls
        {
            get
            {
                return mDrawCallsPerFrame;
            }
        }

        internal SpriteBatch SpriteBatch
        {
            get
            {
                return mSpriteBatch.SpriteBatch;
            }
        }

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

            mSpriteBatch = new SpriteBatchStack(mGraphicsDevice);

            mSinglePixelTexture = new Texture2D(mGraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Color[] pixels = new Color[1];
            pixels[0] = Color.White;
            mSinglePixelTexture.SetData<Color>(pixels);

            mDottedLineTexture = new Texture2D(mGraphicsDevice, 2, 1, false, SurfaceFormat.Color);
            pixels = new Color[2];
            pixels[0] = Color.White;
            pixels[1] = Color.Transparent;
            mDottedLineTexture.SetData<Color>(pixels);

            CreateRasterizerStates();

            mCamera.UpdateClient();
        }

        private void CreateRasterizerStates()
        {
            scissorTestEnabled = new RasterizerState();
            scissorTestDisabled = new RasterizerState();

            scissorTestEnabled.CullMode = CullMode.None;

            scissorTestEnabled.ScissorTestEnable = true;
            scissorTestDisabled.ScissorTestEnable = false;
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
            mDrawCallsPerFrame = 0;

            if(managers == null)
            {
                managers = SystemManagers.Default;
            }
            // Before we draw, make sure all Text objects have their text updated
            managers.TextManager.RenderTextTextures();

            Draw(managers, mLayers);
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
            // If the Layer's clip region has no width or height, then let's
            // skip over rendering it, otherwise XNA crashes:
            var clipRegion = layer.GetScissorRectangleFor(managers.Renderer.Camera);

            if(clipRegion.Width != 0 && clipRegion.Height != 0)
            {
                BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Push);

                layer.SortRenderables();

                foreach (IRenderable renderable in layer.RenderablesWriteable)
                {
                    AdjustRenderStates(mRenderStateVariables, layer, renderable);
                    renderable.Render(mSpriteBatch.SpriteBatch, managers);
                }

                mSpriteBatch.Pop();
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
                BeginSpriteBatch(renderState, layer, BeginType.Begin);
            }
        }

        private void BeginSpriteBatch(RenderStateVariables renderStates, Layer layer, BeginType beginType)
        {

            Matrix matrix = GetZoomAndMatrix(layer);

            SamplerState samplerState = GetSamplerState(renderStates);

            RasterizerState rasterizerState = GetRasterizerState(renderStates, layer);


            Rectangle scissorRectangle = new Rectangle();
            if(rasterizerState.ScissorTestEnable)
            {
                scissorRectangle = layer.GetScissorRectangleFor(mCamera);
            }


            DepthStencilState depthStencilState = DepthStencilState.DepthRead;

            if (beginType == BeginType.Begin)
            {
                mSpriteBatch.Begin(SpriteSortMode.Immediate, renderStates.BlendState,
                    samplerState,
                    depthStencilState,
                    rasterizerState,
                    null, matrix, 
                    scissorRectangle);
            }
            else 
            {
                mSpriteBatch.Push(SpriteSortMode.Immediate, renderStates.BlendState,
                    samplerState,
                    depthStencilState,
                    rasterizerState,
                    null, matrix,
                    scissorRectangle);
            }
            mDrawCallsPerFrame++;
        }
        private RasterizerState GetRasterizerState(RenderStateVariables renderStates, Layer layer)
        {
            bool isFullscreen = layer.ScissorIpso == null;

            if (isFullscreen)
            {
                return scissorTestDisabled;
            }
            else
            {
                return scissorTestEnabled;
            }
        }
        private Microsoft.Xna.Framework.Graphics.SamplerState GetSamplerState(RenderStateVariables renderStates)
        {
            SamplerState samplerState;

            if (renderStates.Wrap)
            {
                if (renderStates.Filtering)
                {
                    samplerState = SamplerState.LinearWrap;
                }
                else
                {
                    samplerState = SamplerState.PointWrap;
                }
            }
            else
            {
                if (renderStates.Filtering)
                {
                    samplerState = SamplerState.LinearClamp;
                }
                else
                {
                    samplerState = SamplerState.PointClamp;
                }
            }
            return samplerState;
        }

        private Matrix GetZoomAndMatrix(Layer layer)
        {
            Matrix matrix;

            if (layer.LayerCameraSettings != null)
            {
                if (layer.LayerCameraSettings.IsInScreenSpace)
                {
                    float zoom = 1;
                    if (layer.LayerCameraSettings.Zoom.HasValue)
                    {
                        zoom = layer.LayerCameraSettings.Zoom.Value;
                    }
                    matrix = Matrix.CreateScale(zoom);
                    CurrentZoom = zoom;
                }
                else
                {
                    float zoom = Camera.Zoom;
                    if (layer.LayerCameraSettings.Zoom.HasValue)
                    {
                        zoom = layer.LayerCameraSettings.Zoom.Value;
                    }
                    matrix = Camera.GetTransformationMatirx(Camera.X, Camera.Y, zoom, Camera.ClientWidth, Camera.ClientHeight);
                    CurrentZoom = zoom;
                }
            }
            else
            {
                matrix = Camera.GetTransformationMatrix();
                CurrentZoom = Camera.Zoom;
            }
            return matrix;
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

        #endregion


    }
}
