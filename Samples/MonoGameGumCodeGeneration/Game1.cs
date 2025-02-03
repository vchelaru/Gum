using Gum.DataTypes;
using Gum.Managers;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
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
            var gumProject = GumService.Default.Initialize(this, "GumProject/GumProject.gumx");

            var screenGue = new MainMenuFullGenerationRuntime();
            screenGue.AddToManagers();
            screenGue.Name = "MainMenu Screen";

            base.Initialize();
        }


        protected override void Update(GameTime gameTime)
        {
            GumService.Default.Update(this, gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            GumService.Default.Draw();
            base.Draw(gameTime);
        }
    }
}
