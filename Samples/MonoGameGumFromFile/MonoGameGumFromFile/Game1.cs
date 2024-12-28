using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.Forms;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using MonoGameGum.Renderables;
using MonoGameGumFromFile.ComponentRuntimes;
using MonoGameGumFromFile.Managers;
using MonoGameGumFromFile.ScreenRuntimes;
using MonoGameGumFromFile.Screens;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using ToolsUtilities;

namespace MonoGameGumFromFile
{
    public class Game1 : Game
    {
        #region Fields/Properties

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        ScreenSave currentGumScreenSave;

        GraphicalUiElement currentScreenGue;

        SingleThreadSynchronizationContext synchronizationContext;

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

        #region Initialize

        protected override void Initialize()
        {
            synchronizationContext = new SingleThreadSynchronizationContext();
            GumService.Default.Initialize(_graphics.GraphicsDevice, "GumProject.gumx");


            // This allows you to resize:
            Window.AllowUserResizing = true;
            // This event is raised whenever a resize occurs, allowing
            // us to perform custom logic on a resize
            Window.ClientSizeChanged += HandleClientSizeChanged;

            // store off the original height so we can use it for zooming
            originalHeight = _graphics.GraphicsDevice.Viewport.Height;

            // This example includes code that assocaites a particular screen with a
            // runtime class
            ElementSaveExtensions.RegisterGueInstantiation("StartScreen",
                () => new StartScreenRuntime());


            InitializeRuntimeMapping();

            ShowScreen("StartScreen");

            base.Initialize();
        }

        bool performZoom = true;
        int originalHeight;

        private void HandleClientSizeChanged(object sender, EventArgs e)
        {
            float zoom = 1;
            if(performZoom)
            {
                zoom = _graphics.GraphicsDevice.Viewport.Height / (float)originalHeight;
            }
            SystemManagers.Default.Renderer.Camera.Zoom = zoom;


            GraphicalUiElement.CanvasWidth = _graphics.GraphicsDevice.Viewport.Width/zoom;
            GraphicalUiElement.CanvasHeight = _graphics.GraphicsDevice.Viewport.Height/zoom;

            // Grab the rootmost object and tell it to resize:
            currentScreenGue.UpdateLayout();

            // If you are using Gum Forms, you should also notify GumForms:
            //MonoGameGum.Forms.Controls.FrameworkElement.Root.UpdateLayout();

        }

        private void InitializeRuntimeMapping()
        {
            ElementSaveExtensions.RegisterGueInstantiationType(
                "Buttons/StandardButton", 
                typeof(ClickableButton));

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

        #endregion

        #region Swap Screens

        private void DoSwapScreenLogic()
        {
            var state = Microsoft.Xna.Framework.Input.Keyboard.GetState();

            if (state.IsKeyDown(Keys.D1))
            {
                ShowScreen("StartScreen");
            }
            else if (state.IsKeyDown(Keys.D2))
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
            else if (state.IsKeyDown(Keys.D3))
            {
                ShowScreen("ParentChildScreen");
            }
            else if (state.IsKeyDown(Keys.D4))
            {
                ShowScreen("TextScreen");
            }
            else if (state.IsKeyDown(Keys.D5))
            {
                ShowScreen("ZoomScreen");
            }
            else if (state.IsKeyDown(Keys.D6))
            {
                var justShowed = ShowScreen("ZoomLayerScreen");
                if (justShowed)
                {
                    InitializeZoomScreen();
                }
            }
            else if (state.IsKeyDown(Keys.D7))
            {
                if(ShowScreen("OffsetLayerScreen"))
                {
                    InitializeOffsetLayerScreen();
                }
            }
            else if(state.IsKeyDown(Keys.D8))
            {
                if(!GetIfIsAlreadyShown("InteractiveGueScreen"))
                {
                    //ElementSaveExtensions.RegisterDefaultInstantiationType(() => new InteractiveGue());
                    ShowScreen("InteractiveGueScreen");
                    //InitializeInteractiveGueScreen();
                }
            }
            else if(state.IsKeyDown(Keys.D9))
            {
                ShowScreen("ResizeScreen");
            }
            else if(state.IsKeyDown(Keys.D0))
            {
                ShowScreen("MvvmScreen");
            }
        }

        private void InitializeInteractiveGueScreen()
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
            if (Microsoft.Xna.Framework.Input.GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            GumService.Default.Update(this, gameTime, currentScreenGue);

            currentScreenGue.AnimateSelf(gameTime.ElapsedGameTime.TotalSeconds);


            synchronizationContext.Update();

            DoSwapScreenLogic();

            var mouseState = Mouse.GetState();

            if(currentGumScreenSave?.Name == "StartScreen")
            {
                DoStartScreenLogic();
            }
            else if(currentGumScreenSave?.Name == "InteractiveGueScreen")
            {
                DoInteractiveGueScreenLogic(gameTime.TotalGameTime.TotalSeconds);
            }

            else if (currentGumScreenSave?.Name == "ZoomScreen")
            {
                DoZoomScreenLogic(mouseState);
            }
            else if(currentGumScreenSave?.Name == "OffsetLayerScreen")
            {
                DoOffsetLayerScreenLogic(mouseState);
            }
            else if(currentScreenGue is MvvmScreenRuntime asMvvmScreen)
            {
                asMvvmScreen.CustomActivity();
            }

            if(mouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released)
            {
                // user just pushed on something so handle a push:
                HandleMousePush(mouseState);
            }

            lastMouseState = mouseState;

            base.Update(gameTime);
        }

        void DoStartScreenLogic() { }

        int clickCount = 0;
        private void DoInteractiveGueScreenLogic(double currentGameTimeInSeconds)
        {
            currentScreenGue.DoUiActivityRecursively(FormsUtilities.Cursor, FormsUtilities.Keyboard, currentGameTimeInSeconds);
        }

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


    }
}
