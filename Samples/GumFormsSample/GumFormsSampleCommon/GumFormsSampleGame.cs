using Gum.Wireframe;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using System.Diagnostics;
using MonoGameGum.Input;
using GumFormsSample.Screens;

namespace GumFormsSample
{
    public class GumFormsSampleGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;


        public GumFormsSampleGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
#if (ANDROID || iOS)
            graphics.IsFullScreen = true;
#endif
        }


        protected override void Initialize()
        {
            SystemManagers.Default = new SystemManagers(); 
            SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);
            FormsUtilities.InitializeDefaults();

            var screen = new FrameworkElementExampleScreen();
            // Uncommment to see customization:
            //var screen = new FormsCustomizationScreen();
            screen.Initialize(FrameworkElement.Root);

            base.Initialize();

        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: Use this.Content to load your game content here
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            var cursor = FormsUtilities.Cursor;



            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            GamePadState gamePadState = default;
            try { gamePadState = GamePad.GetState(PlayerIndex.One); }
            catch (NotImplementedException) { /* ignore gamePadState */ }


            if (keyboardState.IsKeyDown(Keys.Escape) ||
                keyboardState.IsKeyDown(Keys.Back) ||
                gamePadState.Buttons.Back == ButtonState.Pressed)
            {
                int m = 3;
            }



            FormsUtilities.Update(gameTime);

            string windowOver = "<null>";
            if(cursor.WindowOver != null)
            {
                windowOver = $"{cursor.WindowOver.GetType().Name}" ;
            }

            // Uncomment this to see the current window over every frame
            //System.Diagnostics.Debug.WriteLine($"Window over: {windowOver} @ x:{cursor.WindowOver?.X}");

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
