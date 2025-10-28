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
    ReadOnlyCollection<Layer> mLayersReadOnly;

    SpriteRenderer spriteRenderer = new SpriteRenderer();

    RenderStateVariables mRenderStateVariables = new RenderStateVariables();

    GraphicsDevice mGraphicsDevice;

    static Renderer mSelf;
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

    public ReadOnlyCollection<Layer> Layers
    {
        get
        {
            return mLayersReadOnly;
        }
    }

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
        mLayersReadOnly = new ReadOnlyCollection<Layer>(_layers);
        mCamera = new RenderingLibrary.Camera();

    }

    public void Initialize(GraphicsDevice graphicsDevice, SystemManagers managers)
    {
        renderTargetService = new RenderTargetService();

        if (graphicsDevice != null)
        {
            mCamera.ClientWidth = graphicsDevice.Viewport.Width;
            mCamera.ClientHeight = graphicsDevice.Viewport.Height;
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
                    var renderableAlpha = (int)renderable.Alpha;
                    renderableAlpha = System.Math.Min(255, (int)renderableAlpha);
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

public class CustomEffectManager
{
    Effect mEffect;

    // Cached effect members to avoid list lookups while rendering
    public EffectParameter ParameterCurrentTexture;
    public EffectParameter ParameterViewProj;
    public EffectParameter ParameterColorModifier;

    bool mEffectHasNewformat;

    EffectTechnique mTechniqueTexture;
    EffectTechnique mTechniqueAdd;
    EffectTechnique mTechniqueSubtract;
    EffectTechnique mTechniqueModulate;
    EffectTechnique mTechniqueModulate2X;
    EffectTechnique mTechniqueModulate4X;
    EffectTechnique mTechniqueInverseTexture;
    EffectTechnique mTechniqueColor;
    EffectTechnique mTechniqueColorTextureAlpha;
    EffectTechnique mTechniqueInterpolateColor;

    EffectTechnique mTechniqueTexture_CM;
    EffectTechnique mTechniqueAdd_CM;
    EffectTechnique mTechniqueSubtract_CM;
    EffectTechnique mTechniqueModulate_CM;
    EffectTechnique mTechniqueModulate2X_CM;
    EffectTechnique mTechniqueModulate4X_CM;
    EffectTechnique mTechniqueInverseTexture_CM;
    EffectTechnique mTechniqueColor_CM;
    EffectTechnique mTechniqueColorTextureAlpha_CM;
    EffectTechnique mTechniqueInterpolateColor_CM;

    EffectTechnique mTechniqueTexture_LN;
    EffectTechnique mTechniqueAdd_LN;
    EffectTechnique mTechniqueSubtract_LN;
    EffectTechnique mTechniqueModulate_LN;
    EffectTechnique mTechniqueModulate2X_LN;
    EffectTechnique mTechniqueModulate4X_LN;
    EffectTechnique mTechniqueInverseTexture_LN;
    EffectTechnique mTechniqueColor_LN;
    EffectTechnique mTechniqueColorTextureAlpha_LN;
    EffectTechnique mTechniqueInterpolateColor_LN;

    EffectTechnique mTechniqueTexture_LN_CM;
    EffectTechnique mTechniqueAdd_LN_CM;
    EffectTechnique mTechniqueSubtract_LN_CM;
    EffectTechnique mTechniqueModulate_LN_CM;
    EffectTechnique mTechniqueModulate2X_LN_CM;
    EffectTechnique mTechniqueModulate4X_LN_CM;
    EffectTechnique mTechniqueInverseTexture_LN_CM;
    EffectTechnique mTechniqueColor_LN_CM;
    EffectTechnique mTechniqueColorTextureAlpha_LN_CM;
    EffectTechnique mTechniqueInterpolateColor_LN_CM;

    EffectTechnique mTechniqueTexture_Linear;
    EffectTechnique mTechniqueAdd_Linear;
    EffectTechnique mTechniqueSubtract_Linear;
    EffectTechnique mTechniqueModulate_Linear;
    EffectTechnique mTechniqueModulate2X_Linear;
    EffectTechnique mTechniqueModulate4X_Linear;
    EffectTechnique mTechniqueInverseTexture_Linear;
    EffectTechnique mTechniqueColor_Linear;
    EffectTechnique mTechniqueColorTextureAlpha_Linear;
    EffectTechnique mTechniqueInterpolateColor_Linear;

    EffectTechnique mTechniqueTexture_Linear_CM;
    EffectTechnique mTechniqueAdd_Linear_CM;
    EffectTechnique mTechniqueSubtract_Linear_CM;
    EffectTechnique mTechniqueModulate_Linear_CM;
    EffectTechnique mTechniqueModulate2X_Linear_CM;
    EffectTechnique mTechniqueModulate4X_Linear_CM;
    EffectTechnique mTechniqueInverseTexture_Linear_CM;
    EffectTechnique mTechniqueColor_Linear_CM;
    EffectTechnique mTechniqueColorTextureAlpha_Linear_CM;
    EffectTechnique mTechniqueInterpolateColor_Linear_CM;

    EffectTechnique mTechniqueTexture_Linear_LN;
    EffectTechnique mTechniqueAdd_Linear_LN;
    EffectTechnique mTechniqueSubtract_Linear_LN;
    EffectTechnique mTechniqueModulate_Linear_LN;
    EffectTechnique mTechniqueModulate2X_Linear_LN;
    EffectTechnique mTechniqueModulate4X_Linear_LN;
    EffectTechnique mTechniqueInverseTexture_Linear_LN;
    EffectTechnique mTechniqueColor_Linear_LN;
    EffectTechnique mTechniqueColorTextureAlpha_Linear_LN;
    EffectTechnique mTechniqueInterpolateColor_Linear_LN;

    EffectTechnique mTechniqueTexture_Linear_LN_CM;
    EffectTechnique mTechniqueAdd_Linear_LN_CM;
    EffectTechnique mTechniqueSubtract_Linear_LN_CM;
    EffectTechnique mTechniqueModulate_Linear_LN_CM;
    EffectTechnique mTechniqueModulate2X_Linear_LN_CM;
    EffectTechnique mTechniqueModulate4X_Linear_LN_CM;
    EffectTechnique mTechniqueInverseTexture_Linear_LN_CM;
    EffectTechnique mTechniqueColor_Linear_LN_CM;
    EffectTechnique mTechniqueColorTextureAlpha_Linear_LN_CM;
    EffectTechnique mTechniqueInterpolateColor_Linear_LN_CM;

    public Effect Effect
    {
        get { return mEffect; }
        private set
        {
            mEffect = value;

            ParameterViewProj = mEffect.Parameters["ViewProj"];
            ParameterCurrentTexture = mEffect.Parameters["CurrentTexture"];
            try { ParameterColorModifier = mEffect.Parameters["ColorModifier"]; } catch { }

            // Let's check if the shader has the new format (which includes
            // separate versions of techniques for Point and Linear filtering).
            // We try to cache the first technique in order to do so.
            try { mTechniqueTexture = mEffect.Techniques["Texture_Point"]; } catch { }

            if (mTechniqueTexture != null)
            {
                mEffectHasNewformat = true;

                //try { mTechniqueTexture = mEffect.Techniques["Texture_Point"]; } catch { } // This has been already cached
                try { mTechniqueAdd = mEffect.Techniques["Add_Point"]; } catch { }
                try { mTechniqueSubtract = mEffect.Techniques["Subtract_Point"]; } catch { }
                try { mTechniqueModulate = mEffect.Techniques["Modulate_Point"]; } catch { }
                try { mTechniqueModulate2X = mEffect.Techniques["Modulate2X_Point"]; } catch { }
                try { mTechniqueModulate4X = mEffect.Techniques["Modulate4X_Point"]; } catch { }
                try { mTechniqueInverseTexture = mEffect.Techniques["InverseTexture_Point"]; } catch { }
                try { mTechniqueColor = mEffect.Techniques["Color_Point"]; } catch { }
                try { mTechniqueColorTextureAlpha = mEffect.Techniques["ColorTextureAlpha_Point"]; } catch { }
                try { mTechniqueInterpolateColor = mEffect.Techniques["InterpolateColor_Point"]; } catch { }

                try { mTechniqueTexture_CM = mEffect.Techniques["Texture_Point_CM"]; } catch { }
                try { mTechniqueAdd_CM = mEffect.Techniques["Add_Point_CM"]; } catch { }
                try { mTechniqueSubtract_CM = mEffect.Techniques["Subtract_Point_CM"]; } catch { }
                try { mTechniqueModulate_CM = mEffect.Techniques["Modulate_Point_CM"]; } catch { }
                try { mTechniqueModulate2X_CM = mEffect.Techniques["Modulate2X_Point_CM"]; } catch { }
                try { mTechniqueModulate4X_CM = mEffect.Techniques["Modulate4X_Point_CM"]; } catch { }
                try { mTechniqueInverseTexture_CM = mEffect.Techniques["InverseTexture_Point_CM"]; } catch { }
                try { mTechniqueColor_CM = mEffect.Techniques["Color_Point_CM"]; } catch { }
                try { mTechniqueColorTextureAlpha_CM = mEffect.Techniques["ColorTextureAlpha_Point_CM"]; } catch { }
                try { mTechniqueInterpolateColor_CM = mEffect.Techniques["InterpolateColor_Point_CM"]; } catch { }

                try { mTechniqueTexture_LN = mEffect.Techniques["Texture_Point_LN"]; } catch { }
                try { mTechniqueAdd_LN = mEffect.Techniques["Add_Point_LN"]; } catch { }
                try { mTechniqueSubtract_LN = mEffect.Techniques["Subtract_Point_LN"]; } catch { }
                try { mTechniqueModulate_LN = mEffect.Techniques["Modulate_Point_LN"]; } catch { }
                try { mTechniqueModulate2X_LN = mEffect.Techniques["Modulate2X_Point_LN"]; } catch { }
                try { mTechniqueModulate4X_LN = mEffect.Techniques["Modulate4X_Point_LN"]; } catch { }
                try { mTechniqueInverseTexture_LN = mEffect.Techniques["InverseTexture_Point_LN"]; } catch { }
                try { mTechniqueColor_LN = mEffect.Techniques["Color_Point_LN"]; } catch { }
                try { mTechniqueColorTextureAlpha_LN = mEffect.Techniques["ColorTextureAlpha_Point_LN"]; } catch { }
                try { mTechniqueInterpolateColor_LN = mEffect.Techniques["InterpolateColor_Point_LN"]; } catch { }

                try { mTechniqueTexture_LN_CM = mEffect.Techniques["Texture_Point_LN_CM"]; } catch { }
                try { mTechniqueAdd_LN_CM = mEffect.Techniques["Add_Point_LN_CM"]; } catch { }
                try { mTechniqueSubtract_LN_CM = mEffect.Techniques["Subtract_Point_LN_CM"]; } catch { }
                try { mTechniqueModulate_LN_CM = mEffect.Techniques["Modulate_Point_LN_CM"]; } catch { }
                try { mTechniqueModulate2X_LN_CM = mEffect.Techniques["Modulate2X_Point_LN_CM"]; } catch { }
                try { mTechniqueModulate4X_LN_CM = mEffect.Techniques["Modulate4X_Point_LN_CM"]; } catch { }
                try { mTechniqueInverseTexture_LN_CM = mEffect.Techniques["InverseTexture_Point_LN_CM"]; } catch { }
                try { mTechniqueColor_LN_CM = mEffect.Techniques["Color_Point_LN_CM"]; } catch { }
                try { mTechniqueColorTextureAlpha_LN_CM = mEffect.Techniques["ColorTextureAlpha_Point_LN_CM"]; } catch { }
                try { mTechniqueInterpolateColor_LN_CM = mEffect.Techniques["InterpolateColor_Point_LN_CM"]; } catch { }

                try { mTechniqueTexture_Linear = mEffect.Techniques["Texture_Linear"]; } catch { }
                try { mTechniqueAdd_Linear = mEffect.Techniques["Add_Linear"]; } catch { }
                try { mTechniqueSubtract_Linear = mEffect.Techniques["Subtract_Linear"]; } catch { }
                try { mTechniqueModulate_Linear = mEffect.Techniques["Modulate_Linear"]; } catch { }
                try { mTechniqueModulate2X_Linear = mEffect.Techniques["Modulate2X_Linear"]; } catch { }
                try { mTechniqueModulate4X_Linear = mEffect.Techniques["Modulate4X_Linear"]; } catch { }
                try { mTechniqueInverseTexture_Linear = mEffect.Techniques["InverseTexture_Linear"]; } catch { }
                try { mTechniqueColor_Linear = mEffect.Techniques["Color_Linear"]; } catch { }
                try { mTechniqueColorTextureAlpha_Linear = mEffect.Techniques["ColorTextureAlpha_Linear"]; } catch { }
                try { mTechniqueInterpolateColor_Linear = mEffect.Techniques["InterpolateColor_Linear"]; } catch { }

                try { mTechniqueTexture_Linear_CM = mEffect.Techniques["Texture_Linear_CM"]; } catch { }
                try { mTechniqueAdd_Linear_CM = mEffect.Techniques["Add_Linear_CM"]; } catch { }
                try { mTechniqueSubtract_Linear_CM = mEffect.Techniques["Subtract_Linear_CM"]; } catch { }
                try { mTechniqueModulate_Linear_CM = mEffect.Techniques["Modulate_Linear_CM"]; } catch { }
                try { mTechniqueModulate2X_Linear_CM = mEffect.Techniques["Modulate2X_Linear_CM"]; } catch { }
                try { mTechniqueModulate4X_Linear_CM = mEffect.Techniques["Modulate4X_Linear_CM"]; } catch { }
                try { mTechniqueInverseTexture_Linear_CM = mEffect.Techniques["InverseTexture_Linear_CM"]; } catch { }
                try { mTechniqueColor_Linear_CM = mEffect.Techniques["Color_Linear_CM"]; } catch { }
                try { mTechniqueColorTextureAlpha_Linear_CM = mEffect.Techniques["ColorTextureAlpha_Linear_CM"]; } catch { }
                try { mTechniqueInterpolateColor_Linear_CM = mEffect.Techniques["InterpolateColor_Linear_CM"]; } catch { }

                try { mTechniqueTexture_Linear_LN = mEffect.Techniques["Texture_Linear_LN"]; } catch { }
                try { mTechniqueAdd_Linear_LN = mEffect.Techniques["Add_Linear_LN"]; } catch { }
                try { mTechniqueSubtract_Linear_LN = mEffect.Techniques["Subtract_Linear_LN"]; } catch { }
                try { mTechniqueModulate_Linear_LN = mEffect.Techniques["Modulate_Linear_LN"]; } catch { }
                try { mTechniqueModulate2X_Linear_LN = mEffect.Techniques["Modulate2X_Linear_LN"]; } catch { }
                try { mTechniqueModulate4X_Linear_LN = mEffect.Techniques["Modulate4X_Linear_LN"]; } catch { }
                try { mTechniqueInverseTexture_Linear_LN = mEffect.Techniques["InverseTexture_Linear_LN"]; } catch { }
                try { mTechniqueColor_Linear_LN = mEffect.Techniques["Color_Linear_LN"]; } catch { }
                try { mTechniqueColorTextureAlpha_Linear_LN = mEffect.Techniques["ColorTextureAlpha_Linear_LN"]; } catch { }
                try { mTechniqueInterpolateColor_Linear_LN = mEffect.Techniques["InterpolateColor_Linear_LN"]; } catch { }

                try { mTechniqueTexture_Linear_LN_CM = mEffect.Techniques["Texture_Linear_LN_CM"]; } catch { }
                try { mTechniqueAdd_Linear_LN_CM = mEffect.Techniques["Add_Linear_LN_CM"]; } catch { }
                try { mTechniqueSubtract_Linear_LN_CM = mEffect.Techniques["Subtract_Linear_LN_CM"]; } catch { }
                try { mTechniqueModulate_Linear_LN_CM = mEffect.Techniques["Modulate_Linear_LN_CM"]; } catch { }
                try { mTechniqueModulate2X_Linear_LN_CM = mEffect.Techniques["Modulate2X_Linear_LN_CM"]; } catch { }
                try { mTechniqueModulate4X_Linear_LN_CM = mEffect.Techniques["Modulate4X_Linear_LN_CM"]; } catch { }
                try { mTechniqueInverseTexture_Linear_LN_CM = mEffect.Techniques["InverseTexture_Linear_LN_CM"]; } catch { }
                try { mTechniqueColor_Linear_LN_CM = mEffect.Techniques["Color_Linear_LN_CM"]; } catch { }
                try { mTechniqueColorTextureAlpha_Linear_LN_CM = mEffect.Techniques["ColorTextureAlpha_Linear_LN_CM"]; } catch { }
                try { mTechniqueInterpolateColor_Linear_LN_CM = mEffect.Techniques["InterpolateColor_Linear_LN_CM"]; } catch { }
            }
            else
            {
                mEffectHasNewformat = false;

                try { mTechniqueTexture = mEffect.Techniques["Texture"]; } catch { }
                try { mTechniqueAdd = mEffect.Techniques["Add"]; } catch { }
                try { mTechniqueSubtract = mEffect.Techniques["Subtract"]; } catch { }
                try { mTechniqueModulate = mEffect.Techniques["Modulate"]; } catch { }
                try { mTechniqueModulate2X = mEffect.Techniques["Modulate2X"]; } catch { }
                try { mTechniqueModulate4X = mEffect.Techniques["Modulate4X"]; } catch { }
                try { mTechniqueInverseTexture = mEffect.Techniques["InverseTexture"]; } catch { }
                try { mTechniqueColor = mEffect.Techniques["Color"]; } catch { }
                try { mTechniqueColorTextureAlpha = mEffect.Techniques["ColorTextureAlpha"]; } catch { }
                try { mTechniqueInterpolateColor = mEffect.Techniques["InterpolateColor"]; } catch { }
            }
        }
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

        // Shader should be capitalized
        try { Effect = mContentManager.Load<Effect>("Content/Shader"); } catch { }
    }

    static EffectTechnique GetTechniqueVariant(bool useDefaultOrPointFilter, EffectTechnique point, EffectTechnique pointLinearized, EffectTechnique linear, EffectTechnique linearLinearized)
    {
        return useDefaultOrPointFilter ?
            (Renderer.LinearizeTextures ? pointLinearized : point) :
            (Renderer.LinearizeTextures ? linearLinearized : linear);
    }

    public EffectTechnique GetVertexColorTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
    {
        if (mEffect == null)
            throw new Exception("The effect hasn't been set.");

        EffectTechnique technique = null;

        bool useDefaultOrPointFilterInternal;

        if (mEffectHasNewformat)
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
            //    useDefaultOrPointFilterInternal, mTechniqueTexture, mTechniqueTexture_LN, mTechniqueTexture_Linear, mTechniqueTexture_Linear_LN); break;

            //case ColorOperation.Add:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueAdd, mTechniqueAdd_LN, mTechniqueAdd_Linear, mTechniqueAdd_Linear_LN); break;

            //case ColorOperation.Subtract:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueSubtract, mTechniqueSubtract_LN, mTechniqueSubtract_Linear, mTechniqueSubtract_Linear_LN); break;

            case ColorOperation.Modulate:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, mTechniqueModulate, mTechniqueModulate_LN, mTechniqueModulate_Linear, mTechniqueModulate_Linear_LN); break;

            //case ColorOperation.Modulate2X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueModulate2X, mTechniqueModulate2X_LN, mTechniqueModulate2X_Linear, mTechniqueModulate2X_Linear_LN); break;

            //case ColorOperation.Modulate4X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueModulate4X, mTechniqueModulate4X_LN, mTechniqueModulate4X_Linear, mTechniqueModulate4X_Linear_LN); break;

            //case ColorOperation.InverseTexture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueInverseTexture, mTechniqueInverseTexture_LN, mTechniqueInverseTexture_Linear, mTechniqueInverseTexture_Linear_LN); break;

            //case ColorOperation.Color:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueColor, mTechniqueColor_LN, mTechniqueColor_Linear, mTechniqueColor_Linear_LN); break;

            case ColorOperation.ColorTextureAlpha:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, mTechniqueColorTextureAlpha, mTechniqueColorTextureAlpha_LN, mTechniqueColorTextureAlpha_Linear, mTechniqueColorTextureAlpha_Linear_LN); break;

            //case ColorOperation.InterpolateColor:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueInterpolateColor, mTechniqueInterpolateColor_LN, mTechniqueInterpolateColor_Linear, mTechniqueInterpolateColor_Linear_LN); break;

            default: throw new InvalidOperationException();
        }

        return technique;
    }

    public EffectTechnique GetColorModifierTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
    {
        if (mEffect == null)
            throw new Exception("The effect hasn't been set.");

        EffectTechnique technique = null;

        bool useDefaultOrPointFilterInternal;

        if (mEffectHasNewformat)
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
            //    useDefaultOrPointFilterInternal, mTechniqueTexture_CM, mTechniqueTexture_LN_CM, mTechniqueTexture_Linear_CM, mTechniqueTexture_Linear_LN_CM); break;

            //case ColorOperation.Add:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueAdd_CM, mTechniqueAdd_LN_CM, mTechniqueAdd_Linear_CM, mTechniqueAdd_Linear_LN_CM); break;

            //case ColorOperation.Subtract:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueSubtract_CM, mTechniqueSubtract_LN_CM, mTechniqueSubtract_Linear_CM, mTechniqueSubtract_Linear_LN_CM); break;

            case ColorOperation.Modulate:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, mTechniqueModulate_CM, mTechniqueModulate_LN_CM, mTechniqueModulate_Linear_CM, mTechniqueModulate_Linear_LN_CM); break;

            //case ColorOperation.Modulate2X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueModulate2X_CM, mTechniqueModulate2X_LN_CM, mTechniqueModulate2X_Linear_CM, mTechniqueModulate2X_Linear_LN_CM); break;

            //case ColorOperation.Modulate4X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueModulate4X_CM, mTechniqueModulate4X_LN_CM, mTechniqueModulate4X_Linear_CM, mTechniqueModulate4X_Linear_LN_CM); break;

            //case ColorOperation.InverseTexture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueInverseTexture_CM, mTechniqueInverseTexture_LN_CM, mTechniqueInverseTexture_Linear_CM, mTechniqueInverseTexture_Linear_LN_CM); break;

            //case ColorOperation.Color:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueColor_CM, mTechniqueColor_LN_CM, mTechniqueColor_Linear_CM, mTechniqueColor_Linear_LN_CM); break;

            case ColorOperation.ColorTextureAlpha:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, mTechniqueColorTextureAlpha_CM, mTechniqueColorTextureAlpha_LN_CM, mTechniqueColorTextureAlpha_Linear_CM, mTechniqueColorTextureAlpha_Linear_LN_CM); break;

            //case ColorOperation.InterpolateColor:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, mTechniqueInterpolateColor_CM, mTechniqueInterpolateColor_LN_CM, mTechniqueInterpolateColor_Linear_CM, mTechniqueInterpolateColor_Linear_LN_CM); break;

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
