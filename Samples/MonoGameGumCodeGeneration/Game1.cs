using Gum.DataTypes;
using Gum.Managers;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGumCodeGeneration.Components;
using MonoGameGumCodeGeneration.Screens;
using RenderingLibrary;
using System.Linq;
using ToolsUtilities;

namespace MonoGameGumCodeGeneration
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            SystemManagers.Default = new SystemManagers(); 
            SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);

            ElementSaveExtensions.RegisterGueInstantiationType(
                "Popup",
                typeof(PopupRuntime)
            );
            ElementSaveExtensions.RegisterGueInstantiationType(
                "ComponentWithStates",
                typeof(ComponentWithStatesRuntime)
            );

            ElementSaveExtensions.RegisterGueInstantiationType(
                "MainMenu",
                typeof(MainMenuRuntime)
            );


            var gumProject = GumProjectSave.Load("GumProject/GumProject.gumx");

            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();

            FileManager.RelativeDirectory = "Content/GumProject/";

            // This assumes that your project has at least 1 screen
            var screenGue = gumProject.Screens.First().ToGraphicalUiElement(
                SystemManagers.Default, addToManagers: true);


            base.Initialize();
        }


        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            SystemManagers.Default.Draw();

            base.Draw(gameTime);
        }
    }
}
