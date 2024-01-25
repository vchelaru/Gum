using Gum.Converters;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;

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


            // uncomment one of these to create a layout. Only have one uncommented or else UI overlaps
            //CreateMixedLayout();
            //CreateTextLayout();
            CreateInvisibleLayout();

            base.Initialize();
        }

        private void CreateInvisibleLayout()
        {
            GraphicalUiElement.CanvasWidth = 800;
            GraphicalUiElement.CanvasHeight = 600;

            GraphicalUiElement parentContainer = new(new InvisibleRenderable(), null!)
            {
                X = 5,
                Y = 5,
                Width = 40,
                Height = 0,

                HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren,
                ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack,
                WrapsChildren = true
            };

            for (int i = 0; i < 10; i++)
            {
                GraphicalUiElement buttonWrapper = new(new InvisibleRenderable(), null!)
                {
                    WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
                    Width = 20,
                    XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Left,
                    XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall,
                    X = 0,

                    HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
                    Height = 1
                };

                parentContainer.Children.Add(buttonWrapper);
            }

            parentContainer.UpdateLayout();
        }

        private void CreateTextLayout()
        {
            var container = new ContainerRuntime();
            container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            // Give it 2 pixels on each side so text doesn't bump up against the edge of the screen
            container.X = 2;
            container.Y = 2;
            container.Width = -4;
            container.Height = -4;
            container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            container.StackSpacing = 4;
            container.AddToManagers();

            var textRuntime = new TextRuntime();
            textRuntime.Text = "Hi, I'm default text";
            container.Children.Add(textRuntime);

            var withOutline = new TextRuntime();
            withOutline.Text = "I am text that has an outline.";
            (withOutline.Component as RenderingLibrary.Graphics.Text).RenderBoundary = true;
            container.Children.Add(withOutline);
        }

        private static void CreateMixedLayout()
        {
            var container = new ContainerRuntime();
            container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            container.Width = 0;
            container.Height = 0;
            container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            container.AddToManagers();

            AddText(container, "This is a rectangle:");

            var coloredRectangleInstance = new ColoredRectangleRuntime();
            coloredRectangleInstance.X = 10;
            coloredRectangleInstance.Y = 10;
            coloredRectangleInstance.Width = 120;
            coloredRectangleInstance.Height = 48;
            coloredRectangleInstance.Color = Color.White;
            container.Children.Add(coloredRectangleInstance);

            AddText(container, "This is a sprite:");

            var sprite = new SpriteRuntime();
            sprite.X = 10;
            sprite.Y = 10;
            sprite.SourceFileName = "Content/BearTexture.png";
            container.Children.Add(sprite);

            AddText(container, "This is a NineSlice:");
            var nineSlice = new NineSliceRuntime();
            nineSlice.X = 10;
            nineSlice.Y = 10;
            nineSlice.SourceFileName = "Content/Frame.png";
            nineSlice.Width = 256;
            nineSlice.Height = 48;
            container.Children.Add(nineSlice);

            var customText = new TextRuntime();
            customText.Width = 300;
            customText.UseCustomFont = true;
            customText.CustomFontFile = "WhitePeaberryOutline/WhitePeaberryOutline.fnt";
            customText.Text = "Hello, I am using a custom font.\nPretty cool huh?";
            container.Children.Add(customText);


        }

        private static void AddText(ContainerRuntime container, string text)
        {
            var textInstance = new TextRuntime();
            textInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            textInstance.Width = 0;
            textInstance.Text = text;
            container.Children.Add(textInstance);
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
