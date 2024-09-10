using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using ToolsUtilities;

namespace FntLoading
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;

        KeyboardState lastState = new KeyboardState();
        KeyboardState currentState = new KeyboardState();
        BitmapFont bitmapFont;

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

            bitmapFont = new BitmapFont("DummyFont.fnt", SystemManagers.Default);

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            lastState = currentState;
            currentState = Keyboard.GetState();


            if(currentState.IsKeyDown(Keys.Space) && lastState.IsKeyUp(Keys.Space))
            {
                System.Diagnostics.Debug.WriteLine("Loading font pattern...");

                // Load it before 
                string fontContents = FileManager.FromFileText("FontMedievalSharp_Bold30.fnt");

                // you can replace this with your own timing, but it's so slow
                // that this is good enough resolution...
                var before = DateTime.Now;
                bitmapFont.SetFontPattern(fontContents);
                var after = DateTime.Now;

                System.Diagnostics.Debug.WriteLine("Time to load: " + (after - before).TotalMilliseconds.ToString("0.0"));
            }
            SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);


            base.Draw(gameTime);
        }
    }
}
