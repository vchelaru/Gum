using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

public class GumServiceUnitTests : BaseTestClass
{
    [Fact]
    public void TestGumServiceInitialization()
    {
        using var game = new Game1();
        game.RunOneFrame();
    }



    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        GumService GumUI => GumService.Default;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);

        }

        protected override void Update(GameTime gameTime)
        {
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
        }
    }



}
