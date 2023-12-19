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

            var container = new ContainerRuntime();
            container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            container.Width = 0;
            container.Height = 0;
            container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

            container.AddToManagers();


            var TextInstance = new TextRuntime();
            TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.Width = 0;
            TextInstance.Text = "This is a rectangle:";
            container.Children.Add(TextInstance);
            //TextInstance.AddToManagers();





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
