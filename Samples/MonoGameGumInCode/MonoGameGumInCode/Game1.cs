using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Gum;
using KernSmith.Gum;
using MonoGameGumInCode.Screens;
using RenderingLibrary;

namespace MonoGameGumInCode
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private StackPanel _navStrip;
        private FrameworkElement _currentScreen;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            // ContainerRuntime.IsRenderTarget=true cells (see SpriteScreen's
            // alpha-blend row) switch the active render target mid-frame. With
            // the default RenderTargetUsage.DiscardContents, everything drawn
            // to the back buffer before the first RT switch is wiped, which
            // shows up as earlier rows rendering as expected initially and
            // then disappearing the moment any RT cell appears. PreserveContents
            // keeps the back buffer intact across target switches.
            _graphics.PreparingDeviceSettings += (_, e) =>
            {
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                    RenderTargetUsage.PreserveContents;
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            GumService.Default.Initialize(this);

            CustomSetPropertyOnRenderable.InMemoryFontCreator =
                new KernSmithFontCreator(GraphicsDevice);

            // Issue #3206: Gum core ships no shader loader, so the app registers a resolver that
            // turns a ContainerRuntime.SourceShaderFile (.fx path) into a platform Effect. Here it
            // compiles the .fx at runtime via ShadowDusk (see RenderTargetEffectScreen). With no
            // resolver registered, SourceShaderFile is a graceful no-op (the container renders
            // unshaded), matching how a missing texture degrades.
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver =
                path => RenderTargetEffectScreen.CompileEffectFromFile(path);

            // Demo the auto-fit helpers — flip via the Zoom/Expand buttons in the nav strip.
            GumService.Default.EnableZoomToWindow();

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
            _navStrip.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            _navStrip.Width = 0;
            _navStrip.Visual.WrapsChildren = true;
            _navStrip.AddToRoot();

            AddNavButton("Forms", () => ShowScreen<FormsScreen>());
            AddNavButton("Standards", () => ShowScreen<StandardsScreen>());
            AddNavButton("Circles", () => ShowScreen<CirclesScreen>());
            AddNavButton("Rectangles", () => ShowScreen<RectanglesScreen>());
            AddNavButton("Arcs", () => ShowScreen<ArcsScreen>());
            AddNavButton("Polygons", () => ShowScreen<PolygonsScreen>());
            AddNavButton("Gradients", () => ShowScreen<GradientScreen>());
            AddNavButton("Text", () => ShowScreen<TextScreen>());
            AddNavButton("Mixed", () => ShowScreen<MixedScreen>());
            AddNavButton("Invisible", () => ShowScreen<InvisibleScreen>());
            AddNavButton("NineSlice", () => ShowScreen<NineSliceScreen>());
            AddNavButton("Sprite", () => ShowScreen<SpriteScreen>());
            AddNavButton("Clip", () => ShowScreen<ClippingScreen>());
            AddNavButton("RT Effect", () => ShowScreen<RenderTargetEffectScreen>());
            AddNavButton("Render Target", () => ShowScreen<RenderTargetScreen>());

            AddFitModeRadio("Zoom", isChecked: true, () => GumService.Default.EnableZoomToWindow());
            AddFitModeRadio("Expand", isChecked: false, () => GumService.Default.EnableExpandToWindow());
        }

        private void AddFitModeRadio(string text, bool isChecked, System.Action onChecked)
        {
            var radio = new RadioButton();
            radio.GroupName = "FitMode";
            radio.Text = text;
            radio.IsChecked = isChecked;
            radio.Checked += (_, _) => onChecked();
            _navStrip.AddChild(radio);
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
            _currentScreen.Visual.Y = _navStrip.Visual.GetAbsoluteHeight();
            _currentScreen.Visual.Height = -_navStrip.Visual.GetAbsoluteHeight();
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
