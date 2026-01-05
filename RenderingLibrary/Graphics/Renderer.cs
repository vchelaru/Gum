#if MONOGAME || XNA || KNI || FNA
#define XNALIKE
#endif
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Content;
using BlendState = Gum.BlendState;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Gum;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using System.Linq;
using RenderingLibrary.Math.Geometry;
using Gum.Wireframe;
using System.Diagnostics;

namespace RenderingLibrary.Graphics;

#region RenderStateVariables Class

public class RenderStateVariables
{
    public BlendState BlendState;
    public ColorOperation ColorOperation;
    public bool Filtering;
    public bool Wrap;

    public Rectangle? ClipRectangle;
}

#endregion

public class Renderer : IRenderer
{
    /// <summary>
    /// Whether renderable objects should call Render
    /// on contained children. This is true by default, 
    /// results in a hierarchical rendering order.
    /// </summary>
    public static bool RenderUsingHierarchy = true;

    #region Fields


    List<Layer> _layers = new List<Layer>();
    ReadOnlyCollection<Layer> _layersReadOnly;

#if XNALIKE
    SpriteRenderer spriteRenderer = new SpriteRenderer();
#endif


    RenderStateVariables mRenderStateVariables = new RenderStateVariables();

    GraphicsDevice mGraphicsDevice;

    private RenderTargetService renderTargetService;
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
        get { return _layers[0]; }
    }

    internal List<Layer> LayersWritable
    {
        get
        {
            return _layers;
        }
    }

    public ReadOnlyCollection<Layer> Layers => _layersReadOnly;


    /// <summary>
    /// The texture used to render solid objects. If SinglePixelSourceRectangle is null, the entire texture is used. Otherwise
    /// the portion of SinglePixelTexture is applied.
    /// </summary>
    public Texture2D SinglePixelTexture
    {
        get
        {
#if FULL_DIAGNOSTICS && !TEST
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
    /// Returns the SinglePixelTexture if it exists, or null if not. This tolerates nulls, unlike the property.
    /// </summary>
    /// <returns>The SinglePixelTexture if it is not null</returns>
    public Texture2D? TryGetSinglePixelTexture() => mSinglePixelTexture;

    /// <summary>
    /// The rectangle to use when rendering single-pixel texture objects, such as ColoredRectangles.
    /// By default this is null, indicating the entire texture is used.
    /// </summary>
    public Rectangle? SinglePixelSourceRectangle = null;

    public Texture2D DottedLineTexture
    {
        get
        {
#if FULL_DIAGNOSTICS && !TEST
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

    internal Texture2D InternalShapesTexture { get; set; }

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
            if(SystemManagers.Default == null)
            {
                throw new InvalidOperationException(
                    "The SystemManagers.Default is null. You must either specify the default SystemManagers, or use a custom SystemsManager if your app has multiple SystemManagers.");
            }
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

    public SpriteRenderer SpriteRenderer
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

    public bool IsUsingPremultipliedAlpha
    {
        get; set;
    }
    = false;

    /// <summary>
    /// Use the custom effect for rendering. This setting takes priority if 
    /// both UseCustomEffectRendering and UseBasicEffectRendering are enabled.
    /// </summary>
    public static bool UseCustomEffectRendering 
    {
        get => RendererSettings.UseCustomEffectRendering;
        set => RendererSettings.UseCustomEffectRendering = value;
    }
    public static bool UseBasicEffectRendering 
    { 
        get => RendererSettings.UseBasicEffectRendering;
        set => RendererSettings.UseBasicEffectRendering = value;
    }
    public static bool UsingEffect => RendererSettings.UsingEffect;

    public static CustomEffectManager CustomEffectManager { get; } = new CustomEffectManager();

    /// <summary>
    /// When this is enabled texture colors will be translated to linear space before 
    /// any other shader operations are performed. This is useful for games with 
    /// lighting and other special shader effects. If the colors are left in gamma 
    /// space the shader calculations will crush the colors and not look like natural 
    /// lighting. Delinearization must be done by the developer in the last render 
    /// step when rendering to the screen. This technique is called gamma correction.
    /// Requires using the custom effect. Disabled by default.
    /// </summary>
    public static bool LinearizeTextures { get; set; }

    // Vic says March 29 2020
    // For some reason the rendering
    // in the tool works differently than
    // in-game. Not sure if this is a DesktopGL
    // vs XNA thing, but I traced it down to the zoom thing.
    // I'm going to add a bool here to control it.
    public static bool ApplyCameraZoomOnWorldTranslation { get; set; } = false;

    public static TextureFilter TextureFilter { get; set; } = TextureFilter.Point;

#endregion

    public Renderer()
    {

        _layers = new List<Layer>();
        _layersReadOnly = new ReadOnlyCollection<Layer>(_layers);
        mCamera = new RenderingLibrary.Camera();

    }

    public void Initialize(GraphicsDevice graphicsDevice, SystemManagers managers)
    {
        renderTargetService = new RenderTargetService();

        if (graphicsDevice != null)
        {
            mCamera.ClientWidth = graphicsDevice.Viewport.Width;
            mCamera.ClientHeight = graphicsDevice.Viewport.Height;
            mCamera.ClientLeft = graphicsDevice.Viewport.X;
            mCamera.ClientTop = graphicsDevice.Viewport.Y;
        }

                // for open gl (desktop gl) this should be 0
                // for DirectX it should be 0.5 I believe....
#if DIRECTX_RENDERING
        Camera.PixelPerfectOffsetX = .5f;
        Camera.PixelPerfectOffsetY = .5f;
#else
        Camera.PixelPerfectOffsetX = .0f;
        Camera.PixelPerfectOffsetY = .0f;
#endif


        _layers.Add(new Layer());
        _layers[0].Name = "Main Layer";

        mGraphicsDevice = graphicsDevice;

        spriteRenderer.Initialize(graphicsDevice);
        CustomEffectManager.Initialize(graphicsDevice);

        mSinglePixelTexture = new Texture2D(mGraphicsDevice, 1, 1, false, SurfaceFormat.Color);
        Microsoft.Xna.Framework.Color[] pixels = new Microsoft.Xna.Framework.Color[1];
        pixels[0] = Microsoft.Xna.Framework.Color.White;
        mSinglePixelTexture.SetData<Microsoft.Xna.Framework.Color>(pixels);
        mSinglePixelTexture.Name = "Rendering Library Single Pixel Texture";

        mDottedLineTexture = new Texture2D(mGraphicsDevice, 2, 1, false, SurfaceFormat.Color);
        mDottedLineTexture.Name = "Renderer Dotted Line Texture";
        pixels = new Microsoft.Xna.Framework.Color[2];
        pixels[0] = Microsoft.Xna.Framework.Color.White;
        pixels[1] = Microsoft.Xna.Framework.Color.Transparent;
        mDottedLineTexture.SetData<Microsoft.Xna.Framework.Color>(pixels);

        if (GraphicsDevice != null)
        {
            mCamera.ClientWidth = GraphicsDevice.Viewport.Width;
            mCamera.ClientHeight = GraphicsDevice.Viewport.Height;
            mCamera.ClientLeft = GraphicsDevice.Viewport.X;
            mCamera.ClientTop = GraphicsDevice.Viewport.Y;
        }
    }

    #region Add/Remove Layers

    public Layer AddLayer()
    {
        Layer layer = new Layer();
        _layers.Add(layer);
        return layer;
    }

    public void AddLayer(Layer layer) => _layers.Add(layer);

    public void InsertLayer(int index, Layer layer) => _layers.Insert(index, layer);

    public void RemoveLayer(Layer layer) => _layers.Remove(layer);


    //public void AddLayer(SortableLayer sortableLayer, Layer masterLayer)
    //{
    //    if (masterLayer == null)
    //    {
    //        masterLayer = LayersWritable[0];
    //    }

    //    masterLayer.Add(sortableLayer);
    //}

    #endregion

    public void Draw(SystemManagers managers)
    {
        ClearPerformanceRecordingVariables();

        if (managers == null)
        {
            managers = SystemManagers.Default;
        }

        Draw(managers, _layers);

        ForceEnd();
    }

    public void Draw(SystemManagers managers, Layer layer)
    {
        // So that 2 controls don't render at the same time.
        lock (LockObject)
        {
            if (GraphicsDevice != null)
            {
                mCamera.ClientWidth = GraphicsDevice.Viewport.Width;
                mCamera.ClientHeight = GraphicsDevice.Viewport.Height;
                mCamera.ClientLeft = GraphicsDevice.Viewport.X;
                mCamera.ClientTop = GraphicsDevice.Viewport.Y;
            }

            var oldSampler = GraphicsDevice.SamplerStates[0];

            mRenderStateVariables.BlendState = Renderer.NormalBlendState;
            mRenderStateVariables.Wrap = false;

            if (layer.IsLinearFilteringEnabled != null)
            {
                mRenderStateVariables.Filtering = layer.IsLinearFilteringEnabled.Value;
            }
            else
            {
                mRenderStateVariables.Filtering = TextureFilter == TextureFilter.Linear;
            }

            PreRender(layer.Renderables);

            PreRenderWithSourceRenderTargets(layer.Renderables);

            if (layer.IsLinearFilteringEnabled != null)
            {
                mRenderStateVariables.Filtering = layer.IsLinearFilteringEnabled.Value;
            }
            else
            {
                mRenderStateVariables.Filtering = TextureFilter == TextureFilter.Linear;
            }

            RenderLayer(managers, layer, prerender:false);

            if (oldSampler != null)
            {
                GraphicsDevice.SamplerStates[0] = oldSampler;
            }
        }
    }

    public void Draw(SystemManagers managers, List<Layer> layers)
    {
        // So that 2 controls don't render at the same time.
        lock (LockObject)
        {
            if (GraphicsDevice != null)
            {
                mCamera.ClientWidth = GraphicsDevice.Viewport.Width;
                mCamera.ClientHeight = GraphicsDevice.Viewport.Height;
                mCamera.ClientLeft = GraphicsDevice.Viewport.X;
                mCamera.ClientTop = GraphicsDevice.Viewport.Y;
            }

            renderTargetService.ClearUnusedRenderTargetsLastFrame();


            mRenderStateVariables.BlendState = Renderer.NormalBlendState;
            mRenderStateVariables.Wrap = false;

            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if(layer.IsLinearFilteringEnabled != null)
                {
                    mRenderStateVariables.Filtering = layer.IsLinearFilteringEnabled.Value;
                }
                else
                {
                    mRenderStateVariables.Filtering = TextureFilter == TextureFilter.Linear;
                }
                PreRender(layer.Renderables);

                PreRenderWithSourceRenderTargets(layer.Renderables);
            }

            for (int i = 0; i < layers.Count; i++)
            {
                Layer layer = layers[i];
                if (layer.IsLinearFilteringEnabled != null)
                {
                    mRenderStateVariables.Filtering = layer.IsLinearFilteringEnabled.Value;
                }
                else
                {
                    mRenderStateVariables.Filtering = TextureFilter == TextureFilter.Linear;
                }
                RenderLayer(managers, layer, prerender:false);
            }
        }
    }

    void IRenderer.RenderLayer(RenderingLibrary.ISystemManagers managers, RenderingLibrary.Graphics.Layer layer, bool prerender)
    {
        RenderLayer(managers as SystemManagers, layer, prerender);
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

            PreRenderWithSourceRenderTargets(layer.Renderables);
        }

        SpriteBatchStack.PerformStartOfLayerRenderingLogic();

        spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Push, mCamera, null);

        layer.SortRenderables();

        Render(layer.Renderables, managers, layer, prerender);

        lastBatchOwner?.EndBatch(managers);
        lastBatchOwner = null;
        currentBatchKey = string.Empty;

#if !NET8_0_OR_GREATER
        spriteRenderer.EndSpriteBatch();
#endif
    }


    // Immediate mode calls:
    public void Begin(Microsoft.Xna.Framework.Matrix? spriteBatchMatrix = null)
    {
        SpriteBatchStack.PerformStartOfLayerRenderingLogic();
        spriteRenderer.ForcedMatrix = spriteBatchMatrix;
        spriteRenderer.BeginSpriteBatch(mRenderStateVariables, _layers[0], BeginType.Push, mCamera, null);
    }


    public void Draw(IRenderableIpso renderable)
    {
        Draw(SystemManagers.Default, _layers[0], renderable, forceRenderHierarchy:false, isPreRender:false);
    }

    public void End()
    {
        spriteRenderer.ForcedMatrix = null;

        spriteRenderer.EndSpriteBatch();
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
        if(count== 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var renderable = renderables[i];
            if(renderable.Visible || 
                // If it's a render target, then we want to render it fully:
                renderable.IsRenderTarget)
            {

                renderable.PreRender();

                // Some Gum objects, like GraphicalUiElements, may not have children if the object hasn't
                // yet been assigned a visual. Just skip over it...
                if((renderable.Visible || renderable.IsRenderTarget) && renderable.Children != null)
                {
                    PreRender(renderable.Children);
                }
                if(renderable.IsRenderTarget)
                {
                    RenderToRenderTarget(renderable, SystemManagers.Default);
                }
            }
        }
    }

    private void PreRenderWithSourceRenderTargets(IList<IRenderableIpso> renderables)
    {
        var count = renderables.Count;
        if (count == 0)
        {
            return;
        }


        for (int i = 0; i < count; i++)
        {
            var renderable = renderables[i];
            if (renderable.Visible && renderable is IRenderTargetTextureReferencer textureReferencer &&
                textureReferencer.RenderTargetTextureSource != null)
            {
                textureReferencer.Texture = renderTargetService.GetExistingRenderTarget(
                    textureReferencer.RenderTargetTextureSource);
            }

            if (renderable.Visible && renderable.Children != null)
            {
                PreRenderWithSourceRenderTargets(renderable.Children);
            }
        }
    }

    GumBatch gumBatch;

    bool hasSaved = false;

    private void RenderToRenderTarget(IRenderableIpso renderable, SystemManagers systemManagers)
    {


        Texture oldRenderTarget = null;

        // RenderTargetCount isn't supported in raw XNA or KNI
        //if (GraphicsDevice.RenderTargetCount > 0)
        //{
            oldRenderTarget = GraphicsDevice.GetRenderTargets().FirstOrDefault().RenderTarget;
        //}
        var oldCameraWidth = Camera.ClientWidth;
        var oldCameraHeight = Camera.ClientHeight;
        var oldCameraClientLeft = Camera.ClientLeft;
        var oldCameraClientTop = Camera.ClientTop;

        var oldCameraX = Camera.X;
        var oldCameraY = Camera.Y;
        var oldViewport = GraphicsDevice.Viewport;

        var renderTarget = renderTargetService.GetRenderTargetFor(GraphicsDevice, renderable, Camera);

        if(renderTarget != null)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);

            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);

            var oldX = renderable.GetAbsoluteLeft();
            var oldY = renderable.GetAbsoluteTop();

            var cameraLeft = Camera.AbsoluteLeft;
            var cameraTop = Camera.AbsoluteTop;
            

            Camera.ClientWidth = (int)renderTarget.Width;
            Camera.ClientHeight = (int)renderTarget.Height;
            Camera.ClientLeft = 0;
            Camera.ClientTop = 0;

            var left = System.Math.Max(cameraLeft, oldX);
            var top = System.Math.Max(cameraTop, oldY);

            Camera.X = left;
            Camera.Y = top;
            if(Camera.CameraCenterOnScreen == CameraCenterOnScreen.Center)
            {
                Camera.X += (renderTarget.Width / 2.0f)/Camera.Zoom;
                Camera.Y += (renderTarget.Height / 2.0f) / Camera.Zoom;
            }

            // Internally the sprite rendering system snaps to the pixel, so we need to do the same thing:

            var effectivePixelOffsetX = Camera.PixelPerfectOffsetX;
            var effectivePixelOffsetY = Camera.PixelPerfectOffsetY;

            Camera.X = Math.MathFunctions.RoundToInt(Camera.X * CurrentZoom) / CurrentZoom + effectivePixelOffsetX / CurrentZoom;
            Camera.Y = Math.MathFunctions.RoundToInt(Camera.Y * CurrentZoom) / CurrentZoom + effectivePixelOffsetY / CurrentZoom;


            gumBatch = gumBatch ?? new GumBatch();


            // todo  - rotations don't currently work:
            //var rotationRadians = MathHelper.ToRadians(renderable.Rotation);
            //var matrix = Matrix.CreateRotationZ(rotationRadians);
            //gumBatch.Begin(matrix);
            gumBatch.Begin();


            //gumBatch.Draw(renderable);
            //systemManagers.Renderer.Draw(renderable);
            Draw(systemManagers, _layers[0], renderable, forceRenderHierarchy:true, isPreRender:true);

            gumBatch.End();
            GraphicsDevice.SetRenderTarget(oldRenderTarget as RenderTarget2D);

#if DEBUG
            if(!hasSaved)
            {
                hasSaved = true;
                // Uncomment this to test saving...
                //if (!System.IO.File.Exists("Output.png"))
                //{
                //    using var stream = System.IO.File.OpenWrite("Output.png");
                //    renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);
                //}
            }
#endif

            Camera.ClientWidth = oldCameraWidth;
            Camera.ClientHeight = oldCameraHeight;
            Camera.X = oldCameraX;
            Camera.Y = oldCameraY;
            Camera.ClientLeft = oldCameraClientLeft;
            Camera.ClientTop = oldCameraClientTop;

            GraphicsDevice.Viewport = oldViewport;

            // Uncomment this to test saving...
            //if (!System.IO.File.Exists("Output.png"))
            //{
            //    using var stream = System.IO.File.OpenWrite("Output.png");
            //    renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);
            //}

        }

    }

    private void Render(IList<IRenderableIpso> whatToRender, SystemManagers managers, Layer layer, bool isPreRender)
    {
        var count = whatToRender.Count;
        for (int i = 0; i < count; i++)
        {
            var renderable = whatToRender[i];
            Draw(managers, layer, renderable, forceRenderHierarchy:false, isPreRender:isPreRender);
        }
    }


    Sprite renderTargetRenderableSprite = new Sprite((Texture2D)null);

    string currentBatchKey = string.Empty;
    IRenderable? lastBatchOwner;

    private void Draw(SystemManagers managers, Layer layer, IRenderableIpso renderable, bool forceRenderHierarchy, bool isPreRender)
    {
        if (renderable.Visible || ( renderable.IsRenderTarget && isPreRender))
        {
            var oldClip = mRenderStateVariables.ClipRectangle;
            AdjustRenderStates(mRenderStateVariables, layer, renderable);
            bool didClipChange = oldClip != mRenderStateVariables.ClipRectangle;

            if (renderable.IsRenderTarget && !forceRenderHierarchy)
            {
                var renderTarget = renderTargetService.GetRenderTargetFor(GraphicsDevice, renderable, Camera);

                if(renderTarget != null)
                {
                    var renderableAlpha = renderable.Alpha;
                    renderableAlpha = System.Math.Min(255, renderableAlpha);
                    renderableAlpha = System.Math.Max(0, renderableAlpha);

                    var color = System.Drawing.Color.FromArgb(
                        renderableAlpha, Color.White
                        );

                    renderTargetRenderableSprite.X = System.Math.Max(renderable.GetAbsoluteX(), Camera.AbsoluteLeft);
                    renderTargetRenderableSprite.Y = System.Math.Max(renderable.GetAbsoluteY(), Camera.AbsoluteTop);
                    renderTargetRenderableSprite.Width = renderTarget.Width / Camera.Zoom;
                    renderTargetRenderableSprite.Height = renderTarget.Height / Camera.Zoom;


                    Sprite.Render(managers, spriteRenderer, renderTargetRenderableSprite, renderTarget, color, rotationInDegrees:renderable.Rotation, objectCausingRendering: renderable);
                }
            }
            else
            {
                if(!string.IsNullOrEmpty(renderable.BatchKey) && renderable.BatchKey != currentBatchKey)
                {
                    if(lastBatchOwner != null)
                    {
                        lastBatchOwner.EndBatch(managers);
                    }

                    currentBatchKey = renderable.BatchKey;
                    lastBatchOwner = renderable;
                    renderable.StartBatch(managers);
                }

                renderable.Render(managers);


                if (RenderUsingHierarchy)
                {
                    Render(renderable.Children, managers, layer, isPreRender);
                }
            }

            if (didClipChange)
            {
                mRenderStateVariables.ClipRectangle = oldClip;

                if (lastBatchOwner != null)
                {
                    lastBatchOwner.EndBatch(managers);
                    lastBatchOwner = null;
                    currentBatchKey = string.Empty;
                }

                spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Begin, mCamera, $"Un-set {renderable} Clip");
            }
        }
    }

    internal Rectangle GetScissorRectangleFor(Camera camera, IRenderableIpso ipso)
    {
        if (ipso == null)
        {
            return new Rectangle(
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

            Rectangle thisRectangle = new Rectangle(
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

        if(renderState.ColorOperation != renderable.ColorOperation)
        {
            renderState.ColorOperation = renderable.ColorOperation;
            shouldResetStates = true;
        }

        if (renderState.Wrap != wrap)
        {
            renderState.Wrap = wrap;
            shouldResetStates = true;
        }

        if (renderable.ClipsChildren)
        {
            var clipRectangle = GetScissorRectangleFor(Camera, renderable);

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
            spriteRenderer.BeginSpriteBatch(renderState, layer, BeginType.Begin, mCamera, renderable);
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

    // Made public to allow custom renderable objects to be removed:
    public void RemoveRenderable(IRenderableIpso renderable)
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

    public override bool Equals(object? obj)
    {
        return obj is Renderer renderer &&
               EqualityComparer<ReadOnlyCollection<Layer>>.Default.Equals(_layersReadOnly, renderer._layersReadOnly);
    }
}

#region RenderTargetService

class RenderTargetService
{
    HashSet<IRenderableIpso> itemsUsingRenderTargetsThisFrame = new HashSet<IRenderableIpso>();
    Dictionary<IRenderableIpso, RenderTarget2D> RenderTargets = new Dictionary<IRenderableIpso, RenderTarget2D>();
    List<IRenderableIpso> keysToRemove = new List<IRenderableIpso>();

    public void ClearUnusedRenderTargetsLastFrame()
    {
        keysToRemove.Clear();

        foreach (var item in RenderTargets)
        {
            if(itemsUsingRenderTargetsThisFrame.Contains(item.Key) == false)
            {
                keysToRemove.Add(item.Key);
            }
        }

        foreach(var item in keysToRemove)
        {
            RenderTargets[item].Dispose();
            RenderTargets.Remove(item);
        }

        itemsUsingRenderTargetsThisFrame.Clear();
    }

    public RenderTarget2D GetExistingRenderTarget(IRenderableIpso renderable)
    {
        if(RenderTargets.ContainsKey(renderable))
        {
            var renderTarget = RenderTargets[renderable];
            if(renderTarget.IsDisposed == false)
            {
                return RenderTargets[renderable];
            }
        }
        
        return null;
    }

    public RenderTarget2D? GetRenderTargetFor(GraphicsDevice graphicsDevice, IRenderableIpso renderable, Camera camera)
    {
        itemsUsingRenderTargetsThisFrame.Add(renderable);

        var left = renderable.GetAbsoluteLeft();
        var right = renderable.GetAbsoluteRight();
        var top = renderable.GetAbsoluteTop();
        var bottom = renderable.GetAbsoluteBottom();

        left = System.Math.Max(camera.AbsoluteLeft, left);
        right = System.Math.Min(camera.AbsoluteRight, right);
        top = System.Math.Max(camera.AbsoluteTop, top);
        bottom = System.Math.Min(camera.AbsoluteBottom, bottom);

        //System.Diagnostics.Debug.WriteLine($"L:{left} R:{right} T:{top} B:{bottom}");

        var clientWidth = camera.ClientWidth;
        var clientHeight = camera.ClientHeight;

        var width = Math.MathFunctions.RoundToInt((right - left)* camera.Zoom);
        var height = Math.MathFunctions.RoundToInt((bottom - top)* camera.Zoom);

        if(width <= 0 || height <= 0)
        {
            return null;
        }
        else
        {
            if (RenderTargets.ContainsKey(renderable))
            {
                var existingRenderTarget = RenderTargets[renderable];
                if (existingRenderTarget.Width != width || existingRenderTarget.Height != height)
                {
                    existingRenderTarget.Dispose();
                    RenderTargets.Remove(renderable);
                }
            }


            if (RenderTargets.ContainsKey(renderable) == false)
            {
                //var width = GraphicsDevice.Viewport.Width;
                //var height = GraphicsDevice.Viewport.Height;
                RenderTargets[renderable] = new RenderTarget2D(graphicsDevice, (int)width, (int)height);

            }
            var renderTarget = RenderTargets[renderable];

            return renderTarget;

        }
    }
}

#endregion

#region GumBatch

public class GumBatch
{
    enum GumBatchState
    {
        NotRendering,
        BeginCalled
    }


    GumBatchState State;
    SystemManagers systemManagers;
    Text internalTextForRendering;
    public GumBatch()
    {
        systemManagers = SystemManagers.Default;
        internalTextForRendering = new Text(systemManagers);
    }

    public void Begin(Matrix? spriteBatchMatrix = null)
    {
        if(State == GumBatchState.BeginCalled)
        {
            throw new InvalidOperationException("Begin has already been called. You must call End before calling Begin again.");
        }

        State = GumBatchState.BeginCalled;

        systemManagers.Renderer.Camera.ClientWidth = systemManagers.Renderer.GraphicsDevice.Viewport.Width;
        systemManagers.Renderer.Camera.ClientHeight = systemManagers.Renderer.GraphicsDevice.Viewport.Height;
        systemManagers.Renderer.Camera.ClientLeft = systemManagers.Renderer.GraphicsDevice.Viewport.X;
        systemManagers.Renderer.Camera.ClientTop = systemManagers.Renderer.GraphicsDevice.Viewport.Y;

        systemManagers.Renderer.Begin(spriteBatchMatrix);
    }

    public void DrawString(BitmapFont font, string text, Microsoft.Xna.Framework.Vector2 position, Microsoft.Xna.Framework.Color color)
    {
        if (State == GumBatchState.NotRendering)
        {
            throw new InvalidOperationException("You must call Begin before calling DrawString");
        }

        internalTextForRendering.BitmapFont = font;
        internalTextForRendering.Width = null;
        internalTextForRendering.RawText = text;
        internalTextForRendering.X = position.X;
        internalTextForRendering.Y = position.Y;
        internalTextForRendering.Color = color.ToSystemDrawing();
        Draw(internalTextForRendering);
    }

    public void Draw(IRenderableIpso renderable)
    {
        if(State == GumBatchState.NotRendering)
        {
            throw new InvalidOperationException("You must call Begin before calling Draw");
        }

        systemManagers.Renderer.Draw(renderable);
    }

    public void End()
    {
        if(State == GumBatchState.NotRendering)
        {
            throw new InvalidOperationException("You must call Begin before calling End");
        }
        State = GumBatchState.NotRendering;

        systemManagers.Renderer.End();
    }


}

#endregion

#region Custom effect support

/// <summary>
/// Manages custom effects from the custom shader file. Main purposes:
/// <list type="number">
/// <item><description>Caches effect parameters and techniques to avoid lookups during rendering.</description></item>
/// <item><description>Handles compatibility between old and new effect specifications with automatic fallback.</description></item>
/// <item><description>Provides methods to retrieve techniques based on:
/// <list type="bullet">
/// <item><description>Texture filtering (Point/Linear)</description></item>
/// <item><description>Color source (VertexColor/ColorModifier)</description></item>
/// <item><description>Color operation (Add, Subtract, Modulate, etc.)</description></item>
/// <item><description>Gamma correction (Linearize)</description></item>
/// </list>
/// </description></item>
/// </list>
/// This class is designed for use by renderers and custom graphics code.
/// </summary>
public class CustomEffectManager
{
    Effect _effect = null!;

    // Cached effect members to avoid list lookups while rendering
    public EffectParameter ParameterCurrentTexture = null!;
    public EffectParameter ParameterViewProj = null!;
    public EffectParameter? ParameterColorModifier;

    bool _effectHasNewformat;

    EffectTechnique? _techniqueTexture;
    EffectTechnique? _techniqueAdd;
    EffectTechnique? _techniqueSubtract;
    EffectTechnique? _techniqueModulate;
    EffectTechnique? _techniqueModulate2X;
    EffectTechnique? _techniqueModulate4X;
    EffectTechnique? _techniqueInverseTexture;
    EffectTechnique? _techniqueColor;
    EffectTechnique? _techniqueColorTextureAlpha;
    EffectTechnique? _techniqueInterpolateColor;

    EffectTechnique? _techniqueTexture_CM;
    EffectTechnique? _techniqueAdd_CM;
    EffectTechnique? _techniqueSubtract_CM;
    EffectTechnique? _techniqueModulate_CM;
    EffectTechnique? _techniqueModulate2X_CM;
    EffectTechnique? _techniqueModulate4X_CM;
    EffectTechnique? _techniqueInverseTexture_CM;
    EffectTechnique? _techniqueColor_CM;
    EffectTechnique? _techniqueColorTextureAlpha_CM;
    EffectTechnique? _techniqueInterpolateColor_CM;

    EffectTechnique? _techniqueTexture_LN;
    EffectTechnique? _techniqueAdd_LN;
    EffectTechnique? _techniqueSubtract_LN;
    EffectTechnique? _techniqueModulate_LN;
    EffectTechnique? _techniqueModulate2X_LN;
    EffectTechnique? _techniqueModulate4X_LN;
    EffectTechnique? _techniqueInverseTexture_LN;
    EffectTechnique? _techniqueColor_LN;
    EffectTechnique? _techniqueColorTextureAlpha_LN;
    EffectTechnique? _techniqueInterpolateColor_LN;

    EffectTechnique? _techniqueTexture_LN_CM;
    EffectTechnique? _techniqueAdd_LN_CM;
    EffectTechnique? _techniqueSubtract_LN_CM;
    EffectTechnique? _techniqueModulate_LN_CM;
    EffectTechnique? _techniqueModulate2X_LN_CM;
    EffectTechnique? _techniqueModulate4X_LN_CM;
    EffectTechnique? _techniqueInverseTexture_LN_CM;
    EffectTechnique? _techniqueColor_LN_CM;
    EffectTechnique? _techniqueColorTextureAlpha_LN_CM;
    EffectTechnique? _techniqueInterpolateColor_LN_CM;

    EffectTechnique? _techniqueTexture_Linear;
    EffectTechnique? _techniqueAdd_Linear;
    EffectTechnique? _techniqueSubtract_Linear;
    EffectTechnique? _techniqueModulate_Linear;
    EffectTechnique? _techniqueModulate2X_Linear;
    EffectTechnique? _techniqueModulate4X_Linear;
    EffectTechnique? _techniqueInverseTexture_Linear;
    EffectTechnique? _techniqueColor_Linear;
    EffectTechnique? _techniqueColorTextureAlpha_Linear;
    EffectTechnique? _techniqueInterpolateColor_Linear;

    EffectTechnique? _techniqueTexture_Linear_CM;
    EffectTechnique? _techniqueAdd_Linear_CM;
    EffectTechnique? _techniqueSubtract_Linear_CM;
    EffectTechnique? _techniqueModulate_Linear_CM;
    EffectTechnique? _techniqueModulate2X_Linear_CM;
    EffectTechnique? _techniqueModulate4X_Linear_CM;
    EffectTechnique? _techniqueInverseTexture_Linear_CM;
    EffectTechnique? _techniqueColor_Linear_CM;
    EffectTechnique? _techniqueColorTextureAlpha_Linear_CM;
    EffectTechnique? _techniqueInterpolateColor_Linear_CM;

    EffectTechnique? _techniqueTexture_Linear_LN;
    EffectTechnique? _techniqueAdd_Linear_LN;
    EffectTechnique? _techniqueSubtract_Linear_LN;
    EffectTechnique? _techniqueModulate_Linear_LN;
    EffectTechnique? _techniqueModulate2X_Linear_LN;
    EffectTechnique? _techniqueModulate4X_Linear_LN;
    EffectTechnique? _techniqueInverseTexture_Linear_LN;
    EffectTechnique? _techniqueColor_Linear_LN;
    EffectTechnique? _techniqueColorTextureAlpha_Linear_LN;
    EffectTechnique? _techniqueInterpolateColor_Linear_LN;

    EffectTechnique? _techniqueTexture_Linear_LN_CM;
    EffectTechnique? _techniqueAdd_Linear_LN_CM;
    EffectTechnique? _techniqueSubtract_Linear_LN_CM;
    EffectTechnique? _techniqueModulate_Linear_LN_CM;
    EffectTechnique? _techniqueModulate2X_Linear_LN_CM;
    EffectTechnique? _techniqueModulate4X_Linear_LN_CM;
    EffectTechnique? _techniqueInverseTexture_Linear_LN_CM;
    EffectTechnique? _techniqueColor_Linear_LN_CM;
    EffectTechnique? _techniqueColorTextureAlpha_Linear_LN_CM;
    EffectTechnique? _techniqueInterpolateColor_Linear_LN_CM;

    public Effect Effect
    {
        get { return _effect; }
        set
        {
            _effect = value;

            var parameterViewProj = GetParameterSafe("ViewProj");
            if (parameterViewProj == null) // ViewProj is required. Throw exception if null.
            {
                throw new InvalidOperationException("Shader.xnb must contain a parameter called ViewProj.");
            }

            ParameterViewProj = parameterViewProj;

            var parameterCurrentTexture = GetParameterSafe("CurrentTexture");
            if (parameterCurrentTexture == null) // CurrentTexture is required. Throw exception if null.
            {
                throw new InvalidOperationException("Shader.xnb must contain a parameter called CurrentTexture.");
            }

            ParameterCurrentTexture = parameterCurrentTexture;

            ParameterColorModifier = GetParameterSafe("ColorModifier");

            // Let's check if the shader has the new format (which includes
            // separate versions of techniques for Point and Linear filtering).
            // We try to cache the first technique in order to do so.
            _techniqueTexture = GetTechniqueSafe("Texture_Point");

            if (_techniqueTexture != null)
            {
                _effectHasNewformat = true;

                //_techniqueTexture = GetTechniqueSafe("Texture_Point"); // This has been already cached
                _techniqueAdd = GetTechniqueSafe("Add_Point");
                _techniqueSubtract = GetTechniqueSafe("Subtract_Point");
                _techniqueModulate = GetTechniqueSafe("Modulate_Point");
                _techniqueModulate2X = GetTechniqueSafe("Modulate2X_Point");
                _techniqueModulate4X = GetTechniqueSafe("Modulate4X_Point");
                _techniqueInverseTexture = GetTechniqueSafe("InverseTexture_Point");
                _techniqueColor = GetTechniqueSafe("Color_Point");
                _techniqueColorTextureAlpha = GetTechniqueSafe("ColorTextureAlpha_Point");
                _techniqueInterpolateColor = GetTechniqueSafe("InterpolateColor_Point");

                _techniqueTexture_CM = GetTechniqueSafe("Texture_Point_CM");
                _techniqueAdd_CM = GetTechniqueSafe("Add_Point_CM");
                _techniqueSubtract_CM = GetTechniqueSafe("Subtract_Point_CM");
                _techniqueModulate_CM = GetTechniqueSafe("Modulate_Point_CM");
                _techniqueModulate2X_CM = GetTechniqueSafe("Modulate2X_Point_CM");
                _techniqueModulate4X_CM = GetTechniqueSafe("Modulate4X_Point_CM");
                _techniqueInverseTexture_CM = GetTechniqueSafe("InverseTexture_Point_CM");
                _techniqueColor_CM = GetTechniqueSafe("Color_Point_CM");
                _techniqueColorTextureAlpha_CM = GetTechniqueSafe("ColorTextureAlpha_Point_CM");
                _techniqueInterpolateColor_CM = GetTechniqueSafe("InterpolateColor_Point_CM");

                _techniqueTexture_LN = GetTechniqueSafe("Texture_Point_LN");
                _techniqueAdd_LN = GetTechniqueSafe("Add_Point_LN");
                _techniqueSubtract_LN = GetTechniqueSafe("Subtract_Point_LN");
                _techniqueModulate_LN = GetTechniqueSafe("Modulate_Point_LN");
                _techniqueModulate2X_LN = GetTechniqueSafe("Modulate2X_Point_LN");
                _techniqueModulate4X_LN = GetTechniqueSafe("Modulate4X_Point_LN");
                _techniqueInverseTexture_LN = GetTechniqueSafe("InverseTexture_Point_LN");
                _techniqueColor_LN = GetTechniqueSafe("Color_Point_LN");
                _techniqueColorTextureAlpha_LN = GetTechniqueSafe("ColorTextureAlpha_Point_LN");
                _techniqueInterpolateColor_LN = GetTechniqueSafe("InterpolateColor_Point_LN");

                _techniqueTexture_LN_CM = GetTechniqueSafe("Texture_Point_LN_CM");
                _techniqueAdd_LN_CM = GetTechniqueSafe("Add_Point_LN_CM");
                _techniqueSubtract_LN_CM = GetTechniqueSafe("Subtract_Point_LN_CM");
                _techniqueModulate_LN_CM = GetTechniqueSafe("Modulate_Point_LN_CM");
                _techniqueModulate2X_LN_CM = GetTechniqueSafe("Modulate2X_Point_LN_CM");
                _techniqueModulate4X_LN_CM = GetTechniqueSafe("Modulate4X_Point_LN_CM");
                _techniqueInverseTexture_LN_CM = GetTechniqueSafe("InverseTexture_Point_LN_CM");
                _techniqueColor_LN_CM = GetTechniqueSafe("Color_Point_LN_CM");
                _techniqueColorTextureAlpha_LN_CM = GetTechniqueSafe("ColorTextureAlpha_Point_LN_CM");
                _techniqueInterpolateColor_LN_CM = GetTechniqueSafe("InterpolateColor_Point_LN_CM");

                _techniqueTexture_Linear = GetTechniqueSafe("Texture_Linear");
                _techniqueAdd_Linear = GetTechniqueSafe("Add_Linear");
                _techniqueSubtract_Linear = GetTechniqueSafe("Subtract_Linear");
                _techniqueModulate_Linear = GetTechniqueSafe("Modulate_Linear");
                _techniqueModulate2X_Linear = GetTechniqueSafe("Modulate2X_Linear");
                _techniqueModulate4X_Linear = GetTechniqueSafe("Modulate4X_Linear");
                _techniqueInverseTexture_Linear = GetTechniqueSafe("InverseTexture_Linear");
                _techniqueColor_Linear = GetTechniqueSafe("Color_Linear");
                _techniqueColorTextureAlpha_Linear = GetTechniqueSafe("ColorTextureAlpha_Linear");
                _techniqueInterpolateColor_Linear = GetTechniqueSafe("InterpolateColor_Linear");

                _techniqueTexture_Linear_CM = GetTechniqueSafe("Texture_Linear_CM");
                _techniqueAdd_Linear_CM = GetTechniqueSafe("Add_Linear_CM");
                _techniqueSubtract_Linear_CM = GetTechniqueSafe("Subtract_Linear_CM");
                _techniqueModulate_Linear_CM = GetTechniqueSafe("Modulate_Linear_CM");
                _techniqueModulate2X_Linear_CM = GetTechniqueSafe("Modulate2X_Linear_CM");
                _techniqueModulate4X_Linear_CM = GetTechniqueSafe("Modulate4X_Linear_CM");
                _techniqueInverseTexture_Linear_CM = GetTechniqueSafe("InverseTexture_Linear_CM");
                _techniqueColor_Linear_CM = GetTechniqueSafe("Color_Linear_CM");
                _techniqueColorTextureAlpha_Linear_CM = GetTechniqueSafe("ColorTextureAlpha_Linear_CM");
                _techniqueInterpolateColor_Linear_CM = GetTechniqueSafe("InterpolateColor_Linear_CM");

                _techniqueTexture_Linear_LN = GetTechniqueSafe("Texture_Linear_LN");
                _techniqueAdd_Linear_LN = GetTechniqueSafe("Add_Linear_LN");
                _techniqueSubtract_Linear_LN = GetTechniqueSafe("Subtract_Linear_LN");
                _techniqueModulate_Linear_LN = GetTechniqueSafe("Modulate_Linear_LN");
                _techniqueModulate2X_Linear_LN = GetTechniqueSafe("Modulate2X_Linear_LN");
                _techniqueModulate4X_Linear_LN = GetTechniqueSafe("Modulate4X_Linear_LN");
                _techniqueInverseTexture_Linear_LN = GetTechniqueSafe("InverseTexture_Linear_LN");
                _techniqueColor_Linear_LN = GetTechniqueSafe("Color_Linear_LN");
                _techniqueColorTextureAlpha_Linear_LN = GetTechniqueSafe("ColorTextureAlpha_Linear_LN");
                _techniqueInterpolateColor_Linear_LN = GetTechniqueSafe("InterpolateColor_Linear_LN");

                _techniqueTexture_Linear_LN_CM = GetTechniqueSafe("Texture_Linear_LN_CM");
                _techniqueAdd_Linear_LN_CM = GetTechniqueSafe("Add_Linear_LN_CM");
                _techniqueSubtract_Linear_LN_CM = GetTechniqueSafe("Subtract_Linear_LN_CM");
                _techniqueModulate_Linear_LN_CM = GetTechniqueSafe("Modulate_Linear_LN_CM");
                _techniqueModulate2X_Linear_LN_CM = GetTechniqueSafe("Modulate2X_Linear_LN_CM");
                _techniqueModulate4X_Linear_LN_CM = GetTechniqueSafe("Modulate4X_Linear_LN_CM");
                _techniqueInverseTexture_Linear_LN_CM = GetTechniqueSafe("InverseTexture_Linear_LN_CM");
                _techniqueColor_Linear_LN_CM = GetTechniqueSafe("Color_Linear_LN_CM");
                _techniqueColorTextureAlpha_Linear_LN_CM = GetTechniqueSafe("ColorTextureAlpha_Linear_LN_CM");
                _techniqueInterpolateColor_Linear_LN_CM = GetTechniqueSafe("InterpolateColor_Linear_LN_CM");
            }
            else
            {
                _effectHasNewformat = false;

                _techniqueTexture = GetTechniqueSafe("Texture");
                _techniqueAdd = GetTechniqueSafe("Add");
                _techniqueSubtract = GetTechniqueSafe("Subtract");
                _techniqueModulate = GetTechniqueSafe("Modulate");
                _techniqueModulate2X = GetTechniqueSafe("Modulate2X");
                _techniqueModulate4X = GetTechniqueSafe("Modulate4X");
                _techniqueInverseTexture = GetTechniqueSafe("InverseTexture");
                _techniqueColor = GetTechniqueSafe("Color");
                _techniqueColorTextureAlpha = GetTechniqueSafe("ColorTextureAlpha");
                _techniqueInterpolateColor = GetTechniqueSafe("InterpolateColor");
            }
        }
    }

    EffectParameter? GetParameterSafe(string parameterName)
    {
        if (_effect == null)
            return null;

        for (int i = 0; i < _effect.Parameters.Count; i++)
        {
            var parameter = _effect.Parameters[i];
            if (parameter.Name == parameterName)
                return parameter;
        }

        return null;
    }

    EffectTechnique? GetTechniqueSafe(string techniqueName)
    {
        if (_effect == null)
            return null;

        for (int i = 0; i < _effect.Techniques.Count; i++)
        {
            var technique = _effect.Techniques[i];
            if (technique.Name == techniqueName)
                return technique;
        }

        return null;
    }

    public class ServiceContainer : IServiceProvider
    {
        #region Fields

        Dictionary<Type, object> services = new Dictionary<Type, object>();

        #endregion

        #region Methods

        public void AddService<T>(T service)
        {
            services.Add(typeof(T), service);
        }

        public object GetService(Type serviceType)
        {
            object service;

            services.TryGetValue(serviceType, out service);

            return service;
        }

        #endregion
    }

    static ContentManager mContentManager;

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        if (mContentManager == null)
        {
            mContentManager = new ContentManager(
                              new ServiceProvider(
                                   new DeviceManager(graphicsDevice)));
        }

        // Loads the Shader.xnb effect file. The shader is optional; if missing, 
        // the application won't be able to use custom effects.
        // Use of try-catch avoids using platform specific code.
        // Shader should be capitalized.
        try
        { 
            Effect = mContentManager.Load<Effect>("Content/Shader");
        } 
        catch
        {
            Debug.WriteLine("'Content/Shader.xnb' not found. Custom rendering is not available.");
        }
    }

    static EffectTechnique GetTechniqueVariant(bool useDefaultOrPointFilter, EffectTechnique point, EffectTechnique pointLinearized, EffectTechnique linear, EffectTechnique linearLinearized)
    {
        return useDefaultOrPointFilter ?
            (Renderer.LinearizeTextures ? pointLinearized : point) :
            (Renderer.LinearizeTextures ? linearLinearized : linear);
    }

    public EffectTechnique GetVertexColorTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
    {
        if (_effect == null)
            throw new InvalidOperationException("The effect hasn't been set.");

        EffectTechnique technique = null!;

        bool useDefaultOrPointFilterInternal;

        if (_effectHasNewformat)
        {
            // If the shader has the new format both point and linear are available
            if (!useDefaultOrPointFilter.HasValue)
            {
                // Filter not specified, we don't seem to have general setting for
                // filtering in Gum so we'll use the default.
                useDefaultOrPointFilterInternal = true;
            }
            else
            {
                // Filter specified
                useDefaultOrPointFilterInternal = useDefaultOrPointFilter.Value;
            }
        }
        else
        {
            // If the shader doesn't have the new format only one version of
            // the techniques are available, probably using point filtering.
            useDefaultOrPointFilterInternal = true;
        }

        // Only Modulate and ColorTextureAlpha are available in Gum at the moment
        switch (value)
        {
            //case ColorOperation.Texture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueTexture, _techniqueTexture_LN, _techniqueTexture_Linear, _techniqueTexture_Linear_LN); break;

            //case ColorOperation.Add:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueAdd, _techniqueAdd_LN, _techniqueAdd_Linear, _techniqueAdd_Linear_LN); break;

            //case ColorOperation.Subtract:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueSubtract, _techniqueSubtract_LN, _techniqueSubtract_Linear, _techniqueSubtract_Linear_LN); break;

            case ColorOperation.Modulate:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, _techniqueModulate, _techniqueModulate_LN, _techniqueModulate_Linear, _techniqueModulate_Linear_LN); break;

            //case ColorOperation.Modulate2X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueModulate2X, _techniqueModulate2X_LN, _techniqueModulate2X_Linear, _techniqueModulate2X_Linear_LN); break;

            //case ColorOperation.Modulate4X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueModulate4X, _techniqueModulate4X_LN, _techniqueModulate4X_Linear, _techniqueModulate4X_Linear_LN); break;

            //case ColorOperation.InverseTexture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueInverseTexture, _techniqueInverseTexture_LN, _techniqueInverseTexture_Linear, _techniqueInverseTexture_Linear_LN); break;

            //case ColorOperation.Color:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueColor, _techniqueColor_LN, _techniqueColor_Linear, _techniqueColor_Linear_LN); break;

            case ColorOperation.ColorTextureAlpha:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, _techniqueColorTextureAlpha, _techniqueColorTextureAlpha_LN, _techniqueColorTextureAlpha_Linear, _techniqueColorTextureAlpha_Linear_LN); break;

            //case ColorOperation.InterpolateColor:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueInterpolateColor, _techniqueInterpolateColor_LN, _techniqueInterpolateColor_Linear, _techniqueInterpolateColor_Linear_LN); break;

            default: throw new InvalidOperationException();
        }

        return technique;
    }

    public EffectTechnique GetColorModifierTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
    {
        if (_effect == null)
            throw new InvalidOperationException("The effect hasn't been set.");

        EffectTechnique technique = null!;

        bool useDefaultOrPointFilterInternal;

        if (_effectHasNewformat)
        {
            // If the shader has the new format both point and linear are available
            if (!useDefaultOrPointFilter.HasValue)
            {
                // Filter not specified, we don't seem to have general setting for
                // filtering in Gum so we'll use the default.
                useDefaultOrPointFilterInternal = true;
            }
            else
            {
                // Filter specified
                useDefaultOrPointFilterInternal = useDefaultOrPointFilter.Value;
            }
        }
        else
        {
            // If the shader doesn't have the new format only one version of
            // the techniques are available, probably using point filtering.
            useDefaultOrPointFilterInternal = true;
        }

        switch (value)
        {
            //case ColorOperation.Texture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueTexture_CM, _techniqueTexture_LN_CM, _techniqueTexture_Linear_CM, _techniqueTexture_Linear_LN_CM); break;

            //case ColorOperation.Add:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueAdd_CM, _techniqueAdd_LN_CM, _techniqueAdd_Linear_CM, _techniqueAdd_Linear_LN_CM); break;

            //case ColorOperation.Subtract:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueSubtract_CM, _techniqueSubtract_LN_CM, _techniqueSubtract_Linear_CM, _techniqueSubtract_Linear_LN_CM); break;

            case ColorOperation.Modulate:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, _techniqueModulate_CM, _techniqueModulate_LN_CM, _techniqueModulate_Linear_CM, _techniqueModulate_Linear_LN_CM); break;

            //case ColorOperation.Modulate2X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueModulate2X_CM, _techniqueModulate2X_LN_CM, _techniqueModulate2X_Linear_CM, _techniqueModulate2X_Linear_LN_CM); break;

            //case ColorOperation.Modulate4X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueModulate4X_CM, _techniqueModulate4X_LN_CM, _techniqueModulate4X_Linear_CM, _techniqueModulate4X_Linear_LN_CM); break;

            //case ColorOperation.InverseTexture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueInverseTexture_CM, _techniqueInverseTexture_LN_CM, _techniqueInverseTexture_Linear_CM, _techniqueInverseTexture_Linear_LN_CM); break;

            //case ColorOperation.Color:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueColor_CM, _techniqueColor_LN_CM, _techniqueColor_Linear_CM, _techniqueColor_Linear_LN_CM); break;

            case ColorOperation.ColorTextureAlpha:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, _techniqueColorTextureAlpha_CM, _techniqueColorTextureAlpha_LN_CM, _techniqueColorTextureAlpha_Linear_CM, _techniqueColorTextureAlpha_Linear_LN_CM); break;

            //case ColorOperation.InterpolateColor:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueInterpolateColor_CM, _techniqueInterpolateColor_LN_CM, _techniqueInterpolateColor_Linear_CM, _techniqueInterpolateColor_Linear_LN_CM); break;

            default: throw new InvalidOperationException();
        }

        return technique;
    }
}

public class DeviceManager : IGraphicsDeviceService
{
    public DeviceManager(GraphicsDevice device)
    {
        GraphicsDevice = device;
    }

    public GraphicsDevice GraphicsDevice { get; }

    public event EventHandler<EventArgs> DeviceCreated;
    public event EventHandler<EventArgs> DeviceDisposing;

    private EventHandler<EventArgs> deviceReset;
    event EventHandler<EventArgs> IGraphicsDeviceService.DeviceReset
    {
        add
        {
            lock (this)
            {
                deviceReset += value;
            }
        }
        remove
        {
            lock (this)
            {
                deviceReset -= value;
            }
        }
    }

    public event EventHandler<EventArgs> DeviceResetting;
}

public class ServiceProvider : IServiceProvider
{
    private readonly IGraphicsDeviceService deviceService;

    public ServiceProvider(IGraphicsDeviceService deviceService)
    {
        this.deviceService = deviceService;
    }

    public object GetService(Type serviceType)
    {
        return deviceService;
    }
}

#endregion
