using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Gum.Forms;
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

        ColoredRectangleRuntime redBackgroundRectangle;
        ColoredRectangleRuntime halfTransparentRectangle;

        Text text;

        BitmapFont font;
        GumBatch gumBatch;
        // We can mix SpriteBatch with GumBatch draws, including using RenderTargets
        SpriteBatch spriteBatch;

        RenderTarget2D renderTarget;

        float xForMatrix = 0;
        float yForMatrix = 0;

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

            // Use FormsUtilities to get access to keyboard/mouse
            FormsUtilities.InitializeDefaults();

            // "Runtime" objects such as TextRuntime, SpriteRuntime, and ColoredRectangleRuntime
            // are Gum objects which inherit from GraphicalUiElement. They have full support for
            // all of Gum's layout rules (such as X/Y units, origins, and width/height units)
            textRuntime = new TextRuntime();
            textRuntime.UseCustomFont = true;
            textRuntime.CustomFontFile = "Fonts/Font16Jing_Jing.fnt";
            textRuntime.Text = "I am an immediate mode TextRuntime";
            textRuntime.X = 0;
            textRuntime.Y = 50;

            // Any type is supported, not just Text. For example, ColoredRectangleRuntime:
            redBackgroundRectangle = new ColoredRectangleRuntime();
            redBackgroundRectangle.Width = 1000;
            redBackgroundRectangle.Height = 1000;
            redBackgroundRectangle.Color = Color.Red;

            halfTransparentRectangle= new ColoredRectangleRuntime();
            halfTransparentRectangle.Width = 200;
            halfTransparentRectangle.Height = 100;
            halfTransparentRectangle.Color = Color.White;
            halfTransparentRectangle.Alpha = 128;

            


            var blendState = new BlendState();

            blendState.ColorSourceBlend = BlendState.NonPremultiplied.ColorSourceBlend;
            blendState.ColorDestinationBlend = BlendState.NonPremultiplied.ColorDestinationBlend;
            blendState.ColorBlendFunction = BlendState.NonPremultiplied.ColorBlendFunction;

            blendState.AlphaSourceBlend = Blend.SourceAlpha;
            blendState.AlphaDestinationBlend = Blend.DestinationAlpha;
            blendState.AlphaBlendFunction = BlendFunction.Add;

            halfTransparentRectangle.BlendState = blendState;

            gumBatch = new GumBatch();
            spriteBatch = new SpriteBatch(GraphicsDevice);

            font = new BitmapFont("Fonts/Font18Caladea.fnt", SystemManagers.Default);

            renderTarget = new RenderTarget2D(GraphicsDevice, 300, 300);

            base.Initialize();
        }


        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            FormsUtilities.Update(this, gameTime, (GraphicalUiElement) null);

            if(FormsUtilities.Keyboard.KeyPushed(Keys.Left))
            {
                xForMatrix -= 10;
            }
            if (FormsUtilities.Keyboard.KeyPushed(Keys.Right))
            {
                xForMatrix += 10;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {

            GraphicsDevice.SetRenderTarget(renderTarget);
            {
                gumBatch.Begin();
                gumBatch.Draw(redBackgroundRectangle);
                gumBatch.Draw(redBackgroundRectangle);
                gumBatch.Draw(halfTransparentRectangle);
                gumBatch.Draw(halfTransparentRectangle);
                gumBatch.End();
            }
            GraphicsDevice.SetRenderTarget(null);

            GraphicsDevice.Clear(Color.CornflowerBlue);

            var matrix= Matrix.CreateTranslation(xForMatrix, yForMatrix, 0);

            gumBatch.Begin(matrix);

            // We can do immediate mode without creating any objects by calling DrawString:
            gumBatch.DrawString(font, $"This is using Gum Batch, with translation {xForMatrix}, {yForMatrix}", new Vector2(10, 10), Color.White);

            gumBatch.End();

            spriteBatch.Begin();
            spriteBatch.Draw(renderTarget, new Vector2(50, 40), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
