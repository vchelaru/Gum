using System.Collections.Generic;
using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

// This immediate-mode sample intentionally uses the non-shape ColoredRectangleRuntime: no
// ShapeRenderer is initialized here, so the shape-based RectangleRuntime replacement it's
// obsoleted in favor of would silently not draw.
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete

namespace MonoGameGumImmediateMode.Screens
{
    /// <summary>
    /// Demonstrates two coordinate spaces coexisting in the same frame, using real Forms controls
    /// held in two containers: <see cref="_inGameContainerForRenderTarget"/> is drawn onto a low-resolution
    /// <see cref="RenderTarget2D"/> that is blitted to the backbuffer at 3x (visibly blurred by the
    /// default linear sampler), and <see cref="_aboveGameUiContainer"/> is drawn straight to the
    /// backbuffer at 1:1 on top of it. See "Several coordinate spaces at once" in the cursor docs
    /// (docs/code/events-and-interactivity/mouse-and-touch-screen-cursor.md).
    ///
    /// Neither container is added to <c>GumService.Default.Root</c>, so the plain
    /// <c>GumService.Default.Update(gameTime)</c> call in Game1 would skip their controls. This
    /// screen exposes them via <see cref="AdditionalUpdateRoots"/>, which Game1 combines with Root
    /// into a single per-frame Update call, so Forms interactivity (hover, click, drag on the
    /// Window) still runs for the Button/Menu/Window inside them. The game
    /// button is clickable despite the render target's scale because its container carries a
    /// <see cref="GraphicalUiElement.HitTestTransformMatrix"/> (issue #4096) that maps the raw
    /// window pixel back into render-target space; the menu and window need no transform because
    /// they are drawn straight to the backbuffer.
    /// </summary>
    public class RenderTargetScreen : IImmediateModeScreen
    {
        // A deliberately low resolution so the 3x blit is obviously blurred by the default linear
        // sampler, proving the render target really is being scaled up rather than drawn at 1:1.
        private const int GameWidth = 240;
        private const int GameHeight = 180;
        private const float BlitScale = 3f;
        private const float BlitY = 100f;

        private GraphicsDevice _graphicsDevice;
        private RenderTarget2D _renderTarget;

        private ColoredRectangleRuntime _gameBackground;
        private ContainerRuntime _inGameContainerForRenderTarget;
        private ContainerRuntime _aboveGameUiContainer;

        private readonly List<GraphicalUiElement> _roots = new List<GraphicalUiElement>();

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _renderTarget = new RenderTarget2D(graphicsDevice, GameWidth, GameHeight);

            _gameBackground = new ColoredRectangleRuntime();
            _gameBackground.Width = GameWidth;
            _gameBackground.Height = GameHeight;
            _gameBackground.Color = new Color(40, 110, 60);

            var gameButton = new Button();
            gameButton.Text = "Game Button";
            gameButton.Width = 120;
            gameButton.Height = 40;
            gameButton.Anchor(Anchor.Center);

            _inGameContainerForRenderTarget = new ContainerRuntime();
            // Don't fill, because this is using its own intetnal size
            //_inGameContainerForRenderTarget.Dock(Dock.Fill);
            _inGameContainerForRenderTarget.Width = GameWidth;
            _inGameContainerForRenderTarget.Height = GameHeight;

            _inGameContainerForRenderTarget.AddChild(_gameBackground);
            _inGameContainerForRenderTarget.AddChild(gameButton);

            var menu = new Menu();
            menu.Y = 48;
            var fileMenu = new MenuItem();
            fileMenu.Header = "File";
            fileMenu.Items.Add("Item under File");
            menu.Items.Add(fileMenu);
            var editMenu = new MenuItem();
            editMenu.Header = "Edit";
            editMenu.Items.Add("Item under Edit");
            menu.Items.Add(editMenu);

            var window = new Window();
            window.X = 620;
            window.Y = 160;
            window.Width = 260;
            window.Height = 160;
            var windowButton = new Button();
            windowButton.Text = "Button in Window";
            windowButton.Anchor(Anchor.Center);
            window.AddChild(windowButton);

            _aboveGameUiContainer = new ContainerRuntime();
            _aboveGameUiContainer.Dock(Dock.Fill);
            _aboveGameUiContainer.HasEvents = false; // so it doesn't eat all game events

            _aboveGameUiContainer.Children.Add(menu.Visual);
            _aboveGameUiContainer.Children.Add(window.Visual);

            _roots.Add(_inGameContainerForRenderTarget);
            _roots.Add(_aboveGameUiContainer);
        }

        // Neither container is parented to GumService.Default.Root, so Game1 combines this with
        // Root into a single per-frame GumService.Default.Update call - this is what runs
        // hover/click/drag for the Button, Menu, and Window inside them.
        public IEnumerable<GraphicalUiElement> AdditionalUpdateRoots => _roots;

        public void Draw(GumBatch gumBatch, SpriteBatch spriteBatch)
        {
            _graphicsDevice.SetRenderTarget(_renderTarget);
            _graphicsDevice.Clear(Color.Transparent);
            gumBatch.Begin();
            gumBatch.Draw(_inGameContainerForRenderTarget);
            gumBatch.End();
            _graphicsDevice.SetRenderTarget(null);

            // Center the scaled-up render target horizontally, below the menu.
            float blitWidth = GameWidth * BlitScale;
            float blitHeight = GameHeight * BlitScale;
            float blitX = (_graphicsDevice.Viewport.Width - blitWidth) / 2f;

            // Map a raw window pixel back into render-target space: undo the blit offset, then the
            // blit scale. Consumed only by hit-testing (never rendering).
            _inGameContainerForRenderTarget.HitTestTransformMatrix =
                System.Numerics.Matrix3x2.CreateTranslation(-blitX, -BlitY) *
                System.Numerics.Matrix3x2.CreateScale(1f / BlitScale);

            spriteBatch.Begin();
            spriteBatch.Draw(
                _renderTarget,
                new Rectangle((int)blitX, (int)BlitY, (int)blitWidth, (int)blitHeight),
                Color.White);
            spriteBatch.End();

            // Window-space UI, drawn straight to the backbuffer on top of the render target blit,
            // at 1:1 with no HitTestTransformMatrix.
            gumBatch.Begin();
            gumBatch.Draw(_aboveGameUiContainer);
            gumBatch.End();
        }

        public void Dispose()
        {
            _renderTarget?.Dispose();
        }
    }
}

#pragma warning restore CS0618
