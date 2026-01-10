using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGumCodeGeneration.Screens;

namespace MonoGameGumCodeGeneration
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private MainMenuFullGeneration _mainMenu;
        private bool _disposed;
        GumService GumUI => GumService.Default;

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
            GumUI.Initialize(this, "GumProject/GumProject.gumx");
            _mainMenu = new MainMenuFullGeneration
            {
                Name = "MainMenu"
            };
            _mainMenu.AddToRoot();
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            GumUI.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            GumUI.Draw();
            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            GumUI.Root.Children.Clear();
            //_mainMenu?.RemoveFromManagers(); // using GumUI.Root.Children.Clear(); instead
            base.UnloadContent();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                GumUI.Root.Children.Clear();
                //_mainMenu?.RemoveFromManagers();
                _mainMenu = null;
                _graphics?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}