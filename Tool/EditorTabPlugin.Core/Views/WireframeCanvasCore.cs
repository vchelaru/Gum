using EditorTabPlugin_XNA.ViewModels;
using Gum.Input;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
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
using XnaAndWinforms;
using Color = System.Drawing.Color;

namespace Gum.Plugins.InternalPlugins.EditorTab.Views;

/// <summary>
/// The wireframe editor canvas without a UI framework: runtime initialization against the host's
/// device, the per-frame activity (cursor, rulers, selection), the draw, and the neutral key and
/// mouse handling. A head's control (<c>WireframeControl</c> on WPF, the Avalonia canvas control)
/// owns one of these, forwards its frame and input events to it, and implements
/// <see cref="ICanvasHost"/> for what the core needs back.
/// </summary>
public sealed class WireframeCanvasCore
{
    #region Fields

    private readonly ICanvasHost _host;
    private readonly IDialogService _dialogService;
    private readonly IOutputManager _outputManager;
    private readonly IPluginManager _pluginManager;
    private IHotkeyManager? _hotkeyManager;
    private IProjectManager? _projectManager;
    private SelectionManager? _selectionManager;
    private IDragDropManager? _dragDropManager;
    private IToolFontService? _toolFontService;
    private IToolLayerService? _toolLayerService;
    private CameraController? _cameraController;

    private LineRectangle? mCanvasBounds;
    private GridOverlayManager? _gridOverlayManager;

    public Color ScreenBoundsColor = Color.LightBlue;

    private bool mHasInitialized = false;
    private bool isInActivity = false;

    public Ruler? TopRuler { get; private set; }
    public Ruler? LeftRuler { get; private set; }

    /// <summary>Raised when the camera moves or zooms.</summary>
    public event Action? CameraChanged;

    /// <summary>
    /// Raised once per rendered frame after the canvas's own activity and before the draw, for the
    /// plugin's per-frame work (background, wireframe objects, tool layer).
    /// </summary>
    public event Action? FrameUpdate;

    public event EventHandler? AfterXnaInitialize;

    #endregion

    #region Properties

    public bool CanvasBoundsVisible
    {
        get => mCanvasBounds?.Visible == true;
        set
        {
            if (mCanvasBounds != null)
            {
                mCanvasBounds.Visible = value;
            }
        }
    }

    public bool RulersVisible
    {
        get => LeftRuler?.Visible == true;
        set
        {
            if (LeftRuler != null && TopRuler != null)
            {
                LeftRuler.Visible = value;
                TopRuler.Visible = value;
            }
        }
    }

    public bool IsGridOverlayVisible
    {
        get => _gridOverlayManager?.IsVisible == true;
        set
        {
            if (_gridOverlayManager != null)
            {
                _gridOverlayManager.IsVisible = value;
                _gridOverlayManager.Refresh(Camera);
            }
        }
    }

    public int GridSize
    {
        get => _gridOverlayManager?.GridSize ?? 0;
        set
        {
            if (_gridOverlayManager != null)
            {
                _gridOverlayManager.GridSize = value;
                _gridOverlayManager.Refresh(Camera);
            }
        }
    }

    public SystemManagers SystemManagers => SystemManagers.Default;

    public Microsoft.Xna.Framework.Color BackgroundColor { get; set; } = new(75, 75, 75);

    public LineRectangle? ScreenBounds => mCanvasBounds;

    /// <summary>The frame rate the host aims for.</summary>
    public float DesiredFramesPerSecond
    {
        get => _host.DesiredFramesPerSecond;
        set => _host.DesiredFramesPerSecond = value;
    }

    private GraphicsDevice GraphicsDevice => _host.RenderDeviceHost.GraphicsDevice;

    private Camera Camera => Renderer.Self.Camera;

    #endregion

    /// <summary>Creates the core over its host control.</summary>
    public WireframeCanvasCore(ICanvasHost host, IDialogService dialogService, IOutputManager outputManager, IPluginManager pluginManager)
    {
        _host = host;
        _dialogService = dialogService;
        _outputManager = outputManager;
        _pluginManager = pluginManager;
    }

    #region Input

    /// <summary>
    /// Handles a key press on the canvas. The host marks its event handled when
    /// <see cref="GumKeyEventArgs.Handled"/> or <see cref="GumKeyEventArgs.SuppressKeyPress"/>
    /// comes back set, which stops both the key event and any text input it would produce.
    /// </summary>
    public void HandleKeyDown(GumKeyEventArgs keyArgs)
    {
        _hotkeyManager?.HandleEditorKeyDown(keyArgs);
        _cameraController?.HandleKeyPress(keyArgs);
    }

    /// <summary>Handles a key release on the canvas.</summary>
    public void HandleKeyUp() => _hotkeyManager?.HandleKeyUpWireframe();

    /// <summary>
    /// Gives the hotkey manager first refusal on every key, ahead of the framework's own handling
    /// (focus navigation on Tab/arrows in particular). Returns true when the key was consumed.
    /// </summary>
    public bool HandlePreviewKeyDown(GumKeyEventArgs keyArgs) =>
        _hotkeyManager != null && _hotkeyManager.ProcessCmdKeyWireframe(
            keyArgs.Key,
            isShiftDown: keyArgs.IsShiftDown,
            isCtrlDown: keyArgs.IsCtrlDown,
            isAltDown: keyArgs.IsAltDown);

    public void HandleMouseDown(GumMouseEventArgs e) => _cameraController?.HandleMouseDown(e);

    public void HandleMouseMove(GumMouseEventArgs e) => _cameraController?.HandleMouseMove(e);

    public void HandleMouseUp(GumMouseEventArgs e) => _cameraController?.HandleMouseUp(e);

    /// <summary>
    /// Handles the wheel. The host reads <see cref="GumMouseEventArgs.Handled"/> back to suppress
    /// a containing scroll viewer's default scroll.
    /// </summary>
    public void HandleMouseWheel(GumMouseEventArgs e) => _cameraController?.HandleMouseWheel(e);

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

            // The initialization sequence below only depends on the render-host contract, not on
            // the host control type.
            IRenderDeviceHost renderHost = _host.RenderDeviceHost;

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
            // for source compatibility - see ShapeRenderer.Initialize.

            // Issue #4506 - keep the editor's Svg preview on the Skia plugin's Svg.Skia renderable
            // (MainSkiaPlugin.HandleCreateRenderbleFor) rather than the Apos.Shapes-backed
            // SvgRuntime that KniGumShapes registers for shipped games. Apos ignores CSS <style>
            // blocks, text, use, clipPath, mask, filter and pattern, so letting it win here would
            // silently downgrade what a real drawing looks like in the editor. Unlike Arc (#2925),
            // which the plugin gave up so tool and runtime could share one path, Svg keeps two.
            //
            // Assignment order does not matter: RegisterRuntimeTypes is a [ModuleInitializer] that
            // has already run by the time any of this executes, and the flag is read inside the
            // registered factory at creation time.
            Gum.GueDeriving.AposShapeRuntime.IsSvgRuntimeEnabled = false;

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

            LoaderManager.Self.Initialize(null, "Content/TestFont.fnt", _host.Services, null);
            if (global::RenderingLibrary.Graphics.Text.DefaultBitmapFont == null)
            {
                _outputManager.AddError(
                    "Default font file 'Content/TestFont.fnt' was not found. Text in the wireframe editor may not render correctly.");
            }

            _cameraController.Initialize(Camera, editorViewModel, hotkeyManager);
            _cameraController.CameraChanged += () => CameraChanged?.Invoke();

            InputLibrary.Cursor.Self.Initialize(_host.InputHost);

            mCanvasBounds = new LineRectangle();
            mCanvasBounds.IsDotted = true;
            mCanvasBounds.Name = "Gum Screen Bounds";
            mCanvasBounds.Width = 800;
            mCanvasBounds.Height = 600;
            mCanvasBounds.Color = ScreenBoundsColor;

            _gridOverlayManager = new GridOverlayManager(SystemManagers.Default);

            var camera = SystemManagers.Default.Renderer.Camera;
            camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

            if (AfterXnaInitialize != null)
            {
                AfterXnaInitialize(this, EventArgs.Empty);
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
    // exactly how NineSlice ended up missing: an instance whose type has no registration
    // here falls back to a plain GraphicalUiElement wrapping a raw renderable, which most
    // CustomSetPropertyOnRenderable dispatch branches don't handle (they cast to the typed Runtime
    // and silently no-op if that fails) - producing a correctly-valued but unrendered property
    // (e.g. a NineSlice's Red/Green/Blue staying at the renderable's default white).
    //
    // Container is the one type that must stay unregistered - see the comment at its spot below.
    internal static void InitializeDefaultTypeInstantiation()
    {
        ElementSaveExtensions.RegisterGueInstantiation(
            "Circle",
            () => new CircleRuntime());

        ElementSaveExtensions.RegisterGueInstantiation(
            "ColoredRectangle",
            () => new ColoredRectangleRuntime());

        // Container is deliberately absent (issue #4386). Every registered runtime installs its own
        // renderable in its constructor, so SetGraphicalUiElement sees a non-null RenderableComponent
        // and skips CreateGraphicalComponent - the only path that consults
        // GraphicalUiElement.ShowLineRectangles and returns FallbackRenderableFactory's dotted
        // LineRectangle, which is how the editor draws a Container with Show Outlines checked (the
        // runtime has no such concept, which is why it does register ContainerRuntime). Registering
        // ContainerRuntime here replaced that outline with an InvisibleRenderable, making every
        // Container instance invisible in the editor.

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
        _gridOverlayManager!.AddToLayer(layerService.OverlayLayer);

        TopRuler = new Ruler(
            SystemManagers.Default,
            InputLibrary.Cursor.Self,
            _toolFontService!,
            _toolLayerService!,
            layerService,
            _hotkeyManager!);
        LeftRuler = new Ruler(SystemManagers.Default,
            InputLibrary.Cursor.Self,
            _toolFontService!,
            _toolLayerService!,
            layerService,
            _hotkeyManager!);
        LeftRuler.RulerSide = RulerSide.Left;
    }

    #endregion

    private void Activity()
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

                // Camera.ClientWidth/Height come from the GraphicsDevice viewport, which isn't
                // valid yet the first time a project loads (before the first XNA frame has run) -
                // a one-shot Refresh() at load time can compute against a 0x0 viewport and leave
                // the grid invisible even though IsGridOverlayVisible is true. Refreshing here
                // every frame keeps it in sync once the viewport (and therefore the camera) is
                // actually valid, with no extra event wiring needed.
                _gridOverlayManager!.Refresh(Camera);

                SpriteManager.Self.Activity(TimeManager.Self.CurrentTime);

                InputLibrary.Cursor.Self.Activity(TimeManager.Self.CurrentTime);

                bool isOver = TopRuler!.HandleXnaUpdate(InputLibrary.Cursor.Self.IsInWindow) ||
                    LeftRuler!.HandleXnaUpdate(InputLibrary.Cursor.Self.IsInWindow);

                // But we want the selection to update the handles to the selected object
                // after editing is done.  SelectionManager.LateActivity lets us do that.  LateActivity must
                // come after EditingManager.Activity.
                // Update 1/15/2019
                // When the user uses scroll bars we get selection to underlying objects.
                // We don't want that, so we'll check if the mouse has entered the control.
                // I may have to update this at some point to force deselection if the mouse
                // has not entered so things don't stay highlighted when exiting the control
                // Update 2 - yea, we def need to pass in mouseHasEntered == false to force no highlight
                if (TopRuler.IsCursorOver == false && LeftRuler!.IsCursorOver == false)
                {
                    var shouldForceNoHighlight = _host.IsPointerOver == false &&
                        _pluginManager.GetIfShouldSuppressRemoveEditorHighlight() == false;
                    _selectionManager!.Activity(shouldForceNoHighlight);
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

    /// <summary>The host calls this before each draw.</summary>
    public void PreDrawUpdate()
    {
        if (mHasInitialized)
        {
            Activity();
        }
    }

    /// <summary>The host calls this with the render target bound and cleared.</summary>
    public void Draw()
    {
        if (!mHasInitialized)
        {
            return;
        }
        FrameUpdate?.Invoke();
        GraphicsDevice.Clear(BackgroundColor);
        _pluginManager.BeforeRender();
        Renderer.Self.Draw((SystemManagers)null!);
        _pluginManager.AfterRender();
    }

    public void SetGuideColors(Color guidelineColor, Color guideTextColor)
    {
        TopRuler?.SetGuideColors(guidelineColor, guideTextColor);
        LeftRuler?.SetGuideColors(guidelineColor, guideTextColor);
    }
}
