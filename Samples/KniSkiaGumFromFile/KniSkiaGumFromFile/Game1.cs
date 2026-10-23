using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGameRendering;
using SkiaSharp;

namespace KniSkiaGumFromFile;

/// <summary>
/// The KNI Skia version of KniGumFromFile: loads GumProject.gumx and shows its screens, drawn by SkiaGum
/// into a SkiaGameRendering <see cref="SkiaRenderTarget2D"/>. Press 1 to 8 to switch screens.
/// </summary>
public class Game1 : Game
{
    private const int StartWidth = 800;
    private const int StartHeight = 600;

    private readonly GraphicsDeviceManager _graphics;
    private SkiaRenderTarget2D? _canvas;

    private ScreenSave? _currentGumScreenSave;
    private GraphicalUiElement? _currentScreenGue;
    private MouseState _lastMouseState;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = StartWidth,
            PreferredBackBufferHeight = StartHeight,
        };
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // The graphics device exists once the base Initialize has run.
        base.Initialize();

        // Skia reads the project from disk. The csproj copies KniGumFromFileContent to Content/.
        GumService.Default.Initialize(this, "Content/GumProject.gumx");

        ShowScreen("StartScreen");
    }

    #region Swap Screens

    private void DoSwapScreenLogic()
    {
        KeyboardState state = Keyboard.GetState();

        Keys[] screenKeys = { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8 };
        for (int i = 0; i < screenKeys.Length; i++)
        {
            if (state.IsKeyDown(screenKeys[i]))
            {
                SwitchToScreen1Based(i + 1);
                return;
            }
        }
    }

    public void SwitchToScreen1Based(int screen1Based)
    {
        switch (screen1Based)
        {
            case 1:
                ShowScreen("StartScreen");
                break;
            case 2:
                if (ShowScreen("StateScreen"))
                {
                    InitializeStateScreen();
                }
                break;
            case 3:
                ShowScreen("ParentChildScreen");
                break;
            case 4:
                ShowScreen("TextScreen");
                break;
            case 5:
                ShowScreen("ZoomScreen");
                break;
            case 6:
                if (ShowScreen("ZoomLayerScreen"))
                {
                    InitializeZoomLayerScreen();
                }
                break;
            case 7:
                if (ShowScreen("OffsetLayerScreen"))
                {
                    InitializeOffsetLayerScreen();
                }
                break;
            case 8:
                ShowScreen("InteractiveGueScreen");
                break;
        }
    }

    private void InitializeStateScreen()
    {
        GraphicalUiElement setMeInCode = _currentScreenGue!.GetGraphicalUiElementByName("SetMeInCode")!;

        // States can be found in the Gum element's Categories and applied:
        StateSave stateToSet = setMeInCode.ElementSave!.Categories
            .First(item => item.Name == "RightSideCategory")
            .States.First(item => item.Name == "Blue");
        setMeInCode.ApplyState(stateToSet);

        // Alternatively states can be set in an "unqualified" way, which can be easier, but can
        // result in unexpected behavior if there are multiple states with the same name:
        setMeInCode.ApplyState("Green");

        // States can be constructed dynamically too. This state makes the SetMeInCode instance bigger:
        StateSave dynamicState = new StateSave();
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

    private void InitializeZoomLayerScreen()
    {
        GraphicalUiElement layered = _currentScreenGue!.GetGraphicalUiElementByName("Layered")!;
        Layer layer = SystemManagers.Default.Renderer.AddLayer();
        layer.Name = "Zoomed-in Layer";
        // MoveToLayer only moves top-level elements, so detach it from the screen first. The screen sits
        // at the origin and fills the canvas, so the element keeps its position.
        layered.Parent = null;
        layered.MoveToLayer(layer);

        layer.LayerCameraSettings = new LayerCameraSettings()
        {
            Zoom = 2
        };
    }

    private void InitializeOffsetLayerScreen()
    {
        Layer layer = SystemManagers.Default.Renderer.AddLayer();
        layer.Name = "Offset Layer";
        layer.LayerCameraSettings = new LayerCameraSettings()
        {
            IsInScreenSpace = true
        };

        GraphicalUiElement layeredText = _currentScreenGue!.GetGraphicalUiElementByName("LayeredText")!;
        // MoveToLayer only moves top-level elements, so detach it from the screen first. The screen sits
        // at the origin and fills the canvas, so the element keeps its position.
        layeredText.Parent = null;
        layeredText.MoveToLayer(layer);
    }

    private bool GetIfIsAlreadyShown(string screenName)
    {
        ScreenSave? newScreenElement = ObjectFinder.Self.GumProjectSave!.Screens.FirstOrDefault(item => item.Name == screenName);
        return _currentScreenGue != null && _currentScreenGue.Tag == newScreenElement;
    }

    private bool ShowScreen(string screenName)
    {
        bool isAlreadyShown = GetIfIsAlreadyShown(screenName);
        if (!isAlreadyShown)
        {
            _currentGumScreenSave = ObjectFinder.Self.GumProjectSave!.Screens.First(item => item.Name == screenName);
            _currentScreenGue?.RemoveFromManagers();

            Renderer renderer = SystemManagers.Default.Renderer;
            while (renderer.Layers.Count > 1)
            {
                renderer.RemoveLayer(renderer.Layers[renderer.Layers.Count - 1]);
            }

            // The Zoom screen changes the camera, so a newly shown screen starts unzoomed.
            renderer.Camera.Zoom = 1;
            GraphicalUiElement.CanvasWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            GraphicalUiElement.CanvasHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

            _currentScreenGue = _currentGumScreenSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
        }
        return !isAlreadyShown;
    }

    #endregion

    protected override void Update(GameTime gameTime)
    {
        MouseState mouseState = Mouse.GetState();

        if (_currentScreenGue != null)
        {
            GumService.Default.Update(gameTime, _currentScreenGue);
        }

        DoSwapScreenLogic();

        switch (_currentGumScreenSave?.Name)
        {
            case "StartScreen":
                DoStartScreenLogic();
                break;
            case "ZoomScreen":
                DoZoomScreenLogic(mouseState);
                break;
            case "OffsetLayerScreen":
                DoOffsetLayerScreenLogic(mouseState);
                break;
        }

        if (mouseState.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released)
        {
            // user just pushed on something so handle a push:
            HandleMousePush(mouseState);
        }

        _lastMouseState = mouseState;

        base.Update(gameTime);
    }

    private void DoStartScreenLogic()
    {
        GraphicalUiElement? setMeInCode = _currentScreenGue?.GetGraphicalUiElementByName("ComponentWithExposedVariableInstance");
        setMeInCode?.SetProperty("Text", $"Resolution {GraphicsDevice.Viewport.Width}x{GraphicsDevice.Viewport.Height}");
    }

    private void DoOffsetLayerScreenLogic(MouseState mouseState)
    {
        Layer layer = SystemManagers.Default.Renderer.Layers[1];

        layer.LayerCameraSettings!.Position = new System.Numerics.Vector2(
            -mouseState.Position.X,
            -mouseState.Position.Y);
    }

    private void DoZoomScreenLogic(MouseState mouseState)
    {
        Camera camera = SystemManagers.Default.Renderer.Camera;

        bool needsRefresh = false;
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            camera.Zoom *= 1.01f;
            needsRefresh = true;
        }
        else if (mouseState.RightButton == ButtonState.Pressed)
        {
            camera.Zoom *= .99f;
            needsRefresh = true;
        }

        if (needsRefresh)
        {
            GraphicalUiElement.CanvasWidth = GraphicsDevice.PresentationParameters.BackBufferWidth / camera.Zoom;
            GraphicalUiElement.CanvasHeight = GraphicsDevice.PresentationParameters.BackBufferHeight / camera.Zoom;

            // need to update the layout in response to the canvas size changing:
            _currentScreenGue?.UpdateLayout();
        }
    }

    private void HandleMousePush(MouseState mouseState)
    {
        GraphicalUiElement? itemOver = GetItemOver(mouseState.X, mouseState.Y, _currentScreenGue);

        if (itemOver is TextRuntime asTextRuntime && itemOver.Tag is InstanceSave instanceSave && instanceSave.Name == "ToggleFontSizes")
        {
            asTextRuntime.FontSize = asTextRuntime.FontSize == 16 ? 32 : 16;
        }
    }

    private static GraphicalUiElement? GetItemOver(int x, int y, GraphicalUiElement? graphicalUiElement)
    {
        // Only a top-level screen (no Children list) is searched, as in KniGumFromFile.
        if (graphicalUiElement?.Children != null)
        {
            return null;
        }

        foreach (GraphicalUiElement child in graphicalUiElement?.ContainedElements ?? Enumerable.Empty<GraphicalUiElement>())
        {
            bool isOver =
                x >= child.GetAbsoluteLeft() &&
                x < child.GetAbsoluteRight() &&
                y >= child.GetAbsoluteTop() &&
                y < child.GetAbsoluteBottom();
            if (isOver)
            {
                return child;
            }

            GraphicalUiElement? foundItem = GetItemOver(x, y, child);
            if (foundItem != null)
            {
                return foundItem;
            }
        }
        return null;
    }

    protected override void Draw(GameTime gameTime)
    {
        // SkiaRenderer attaches to the GraphicsDevice once KNI has created it.
        if (!SkiaRenderer.IsInitialized && SkiaRenderer.IsReady)
        {
            SkiaRenderer.Initialize(GraphicsDevice);
        }

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        if (SkiaRenderer.IsInitialized)
        {
            DrawGum();
        }

        base.Draw(gameTime);
    }

    private void DrawGum()
    {
        int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = GraphicsDevice.PresentationParameters.BackBufferHeight;

        // A SkiaRenderTarget2D has a fixed size, so a resized window needs a new one.
        bool hadCanvas = _canvas != null;
        bool resized = _canvas == null || _canvas.Texture.Width != width || _canvas.Texture.Height != height;
        if (resized)
        {
            _canvas?.Dispose();
            _canvas = new SkiaRenderTarget2D(GraphicsDevice, width, height);
        }

        _canvas!.Begin();
        SKCanvas canvas = _canvas.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (hadCanvas && resized)
        {
            GumService.Default.HandleResize(width, height);
            // The screen is not under GumService.Default.Root, so it lays itself out again.
            _currentScreenGue?.UpdateLayout();
        }

        GumService.Default.Draw(canvas);

        // Composites the target onto the back buffer at the origin, at native size.
        _canvas.End();
    }

    protected override void UnloadContent()
    {
        _canvas?.Dispose();
        _canvas = null;
        SkiaRenderer.Dispose();
        base.UnloadContent();
    }
}
