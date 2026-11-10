using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Gum;
using KernSmith.Gum;
using Gum.Renderables;
using GumSamples;
using RenderingLibrary;

namespace MonoGameGumInCode
{
    public class Game1 : Game
    {
        GumService GumUI => GumService.Default;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private readonly GumGallery gallery = new GumGallery();

#if DEBUG
        // Plays an NDJSON script when launched with --gum-automation <script>; null otherwise.
        // Automation/smoke.ndjson has the launch command; GumAutomationHost documents the flags.
        private readonly Gum.Automation.GumAutomationHost _automation;
#endif

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            // Apos.Shapes (the shape fill/effect renderer behind CircleRuntime/RectangleRuntime/
            // ArcRuntime) uses a Shader Model 4 effect, which requires the HiDef profile — Reach
            // cannot load it and shapes silently fail to draw.
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            _graphics.PreferredBackBufferWidth = 1400;
            _graphics.PreferredBackBufferHeight = 900;
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

#if DEBUG
            _automation = Gum.Automation.GumAutomationHost.FromArgs(System.Environment.GetCommandLineArgs(), this);
#endif
        }

        protected override void Initialize()
        {
            GumUI.Initialize(this);

            GumUI.UseKeyboardDefaults();

            GumUI.UseGamepadDefaults();

            // Gum core ships no shader loader. The Gum.MonoGame.ShadowDusk package registers one that
            // compiles a ContainerRuntime.SourceShaderFile (.fx or .slang path) at runtime (#3206, #7053).
            // With no resolver registered, a SourceShaderFile fails like a missing file
            // (call UseShadowDusk() before building any screen that uses one).
            GumUI.UseShadowDusk();

            // Required for Apos.Shapes-backed shape fill/effects (see the shapes doc). Must come
            // after GumUI.Initialize, which gives it the graphics device.
            GumUI.UseShapes();

            // Wire up KernSmith so any TextRuntime can get a font for any (family, size, style)
            // without a .fnt file on disk.
            GumUI.UseKernSmith();

            // Demo the auto-fit helpers — flip via the Zoom/Expand buttons in the nav strip.
            GumUI.EnableZoomToWindow();

            // Mixed is the broadest demo, so it's the default landing page.
            gallery.Build("Mixed");

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            bool isAutomated = false;
#if DEBUG
            _automation?.BeforeUpdate();
            // An automation run reads no OS input; the script's scripted cursor and keyboard drive Gum.
            isAutomated = _automation != null;
#endif
            if (!isAutomated && (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape)))
                Exit();

            GumUI.Update(gameTime);

            gallery.Tick(gameTime.ElapsedGameTime.TotalSeconds);

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

            GumUI.Draw();

            base.Draw(gameTime);
        }
    }
}
