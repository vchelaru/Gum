using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Graphics
{
    public class SpriteRenderer
    {
        #region Fields

        private SpriteBatchStack mSpriteBatch;

        RasterizerState scissorTestEnabled;
        RasterizerState scissorTestDisabled;

        public IEnumerable<BeginParameters> LastFrameDrawStates
        {
            get
            {
                return mSpriteBatch.LastFrameDrawStates;
            }
        }

        #endregion

        #region Properties

        internal float CurrentZoom
        {
            get;
            private set;
        }

        #endregion

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            mSpriteBatch = new SpriteBatchStack(graphicsDevice);

            CreateRasterizerStates();
        }

        public void EndSpriteBatch()
        {
            mSpriteBatch.PopRenderStates();

        }

        public void BeginSpriteBatch(RenderStateVariables renderStates, Layer layer, BeginType beginType, Camera camera)
        {

            Matrix matrix = GetZoomAndMatrix(layer, camera);

            SamplerState samplerState = GetSamplerState(renderStates);


            bool isFullscreen = renderStates.ClipRectangle == null;

            RasterizerState rasterizerState;
            if (isFullscreen)
            {
                rasterizerState = scissorTestDisabled;
            }
            else
            {
                rasterizerState = scissorTestEnabled;
            }


            Rectangle scissorRectangle = new Rectangle();
            if (rasterizerState.ScissorTestEnable)
            {
                scissorRectangle = renderStates.ClipRectangle.Value;

                // make sure values of with and height are never less than 0:
                if(scissorRectangle.Width <0)
                {
                    scissorRectangle.Width = 0;
                }
                if(scissorRectangle.Height < 0)
                {
                    scissorRectangle.Height = 0;
                }
            }


            DepthStencilState depthStencilState = DepthStencilState.DepthRead;

            if (beginType == BeginType.Begin)
            {
                mSpriteBatch.ReplaceRenderStates(SpriteSortMode.Immediate, renderStates.BlendState,
                    samplerState,
                    depthStencilState,
                    rasterizerState,
                    null, matrix,
                    scissorRectangle);
            }
            else
            {
                mSpriteBatch.PushRenderStates(SpriteSortMode.Immediate, renderStates.BlendState,
                    samplerState,
                    depthStencilState,
                    rasterizerState,
                    null, matrix,
                    scissorRectangle);
            }
        }


        private Matrix GetZoomAndMatrix(Layer layer, Camera camera)
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
                    float zoom = camera.Zoom;
                    if (layer.LayerCameraSettings.Zoom.HasValue)
                    {
                        zoom = layer.LayerCameraSettings.Zoom.Value;
                    }
                    matrix = Camera.GetTransformationMatirx(camera.X, camera.Y, zoom, camera.ClientWidth, camera.ClientHeight);
                    CurrentZoom = zoom;
                }
            }
            else
            {
                matrix = camera.GetTransformationMatrix();
                CurrentZoom = camera.Zoom;
            }
            return matrix;
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


        private void CreateRasterizerStates()
        {
            scissorTestEnabled = new RasterizerState();
            scissorTestDisabled = new RasterizerState();

            scissorTestEnabled.CullMode = CullMode.None;

            scissorTestEnabled.ScissorTestEnable = true;
            scissorTestDisabled.ScissorTestEnable = false;
        }

        public void Begin()
        {
            mSpriteBatch.Begin();
        }

        internal void End()
        {
            mSpriteBatch.End();
        }

        internal void Draw(Texture2D texture2D, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color, object objectRequestingChange)
        {
            mSpriteBatch.Draw(texture2D, destinationRectangle, sourceRectangle, color, objectRequestingChange);
        }

        internal void DrawString(SpriteFont font, string line, Vector2 offset, Color color, object objectRequestingChange)
        {
            mSpriteBatch.DrawString(font, line, offset, color, objectRequestingChange);
        }

        internal void Draw(Texture2D textureToUse, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 vector2, SpriteEffects effects, int layerDepth, object objectRequestingChange)
        {
            mSpriteBatch.Draw(textureToUse, destinationRectangle, sourceRectangle, color, rotation, vector2, effects, layerDepth, objectRequestingChange);
        }

        internal void Draw(Texture2D textureToUse, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 vector22, Vector2 scale, SpriteEffects effects, float depth, object objectRequestingChange)
        {
            mSpriteBatch.Draw(textureToUse, position, sourceRectangle, color, rotation, vector22, scale, effects, depth, objectRequestingChange);
        }

        internal void ClearPerformanceRecordingVariables()
        {
            mSpriteBatch.ClearPerformanceRecordingVariables();
        }
    }
}
