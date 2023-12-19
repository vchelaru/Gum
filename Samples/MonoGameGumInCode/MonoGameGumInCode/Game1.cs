using Gum.Converters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using RenderingLibrary;

namespace MonoGameGumInCode
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

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


            var TextInstance = new TextRuntime();
            TextInstance.AddToManagers(SystemManagers.Default, null);

            TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            TextInstance.Text = "Helloi";
            TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            TextInstance.X = 0f;
            TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            TextInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            TextInstance.Y = 0f;
            TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            TextInstance.YUnits = GeneralUnitType.PixelsFromSmall;



            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

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
