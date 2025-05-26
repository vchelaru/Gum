using Gum.DataTypes;
using Gum.Managers;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGumCodeGeneration.Screens;
using RenderingLibrary;

namespace MonoGameGumCodeGeneration
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private MainMenuFullGenerationRuntime _mainMenu;
        private bool _disposed;
        GumService Gum => GumService.Default;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            Gum.Initialize(this, "GumProject/GumProject.gumx");
            _mainMenu = new MainMenuFullGenerationRuntime
            {
                Name = "MainMenu"
            };
            _mainMenu.AddToManagers();
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            Gum.Update(this, gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            Gum.Draw();
            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _mainMenu?.RemoveFromManagers();
            base.UnloadContent();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _mainMenu?.RemoveFromManagers();
                _mainMenu = null;
                _graphics?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}