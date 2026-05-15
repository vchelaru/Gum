using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGumInCode.Screens;
using RenderingLibrary;

namespace MonoGameGumInCode
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private const float NavStripHeight = 40;

        private StackPanel _navStrip;
        private FrameworkElement _currentScreen;

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
            GumService.Default.Initialize(this);

            BuildNavStrip();

            // Mixed is the broadest demo, so it's the default landing page.
            ShowScreen<MixedScreen>();

            base.Initialize();
        }

        private void BuildNavStrip()
        {
            _navStrip = new StackPanel();
            _navStrip.Orientation = Orientation.Horizontal;
            _navStrip.Spacing = 4;
            _navStrip.Visual.X = 4;
            _navStrip.Visual.Y = 4;
            _navStrip.AddToRoot();

            AddNavButton("Forms", () => ShowScreen<FormsScreen>());
            AddNavButton("Standards", () => ShowScreen<StandardsScreen>());
            AddNavButton("Circles", () => ShowScreen<CirclesScreen>());
            AddNavButton("Text", () => ShowScreen<TextScreen>());
            AddNavButton("Mixed", () => ShowScreen<MixedScreen>());
            AddNavButton("Invisible", () => ShowScreen<InvisibleScreen>());
            AddNavButton("NineSlice", () => ShowScreen<NineSliceScreen>());
        }

        private void AddNavButton(string text, System.Action onClick)
        {
            var button = new Button();
            button.Text = text;
            button.Click += (_, _) => onClick();
            _navStrip.AddChild(button);
        }

        private void ShowScreen<T>() where T : FrameworkElement, new()
        {
            if (_currentScreen != null)
            {
                _currentScreen.RemoveFromRoot();
            }

            _currentScreen = new T();
            // Offset the screen so it doesn't sit underneath the nav strip. The screen's
            // ctor calls Dock(Fill); reset the vertical anchoring to top + shrink height
            // by the nav strip's footprint so the two never overlap.
            _currentScreen.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Top;
            _currentScreen.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            _currentScreen.Visual.Y = NavStripHeight;
            _currentScreen.Visual.Height = -NavStripHeight;
            _currentScreen.AddToRoot();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            GumService.Default.Update(gameTime);

            bool moveCameraWithMouse = false;
            if (moveCameraWithMouse)
            {
                MoveCameraWithMouse();
            }

            base.Update(gameTime);
        }

        private static void MoveCameraWithMouse()
        {
            var camera = SystemManagers.Default.Renderer.Camera;
            var mouseState = Mouse.GetState();
            camera.X = mouseState.X;
            camera.Y = mouseState.Y;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            GumService.Default.Draw();

            base.Draw(gameTime);
        }
    }
}
