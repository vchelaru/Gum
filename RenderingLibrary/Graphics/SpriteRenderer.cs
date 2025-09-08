using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;

namespace RenderingLibrary.Graphics;

public enum DimensionSnapping
{
    /// <summary>
    /// Snaps each side (top, bottom, left, and right) independently.
    /// For example, the bottom
    /// will get rounded to the nearest screen pixel regardless of
    /// the height of an object
    /// </summary>
    SideSnapping,
    /// <summary>
    /// Snaps 
    /// </summary>
    DimensionSnapping
}

public class SpriteRenderer
{
    #region Fields

    private SpriteBatchStack mSpriteBatch;

    RasterizerState scissorTestEnabled;
    RasterizerState scissorTestDisabled;

    BasicEffect basicEffect;

    // This is used by GumBatch to force a matrix for all calls in its own Begin/End pair
    public Microsoft.Xna.Framework.Matrix? ForcedMatrix { get; set; }

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

    public void BeginSpriteBatch(RenderStateVariables renderStates, Layer layer, BeginType beginType, Camera camera, object objectStartingSpriteBatch)
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

        // February 8, 2025
        // Gum used to DepthRead
        // on draw, causing Gum to 
        // not render if the depth buffer
        // wasn't cleared. This is probably
        // not what the user wants in most cases,
        // but if they do we should enable this as
        // a setting on Renderer. For now, switching
        // it.
        //var depthStencilState = DepthStencilState.DepthRead;
        var depthStencilState = DepthStencilState.None;

        int width = camera.ClientWidth;
        int height = camera.ClientHeight;

        Effect effectiveEffect = null;

        if (Renderer.UseCustomEffectRendering)
        {
            var effectManager = Renderer.CustomEffectManager;

            var projection = Microsoft.Xna.Framework.Matrix.CreateOrthographic(width, -height, -1, 1);
            var view = GetZoomAndMatrix(layer, camera);

            if (camera.CameraCenterOnScreen == CameraCenterOnScreen.TopLeft || 
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

            basicEffect.World = ForcedMatrix ?? Microsoft.Xna.Framework.Matrix.Identity;

            //effect.Projection = Matrix.CreateOrthographic(100, 100, 0.0001f, 1000);
            basicEffect.Projection = Microsoft.Xna.Framework.Matrix.CreateOrthographic(
                width,
                -height,
                -1, 1);

            basicEffect.View =  GetZoomAndMatrix(layer, camera);

            var shouldOffset = camera.CameraCenterOnScreen == CameraCenterOnScreen.TopLeft ||
                layer.LayerCameraSettings?.IsInScreenSpace == true;
            if(shouldOffset)
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
        // Victor April 27, 2024
        // This seems to work fine in all cases
        if (beginType == BeginType.Begin)
        {
            mSpriteBatch.ReplaceRenderStates(SpriteSortMode.Deferred,
                renderStates.BlendState,
                samplerState,
                depthStencilState,
                rasterizerState,
                effectiveEffect,
                ForcedMatrix ?? spriteBatchTransformMatrix,
                scissorRectangle,
                objectStartingSpriteBatch
                );

        }
        else
        {
            mSpriteBatch.PushRenderStates(SpriteSortMode.Deferred,
                renderStates.BlendState,
                samplerState,
                depthStencilState,
                rasterizerState,
                effectiveEffect,
                ForcedMatrix ?? spriteBatchTransformMatrix,
                scissorRectangle,
                objectStartingSpriteBatch);
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

    internal void Draw(Texture2D textureToUse, Vector2 position, Rectangle? sourceRectangle, Color color, 
        float rotation, Vector2 origin, 
        Vector2 scale, SpriteEffects effects, 
        float depth, 
        object objectRequestingChange, 
        Renderer renderer = null, 
        bool offsetPixel = true,
        DimensionSnapping dimensionSnapping = DimensionSnapping.SideSnapping)
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

        if (!isIntegerScale && scale.X == scale.Y)
        {
            var zoomScale = CurrentZoom * scale.X;
            isIntegerScale = System.Math.Abs(MathFunctions.RoundToInt(zoomScale) - zoomScale) < .01f;
        }

        if (radiansFromPerfectRotation < errorToTolerate && isIntegerScale && offsetPixel)
        {
            var effectivePixelOffsetX = Camera.PixelPerfectOffsetX;
            var effectivePixelOffsetY = Camera.PixelPerfectOffsetY;

            // The rendering of Sprites is done to attempt to minimize distortions, gaps, and overlaps.
            // There are two primary cases that we care about:
            // 1. Prevent gaps/overlaps, mainly when dealing with stacked/adjacent objects. This is primarly
            //    the case for containers that use TopToBottomStack or LeftToRightStack, as well as the individual
            //    sprites in NineSlice.
            // 2. Prevent distortions. This is primarily a concern when rendering fonts.
            // 
            // Until July 1, 2025 Gum prioritized (1), at the cost of (2). However, this was usually an acceptable
            // tradeoff because most of the time Gum is rendered at integer scale. But when it isn't, then (2) is a
            // bigger problem for fonts (such as on Deadvivors https://github.com/vchelaru/Gum/issues/848
            // 
            // To solve (1), the logic for rendering is as follows: 
            // 1. Find each side of the object (top, left, right, bottom)
            // 2. Use rounding to "snap" each side to a screen pixel (considering zoom)
            // 3. After snapping, use the new sizes (right-left, bottom-top) to calculate a new (width, height)
            // 4. Use the new (width,height) to determine the scale values to send to the SpriteBatch
            //
            // This logic can result in some bad sizes when dealing with fonts. As a simplified example, consider a
            // sprite (which could be a letter) that has Y = -0.4999, Height = 1.9999
            // When using side snapping (as outlined above), this would get snapped to:
            // Top = 0, bottom = 1
            // This happens because -.4999 + 1.9999 = 1.4999, which rounds to 1
            // However, when rendering fonts, this really should round to a height of 2.
            // Therefore, the solution is that we need to continue to use the same side-snapping
            // logic when dealing with everything EXCEPT fonts. When dealing with fonts, do size snapping
            // instead.
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

                // See above where float x and y are rounded for information on why we do this:
                float worldRightRounded;
                float worldBottomRounded;
                float roundedWidth;
                float roundedHeight;

                if (dimensionSnapping == DimensionSnapping.SideSnapping)
                {
                    worldRightRounded = MathFunctions.RoundToInt(worldRight * CurrentZoom) / CurrentZoom + effectivePixelOffsetX / CurrentZoom;
                    worldBottomRounded = MathFunctions.RoundToInt(worldBottom * CurrentZoom) / CurrentZoom + effectivePixelOffsetY / CurrentZoom;
                    
                    roundedWidth = worldRightRounded - x;
                    roundedHeight = worldBottomRounded - y;
                }
                else // size snapping
                {
                    roundedWidth = MathFunctions.RoundToInt(worldWidth * CurrentZoom) / CurrentZoom;
                    roundedHeight = MathFunctions.RoundToInt(worldHeight * CurrentZoom) / CurrentZoom;
                }

                if(flipVerticalHorizontal)
                {
                    scale.X = roundedHeight / sourceHeight;
                    scale.Y = roundedWidth / sourceWidth;
                }
                else
                {
                    scale.X = roundedWidth / sourceWidth;
                    scale.Y = roundedHeight / sourceHeight;

                    }
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
