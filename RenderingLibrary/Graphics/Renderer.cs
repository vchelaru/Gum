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
    public class Renderer
    {
        #region Fields
        
        List<Layer> mLayers = new List<Layer>();
        ReadOnlyCollection<Layer> mLayersReadOnly;

        protected SpriteBatch mSpriteBatch;

        GraphicsDevice mGraphicsDevice;

        static Renderer mSelf;

        Camera mCamera;

        Texture2D mSinglePixelTexture;

        static object mLockObject = new object();
        #endregion

        #region Properties

        internal float CurrentZoom
        {
            get;
            private set;
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

        #endregion

        public void Initialize(GraphicsDevice graphicsDevice, SystemManagers managers)
        {
            SamplerState = SamplerState.LinearClamp;
            mCamera = new RenderingLibrary.Camera(managers);
            mLayersReadOnly = new ReadOnlyCollection<Layer>(mLayers);

            mLayers.Add(new Layer());
            mLayers[0].Name = "Main Layer";

            mGraphicsDevice = graphicsDevice;

            mSpriteBatch = new SpriteBatch(mGraphicsDevice);

            mSinglePixelTexture = new Texture2D(mGraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Color[] pixels = new Color[1];
            pixels[0] = Color.White;
            mSinglePixelTexture.SetData<Color>(pixels);

            
        }

        public Layer AddLayer()
        {
            Layer layer = new Layer();
            mLayers.Add(layer);
            return layer;
        }

        public void Draw(SystemManagers managers)
        {
            // So that 2 controls don't render at the same time.
            lock (mLockObject)
            {
                mCamera.UpdateClient();

                BlendState blendState = BlendState.NonPremultiplied;



                foreach (Layer layer in mLayers)
                {
                    BeginSpriteBatch(blendState, layer);

                    layer.SortRenderables();

                    foreach (IRenderable renderable in layer.Renderables)
                    {
                        BlendState renderBlendState = renderable.BlendState;
                        if (renderBlendState == null)
                        {
                            renderBlendState = BlendState.NonPremultiplied;
                        }
                        if (blendState != renderBlendState)
                        {
                            blendState = renderable.BlendState;
                            mSpriteBatch.End();
                            BeginSpriteBatch(blendState, layer);

                        }
                        renderable.Render(mSpriteBatch, managers);
                    }
                    mSpriteBatch.End();
                }

            }
        }

        private void BeginSpriteBatch(BlendState blendState, Layer layer)
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

            mSpriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState, DepthStencilState.Default, RasterizerState.CullNone,
                null, matrix);
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

    }
}
