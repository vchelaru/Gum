using System.Linq;
using System.Runtime.InteropServices;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SokolGum;
using static Sokol.SApp;
using static Sokol.SG;
using static Sokol.SGP;
using static Sokol.SGlue;
using static Sokol.SLog;

namespace SokolGumFromFile;

/// <summary>
/// .gumx-loading sample. Loads Content/GumProject/GumProject.gumx at
/// startup via <see cref="GumService"/>, shows StartScreen, and supports
/// keyboard switching between screens (1-7, 9, Escape) to match the
/// MonoGame from-file sample for side-by-side comparison.
///
/// The region layout below intentionally mirrors
/// MonoGameGumFromFile/Game1.cs so a side-by-side diff makes backend gaps
/// visually obvious. Methods that can't be implemented today because of
/// missing SokolGum capabilities (Forms, code-only screen runtimes, etc.)
/// are left as stubs with a TODO comment naming the missing capability.
/// </summary>
public static unsafe class Program
{
    #region Fields/Properties

    // MG has _graphics (GraphicsDeviceManager) and _spriteBatch — no Sokol equivalent; dropped.
    private static sg_pass_action _passAction;

    // Matches MG's `ScreenSave currentGumScreenSave` — replaces the previous
    // `_currentScreenName` string tracking so the two samples line up.
    private static ScreenSave? currentGumScreenSave;

    // MG has `SingleThreadSynchronizationContext synchronizationContext` —
    // no Sokol equivalent right now; dropped.

    private static bool performZoom = true;
    private static int originalHeight;

    // MG tracks `MouseState lastMouseState`. Sokol doesn't have a polled
    // mouse state — we accumulate state from sapp events into these fields
    // and use _last* for edge detection in HandleMousePush.
    private static float _mouseX;
    private static float _mouseY;
    private static bool _leftMouseDown;
    private static bool _rightMouseDown;
    private static bool _lastLeftMouseDown;
    private static bool _lastRightMouseDown;

    // Per-frame key dispatch queue — sapp delivers keydown via Event, but
    // MG's DoSwapScreenLogic runs every Update. We queue the last keydown
    // and consume it in Frame so the method bodies match MG's shape.
    private static sapp_keycode? _pendingKey;

    #endregion

    #region Other Methods

    // Equivalent to MG's constructor (Game1()) + LoadContent + Draw,
    // expressed as sokol_app lifecycle callbacks.

    public static void Main()
    {
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            var appPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName)!;
            Directory.SetCurrentDirectory(appPath);
        }

        sapp_run(new sapp_desc
        {
            init_cb    = &Init,
            frame_cb   = &Frame,
            event_cb   = &Event,
            cleanup_cb = &Cleanup,
            width        = 1024,
            height       = 768,
            sample_count = 4,
            high_dpi     = true,
            window_title = "SokolGum FromFile — .gumx loader",
            icon   = { sokol_default = true },
            logger = { func = &slog_func },
        });
    }

    [UnmanagedCallersOnly]
    private static void Frame()
    {
        // Draw (equivalent of MG's Draw). sokol requires us to bracket
        // rendering with a pass — GumService.Default.Draw() is MG's
        // SystemManagers.Default.Draw() equivalent.
        sg_begin_pass(new sg_pass { action = _passAction, swapchain = sglue_swapchain() });

        // Per-frame logic (equivalent of MG's Update) runs first so
        // screen-specific mouse logic can update layout before drawing.
        Update();

        GumService.Default.Draw();
        sg_end_pass();
        sg_commit();
    }

    [UnmanagedCallersOnly]
    private static void Event(sapp_event* e)
    {
        switch (e->type)
        {
            case sapp_event_type.SAPP_EVENTTYPE_KEY_DOWN:
                _pendingKey = e->key_code;
                break;

            case sapp_event_type.SAPP_EVENTTYPE_MOUSE_MOVE:
                _mouseX = e->mouse_x;
                _mouseY = e->mouse_y;
                break;

            case sapp_event_type.SAPP_EVENTTYPE_MOUSE_DOWN:
                _mouseX = e->mouse_x;
                _mouseY = e->mouse_y;
                if (e->mouse_button == sapp_mousebutton.SAPP_MOUSEBUTTON_LEFT)  _leftMouseDown  = true;
                if (e->mouse_button == sapp_mousebutton.SAPP_MOUSEBUTTON_RIGHT) _rightMouseDown = true;
                break;

            case sapp_event_type.SAPP_EVENTTYPE_MOUSE_UP:
                _mouseX = e->mouse_x;
                _mouseY = e->mouse_y;
                if (e->mouse_button == sapp_mousebutton.SAPP_MOUSEBUTTON_LEFT)  _leftMouseDown  = false;
                if (e->mouse_button == sapp_mousebutton.SAPP_MOUSEBUTTON_RIGHT) _rightMouseDown = false;
                break;

            case sapp_event_type.SAPP_EVENTTYPE_RESIZED:
                HandleClientSizeChanged(e->window_width, e->window_height);
                break;
        }
    }

    [UnmanagedCallersOnly]
    private static void Cleanup()
    {
        GumService.Default.SystemManagers?.Dispose();
        sgp_shutdown();
        sg_shutdown();
    }

    #endregion

    #region Initialize

    [UnmanagedCallersOnly]
    private static void Init()
    {
        sg_setup(new sg_desc
        {
            environment = sglue_environment(),
            logger = { func = &slog_func },
        });
        sgp_setup(new sgp_desc());

        _passAction = default;
        _passAction.colors[0].load_action = sg_load_action.SG_LOADACTION_CLEAR;
        _passAction.colors[0].clear_value = new sg_color { r = 0.10f, g = 0.12f, b = 0.15f, a = 1.0f };

        GumService.Default.Initialize("Content/GumProject/GumProject.gumx");

        // MG sets Window.AllowUserResizing = true; sokol_app windows are
        // resizable by default, nothing to wire.

        // Resize event is wired via the Event callback
        // (SAPP_EVENTTYPE_RESIZED) — equivalent to MG's
        // Window.ClientSizeChanged += HandleClientSizeChanged.

        // store off the original height so we can use it for zooming
        originalHeight = sapp_height();

        InitializeRuntimeMapping();

        ShowScreen("StartScreen");
    }

    private static void HandleClientSizeChanged(int newWidth, int newHeight)
    {
        float zoom = 1;
        if (performZoom)
        {
            zoom = newHeight / (float)originalHeight;
        }

        GumService.Default.SystemManagers.Renderer.Camera.Zoom = zoom;

        GraphicalUiElement.CanvasWidth = newWidth / zoom;
        GraphicalUiElement.CanvasHeight = newHeight / zoom;
    }

    private static void InitializeRuntimeMapping()
    {
        // TODO: Forms/FrameworkElement integration not yet available in SokolGum.
        // MG registers ClickableButton for "Buttons/StandardButton" here.
    }

    private static void SetSinglePixelTexture()
    {
        // TODO: sokol_gp has its own solid-rect primitive; no SinglePixelTexture
        // wiring needed. MG loads MainSpriteSheet.png and assigns a 1x1 source rect.
    }

    private static void InitializeComponentInCode()
    {
        // TODO: code-only component creation demo from MG not ported yet.
        // MG instantiates ColoredRectangleComponent via ToGraphicalUiElement and
        // anchors it to the bottom-right of the screen.
    }

    #endregion

    #region Swap Screens

    private static void DoSwapScreenLogic()
    {
        if (_pendingKey == null)
        {
            return;
        }

        var key = _pendingKey.Value;
        _pendingKey = null;

        switch (key)
        {
            case sapp_keycode.SAPP_KEYCODE_1:
                ShowScreen("StartScreen");
                break;
            case sapp_keycode.SAPP_KEYCODE_2:
                {
                    var newScreen = ShowScreen("StateScreen");
                    if (newScreen != null)
                    {
                        var setMeInCode = newScreen.GetGraphicalUiElementByName("SetMeInCode");

                        // States can be found in the Gum element's Categories and applied:
                        var stateToSet = setMeInCode.ElementSave.Categories
                            .FirstOrDefault(item => item.Name == "RightSideCategory")
                            .States.Find(item => item.Name == "Blue");
                        setMeInCode.ApplyState(stateToSet);

                        // Alternatively states can be set in an "unqualified" way, which can be easier, but can
                        // result in unexpected behavior if there are multiple states with the same name:
                        setMeInCode.ApplyState("Green");

                        // states can be constructed dynamically too. This state makes the SetMeInCode instance bigger:
                        var dynamicState = new StateSave();
                        dynamicState.Variables.Add(new VariableSave()
                        {
                            Value = 300f,
                            Name = "Width",
                            Type = "float",
                            // values can exist on a state but be "disabled"
                            SetsValue = true
                        });
                        dynamicState.Variables.Add(new VariableSave()
                        {
                            Value = 250f,
                            Name = "Height",
                            Type = "float",
                            SetsValue = true
                        });
                        setMeInCode.ApplyState(dynamicState);
                    }
                }
                break;
            case sapp_keycode.SAPP_KEYCODE_3:
                ShowScreen("ParentChildScreen");
                break;
            case sapp_keycode.SAPP_KEYCODE_4:
                ShowScreen("TextScreen");
                break;
            case sapp_keycode.SAPP_KEYCODE_5:
                ShowScreen("ZoomScreen");
                break;
            case sapp_keycode.SAPP_KEYCODE_6:
                {
                    var newScreen = ShowScreen("ZoomLayerScreen");
                    if (newScreen != null)
                    {
                        InitializeZoomScreen(newScreen);
                    }
                }
                break;
            case sapp_keycode.SAPP_KEYCODE_7:
                {
                    var newScreen = ShowScreen("OffsetLayerScreen");
                    if (newScreen != null)
                    {
                        InitializeOffsetLayerScreen(newScreen);
                    }
                }
                break;
            // 8: InteractiveGueScreen — requires InteractiveGue Forms registration (not yet in SokolGum).
            // 0: MvvmScreen — requires MvvmScreenRuntime code-only type (sample-local class; not ported).
            case sapp_keycode.SAPP_KEYCODE_9:
                ShowScreen("ResizeScreen");
                break;
            case sapp_keycode.SAPP_KEYCODE_ESCAPE:
                RemoveScreenAndLayers();
                break;
        }
    }

    private static void RemoveScreenAndLayers()
    {
        GumService.Default.Root.Children.Clear();
        var renderer = GumService.Default.SystemManagers.Renderer;
        var layers = renderer.Layers;
        while (layers.Count > 1)
        {
            renderer.RemoveLayer(renderer.Layers.LastOrDefault());
        }
    }

    private static void InitializeInteractiveGueScreen()
    {
        // Empty in MG too — kept to match shape.
    }

    private static void InitializeZoomScreen(GraphicalUiElement newScreen)
    {
        var layered = newScreen.GetGraphicalUiElementByName("Layered");
        var layer = GumService.Default.SystemManagers.Renderer.AddLayer();
        layer.Name = "Zoomed-in Layer";
        layered.MoveToLayer(layer);

        layer.LayerCameraSettings = new LayerCameraSettings()
        {
            Zoom = 2
        };
    }

    private static void InitializeOffsetLayerScreen(GraphicalUiElement newScreen)
    {
        var layer = GumService.Default.SystemManagers.Renderer.AddLayer();
        layer.Name = "Offset Layer";
        layer.LayerCameraSettings = new LayerCameraSettings()
        {
            IsInScreenSpace = true
        };

        var layeredText = newScreen.GetGraphicalUiElementByName("LayeredText");
        layeredText.MoveToLayer(layer);
    }

    private static bool GetIfIsAlreadyShown(string screenName)
    {
        var newScreenElement = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name == screenName);

        var isAlreadyShown = GumService.Default.Root.Children.Any(item => item.Tag == newScreenElement);

        return isAlreadyShown;
    }

    private static GraphicalUiElement? ShowScreen(string screenName)
    {
        var isAlreadyShown = GetIfIsAlreadyShown(screenName);

        GraphicalUiElement? newScreen = null;

        if (!isAlreadyShown)
        {
            GumService.Default.Root.Children.Clear();

            var layers = GumService.Default.SystemManagers.Renderer.Layers;
            while (layers.Count > 1)
            {
                GumService.Default.SystemManagers.Renderer.RemoveLayer(GumService.Default.SystemManagers.Renderer.Layers.LastOrDefault());
            }

            if (screenName == "StartScreen")
            {
                // Sokol doesn't have StartScreenRuntime (Forms-backed); loading StartScreen from the .gumx instead.
                var newScreenElement = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name == screenName);
                newScreen = newScreenElement.ToGraphicalUiElement(GumService.Default.SystemManagers);
                newScreen.AddToRoot();

                var textRuntime = (newScreen.GetGraphicalUiElementByName("TextInstance") as TextRuntime);
                if (textRuntime != null)
                {
                    textRuntime.Text = "Meow";
                }
            }
            else
            {
                var newScreenElement = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name == screenName);
                newScreen = newScreenElement.ToGraphicalUiElement(GumService.Default.SystemManagers);
                newScreen.AddToRoot();

                var textRuntime = (newScreen.GetGraphicalUiElementByName("TextInstance") as TextRuntime);
                if (textRuntime != null)
                {
                    textRuntime.Text = "Meow";
                }
            }

            currentGumScreenSave = newScreen?.Tag as ScreenSave;
        }
        return newScreen;
    }

    #endregion

    #region Update

    private static void Update()
    {
        GumService.Default.Update();

        // MG also calls GumService.Default.Root.AnimateSelf(elapsedSeconds) —
        // SokolGum's GumService.Update wires animation internally, so no
        // separate call is needed here.

        // MG calls synchronizationContext.Update() — no Sokol equivalent.

        DoSwapScreenLogic();

        if (currentGumScreenSave?.Name == "StartScreen")
        {
            DoStartScreenLogic();
        }
        else if (currentGumScreenSave?.Name == "ZoomScreen")
        {
            DoZoomScreenLogic();
        }
        else if (currentGumScreenSave?.Name == "OffsetLayerScreen")
        {
            DoOffsetLayerScreenLogic();
        }

        // MG iterates Root.Children looking for MvvmScreenRuntime —
        // that type is a MonoGame sample-local class, not ported to Sokol.

        if (_leftMouseDown && !_lastLeftMouseDown)
        {
            // user just pushed on something so handle a push:
            HandleMousePush();
        }

        _lastLeftMouseDown = _leftMouseDown;
        _lastRightMouseDown = _rightMouseDown;
    }

    private static void DoStartScreenLogic() { }

    private static void DoOffsetLayerScreenLogic()
    {
        var layer = GumService.Default.SystemManagers.Renderer.Layers[1];

        layer.LayerCameraSettings.Position = new System.Numerics.Vector2(
            -_mouseX,
            -_mouseY);
    }

    private static void DoZoomScreenLogic()
    {
        var camera = GumService.Default.SystemManagers.Renderer.Camera;

        var needsRefresh = false;
        if (_leftMouseDown)
        {
            camera.Zoom *= 1.01f;
            needsRefresh = true;
        }
        else if (_rightMouseDown)
        {
            camera.Zoom *= .99f;
            needsRefresh = true;
        }

        if (needsRefresh)
        {
            GraphicalUiElement.CanvasWidth = 800 / camera.Zoom;
            GraphicalUiElement.CanvasHeight = 600 / camera.Zoom;

            // need to update the layout in response to the canvas size changing:
            GumService.Default.Root.UpdateLayout();
        }
    }

    private static void HandleMousePush()
    {
        var itemOver = GetItemOver((int)_mouseX, (int)_mouseY, GumService.Default.Root);

        if (itemOver is TextRuntime textRuntime && itemOver?.Tag is InstanceSave instanceSave && instanceSave.Name == "ToggleFontSizes")
        {
            if (textRuntime.FontSize == 16)
            {
                textRuntime.FontSize = 32;
            }
            else
            {
                textRuntime.FontSize = 16;
            }
        }
    }

    private static GraphicalUiElement? GetItemOver(int x, int y, GraphicalUiElement graphicalUiElement)
    {
        if (graphicalUiElement.Children == null)
        {
            // this is a top level screen
            foreach (var child in graphicalUiElement.ContainedElements)
            {
                var isOver =
                    x >= child.GetAbsoluteLeft() &&
                    x < child.GetAbsoluteRight() &&
                    y >= child.GetAbsoluteTop() &&
                    y < child.GetAbsoluteBottom();
                if (isOver)
                {
                    return child;
                }
                else
                {
                    var foundItem = GetItemOver(x, y, child);
                    if (foundItem != null)
                    {
                        return foundItem;
                    }
                }
            }
        }
        else
        {
            // todo
        }
        return null;
    }

    #endregion
}
