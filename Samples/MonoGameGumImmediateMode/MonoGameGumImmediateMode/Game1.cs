using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace MonoGameGumImmediateMode
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;

        // See Initialize for an explanation of the different types (such as TextRuntime vs normal Text)
        TextRuntime textRuntime;
        Text text;

        BitmapFont font;
        GumBatch gumBatch;

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

            // "Runtime" objects such as TextRuntime, SpriteRuntime, and ColoredRectangleRuntime
            // are Gum objects which inherit from GraphicalUiElement. They have full support for
            // all of Gum's layout rules (such as X/Y units, origins, and width/height units)...
            textRuntime = new TextRuntime();
            textRuntime.UseCustomFont = true;
            textRuntime.CustomFontFile = "Fonts/Font16Jing_Jing.fnt";
            textRuntime.Text = "I am an immediate mode TextRuntime";
            textRuntime.X = 0;
            textRuntime.Y = 50;

            // ...whereas "normal" Gum objects such as Text, Sprite, and ColoredRectangle are
            // renderables, but they always render according to their top-left corner, relative to 
            // either the top-left of the screen or the top-left of their parent.
            // These are lighter-weight and simpler to use, but do not have full support for Gum's
            // layout rules.
            text = new Text();
            text.X = 0;
            text.Y = 100;

            gumBatch = new GumBatch();

            font = new BitmapFont("Fonts/Font18Caladea.fnt", SystemManagers.Default);

            base.Initialize();
        }


        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // The Renderer is how Gum performs its rendering. 
            // We can use Begin/Draw/End just like if we were using a SpriteBatch.
            // If we do this, we do not need to call SystemManagers.Default.Draw(),
            // which is used for "retained mode" rendering.
            var renderer = SystemManagers.Default.Renderer;

            // call begin, just like if using a SpriteBatch
            renderer.Begin();
            renderer.Draw(textRuntime);
            renderer.Draw(text);
            renderer.End();

            // We can also do immediate mode without creating any objects:
            gumBatch.Begin();
            gumBatch.DrawString(font, "This is using Gum Batch", new Vector2(0, 150), Color.White);
            gumBatch.End();
            base.Draw(gameTime);
        }
    }
}
