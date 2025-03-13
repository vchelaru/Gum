using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;

using System.Linq;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using MonoGameGum.Forms.Controls;
using Gum.Wireframe;
using FnaSample.Screens;

namespace FnaSample
{
    public partial class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        SpriteBatch _spriteBatch;

        GraphicalUiElement Root;

        public Game1() : base()
        {
            graphics = new GraphicsDeviceManager(this);


            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
        }

        Texture2D TextureFile;

        protected override void Initialize()
        {

            GumService.Default.Initialize(this, "GumProject/GumProject.gumx");

            IsMouseVisible = true;

            Root = new MainScreenRuntime();
            Root.AddToManagers();

            //var rectangle = new ColoredRectangleRuntime();

            //Root = new ContainerRuntime();
            //Root.AddToManagers();

            //var button = new Button();
            //button.Text = "Click Me!";
            //Root.Children.Add(button.Visual);

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            GumService.Default.Update(this, gameTime, Root);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GumService.Default.Draw();

            base.Draw(gameTime);
        }
    }
}
