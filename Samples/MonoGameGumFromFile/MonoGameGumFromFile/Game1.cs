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
using System;
using System.Linq;

namespace MonoGameGumFromFile
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        GraphicalUiElement currentScreenElement;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
        }

        protected override void Initialize()
        {
            SystemManagers.Default = new SystemManagers();
            SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);

            LoadGumProject();

            ShowScreen("StartScreen");
            
            base.Initialize();
        }

        private void ShowScreen(string screenName)
        {
            var gumScreenSave = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name == screenName);

            var isAlreadyShown = false;
            if (currentScreenElement != null)
            {
                isAlreadyShown = currentScreenElement.Tag == gumScreenSave;
            }

            if (!isAlreadyShown)
            {
                currentScreenElement?.RemoveFromManagers();
                currentScreenElement = gumScreenSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: true);
            }

        }

        private static GumProjectSave LoadGumProject()
        {
            var gumProject = GumProjectSave.Load("GumProject.gumx", out _);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            return gumProject;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);

            DoSwapScreenLogic();

            base.Update(gameTime);
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
                ShowScreen("StateScreen");

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

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            SystemManagers.Default.Draw();

            base.Draw(gameTime);
        }
    }
}
