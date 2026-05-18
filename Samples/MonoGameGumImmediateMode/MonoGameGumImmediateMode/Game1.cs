using System;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGumImmediateMode.Screens;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace MonoGameGumImmediateMode
{
    /// <summary>
    /// Sample showing GumBatch (immediate-mode rendering) in action. A retained-mode
    /// nav strip at the top — built with Gum Forms controls — switches between
    /// example screens. Each screen demonstrates a different way to use GumBatch.
    ///
    /// Fonts are generated in memory by KernSmith — there are no .fnt files
    /// shipped with this sample.
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private GumBatch _gumBatch;

        private StackPanel _navStrip;
        private IImmediateModeScreen _currentScreen;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // GumService initialization sets up SystemManagers, Forms input, and
            // everything needed for retained-mode controls. We use it for the nav
            // strip; the example screens themselves use GumBatch in Draw.
            GumService.Default.Initialize(this);

            // Wire up KernSmith so any TextRuntime or DrawString call can get a
            // font for any (family, size, style) without a .fnt file on disk.
            CustomSetPropertyOnRenderable.InMemoryFontCreator =
                new KernSmithFontCreator(GraphicsDevice);

            _gumBatch = new GumBatch();
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            BuildNavStrip();
            ShowScreen(new DrawStringScreen());

            base.Initialize();
        }

        private void BuildNavStrip()
        {
            _navStrip = new StackPanel();
            _navStrip.Orientation = Orientation.Horizontal;
            _navStrip.Spacing = 4;
            _navStrip.Visual.X = 4;
            _navStrip.Visual.Y = 4;
            _navStrip.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            _navStrip.Width = 0;
            _navStrip.Visual.WrapsChildren = true;
            _navStrip.AddToRoot();

            AddNavButton("DrawString", () => ShowScreen(new DrawStringScreen()));
            AddNavButton("TextRuntime", () => ShowScreen(new TextRuntimeScreen()));
            AddNavButton("Parent/Child", () => ShowScreen(new ParentChildScreen()));
            AddNavButton("RenderTarget", () => ShowScreen(new RenderTargetScreen()));
        }

        private void AddNavButton(string text, Action onClick)
        {
            Button button = new Button();
            button.Text = text;
            button.Click += (_, _) => onClick();
            _navStrip.AddChild(button);
        }

        private void ShowScreen(IImmediateModeScreen screen)
        {
            _currentScreen?.Dispose();
            _currentScreen = screen;
            _currentScreen.Initialize(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            GumService.Default.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // The current example screen does its immediate-mode draws first…
            _currentScreen?.Draw(_gumBatch, _spriteBatch);

            // …then GumService.Default.Draw() renders the retained-mode nav strip
            // on top, so the buttons stay visible no matter what the screen drew.
            GumService.Default.Draw();

            base.Draw(gameTime);
        }
    }
}
