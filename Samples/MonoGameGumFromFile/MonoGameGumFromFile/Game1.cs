using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Linq;
using ToolsUtilities;

namespace MonoGameGumFromFile
{
    public class Game1 : Game
    {
        #region Fields/Properties

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        ScreenSave currentGumScreenSave;

        GraphicalUiElement currentScreenElement;

        #endregion

        #region Other Methods

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            SystemManagers.Default.Draw();

            base.Draw(gameTime);
        }
        #endregion

        protected override void Initialize()
        {
            SystemManagers.Default = new SystemManagers();
            SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);

            //SetSinglePixelTexture();

            LoadGumProject();

            ShowScreen("StartScreen");

            base.Initialize();
        }

        private void SetSinglePixelTexture()
        {
            var singlePixelTexture = Texture2D.FromFile(_graphics.GraphicsDevice, "Content/MainSpriteSheet.png");

            SystemManagers.Default.Renderer.SinglePixelTexture = singlePixelTexture;
            SystemManagers.Default.Renderer.SinglePixelSourceRectangle = new System.Drawing.Rectangle(1, 1, 1, 1);
        }

        private void InitializeComponentInCode()
        {
            var componentSave = ObjectFinder.Self.GumProjectSave.Components
                .First(item => item.Name == "ColoredRectangleComponent");

            var componentRuntime = componentSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);

            componentRuntime.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            componentRuntime.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;

            componentRuntime.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            componentRuntime.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Bottom;

        }

        private bool ShowScreen(string screenName)
        {
            FileManager.RelativeDirectory = "content/";
            currentGumScreenSave = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name == screenName);

            var isAlreadyShown = false;
            if (currentScreenElement != null)
            {
                isAlreadyShown = currentScreenElement.Tag == currentGumScreenSave;
            }

            if (!isAlreadyShown)
            {
                currentScreenElement?.RemoveFromManagers();
                var layers = SystemManagers.Default.Renderer.Layers;
                while(layers.Count > 1)
                {
                    SystemManagers.Default.Renderer.RemoveLayer(SystemManagers.Default.Renderer.Layers.LastOrDefault());
                }

                currentScreenElement = currentGumScreenSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
            }
            return !isAlreadyShown;
        }

        private static GumProjectSave LoadGumProject()
        {
            var gumProject = GumProjectSave.Load("GumProject.gumx", out _);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            return gumProject;
        }

        MouseState lastMouseState;
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);

            DoSwapScreenLogic();

            var mouseState = Mouse.GetState();

            if(currentGumScreenSave?.Name == "ZoomScreen")
            {
                DoZoomScreenLogic(mouseState);
            }

            if(mouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released)
            {
                // user just pushed on something so handle a push:
                HandleMousePush(mouseState);
            }

            lastMouseState = mouseState;

            base.Update(gameTime);
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
                currentScreenElement?.UpdateLayout();
            }
        }

        private void HandleMousePush(MouseState mouseState)
        {
            var itemOver = GetItemOver(mouseState.X, mouseState.Y, currentScreenElement);

            if(itemOver?.Tag is InstanceSave instanceSave && instanceSave.Name == "ToggleFontSizes")
            {
                if(itemOver.FontSize == 16)
                {
                    itemOver.FontSize = 32;
                }
                else
                {
                    itemOver.FontSize = 16;
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

        private void DoSwapScreenLogic()
        {
            var state = Keyboard.GetState();

            if(state.IsKeyDown(Keys.D1))
            {
                ShowScreen("StartScreen");
            }
            else if(state.IsKeyDown(Keys.D2))
            {
                var justShowed = ShowScreen("StateScreen");
                if(justShowed)
                {
                    var setMeInCode = currentScreenElement.GetGraphicalUiElementByName("SetMeInCode");

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
            else if(state.IsKeyDown(Keys.D3))
            {
                ShowScreen("ParentChildScreen");
            }
            else if(state.IsKeyDown(Keys.D4))
            {
                ShowScreen("TextScreen");
            }
            else if(state.IsKeyDown(Keys.D5))
            {
                ShowScreen("ZoomScreen");
            }
            else if(state.IsKeyDown(Keys.D6))
            {
                var justShowed = ShowScreen("ZoomLayerScreen");
                if(justShowed)
                {
                    UpdateZoomScreen();
                }
            }
        }

        private void UpdateZoomScreen()
        {
            var layered = currentScreenElement.GetGraphicalUiElementByName("Layered");
            var layer = SystemManagers.Default.Renderer.AddLayer();
            layer.Name = "Zoomed-in Layer";
            layered.MoveToLayer(layer);

            layer.LayerCameraSettings = new LayerCameraSettings()
            {
                Zoom = 2
            };

        }
    }
}
