using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;

using System.Linq;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FnaSample
{
    public partial class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        SpriteBatch _spriteBatch;

        public Game1() : base()
        {
            graphics = new GraphicsDeviceManager(this);


            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
        }

        Texture2D TextureFile;

        protected override void Initialize()
        {

            using var stream = System.IO.File.OpenRead("Content/GlobalContent/TextureFile.png");
            TextureFile =
                Texture2D.FromStream(GraphicsDevice, stream);
            _spriteBatch = new SpriteBatch(graphics.GraphicsDevice);

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();

            _spriteBatch.Draw(TextureFile, new Rectangle(0,0,150,150), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
