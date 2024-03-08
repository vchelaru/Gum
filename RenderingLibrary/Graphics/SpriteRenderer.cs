using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;

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
            var spriteBatchTransformMatrix =  Renderer.UsingEffect 
                ? GetZoomMatrixFromLayerCameraSettings()
                : GetZoomAndMatrix(layer, camera);

            Microsoft.Xna.Framework.Matrix GetZoomMatrixFromLayerCameraSettings()
            {
                if(layer.LayerCameraSettings?.Zoom != null)
                {
                    return Microsoft.Xna.Framework.Matrix.CreateScale(layer.LayerCameraSettings.Zoom.Value);
                }
                else
                {
                    return Microsoft.Xna.Framework.Matrix.Identity;
                }
            }

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

                var projection = Microsoft.Xna.Framework.Matrix.CreateOrthographic(width, -height, -1, 1);
                var view = GetZoomAndMatrix(layer, camera);

                if (Renderer.ApplyCameraZoomOnWorldTranslation ||
                    layer.LayerCameraSettings?.IsInScreenSpace == true)
                {
                    view *= Microsoft.Xna.Framework.Matrix.CreateTranslation(-camera.ClientWidth / 2.0f, -camera.ClientHeight / 2.0f, 0);
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

                var effectTechnique = effectManager.GetVertexColorTechniqueFromColorOperation(colorOperationToUse, useDefaultOrPointFilter: !renderStates.Filtering);

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

                basicEffect.World = Microsoft.Xna.Framework.Matrix.Identity;

                //effect.Projection = Matrix.CreateOrthographic(100, 100, 0.0001f, 1000);
                basicEffect.Projection = Microsoft.Xna.Framework.Matrix.CreateOrthographic(
                    width,
                    -height,
                    -1, 1);

                basicEffect.View =
                    GetZoomAndMatrix(layer, camera);

                if(Renderer.ApplyCameraZoomOnWorldTranslation || 
                    layer.LayerCameraSettings?.IsInScreenSpace == true)
                {
                    basicEffect.View *= Microsoft.Xna.Framework.Matrix.CreateTranslation(-camera.ClientWidth / 2.0f, -camera.ClientHeight / 2.0f, 0);
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


        private Microsoft.Xna.Framework.Matrix GetZoomAndMatrix(Layer layer, Camera camera)
        {
            Microsoft.Xna.Framework.Matrix matrix;

            if (layer.LayerCameraSettings != null)
            {
                var layerCameraSettings = layer.LayerCameraSettings;
                float zoom = 1;

                if(layerCameraSettings.IsInScreenSpace)
                {
                    zoom = layerCameraSettings.Zoom ?? 1;
                }
                else
                {
                    zoom = layerCameraSettings.Zoom ?? camera.Zoom;
                }
                CurrentZoom = zoom;

                float x;
                float y;

                if (layerCameraSettings.IsInScreenSpace)
                {
                    x = layerCameraSettings.Position?.X ?? 0;
                    y = layerCameraSettings.Position?.Y ?? 0;
                }
                else
                {
                    x = camera.X;
                    y = camera.Y;

                    if (layerCameraSettings.Position != null)
                    {
                        x += layerCameraSettings.Position.Value.X;
                        y += layerCameraSettings.Position.Value.Y;
                    }
                }

                // March 5, 2024
                // Why do we use a
                // zoom of 1 if Renderer.UsingEffect
                // is true? This makes rendering zoomed-in
                // layers not work in MonoGame.
                //zoom = Renderer.UsingEffect ? 1 : zoom;

                matrix = Camera.GetTransformationMatrix(
                    x, 
                    y, 
                    zoom, 
                    camera.ClientWidth, 
                    camera.ClientHeight, 
                    forRendering:true).ToXNA();
            }
            else
            {
                matrix = camera.GetTransformationMatrix(forRendering:true).ToXNA();
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

        internal void Draw(Texture2D textureToUse, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float depth, object objectRequestingChange, Renderer renderer = null, bool offsetPixel = true)
        {
#if DEBUG
            if(float.IsPositiveInfinity(scale.X))
            {
                throw new ArgumentException("scale.X cannot be positive infinity");
            }
            if (float.IsPositiveInfinity(scale.Y))
            {
                throw new ArgumentException("scale.Y cannot be positive infinity");
            }
#endif

            if (!Renderer.UseCustomEffectRendering && Renderer.UseBasicEffectRendering && basicEffect.FogEnabled)
            {
                basicEffect.FogColor = new Microsoft.Xna.Framework.Vector3(color.R/255, color.G/255, color.B/255f);
            }

            var quarterRotations = System.Math.Abs(rotation / MathHelper.PiOver2);
            var radiansFromPerfectRotation = System.Math.Abs(quarterRotations - MathFunctions.RoundToInt(quarterRotations));


            // 1/90 would be 1 degree. Let's go 1/10th of a degree
            const float errorToTolerate = .1f/90f;

            // don't attempt to do adjustments unless the scale is integer scale
            var isIntegerScale = System.Math.Abs(MathFunctions.RoundToInt(CurrentZoom) - CurrentZoom) < .01f;

            if(radiansFromPerfectRotation < errorToTolerate && isIntegerScale && offsetPixel)
            {

                // Adjust offsets according to zoom
                //float x = ((int)(position.X * CurrentZoom) + Camera.PixelPerfectOffsetX)/CurrentZoom;
                //float y = ((int)(position.Y * CurrentZoom) + Camera.PixelPerfectOffsetY)/CurrentZoom;

                float cameraOffsetX = 0;
                float cameraOffsetY = 0;

                var effectivePixelOffsetX = Camera.PixelPerfectOffsetX;
                var effectivePixelOffsetY = Camera.PixelPerfectOffsetY;

                if (renderer != null)
                {
                    cameraOffsetX = renderer.Camera.X * CurrentZoom;
                    cameraOffsetY = renderer.Camera.Y * CurrentZoom;
                    // todo - continue working here. This doesn't seem to solve the problem:
                    //effectivePixelOffsetX -= ( (int)cameraOffsetX - cameraOffsetX);
                    //effectivePixelOffsetY -= ( (int)cameraOffsetY - cameraOffsetY);
                }

                float x = MathFunctions.RoundToInt(position.X * CurrentZoom) / CurrentZoom + effectivePixelOffsetX / CurrentZoom;
                float y = MathFunctions.RoundToInt(position.Y * CurrentZoom) / CurrentZoom + effectivePixelOffsetY / CurrentZoom;

                // need to also adjust scale:
                if(textureToUse != null)
                {
                    int sourceWidth = sourceRectangle?.Width ?? textureToUse.Width;
                    int sourceHeight = sourceRectangle?.Height ?? textureToUse.Height;

                    var worldWidth = sourceWidth * scale.X;
                    var worldHeight = sourceHeight * scale.Y;

                    var worldRight = position.X + worldWidth;
                    var worldBottom = position.Y + worldHeight;

                    var flipVerticalHorizontal =
                        quarterRotations == 1 || quarterRotations == 3;
                    if (flipVerticalHorizontal)
                    {
                        // invert X/Y width/Heights:
                        worldRight = position.X + worldHeight;
                        worldBottom = position.Y + worldWidth;
                    }

                    float worldRightRounded = MathFunctions.RoundToInt(worldRight * CurrentZoom) / CurrentZoom + effectivePixelOffsetX / CurrentZoom;
                    float worldBottomRounded = MathFunctions.RoundToInt(worldBottom * CurrentZoom) / CurrentZoom + effectivePixelOffsetY / CurrentZoom;

                    var roundedWidth = worldRightRounded - x;
                    var roundedHeight = worldBottomRounded - y;

                    if(flipVerticalHorizontal)
                    {
                        scale.X = roundedHeight / sourceWidth;
                        scale.Y = roundedWidth / sourceHeight;
                    }
                    else
                    {
                        scale.X = roundedWidth / sourceWidth;
                        scale.Y = roundedHeight / sourceHeight;

                    }
                }

                position.X = x;
                position.Y = y;
            }

            mSpriteBatch.Draw(textureToUse, position, sourceRectangle, color, rotation, origin, scale, effects, depth, objectRequestingChange);
        }

        internal void ClearPerformanceRecordingVariables()
        {
            mSpriteBatch.ClearPerformanceRecordingVariables();
        }
    }
}
