using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using System;

namespace MonoGameAndGum
{
    public class Game1 : Game, IDisposable
    {
        private readonly GumService _gumService;
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private bool _disposed;

        private GraphicalUiElement Root;

        public Game1(GumService gumService)
        {
            _gumService = gumService ?? throw new ArgumentNullException(nameof(gumService));
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = Configuration.ContentRoot; // Use configuration
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _gumService.Initialize(this, Configuration.GumProjectPath); // Use configuration
            var screen = new MainMenuRuntime();
            screen.AddToManagers();
            Root = screen;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            _gumService.Update(this, gameTime, Root);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _gumService.Draw();
            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _spriteBatch?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }

    // Example configuration class (to be implemented)
    public static class Configuration
    {
        public static string ContentRoot => "Content";
        public static string GumProjectPath => "GumProject/GumProject.gumx";
    }
}