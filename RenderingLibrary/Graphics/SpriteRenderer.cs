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

        BasicEffect basicEffect;

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

            CreateBasicEffect(graphicsDevice);
        }

        public void EndSpriteBatch()
        {
            mSpriteBatch.PopRenderStates();

        }

        public void BeginSpriteBatch(RenderStateVariables renderStates, Layer layer, BeginType beginType, Camera camera)
        {
            var spriteBatchTransformMatrix =  Renderer.UsingEffect ? Matrix.Identity : GetZoomAndMatrix(layer, camera);

            var samplerState = GetSamplerState(renderStates);

            bool isFullscreen = renderStates.ClipRectangle == null;

            var rasterizerState = isFullscreen ? scissorTestDisabled : scissorTestEnabled;

            var scissorRectangle = new Rectangle();
            if (rasterizerState.ScissorTestEnable)
            {
                scissorRectangle = renderStates.ClipRectangle.Value;

                // Make sure values of with and height are never less than 0:
                if (scissorRectangle.Width < 0)
                {
                    scissorRectangle.Width = 0;
                }
                if (scissorRectangle.Height < 0)
                {
                    scissorRectangle.Height = 0;
                }
            }

            var depthStencilState = DepthStencilState.DepthRead;

            int width = camera.ClientWidth;
            int height = camera.ClientHeight;

            Effect effectiveEffect = null;

            if (Renderer.UseCustomEffectRendering)
            {
                var effectManager = Renderer.CustomEffectManager;

                var projection = Matrix.CreateOrthographic(width, -height, -1, 1);
                var view = GetZoomAndMatrix(layer, camera);

                if (Renderer.ApplyCameraZoomOnWorldTranslation ||
                    layer.LayerCameraSettings?.IsInScreenSpace == true)
                {
                    view *= Matrix.CreateTranslation(-camera.ClientWidth / 2.0f, -camera.ClientHeight / 2.0f, 0);
                }

                effectManager.ParameterViewProj.SetValue(view * projection);

                effectiveEffect = effectManager.Effect;

                ColorOperation colorOperationToUse;

                switch (renderStates.ColorOperation)
                {
                    case ColorOperation.ColorTextureAlpha:
                        colorOperationToUse = ColorOperation.ColorTextureAlpha;
                        break;
                    case ColorOperation.Modulate:
                        colorOperationToUse = ColorOperation.Modulate;
                        break;
                    default:
                        colorOperationToUse = ColorOperation.Modulate;
                        break;
                }

                var effectTechnique = effectManager.GetTechniqueVariantFromColorOperation(colorOperationToUse, useDefaultOrPointFilter: !renderStates.Filtering);

                if (effectiveEffect.CurrentTechnique != effectTechnique)
                    effectiveEffect.CurrentTechnique = effectTechnique;
            }
            else if (Renderer.UseBasicEffectRendering)
            {
                //float zoom = 1;
                //if(Renderer.ApplyCameraZoomOnWorldTranslation)
                //{
                //    zoom = camera.Zoom;

                //    if(layer.LayerCameraSettings?.Zoom != null)
                //    {
                //        zoom /= layer.LayerCameraSettings.Zoom.Value;
                //    }
                //}

                basicEffect.World = Matrix.Identity;

                //effect.Projection = Matrix.CreateOrthographic(100, 100, 0.0001f, 1000);
                basicEffect.Projection = Matrix.CreateOrthographic(
                    width,
                    -height,
                    -1, 1);

                basicEffect.View =
                    GetZoomAndMatrix(layer, camera);

                if(Renderer.ApplyCameraZoomOnWorldTranslation || 
                    layer.LayerCameraSettings?.IsInScreenSpace == true)
                {
                    basicEffect.View *= Matrix.CreateTranslation(-camera.ClientWidth / 2.0f, -camera.ClientHeight / 2.0f, 0);
                }

                effectiveEffect = basicEffect;

                switch (renderStates.ColorOperation)
                {
                    case ColorOperation.ColorTextureAlpha:
                        basicEffect.TextureEnabled = true;
                        basicEffect.VertexColorEnabled = true;

                        // Since MonoGame doesn't use custom shaders, we have to hack this
                        // using Fog. It works...but it's slow and introduces a lot of render breaks. 
                        // At some point in the future we should try to fix this.
                        basicEffect.FogEnabled = true;
                        basicEffect.FogStart = 0;
                        basicEffect.FogEnd = 0;
                        break;
                    case ColorOperation.Modulate:

                        basicEffect.VertexColorEnabled = true;

                        basicEffect.FogEnabled = false;
                        basicEffect.TextureEnabled = true;
                        break;
                }
            }

            // Miguel 2023/08/10
            // Why use Immediate instead of Deferred if it's slower?
            // I'll change it and let's see if it breaks anything.
            if (beginType == BeginType.Begin)
            {
                mSpriteBatch.ReplaceRenderStates(SpriteSortMode.Deferred,
                    renderStates.BlendState,
                    samplerState,
                    depthStencilState,
                    rasterizerState,
                    effectiveEffect,
                    spriteBatchTransformMatrix,
                    scissorRectangle);
            }
            else
            {
                mSpriteBatch.PushRenderStates(SpriteSortMode.Deferred,
                    renderStates.BlendState,
                    samplerState,
                    depthStencilState,
                    rasterizerState,
                    effectiveEffect,
                    spriteBatchTransformMatrix,
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
                    // set this before applying the override
                    CurrentZoom = zoom;
                    //zoom = Renderer.UseBasicEffectRendering ? 1 : zoom;

                    matrix = Matrix.CreateScale(zoom);

                }
                else
                {
                    float zoom = camera.Zoom;
                    if (layer.LayerCameraSettings.Zoom.HasValue)
                    {
                        zoom = layer.LayerCameraSettings.Zoom.Value;
                    }
                    // set this before setting the overriding zoom
                    CurrentZoom = zoom;
                    zoom = Renderer.UsingEffect ? 1 : zoom;
                    matrix = Camera.GetTransformationMatrix(
                        camera.X, 
                        camera.Y, 
                        zoom, 
                        camera.ClientWidth, 
                        camera.ClientHeight, 
                        forRendering:true);
                }
            }
            else
            {
                matrix = camera.GetTransformationMatrix(forRendering:true);
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

        private void CreateBasicEffect(GraphicsDevice graphicsDevice)
        {
            basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.TextureEnabled = true;
            basicEffect.LightingEnabled = false;
            basicEffect.FogEnabled = false;
            basicEffect.VertexColorEnabled = true;
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

        internal void Draw(Texture2D textureToUse, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, int layerDepth, object objectRequestingChange)
        {
            mSpriteBatch.Draw(textureToUse, destinationRectangle, sourceRectangle, color, rotation, origin, effects, layerDepth, objectRequestingChange);
        }

        internal void Draw(Texture2D textureToUse, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float depth, object objectRequestingChange)
        {
            if (!Renderer.UseCustomEffectRendering && Renderer.UseBasicEffectRendering && basicEffect.FogEnabled)
            {
                basicEffect.FogColor = new Vector3(color.R/255, color.G/255, color.B/255f);
            }

            // Adjust offsets according to zoom
            float x = ((int)(position.X * CurrentZoom) + Camera.PixelPerfectOffsetX)/CurrentZoom;
            float y = ((int)(position.Y * CurrentZoom) + Camera.PixelPerfectOffsetY)/CurrentZoom;

            position.X = x;
            position.Y = y;

            mSpriteBatch.Draw(textureToUse, position, sourceRectangle, color, rotation, origin, scale, effects, depth, objectRequestingChange);
        }

        internal void ClearPerformanceRecordingVariables()
        {
            mSpriteBatch.ClearPerformanceRecordingVariables();
        }
    }
}
