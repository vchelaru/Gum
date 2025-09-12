using Gum.Wireframe;
using GumFormsSample.Logging;
using GumFormsSample.Screens;
using GumFormsSample.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using System;

namespace GumFormsSample
{
    public class GumFormsSampleGame : Game, IDisposable
    {
        private readonly GraphicsDeviceManager _graphics;
        private readonly GumFormsSampleConfig _config = new();
        private SpriteBatch _spriteBatch;
        private RenderTarget2D _renderTarget;
        private readonly GumFormsSampleScreenFactory _screenFactory = new();
        private readonly InputService _inputService = new();
        private readonly RenderService _renderService = new();
        private readonly IGumFormsSampleLogger _logger = new DebugLogger();
        private BindableGue _currentScreen;
        private bool _disposed;
        GumService GumUI => GumService.Default;

        public GumFormsSampleGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _config.Apply(_graphics);
        }

        protected override void Initialize()
        {
            _renderTarget = new RenderTarget2D(GraphicsDevice, _config.Width, _config.Height);
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            GumUI.Initialize(this, "FormsGumProject/GumProject.gumx");
            // temporary until this is pulled from .gumx
            RenderingLibrary.Graphics.Text.IsMidWordLineBreakEnabled = true;
            GumUI.Cursor.TransformMatrix = Matrix.CreateScale(1 / _config.Scale);

            _currentScreen = _screenFactory.DefaultScreen;
            _currentScreen.AddToRoot();


            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            GumUI.Update(gameTime);
            if(InteractiveGue.CurrentInputReceiver == null)
            {
                int keyResult = _inputService.Update();
                if (keyResult >= 0 && keyResult <= 6)
                {
                    if (_currentScreen != null)
                    {
                        GumUI.Root.Children.Remove(_currentScreen);
                    }

                    _currentScreen = _screenFactory.CreateScreen(keyResult);
                    _currentScreen.AddToRoot();
                }
            }

            foreach (var item in GumUI.Root.Children)
            {
                (item as IUpdateScreen)?.Update(gameTime);
            }

            System.Diagnostics.Debug.WriteLine(GumUI.Cursor.WindowOver);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _renderService.Draw(GraphicsDevice, _renderTarget, _spriteBatch, _config);
            base.Draw(gameTime);
        }

    }
}