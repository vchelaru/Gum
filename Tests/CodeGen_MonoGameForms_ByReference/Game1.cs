using CodeGenProject.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.Input;

namespace CodeGenProject
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        FormsScreenWithVariablesSet screen;
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

            GumUI.Initialize(this, "GumProject/CodeGenTestProject.gumx");

            screen= new Screens.FormsScreenWithVariablesSet();
            screen.AddToRoot();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            // TODO: Add your update logic here
            GumUI.Update(gameTime);

            var cursor = GumUI.Cursor;
            System.Diagnostics.Debug.WriteLine(cursor.FrameworkElementOver);
            var failureReason = cursor.GetEventFailureReason(screen.TextBoxInstance);
            System.Diagnostics.Debug.WriteLine(failureReason);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            GumUI.Draw();

            base.Draw(gameTime);
        }
    }
}
