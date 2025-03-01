using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using System.Linq;

namespace MonoGameAndGum
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        GraphicalUiElement Root;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            MonoGameGum.GumService.Default.Initialize(this, "GumProject/GumProject.gumx");

            //var screen = ObjectFinder.Self.GumProjectSave.Screens.First();
            //Root = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers:true);
            var screen = new MainMenuRuntime();
            screen.AddToManagers();
            Root = screen;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            MonoGameGum.GumService.Default.Update(this, gameTime, Root);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            MonoGameGum.GumService.Default.Draw();

            base.Draw(gameTime);
        }
    }
}
