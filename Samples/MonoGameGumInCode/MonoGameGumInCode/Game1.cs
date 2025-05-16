using Gum.Converters;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using MonoGameGumInCode.Screens;
using MonoGameGumInCode.Services;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;

namespace MonoGameGumInCode
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        Layer layer;
        private readonly InputService _inputService = new();
        private GraphicalUiElement _currentScreen;
        private readonly ScreenFactory _screenFactory = new();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            GumService.Default.Initialize(this);

            // adjust this to zoom in or out
            //SystemManagers.Default.Renderer.Camera.Zoom = 3;
            // This can be used to make everything render with linear:
            //Renderer.TextureFilter = TextureFilter.Linear;

            // uncomment one of these to create a layout. Only have one uncommented or else UI overlaps
            //CreateFormsScreen();
            //CreateMixedLayout();
            //CreateTextLayout();
            //CreateInvisibleLayout();

            _currentScreen = _screenFactory.DefaultScreen;
            _currentScreen.AddToRoot();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            GumService.Default.Update(this, gameTime);

            int keyResult = _inputService.Update();
            if (keyResult >= 0 && keyResult <= 5)
            {
                if (_currentScreen != null)
                {
                    GumService.Default.Root.Children.Remove(_currentScreen);
                }

                _currentScreen = _screenFactory.CreateScreen(keyResult);
                _currentScreen.AddToRoot();
            }

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
