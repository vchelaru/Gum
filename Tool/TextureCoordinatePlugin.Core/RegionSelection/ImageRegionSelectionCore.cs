using System;
using System.Collections.Generic;
using System.Linq;
using XnaAndWinforms;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using FlatRedBall.SpecializedXnaControls;
using FlatRedBall.SpecializedXnaControls.Zooming;
using RenderingLibrary;
using RenderingLibrary.Content;
using ToolsUtilities;
using System.Reflection;
using RenderingLibrary.Math;
using InputLibrary;
using ToolsUtilitiesStandard.Helpers;
using Gum.Services;

namespace TextureCoordinateSelectionPlugin.RegionSelection;

public enum ZoomDirection
{
    // Zoom in, making everything bigger
    ZoomIn,
    // Zoom out, making everything smaller
    ZoomOut
}

/// <summary>
/// The texture-coordinate editing canvas without a UI framework: a texture with draggable
/// <see cref="RectangleSelector"/> regions over it and zoom levels. A head's control
/// (<c>ImageRegionSelectionControl</c> on WPF, the Avalonia canvas control) owns one of these,
/// forwards its frames, and implements <see cref="ICanvasHost"/>. Camera panning and wheel zoom
/// are not handled here: the display controller drives the shared <c>CameraController</c> from the
/// head's mouse and key events and sets <see cref="IsCameraPanning"/> while a drag is in progress.
/// </summary>
public class ImageRegionSelectionCore
{
    #region Fields

    ImageData maxAlphaImageData;

    IInputHostControl mInputHost;

    Texture2D mCurrentTexture;
    Texture2D maxAlphaTexture;

    bool mRoundRectangleSelectorToUnit = true;
    List<RectangleSelector> mRectangleSelectors = new List<RectangleSelector>();

    InputLibrary.Cursor mCursor;
    InputLibrary.Keyboard mKeyboard;
    SystemManagers mManagers;

    TimeManager mTimeManager;

    Sprite mCurrentTextureSprite;

    public ZoomNumbers ZoomNumbers
    {
        get;
        private set;
    }

    IList<int> mAvailableZoomLevels;

    bool showFullAlpha;

    #endregion

    #region Properties

    public bool RoundRectangleSelectorToUnit
    {
        get { return mRoundRectangleSelectorToUnit; }
        set
        {
            mRoundRectangleSelectorToUnit = value;

            foreach (var item in mRectangleSelectors)
            {
                item.RoundToUnitCoordinates = mRoundRectangleSelectorToUnit;
            }
        }
    }

    int? snappingGridSize;
    public int? SnappingGridSize
    {
        get
        {
            return snappingGridSize;
        }
        set
        {
            snappingGridSize = value;
            foreach (var item in mRectangleSelectors)
            {
                item.SnappingGridSize = snappingGridSize;
            }
        }
    }

    Camera Camera
    {
        get
        {
            return mManagers.Renderer.Camera;
        }
    }

    public SystemManagers SystemManagers
    {
        get { return mManagers; }
    }

    public RectangleSelector RectangleSelector
    {
        get 
        {
            if (mRectangleSelectors.Count != 0)
            {
                return mRectangleSelectors[0];
            }
            else
            {
                return null;
            }
        }
    }

    public List<RectangleSelector> RectangleSelectors
    {
        get
        {
            return mRectangleSelectors;
        }
    }

    public Texture2D CurrentTexture
    {
        get { return mCurrentTexture; }
        set
        {
            bool didChange = mCurrentTexture != value;

            if (didChange)
            {
                mCurrentTexture = value;
                if (mManagers != null)
                {
                    bool hasCreateVisuals = mCurrentTextureSprite != null;

                    if (!hasCreateVisuals)
                    {
                        CreateVisuals();
                    }
                    if (mCurrentTexture == null)
                    {
                        mCurrentTextureSprite.Visible = false;
                    }
                    else
                    {
                        CreateMaxAlphaTexture();
                        mCurrentTextureSprite.Visible = true;
                        if (showFullAlpha)
                        {
                            mCurrentTextureSprite.Texture = maxAlphaTexture;
                        }
                        else
                        {
                            mCurrentTextureSprite.Texture = mCurrentTexture;
                        }
                        mCurrentTextureSprite.Width = mCurrentTexture.Width;
                        mCurrentTextureSprite.Height = mCurrentTexture.Height;

                    }
                    // No explicit redraw request - the WPF host renders continuously off
                    // CompositionTarget.Rendering, so the new texture shows on the next frame.
                }
            }
        }
    }

    private void CreateMaxAlphaTexture()
    {
        if (maxAlphaImageData == null)
        {
            maxAlphaImageData = new ImageData(mCurrentTexture.Width, mCurrentTexture.Height, mManagers);
            maxAlphaImageData.CopyFrom(mCurrentTexture);

            MaximizeAlpha();

            maxAlphaTexture = maxAlphaImageData.ToTexture2D(generateMipmaps: false);
        }
        else
        {
            bool showingBiggerTexture = mCurrentTexture.Width > maxAlphaImageData.Width || mCurrentTexture.Height > maxAlphaImageData.Height;
            if (showingBiggerTexture)
            {
                maxAlphaImageData = new ImageData(mCurrentTexture.Width, mCurrentTexture.Height, mManagers);
            }

            maxAlphaImageData.CopyFrom(mCurrentTexture);

            MaximizeAlpha();

            if (showingBiggerTexture)
            {
                if (maxAlphaTexture != null)
                {
                    maxAlphaTexture.Dispose();
                }
                maxAlphaTexture = maxAlphaImageData.ToTexture2D(generateMipmaps: false);
            }
            else
            {
                maxAlphaImageData.ToTexture2D(maxAlphaTexture);
            }

        }
    }

    private void MaximizeAlpha()
    {
        for (int i = 0; i < maxAlphaImageData.Data.Length; i++)
        {
            if (maxAlphaImageData.Data[i].A > 0)
            {
                maxAlphaImageData.Data[i].A = 255;
            }
        }
    }

    private void CreateVisuals()
    {
        mCurrentTextureSprite = new Sprite(mCurrentTexture);
        mCurrentTextureSprite.Name = "Image Region Selection Main Sprite";
        mManagers.SpriteManager.Add(mCurrentTextureSprite);
    }

    public InputLibrary.Cursor XnaCursor
    {
        get { return mCursor; }
    }

    public bool SelectorVisible
    {
        get
        {
            return mRectangleSelectors.Count != 0 && mRectangleSelectors[0].Visible;
        }
        set
        {
            // This causes problems in VS designer mode.
            if (mRectangleSelectors != null)
            {
                foreach (var selector in mRectangleSelectors)
                {
                    selector.Visible = value;
                }
            }
        }
    }

    /// <summary>
    /// A zoom value in percent, where 100 means 100% zoom (native scale)
    /// </summary>
    public int ZoomValue
    {
        get
        {
            return MathFunctions.RoundToInt(mManagers.Renderer.Camera.Zoom * 100);
        }
        set
        {
            if (mManagers != null && mManagers.Renderer != null)
            {
                mManagers.Renderer.Camera.Zoom = value / 100.0f;
            }
        }
    }

    /// <summary>
    /// Sets the available zoom levels, where 100 is 100. These values must be set for zooming to be enabled.
    /// </summary>
    public IList<int> AvailableZoomLevels
    {
        get => mAvailableZoomLevels;
        set => mAvailableZoomLevels = value;
    }

    public int ZoomIndex
    {
        get
        {
            if (mAvailableZoomLevels != null)
            {
                return mAvailableZoomLevels.IndexOf(ZoomValue);
            }
            return -1;
        }
    }

    /// <summary>
    /// Creates and destroys the internal rectangle selectors to match the desired count.
    /// </summary>
    public int DesiredSelectorCount
    {
        set
        {
            while (value > this.mRectangleSelectors.Count)
            {
                CreateNewSelector();
            }

            while (value < this.mRectangleSelectors.Count)
            {
                var selector = mRectangleSelectors.Last();

                selector.RemoveFromManagers();
                mRectangleSelectors.RemoveAt(mRectangleSelectors.Count - 1);
            }
        }
    }

    bool canChangeX = true;
    public bool CanChangeX
    {
        get => canChangeX;
        set
        {
            canChangeX = value;
            foreach (var item in mRectangleSelectors)
            {
                item.CanChangeX = canChangeX;
            }
        }
    }

    bool canChangeY = true;
    public bool CanChangeY
    {
        get => canChangeY;
        set
        {
            canChangeY = value;
            foreach (var item in mRectangleSelectors)
            {
                item.CanChangeY = canChangeY;
            }
        }
    }

    bool canChangeWidth = true;
    public bool CanChangeWidth
    {
        get => canChangeWidth;
        set
        {
            canChangeWidth = value;
            foreach (var item in mRectangleSelectors)
            {
                item.CanChangeWidth = canChangeWidth;
            }
        }
    }

    bool canChangeHeight = true;
    public bool CanChangeHeight
    {
        get => canChangeHeight;
        set
        {
            canChangeHeight = value;
            foreach (var item in mRectangleSelectors)
            {
                item.CanChangeHeight = canChangeHeight;
            }
        }
    }

    /// <summary>
    /// Whether the camera is being dragged. While true the rectangle selectors skip their input,
    /// so a Space+left-drag pans without also dragging a region's handle.
    /// </summary>
    public bool IsCameraPanning
    {
        get;
        set;
    }

    public bool ShowFullAlpha
    {
        get
        {
            return showFullAlpha;
        }
        set
        {
            showFullAlpha = value;

            if(mCurrentTextureSprite != null)
            {
                if (showFullAlpha)
                {
                    mCurrentTextureSprite.Texture = maxAlphaTexture;
                }
                else
                {
                    mCurrentTextureSprite.Texture = mCurrentTexture;
                }
            }
        }
    }

    #endregion

    #region Events

    public event EventHandler? StartRegionChanged;
    public event EventHandler? RegionChanged;
    public event EventHandler? EndRegionChanged;

    /// <summary>
    /// Raised when the canvas is double-clicked. WPF panels have no built-in double-click event,
    /// so this stands in for the WinForms <c>Control.DoubleClick</c> the control used to expose.
    /// </summary>
    public event EventHandler? DoubleClick;

    /// <summary>
    /// Raised when <see cref="DisplayScale"/> changes, such as when the window moves to a monitor
    /// with a different scale.
    /// </summary>
    public event Action? DisplayScaleChanged;
    #endregion

    #region Methods

    private readonly ICanvasHost _host;

    /// <summary>
    /// The host's OS display scale (1 at 100%), read every frame. The selectors size their strokes
    /// and handles by it.
    /// </summary>
    public float DisplayScale => _displayScale.DisplayScale;

    private readonly CanvasDisplayScale _displayScale = new CanvasDisplayScale();

    /// <summary>Creates the canvas over its host control and initializes rendering.</summary>
    public ImageRegionSelectionCore(ICanvasHost host)
    {
        _host = host;
        CustomInitialize();
    }

    public void CreateDefaultZoomLevels()
    {

    }

    public void CustomInitialize()
    {
        {
            mTimeManager = new TimeManager();


            // Route the GPU device/content-service lookup through IRenderDeviceHost rather than
            // reading GraphicsDevice/Services directly off this control, so the initialization
            // sequence below only depends on the render-host contract, not on a concrete control type.
            IRenderDeviceHost renderHost = _host.RenderDeviceHost;

            mManagers = new SystemManagers();
            mManagers.Initialize(renderHost.GraphicsDevice);
            mManagers.Name = "Image Region Selection";
            // The default font is an embedded resource of XnaAndWinforms, so resolve that assembly
            // through one of its types.
            Assembly assembly = typeof(RenderTargetFrameLoop).Assembly;

            FilePath targetFntFileName = FileManager.UserApplicationDataForThisApplication + "Font18Arial.fnt";
            FilePath targetPngFileName = FileManager.UserApplicationDataForThisApplication + "Font18Arial_0.png";

            if(!targetFntFileName.Exists())
            {
                try
                {
                    FileManager.SaveEmbeddedResource(
                        assembly,
                        "XnaAndWinforms.Content.Font18Arial.fnt",
                        targetFntFileName.FullPath);
                }
                catch(System.IO.IOException)
                {
                    // could be busy with another instance of Gum writing to this
                }
            }

            if(!targetPngFileName.Exists())
            {
                try
                {
                    FileManager.SaveEmbeddedResource(
                        assembly,
                        "XnaAndWinforms.Content.Font18Arial_0.png",
                        targetPngFileName.FullPath);
                }
                catch (System.IO.IOException)
                {
                    // could be busy with another instance of Gum writing to this
                }
            }



            var contentLoader = new ContentLoader();
            contentLoader.SystemManagers = mManagers;

            LoaderManager.Self.ContentLoader = contentLoader;
            LoaderManager.Self.Initialize("Content/InvalidTexture.png", targetFntFileName.FullPath, renderHost.Services, mManagers);

            CreateNewSelector();

            mInputHost = _host.InputHost;

            mCursor = new InputLibrary.Cursor();
            mCursor.Initialize(mInputHost);

            mKeyboard = new InputLibrary.Keyboard();
            mKeyboard.Initialize(mInputHost);

            var camera = mManagers.Renderer.Camera;
            camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
            ZoomNumbers = new ZoomNumbers();
        }
    }

    /// <summary>The host calls this on a double click over the canvas.</summary>
    public void RaiseDoubleClick() => DoubleClick?.Invoke(this, EventArgs.Empty);

    private RectangleSelector CreateNewSelector()
    {
        var newSelector = new RectangleSelector(mManagers, _displayScale);
        newSelector.AddToManagers(mManagers);
        newSelector.Visible = false;
        newSelector.StartRegionChanged += HandleStartRegionChanged;
        newSelector.RegionChanged += new EventHandler(RegionChangedInternal);
        newSelector.EndRegionChanged += EndRegionChangedInternal;
        newSelector.SnappingGridSize = snappingGridSize;
        newSelector.RoundToUnitCoordinates = mRoundRectangleSelectorToUnit;
        newSelector.CanChangeX = canChangeX;
        newSelector.CanChangeY = canChangeY;
        newSelector.CanChangeWidth = canChangeWidth;
        newSelector.CanChangeHeight = canChangeHeight;

        mRectangleSelectors.Add(newSelector);

        return newSelector;
    }

    /// <summary>
    /// Keeps the camera within half a screen of the texture's edges. The display controller calls
    /// this after every camera move so a pan can't scroll the texture out of view.
    /// </summary>
    public void ClampCameraToTexture()
    {
        var cameraWidth = this.Camera.ClientWidth / this.Camera.Zoom;
        var cameraHeight = this.Camera.ClientHeight / this.Camera.Zoom;

        this.Camera.X = Math.Max(Camera.X, -cameraWidth / 2.0f);
        this.Camera.Y = Math.Max(Camera.Y, -cameraHeight / 2.0f);

        if(CurrentTexture != null)
        {
            this.Camera.X = Math.Min(Camera.X, CurrentTexture.Width + -cameraWidth / 2f);
            this.Camera.Y = Math.Min(Camera.Y, CurrentTexture.Height + -cameraHeight / 2f);
        }
    }

    private void HandleStartRegionChanged(object? sender, EventArgs e)
    {
        StartRegionChanged?.Invoke(this, null);
    }

    void RegionChangedInternal(object? sender, EventArgs e)
    {
        RegionChanged?.Invoke(this, null);
    }

    void EndRegionChangedInternal(object? sender, EventArgs e)
    {
        EndRegionChanged?.Invoke(this, null);
    }

    void PerformActivity()
    {
        UpdateDisplayScale();

        mTimeManager.Activity();

        mCursor.Activity(mTimeManager.CurrentTime);
        mKeyboard.Activity();


        if (!IsCameraPanning)
        {
            foreach (var item in mRectangleSelectors)
            {
                item.Activity(mCursor, mKeyboard, mInputHost);
            }
        }
    }

    // Read every frame so the overlay follows the window to a monitor with a different scale.
    private void UpdateDisplayScale()
    {
        float hostScale = (float)_host.DisplayScale;
        if (hostScale == DisplayScale)
        {
            return;
        }

        _displayScale.DisplayScale = hostScale;
        foreach (var selector in mRectangleSelectors)
        {
            selector.RefreshDisplayScale();
        }
        DisplayScaleChanged?.Invoke();
    }

    /// <summary>The host calls this with the render target bound and cleared.</summary>
    public void Draw()
    {
        this.PerformActivity();

        // Plugins should be removing textures if they are null, but a texture may become null and a plugin
        // may not react in time. Therefore we should draw only if the textur is not disposed
        var isDisposed = this.CurrentTexture?.IsDisposed;
        if(isDisposed == false)
        {
            mManagers.Renderer.Draw(mManagers);
        }
    }

    /// <summary>
    /// Steps to the next zoom level in <paramref name="zoomDirection"/>, keeping the world point at
    /// the center of the view fixed. Does nothing at the end of the level list.
    /// </summary>
    public void HandleZoom(ZoomDirection zoomDirection)
    {
        float oldZoom = ZoomValue / 100.0f;
        int index = ZoomIndex;

        float centerWorldX = Camera.X + Camera.ClientWidth / (2 * Camera.Zoom);
        float centerWorldY = Camera.Y + Camera.ClientHeight / (2 * Camera.Zoom);

        bool didZoom = false;
        if (zoomDirection == ZoomDirection.ZoomIn && index > 0)
        {
            ZoomValue = mAvailableZoomLevels[index - 1];
            didZoom = true;
        }
        else if (zoomDirection == ZoomDirection.ZoomOut && index != -1 && index < mAvailableZoomLevels.Count - 1)
        {
            ZoomValue = mAvailableZoomLevels[index + 1];
            didZoom = true;
        }

        if (didZoom)
        {
            float differenceX = Camera.X - centerWorldX;
            float differenceY = Camera.Y - centerWorldY;

            Camera.X = centerWorldX + differenceX * oldZoom / Camera.Zoom;
            Camera.Y = centerWorldY + differenceY * oldZoom / Camera.Zoom;
        }
    }

    public void BringSpriteInView()
    {
        if (mCurrentTexture != null)
        {

            bool isAbove = mCurrentTextureSprite.Y + mCurrentTexture.Height < Camera.AbsoluteTop;
            bool isBelow = mCurrentTextureSprite.Y > Camera.AbsoluteBottom;

            bool isLeft = mCurrentTextureSprite.X + mCurrentTexture.Width < Camera.AbsoluteLeft;
            bool isRight = mCurrentTextureSprite.X> Camera.AbsoluteRight;

            // If it's both above and below, that means the user has zoomed in a lot so that the Sprite is bigger than
            // the camera view.  
            // If it's neither, then the entire Sprite is in view.
            // If it's only one or the other, that means that part of the Sprite is hanging off the edge, and we can adjust.
            bool adjustY = (isAbove || isBelow) && !(isAbove && isBelow);
            bool adjustX = (isLeft || isRight) && !(isLeft && isRight);

            if (adjustY)
            {
                bool isTallerThanCamera = mCurrentTexture.Height * Camera.Zoom > Camera.ClientHeight;

                if ((isTallerThanCamera && isAbove) || (!isTallerThanCamera && isBelow))
                {
                    // Move Camera so Sprite is on bottom
                    Camera.Y = mCurrentTextureSprite.Y + mCurrentTexture.Height / Camera.Zoom;
                }
                else
                {
                    // Move Camera so Sprite is on top
                    Camera.Y = mCurrentTextureSprite.Y / Camera.Zoom;
                }
            }

            if (adjustX)
            {
                bool isWiderThanCamera = mCurrentTexture.Width * Camera.Zoom > Camera.ClientWidth;

                if ((isWiderThanCamera && isLeft) || (!isWiderThanCamera && isRight))
                {
                    Camera.X = mCurrentTextureSprite.X + mCurrentTexture.Width / Camera.Zoom;
                }
                else
                {
                    Camera.X = mCurrentTextureSprite.X / Camera.Zoom;
                }
            }
        }
    }

    #endregion

}
