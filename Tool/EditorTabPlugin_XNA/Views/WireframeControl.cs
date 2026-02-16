using EditorTabPlugin_XNA.ViewModels;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Security.Policy;
using System.Windows.Forms;
using XnaAndWinforms;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;
using WinCursor = System.Windows.Forms.Cursor;

namespace Gum.Plugins.InternalPlugins.EditorTab.Views;


public class WireframeControl : GraphicsDeviceControl
{
    #region Fields

    private IHotkeyManager _hotkeyManager;
    private IProjectManager _projectManager;
    private SelectionManager _selectionManager;
    private DragDropManager _dragDropManager;
    LineRectangle mCanvasBounds;

    public Color ScreenBoundsColor = Color.LightBlue;

    bool mHasInitialized = false;

    public Ruler TopRuler { get; private set; }
    public Ruler LeftRuler { get; private set; }

    public event Action? CameraChanged;

    bool mouseHasEntered = false;


    public bool CanvasBoundsVisible
    {
        get => mCanvasBounds.Visible;
        set => mCanvasBounds.Visible = value;
    }

    public bool RulersVisible
    {
        get => LeftRuler.Visible;
        set
        {
            LeftRuler.Visible = value;
            TopRuler.Visible = value;
        }
    }

    public SystemManagers SystemManagers => SystemManagers.Default;

    #endregion

    #region Properties

    public Microsoft.Xna.Framework.Color BackgroundColor { get; set; } = new(75, 75, 75);

    public LineRectangle ScreenBounds
    {
        get { return mCanvasBounds; }
    }

    new InputLibrary.Cursor Cursor
    {
        get
        {
            return InputLibrary.Cursor.Self;
        }
    }

    Camera Camera
    {
        get { return Renderer.Self.Camera; }
    }

    #endregion

    public event EventHandler AfterXnaInitialize;

    #region Event Methods


    void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        _hotkeyManager.HandleEditorKeyDown(e);
        _cameraController.HandleKeyPress(e);
    }

    private void HandleKeyUp(object? sender, KeyEventArgs e)
    {
        _hotkeyManager.HandleKeyUpWireframe(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        bool handled = _hotkeyManager.ProcessCmdKeyWireframe(ref msg, keyData);

        if (handled)
        {
            return true;
        }
        else
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }


    #endregion

    #region Initialize Methods

    public void Initialize(
        Panel wireframeParentPanel,
        IHotkeyManager hotkeyManager,
        SelectionManager selectionManager,
        DragDropManager dragDropManager,
        EditorViewModel editorViewModel,
        IProjectManager projectManager)
    {
        _selectionManager = selectionManager;
        _dragDropManager = dragDropManager;
        _hotkeyManager = hotkeyManager;
        _projectManager = projectManager;
        try
        {
            LoaderManager.Self.ContentLoader = new ContentLoader();

            SystemManagers.Default = new SystemManagers();
            SystemManagers.Default.Initialize(GraphicsDevice);

            InitializeDefaultTypeInstantiation();

            ToolFontService.Self.Initialize();
            ToolLayerService.Self.Initialize();

            Renderer.TextureFilter = TextureFilter.Point;

            _cameraController = new CameraController();

            LoaderManager.Self.Initialize(null, "content/TestFont.fnt", Services, null);
            _cameraController.Initialize(Camera, editorViewModel, Width, Height, hotkeyManager);
            _cameraController.CameraChanged += () => CameraChanged?.Invoke();

            InputLibrary.Cursor.Self.Initialize(this);

            mCanvasBounds = new LineRectangle();
            mCanvasBounds.IsDotted = true;
            mCanvasBounds.Name = "Gum Screen Bounds";
            mCanvasBounds.Width = 800;
            mCanvasBounds.Height = 600;
            mCanvasBounds.Color = ScreenBoundsColor;


            var camera = SystemManagers.Default.Renderer.Camera;
            camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

            KeyDown += HandleKeyDown;
            KeyUp += HandleKeyUp;
            MouseDown += _cameraController.HandleMouseDown;
            MouseMove += _cameraController.HandleMouseMove;
            MouseWheel += _cameraController.HandleMouseWheel;

            MouseEnter += (not, used) =>
            {
                mouseHasEntered = true;
            };
            MouseLeave += (not, used) =>
            {
                mouseHasEntered = false;
            };

            if (AfterXnaInitialize != null)
            {
                AfterXnaInitialize(this, null);
            }

            editorViewModel.RefreshCanvasSize();

            UpdateCanvasBoundsToProject();

            mHasInitialized = true;

        }
        catch (Exception exception)
        {
            Locator.GetRequiredService<IDialogService>().ShowMessage("Error initializing the wireframe control\n\n" + exception);
        }
    }


    private void InitializeDefaultTypeInstantiation()
    {
        ElementSaveExtensions.RegisterGueInstantiation(
            "Text",
            () =>
            {
                // Set this to false to make Text instantiation faster - we always set defaults explicitly
                TextRuntime.AssignFontInConstructor = false;
                return new TextRuntime(systemManagers: this.SystemManagers);
            });

        ElementSaveExtensions.RegisterGueInstantiation(
            "Sprite",
            () => new SpriteRuntime());
    }

    public void ShareLayerReferences(LayerService layerService)
    {

        ShapeManager.Self.Add(mCanvasBounds, layerService.OverlayLayer);


        TopRuler = new Ruler(this, 
            SystemManagers.Default,
            InputLibrary.Cursor.Self,
            ToolFontService.Self,
            ToolLayerService.Self,
            layerService,
            _hotkeyManager);
        LeftRuler = new Ruler(this, SystemManagers.Default,
            InputLibrary.Cursor.Self,
            ToolFontService.Self,
            ToolLayerService.Self,
            layerService,
            _hotkeyManager);
        LeftRuler.RulerSide = RulerSide.Left;

    }

    #endregion

    bool isInActivity = false;
    private CameraController _cameraController;

    void Activity()
    {
        if (!isInActivity)
        {
            isInActivity = true;
#if DEBUG
            try
#endif
            {
                InputLibrary.Cursor.Self.StartCursorSettingFrameStart();
                TimeManager.Self.Activity();

                SpriteManager.Self.Activity(TimeManager.Self.CurrentTime);


                InputLibrary.Cursor.Self.Activity(TimeManager.Self.CurrentTime);

                // This doesn't work, I think it might be because the Window isn't reading keys unless
                // it is focused...
                //if(InputLibrary.Keyboard.Self.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Escape))
                //{

                //}
                bool isOver = TopRuler.HandleXnaUpdate(InputLibrary.Cursor.Self.IsInWindow) ||
                    LeftRuler.HandleXnaUpdate(InputLibrary.Cursor.Self.IsInWindow);


                // But we want the selection to update the handles to the selected object
                // after editing is done.  SelectionManager.LateActivity lets us do that.  LateActivity must
                // come after EditingManager.Activity.

                // Update 1/15/2019
                // When the user uses scroll bars we get selection to underlying objects.
                // We don't want that, so we'll check if the mouse has entered the control.
                // I may have to update this at some point to force deselection if the mouse
                // has not entered so things don't stay highlighted when exiting the control
                // Update 2 - yea, we def need to pass in mouseHasEntered == false to force no highlight

                if (TopRuler.IsCursorOver == false && LeftRuler.IsCursorOver == false)
                {
                    var shouldForceNoHighlight = mouseHasEntered == false &&
                        PluginManager.Self.GetIfShouldSuppressRemoveEditorHighlight() == false;


                    _selectionManager.Activity(shouldForceNoHighlight);

                    _selectionManager.LateActivity(this.SystemManagers);
                }

                InputLibrary.Cursor.Self.EndCursorSettingFrameStart();
            }
#if DEBUG
            catch (Exception e)
            {
                Locator.GetRequiredService<IDialogService>().ShowMessage(e.ToString());
            }
#endif
        }

        isInActivity = false;
    }

    /// <summary>
    /// Updates the wireframe to match the project settings - specifically the canvas width/height
    /// </summary>
    public void UpdateCanvasBoundsToProject()
    {

        var gumProject = _projectManager.GumProjectSave;
        if (mCanvasBounds != null && gumProject != null)
        {
            mCanvasBounds.Width = GraphicalUiElement.CanvasWidth;
            mCanvasBounds.Height = GraphicalUiElement.CanvasHeight;

            CanvasBoundsVisible = gumProject.ShowCanvasOutline;
            RulersVisible = gumProject.ShowRuler;
        }
    }

    protected override void PreDrawUpdate()
    {
        if (mHasInitialized)
        {
            Activity();
        }
    }

    protected override void Draw()
    {
        if (mHasInitialized)
        {
            GraphicsDevice.Clear(BackgroundColor);

            PluginManager.Self.BeforeRender();

            Renderer.Self.Draw((SystemManagers)null);

            PluginManager.Self.AfterRender();

        }
    }

    internal void SetGuideColors(Color guidelineColor, Color guideTextColor)
    {
        TopRuler.SetGuideColors(guidelineColor, guideTextColor);
        LeftRuler.SetGuideColors(guidelineColor, guideTextColor);
    }
}
