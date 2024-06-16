using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using RenderingLibrary;

namespace GumFormsSample
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        ContainerRuntime Root;
        Cursor cursor;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            SystemManagers.Default = new SystemManagers(); 
            SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);
            cursor = new Cursor();

            FrameworkElement.DefaultFormsComponents[typeof(Button)] = 
                typeof(DefaultButtonRuntime);
            FrameworkElement.MainCursor = cursor;

            Root = new ContainerRuntime();
            Root.Width = 0;
            Root.Height = 0;
            Root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            Root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            Root.AddToManagers();


            var button = new Button();
            Root.Children.Add(button.Visual);
            button.X = 0;
            button.Y = 0;
            button.Width = 100;
            button.Height = 50;
            button.Text = "Hello MonoGame!";



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
            cursor.Activity(gameTime.TotalGameTime.TotalSeconds);
            Root.DoUiActivityRecursively(cursor);

            SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            SystemManagers.Default.Draw();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
