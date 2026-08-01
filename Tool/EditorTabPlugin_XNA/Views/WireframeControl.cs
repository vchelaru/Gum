using EditorTabPlugin_XNA.ViewModels;
using Gum.Input;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Windows.Input;
using XnaAndWinforms;
using Color = System.Drawing.Color;

namespace Gum.Plugins.InternalPlugins.EditorTab.Views;


public class WireframeControl : WpfGraphicsDeviceControl
{
    #region Fields

    private readonly IDialogService _dialogService;
    private readonly IOutputManager _outputManager;
    private readonly IPluginManager _pluginManager;

    private IHotkeyManager _hotkeyManager;
    private IProjectManager _projectManager;
    private SelectionManager _selectionManager;
    private IDragDropManager _dragDropManager;
    private IToolFontService _toolFontService;
    private IToolLayerService _toolLayerService;
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

    public WireframeControl(IDialogService dialogService, IOutputManager outputManager, IPluginManager pluginManager)
    {
        _dialogService = dialogService;
        _outputManager = outputManager;
        _pluginManager = pluginManager;

        // Ctrl+= / Ctrl+- zoom this canvas's camera, not the app-wide font size.
        CameraZoomScope.SetOwnsCameraZoom(this, true);
    }

    #region Properties

    public Microsoft.Xna.Framework.Color BackgroundColor { get; set; } = new(75, 75, 75);

    public LineRectangle ScreenBounds
    {
        get { return mCanvasBounds; }
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
        GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
        _hotkeyManager.HandleEditorKeyDown(keyArgs);
        // WPF has no SuppressKeyPress equivalent; marking the event handled stops both the key
        // event and the text input it would otherwise produce.
        e.Handled = keyArgs.Handled || keyArgs.SuppressKeyPress;
        _cameraController.HandleKeyPress(keyArgs);
    }

    // Focus is taken by WpfGraphicsDeviceControl.OnMouseDown, which runs before this.
    void HandleMouseDown(object? sender, MouseButtonEventArgs e) =>
        _cameraController.HandleMouseDown(e.ToGumMouseEventArgs(this));

    void HandleMouseMove(object? sender, MouseEventArgs e) =>
        _cameraController.HandleMouseMove(e.ToGumMouseEventArgs(this));

    void HandleMouseUp(object? sender, MouseButtonEventArgs e) =>
        _cameraController.HandleMouseUp(e.ToGumMouseEventArgs(this));

    void HandleMouseWheel(object? sender, MouseWheelEventArgs e)
    {
        GumMouseEventArgs gumMouseArgs = e.ToGumMouseEventArgs(this);
        _cameraController.HandleMouseWheel(gumMouseArgs);

        // Read the neutral Handled back to suppress a containing scroll viewer's default scroll
        // behavior, mirroring HandleKeyDown above.
        e.Handled = gumMouseArgs.Handled;
    }

    private void HandleKeyUp(object? sender, KeyEventArgs e)
    {
        _hotkeyManager.HandleKeyUpWireframe();
    }

    /// <summary>
    /// Gives the hotkey manager first refusal on every key, ahead of WPF's own handling (focus
    /// navigation on Tab/arrows in particular). The WinForms counterpart was ProcessCmdKey.
    /// </summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();

        if (_hotkeyManager != null && _hotkeyManager.ProcessCmdKeyWireframe(
                keyArgs.Key,
                isShiftDown: keyArgs.IsShiftDown,
                isCtrlDown: keyArgs.IsCtrlDown,
                isAltDown: keyArgs.IsAltDown))
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }


    #endregion

    #region Initialize Methods

    public void Initialize(
        IHotkeyManager hotkeyManager,
        SelectionManager selectionManager,
        IDragDropManager dragDropManager,
        EditorViewModel editorViewModel,
        IProjectManager projectManager,
        IToolFontService toolFontService,
        IToolLayerService toolLayerService)
    {
        _selectionManager = selectionManager;
        _dragDropManager = dragDropManager;
        _hotkeyManager = hotkeyManager;
        _projectManager = projectManager;
        _toolFontService = toolFontService;
        _toolLayerService = toolLayerService;
        try
        {
            LoaderManager.Self.ContentLoader = new ContentLoader();

            // Route the GPU device/content-service lookup through IRenderDeviceHost rather than
            // reading GraphicsDevice/Services directly off this control, so the initialization
            // sequence below only depends on the render-host contract, not on a concrete control type.
            IRenderDeviceHost renderHost = RenderDeviceHost;

            SystemManagers.Default = new SystemManagers();
            SystemManagers.Default.Initialize(renderHost.GraphicsDevice);

            // Touching a type from KniGumShapes here forces the assembly to load, which fires its
            // [ModuleInitializer] -> AposShapeRuntime.RegisterRuntimeTypes (registers Apos.Shapes-backed
            // factories with RenderableRegistry). Then we initialize the ShapeBatch so those shapes
            // actually have a renderer to draw into. Without this, Circle/Rectangle runtimes in the
            // tool fall back to LineCircle/LineRectangle defaults. See issue #2925.
            //
            // The ContentManager is unused by ShapeRenderer as of Apos.Shapes.KNI 0.7.2+ (the shader
            // is embedded in the assembly, not loaded via content pipeline) but the parameter stays
            // for source compatibility — see ShapeRenderer.Initialize.
            if (!ShapeRenderer.Self.IsInitialized)
            {
                ContentManager shapesContentManager = new ContentManager(renderHost.Services, "Content");
                ShapeRenderer.Self.Initialize(renderHost.GraphicsDevice, shapesContentManager);
            }

            InitializeDefaultTypeInstantiation();

            _toolFontService.Initialize();
            _toolLayerService.Initialize();

            Renderer.TextureFilter = TextureFilter.Point;

            _cameraController = new CameraController();

            LoaderManager.Self.Initialize(null, "Content/TestFont.fnt", Services, null);
            if (global::RenderingLibrary.Graphics.Text.DefaultBitmapFont == null)
            {
                _outputManager.AddError(
                    "Default font file 'Content/TestFont.fnt' was not found. Text in the wireframe editor may not render correctly.");
            }
            _cameraController.Initialize(Camera, editorViewModel, hotkeyManager);
            _cameraController.CameraChanged += () => CameraChanged?.Invoke();

            InputLibrary.Cursor.Self.Initialize(new InputLibrary.WpfInputHostAdapter(this));

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
            MouseDown += HandleMouseDown;
            MouseMove += HandleMouseMove;
            MouseUp += HandleMouseUp;
            MouseWheel += HandleMouseWheel;

            MouseEnter += (_, _) =>
            {
                mouseHasEntered = true;
            };
            MouseLeave += (_, _) =>
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
            var message = "Error initializing the wireframe control\n\n" + exception;
            _dialogService.ShowMessage(message);
        }
    }


    // internal (not private) and static so GumToolUnitTests can verify every standard type the
    // tool needs to render is registered here - this list drifting out of sync with the runtime's
    // own registration (RenderingLibrary.SystemManagers.RegisterComponentRuntimeInstantiations) is
    // exactly how NineSlice/Container ended up missing: an instance whose type has no registration
    // here falls back to a plain GraphicalUiElement wrapping a raw renderable, which most
    // CustomSetPropertyOnRenderable dispatch branches don't handle (they cast to the typed Runtime
    // and silently no-op if that fails) - producing a correctly-valued but unrendered property
    // (e.g. a NineSlice's Red/Green/Blue staying at the renderable's default white).
    internal static void InitializeDefaultTypeInstantiation()
    {
        ElementSaveExtensions.RegisterGueInstantiation(
            "Circle",
            () => new CircleRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "ColoredRectangle",
            () => new ColoredRectangleRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Container",
            () => new ContainerRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "NineSlice",
            () => new NineSliceRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Polygon",
            () => new PolygonRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Rectangle",
            () => new RectangleRuntime(systemManagers: SystemManagers.Default));

        ElementSaveExtensions.RegisterGueInstantiation(
            "Sprite",
            () => new SpriteRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "Text",
            () =>
            {
                // Set this to false to make Text instantiation faster - we always set defaults explicitly
                TextRuntime.AssignFontInConstructor = false;
                return new TextRuntime(systemManagers: SystemManagers.Default);
            });


    }

    public void ShareLayerReferences(LayerService layerService)
    {

        ShapeManager.Self.Add(mCanvasBounds, layerService.OverlayLayer);


        TopRuler = new Ruler(
            SystemManagers.Default,
            InputLibrary.Cursor.Self,
            _toolFontService,
            _toolLayerService,
            layerService,
            _hotkeyManager);
        LeftRuler = new Ruler(SystemManagers.Default,
            InputLibrary.Cursor.Self,
            _toolFontService,
            _toolLayerService,
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
                        _pluginManager.GetIfShouldSuppressRemoveEditorHighlight() == false;


                    _selectionManager.Activity(shouldForceNoHighlight);

                    _selectionManager.LateActivity();
                }

                InputLibrary.Cursor.Self.EndCursorSettingFrameStart();
            }
#if DEBUG
            catch (Exception e)
            {
                _dialogService.ShowMessage(e.ToString());
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
        if (_projectManager == null)
        {
            return;
        }

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

            _pluginManager.BeforeRender();

            Renderer.Self.Draw((SystemManagers)null);

            _pluginManager.AfterRender();

        }
    }

    internal void SetGuideColors(Color guidelineColor, Color guideTextColor)
    {
        TopRuler.SetGuideColors(guidelineColor, guideTextColor);
        LeftRuler.SetGuideColors(guidelineColor, guideTextColor);
    }
}
