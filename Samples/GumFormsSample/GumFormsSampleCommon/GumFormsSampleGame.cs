using Gum.Wireframe;
using GumFormsSample.Logging;
using GumFormsSample.Screens;
using GumFormsSample.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGum.Forms;
using System;

namespace GumFormsSample
{
    public class GumFormsSampleGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private readonly GumFormsSampleConfig _config = new();
        private SpriteBatch _spriteBatch;
        private RenderTarget2D _renderTarget;
        private readonly GumFormsSampleScreenFactory _screenFactory = new();
        private readonly InputService _inputService = new();
        private readonly RenderService _renderService = new();
        private readonly IGumFormsSampleLogger _logger = new DebugLogger();
        private BindableGue _currentScreen; // Track active screen

        public GumFormsSampleGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _config.Apply(_graphics);
        }

        protected override void Initialize()
        {
            try
            {
                _renderTarget = new RenderTarget2D(GraphicsDevice, _config.Width, _config.Height);
                _spriteBatch = new SpriteBatch(GraphicsDevice);

                GumService.Default.Initialize(this, "FormsGumProject/GumProject.gumx");
                FormsUtilities.Cursor.TransformMatrix = Matrix.CreateScale(1 / _config.Scale);

                _currentScreen = _screenFactory.CreateScreen(1);
                if (_currentScreen is IGumFormsSampleScreen demoScreen)
                {
                    demoScreen.Initialize();
                }
                _currentScreen.AddToRoot();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Initialization failed: {ex.Message}");
                Exit();
            }

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            try
            {
                int keyResult = _inputService.Update();
                if (keyResult >= 0 && keyResult <= 5)
                {
                    // Remove current screen
                    if (_currentScreen != null)
                    {
                        GumService.Default.Root.Children.Remove(_currentScreen);
                    }

                    // Create and show new screen
                    _currentScreen = _screenFactory.CreateScreen(keyResult);
                    if (_currentScreen is IGumFormsSampleScreen demoScreen)
                    {
                        demoScreen.Initialize();
                    }
                    _currentScreen.AddToRoot();
                }

                GumService.Default.Update(this, gameTime);

                foreach (var item in GumService.Default.Root.Children)
                {
                    (item as IUpdateScreen)?.Update(gameTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update failed: {ex.Message}");
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _renderService.Draw(GraphicsDevice, _renderTarget, _spriteBatch, _config);
            base.Draw(gameTime);
        }
    }
}