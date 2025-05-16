using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using System;

namespace MonoGameAndGum
{
    /// <summary>
    /// The main game class, integrating MonoGame with Gum for rendering andsing and UI.
    /// </summary>
    public class Game1 : Game, IDisposable
    {
        private readonly GumService _gumService;
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private bool _disposed;
        private GraphicalUiElement Root;

        /// <summary>
        /// Initializes a new instance of the <see cref="Game1"/> class.
        /// </summary>
        /// <param name="gumService">The Gum service for UI management.</param>
        /// <exception cref="ArgumentNullException">Thrown if gumService is null.</exception>
        public Game1(GumService gumService)
        {
            _gumService = gumService ?? throw new ArgumentNullException(nameof(gumService));
            _graphics = new GraphicsDeviceManager(this);
            // Set content root directory
            Content.RootDirectory = Configuration.ContentRoot;
            IsMouseVisible = true;
        }

        /// <summary>
        /// Initializes the game, setting up Gum UI and managers.
        /// </summary>
        protected override void Initialize()
        {
            // Initialize Gum with the project file
            _gumService.Initialize(this, Configuration.GumProjectPath);
            var screen = new MainMenuRuntime();
            screen.AddToManagers();
            Root = screen;

            base.Initialize();
        }

        /// <summary>
        /// Loads game content, such as the sprite batch.
        /// </summary>
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        /// <summary>
        /// Updates the game state and Gum UI.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            _gumService.Update(this, gameTime, Root);
            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the game, clearing the screen and rendering Gum UI.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _gumService.Draw();
            base.Draw(gameTime);
        }

        /// <summary>
        /// Disposes of game resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose; false if from finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Dispose managed resources
                _spriteBatch?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Static configuration class for game settings.
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Gets the root directory for game content.
        /// </summary>
        public static string ContentRoot => "Content";

        /// <summary>
        /// Gets the path to the Gum project file.
        /// </summary>
        public static string GumProjectPath => "GumProject/GumProject.gumx";
    }
}