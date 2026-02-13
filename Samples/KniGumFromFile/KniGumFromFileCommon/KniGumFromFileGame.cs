using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using MonoGameGum.Renderables;
using KniGumFromFile.ComponentRuntimes;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using ToolsUtilities;
using MonoGameGum;
using MonoGameGum.Forms;

namespace KniGumFromFile;

public class KniGumFromFileGame : Game
{
    public static KniGumFromFileGame Self
    {
        get; private set;
    }

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    ScreenSave currentGumScreenSave;

    GraphicalUiElement currentScreenGue;

    public KniGumFromFileGame()
    {
        Self = this;
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;

        _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
#if (ANDROID || iOS)
        graphics.IsFullScreen = true;
#endif

    }

    protected override void Initialize()
    {
        try
        {
            GumService.Default.Initialize(this, "GumProject.gumx");

            ShowScreen("StartScreen");
            InitializeStartScreen();

            base.Initialize();
        }
        catch(Exception e)
        {
            var errorInfo = e.ToString();
            System.Diagnostics.Debug.WriteLine(e);
            if(e.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine(e.InnerException);
            }
            System.Diagnostics.Debugger.Break();
        }

    }


    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void UnloadContent()
    {
    }

    #region Swap Screens

    private void DoSwapScreenLogic()
    {
        var state = Microsoft.Xna.Framework.Input.Keyboard.GetState();

        int? screen1Based = null;

        if (state.IsKeyDown(Keys.D1))
        {
            screen1Based = 1;
        }
        else if (state.IsKeyDown(Keys.D2))
        {
            screen1Based = 2;
        }
        else if (state.IsKeyDown(Keys.D3))
        {
            screen1Based = 3;
        }
        else if (state.IsKeyDown(Keys.D4))
        {
            screen1Based = 4;
        }
        else if (state.IsKeyDown(Keys.D5))
        {
            screen1Based = 5;
        }
        else if (state.IsKeyDown(Keys.D6))
        {
            screen1Based = 6;
        }
        else if (state.IsKeyDown(Keys.D7))
        {
            screen1Based = 7;
        }
        else if (state.IsKeyDown(Keys.D8))
        {
            screen1Based = 8;
        }

        if(screen1Based != null)
        {
            SwitchToScreen1Based(screen1Based.Value);
        }
    }

    public void SwitchToScreen1Based(int screen1Based)
    {
        switch (screen1Based)
        {
            case 1:
                {
                    if (ShowScreen("StartScreen"))
                    {
                        InitializeStartScreen();
                    }
                }
                break;
            case 2:
                {
                    var justShowed = ShowScreen("StateScreen");
                    if (justShowed)
                    {
                        var setMeInCode = currentScreenGue.GetGraphicalUiElementByName("SetMeInCode");

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
                {
                    var justShowed = ShowScreen("ZoomLayerScreen");
                    if (justShowed)
                    {
                        InitializeZoomScreen();
                    }
                }
                break;
            case 7:
                if (ShowScreen("OffsetLayerScreen"))
                {
                    InitializeOffsetLayerScreen();
                }
                break;
            case 8:
                if (!GetIfIsAlreadyShown("InteractiveGueScreen"))
                {
                    //ElementSaveExtensions.RegisterDefaultInstantiationType(() => new InteractiveGue());
                    ShowScreen("InteractiveGueScreen");
                    //InitializeInteractiveGueScreen();
                }
                break;
        }
    }

    private void InitializeInteractiveGueScreen()
    {

    }

    private void InitializeStartScreen()
    {

    }

    private void InitializeZoomScreen()
    {
        var layered = currentScreenGue.GetGraphicalUiElementByName("Layered");
        var layer = SystemManagers.Default.Renderer.AddLayer();
        layer.Name = "Zoomed-in Layer";
        layered.MoveToLayer(layer);

        layer.LayerCameraSettings = new LayerCameraSettings()
        {
            Zoom = 2
        };

    }

    private void InitializeOffsetLayerScreen()
    {
        var layer = SystemManagers.Default.Renderer.AddLayer();
        layer.Name = "Offset Layer";
        layer.LayerCameraSettings = new LayerCameraSettings()
        {
            IsInScreenSpace= true
        };

        var layeredText = currentScreenGue.GetGraphicalUiElementByName("LayeredText");
        layeredText.MoveToLayer(layer);
    }

    bool GetIfIsAlreadyShown(string screenName)
    {
        var newScreenElement = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name == screenName);

        var isAlreadyShown = false;
        if (currentScreenGue != null)
        {
            isAlreadyShown = currentScreenGue.Tag == newScreenElement;
        }

        return isAlreadyShown;
    }

    private bool ShowScreen(string screenName)
    {
        var isAlreadyShown = GetIfIsAlreadyShown(screenName);
        if (!isAlreadyShown)
        {
            var newScreenElement = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name == screenName);
            FileManager.RelativeDirectory = "Content" + Path.DirectorySeparatorChar;
            currentGumScreenSave = newScreenElement;
            currentScreenGue?.RemoveFromManagers();
            var layers = SystemManagers.Default.Renderer.Layers;
            while(layers.Count > 1)
            {
                SystemManagers.Default.Renderer.RemoveLayer(SystemManagers.Default.Renderer.Layers.LastOrDefault());
            }

            currentScreenGue = currentGumScreenSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
        }
        return !isAlreadyShown;
    }

    #endregion

    MouseState lastMouseState;
    protected override void Update(GameTime gameTime)
    {
        MouseState mouseState = Mouse.GetState();

        GumService.Default.Update(this, gameTime, currentScreenGue);

        var cursor = FormsUtilities.Cursor;
        var output =
            $"PrimaryDown: {cursor.PrimaryDown} " +
            $"PrimaryClick: {cursor.PrimaryClick} " +
            $"WindowOver:{cursor.WindowOver} " +
            $"WindowPushed:{cursor.WindowPushed}";
        if(cursor.PrimaryClick)
        {
            System.Console.WriteLine(output);
        }

        DoSwapScreenLogic();

        if (GraphicsDevice.Viewport.Width != GraphicalUiElement.CanvasWidth ||
            GraphicsDevice.Viewport.Height != GraphicalUiElement.CanvasHeight)
        {
            GraphicalUiElement.CanvasWidth = GraphicsDevice.Viewport.Width;
            GraphicalUiElement.CanvasHeight = GraphicsDevice.Viewport.Height;
            currentScreenGue?.UpdateLayout();
        }

        if (currentGumScreenSave?.Name == "StartScreen")
        {
            DoStartScreenLogic();
        }

        else if (currentGumScreenSave?.Name == "ZoomScreen")
        {
            DoZoomScreenLogic(mouseState);
        }
        else if(currentGumScreenSave?.Name == "OffsetLayerScreen")
        {
            DoOffsetLayerScreenLogic(mouseState);
        }

        if(mouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released)
        {
            // user just pushed on something so handle a push:
            HandleMousePush(mouseState);
        }

        lastMouseState = mouseState;

        base.Update(gameTime);
    }

    void DoStartScreenLogic() 
    {
        var setMeInCode = currentScreenGue?.GetGraphicalUiElementByName("ComponentWithExposedVariableInstance");
        setMeInCode?.SetProperty("Text", $"Resolution {GraphicsDevice.Viewport.Width}x{GraphicsDevice.Viewport.Height}");
    }

    int clickCount = 0;


    private void DoOffsetLayerScreenLogic(MouseState mouseState)
    {
        var layer = SystemManagers.Default.Renderer.Layers[1];

        layer.LayerCameraSettings.Position = new System.Numerics.Vector2(
            -mouseState.Position.X,
            -mouseState.Position.Y);
    }

    private void DoZoomScreenLogic(MouseState mouseState)
    {
        var camera = SystemManagers.Default.Renderer.Camera;

        var needsRefresh = false;
        if(mouseState.LeftButton == ButtonState.Pressed)
        {
            camera.Zoom *= 1.01f;
            needsRefresh = true;
        }
        else if(mouseState.RightButton == ButtonState.Pressed)
        {
            camera.Zoom *= .99f;
            needsRefresh = true;
        }

        if(needsRefresh)
        {
            GraphicalUiElement.CanvasWidth = 800 / camera.Zoom;
            GraphicalUiElement.CanvasHeight = 600 / camera.Zoom;

            // need to update the layout in response to the canvas size changing:
            currentScreenGue?.UpdateLayout();
        }
    }

    private void HandleMousePush(MouseState mouseState)
    {
        var itemOver = GetItemOver(mouseState.X, mouseState.Y, currentScreenGue);

        if(itemOver is TextRuntime asTextRuntime && itemOver?.Tag is InstanceSave instanceSave && instanceSave.Name == "ToggleFontSizes")
        {
            if(asTextRuntime.FontSize == 16)
            {
                asTextRuntime.FontSize = 32;
            }
            else
            {
                asTextRuntime.FontSize = 16;
            }
        }
    }

    private GraphicalUiElement GetItemOver(int x, int y, GraphicalUiElement graphicalUiElement)
    {
        if(graphicalUiElement.Children == null)
        {
            // this is a top level screen
            foreach(var child in graphicalUiElement.ContainedElements)
            {
                var isOver = 
                    x >= child.GetAbsoluteLeft() &&
                    x < child.GetAbsoluteRight() &&
                    y >= child.GetAbsoluteTop() &&
                    y < child.GetAbsoluteBottom();
                if(isOver)
                {
                    return child;
                }
                else
                {
                    var foundItem = GetItemOver(x, y, child);
                    if(foundItem != null)
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


    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        SystemManagers.Default.Draw();

        base.Draw(gameTime);
    }
}
