using System;
using System.Collections.Generic;
using System.Linq;
using XnaAndWinforms;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using FlatRedBall.SpecializedXnaControls.RegionSelection;
using RenderingLibrary;
using FlatRedBall.SpecializedXnaControls.Input;
using RenderingLibrary.Content;
using ToolsUtilities;
using System.Reflection;
using System.Windows.Forms;
using RenderingLibrary.Math;
using InputLibrary;
using ToolsUtilitiesStandard.Helpers;
using System.ComponentModel.Design;

namespace FlatRedBall.SpecializedXnaControls;

public enum ZoomDirection
{
    // Zoom in, making everything bigger
    ZoomIn,
    // Zoom out, making everything smaller
    ZoomOut
}

public class ImageRegionSelectionControl : GraphicsDeviceControl
{
    #region Fields

    ImageData maxAlphaImageData;

    Texture2D mCurrentTexture;
    Texture2D maxAlphaTexture;

    bool mRoundRectangleSelectorToUnit = true;
    List<RectangleSelector> mRectangleSelectors = new List<RectangleSelector>();

    CameraPanningLogic mCameraPanningLogic;

    InputLibrary.Cursor mCursor;
    InputLibrary.Keyboard mKeyboard;
    SystemManagers mManagers;

    TimeManager mTimeManager;

    Sprite mCurrentTextureSprite;

    public Zooming.ZoomNumbers ZoomNumbers
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
                    this.RefreshDisplay();
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
    public new event EventHandler? RegionChanged;
    public event EventHandler? EndRegionChanged;

    public event EventHandler? MouseWheelZoom;
    public event Action? Panning;
    #endregion

    #region Methods

    protected override void Initialize(IServiceProvider services)
    {
        CustomInitialize(services);

        base.Initialize(services);

    }

    public void DisableHotkeyPanning()
    {
        mCameraPanningLogic.IsHotkeyPanningEnabled = false;
    }

    public void CreateDefaultZoomLevels()
    {

    }

    public void CustomInitialize(IServiceProvider services)
    {
        if (!DesignMode)
        {
            mTimeManager = new TimeManager();


            mManagers = new SystemManagers();
            var contentLoader = new ContentLoader();
            // create one here since we need one anyway:
            contentLoader.XnaContentManager = new Microsoft.Xna.Framework.Content.ContentManager(services);
            mManagers.Initialize(GraphicsDevice, fullInstantiation:false, contentLoader:contentLoader);
            mManagers.Name = "Image Region Selection";
            Assembly assembly = Assembly.GetAssembly(typeof(GraphicsDeviceControl));// Assembly.GetCallingAssembly();

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



            contentLoader.SystemManagers = mManagers;

            LoaderManager.Self.ContentLoader = contentLoader;
            LoaderManager.Self.Initialize("Content/InvalidTexture.png", targetFntFileName.FullPath, Services, mManagers);

            CreateNewSelector();

            mCursor = new InputLibrary.Cursor();
            mCursor.Initialize(this);

            mKeyboard = new InputLibrary.Keyboard();
            mKeyboard.Initialize(this);

            mCameraPanningLogic = new CameraPanningLogic(this, mManagers, mCursor, mKeyboard);
            var camera = mManagers.Renderer.Camera;
            camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
            mCameraPanningLogic.Panning += HandlePanning;



            MouseWheel += new MouseEventHandler(HandleMouseWheel);
            ZoomNumbers = new Zooming.ZoomNumbers();
        }
    }

    private RegionSelection.RectangleSelector CreateNewSelector()
    {
        var newSelector = new RectangleSelector(mManagers);
        newSelector.AddToManagers(mManagers);
        newSelector.Visible = false;
        newSelector.StartRegionChanged += HandleStartRegionChanged;
        newSelector.RegionChanged += new EventHandler(RegionChangedInternal);
        newSelector.EndRegionChanged += EndRegionChangedInternal;
        newSelector.SnappingGridSize = snappingGridSize;
        newSelector.RoundToUnitCoordinates = mRoundRectangleSelectorToUnit;

        mRectangleSelectors.Add(newSelector);

        return newSelector;
    }

    private void HandlePanning()
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

        if (Panning != null)
        {
            Panning();
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
        mTimeManager.Activity();

        mCursor.Activity(mTimeManager.CurrentTime);
        mKeyboard.Activity();


        foreach (var item in mRectangleSelectors)
        {
            item.Activity(mCursor, mKeyboard, this);
        }
    }

    protected override void Draw()
    {
        this.PerformActivity();

        // Plugins should be removing textures if they are null, but a texture may become null and a plugin
        // may not react in time. Therefore we should draw only if the textur is not disposed
        var isDisposed = this.CurrentTexture?.IsDisposed;
        if(isDisposed == false)
        {
            base.Draw();
            mManagers.Renderer.Draw(mManagers);
        }
    }

    void HandleMouseWheel(object? sender, MouseEventArgs e)
    {
        if (mAvailableZoomLevels != null)
        {
            if (ZoomIndex != -1)
            {
                float value = e.Delta;


                ZoomDirection? zoomDirection = null;

                if (value < 0)
                {
                    zoomDirection = ZoomDirection.ZoomOut;
                }
                else if (value > 0)
                {
                    zoomDirection = ZoomDirection.ZoomIn;
                }

                if(zoomDirection != null)
                {
                    HandleZoom(zoomDirection.Value, true);
                }
            }
        }
    }

    public void HandleZoom(ZoomDirection zoomDirection, bool considerCursor)
    {
        bool didZoom = false;
        float oldZoom = ZoomValue / 100.0f;
        int index = ZoomIndex;

        float worldX = mCursor.GetWorldX(mManagers);
        float worldY = mCursor.GetWorldY(mManagers);

        if (!considerCursor)
        {
            worldX = Camera.X + Camera.ClientWidth / (2 * Camera.Zoom);
            worldY = Camera.Y + Camera.ClientHeight / (2 * Camera.Zoom);
        }

        if (zoomDirection == ZoomDirection.ZoomIn && index > 0)
        {
            ZoomValue = mAvailableZoomLevels[index - 1];

            didZoom = true;
        }
        else if (zoomDirection == ZoomDirection.ZoomOut && index < mAvailableZoomLevels.Count - 1)
        {
            ZoomValue = mAvailableZoomLevels[index + 1];

            didZoom = true;
        }


        if (didZoom)
        {
            float oldCameraX = Camera.X;
            float oldCameraY = Camera.Y;

            AdjustCameraPositionAfterZoom(worldX, worldY,
                oldCameraX, oldCameraY, oldZoom, ZoomValue, Camera);

            if (MouseWheelZoom != null)
            {
                MouseWheelZoom(this, null);
            }
        }
    }

    public static void AdjustCameraPositionAfterZoom(float oldCursorWorldX, float oldCursorWorldY, 
        float oldCameraX, float oldCameraY, float oldZoom, float newZoom, Camera camera)
    {
        float differenceX = oldCameraX - oldCursorWorldX;
        float differenceY = oldCameraY - oldCursorWorldY;

        float zoomAsFloat = newZoom / 100.0f;

        float modifiedDifferenceX = differenceX * oldZoom / zoomAsFloat;
        float modifiedDifferenceY = differenceY * oldZoom / zoomAsFloat;

        camera.X = oldCursorWorldX + modifiedDifferenceX;
        camera.Y = oldCursorWorldY + modifiedDifferenceY;

        // This makes the zooming behavior feel weird.  We'll do this when the user selects a new 
        // AnimationChain, but not when zooming.
        //BringSpriteInView();
    }

    public void BringSpriteInView()
    {
        if (mCurrentTexture != null)
        {
            const float pixelBorder = 10;

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
