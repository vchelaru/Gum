using Gum.Forms;
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
    /// Demonstrates using <see cref="GumBatch"/> to draw onto a <see cref="RenderTarget2D"/>,
    /// then presenting that texture via <see cref="SpriteBatch"/> at a different scale. Also
    /// shows the custom BlendState pattern required when drawing partially-transparent objects
    /// onto a render target.
    ///
    /// The blue box is interactive even though it lives inside a render target that is blitted
    /// to the screen at 1.5x: its container carries a <see cref="GraphicalUiElement.HitTestTransformMatrix"/>
    /// that maps the raw window pixel back into render-target space, so hit-testing lines up with
    /// where the content actually appears (issue #4096). Hover the blue box — it highlights.
    /// </summary>
    public class RenderTargetScreen : IImmediateModeScreen
    {
        // The render target is blitted to the backbuffer scaled up by this factor and offset by
        // this amount, so a raw window pixel must be mapped back by the inverse before hit-testing.
        private const float BlitScale = 1.5f;
        private static readonly Vector2 BlitOffset = new Vector2(60, 100);
        private const int RenderTargetSize = 300;

        private GraphicsDevice _graphicsDevice;
        private RenderTarget2D _renderTarget;
        private ColoredRectangleRuntime _redBackground;
        private ColoredRectangleRuntime _halfTransparentRectangle;
        private ContainerRuntime _interactiveButton;
        private ColoredRectangleRuntime _buttonBackground;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _renderTarget = new RenderTarget2D(graphicsDevice, RenderTargetSize, RenderTargetSize);

            _redBackground = new ColoredRectangleRuntime();
            _redBackground.Width = RenderTargetSize;
            _redBackground.Height = RenderTargetSize;
            _redBackground.Color = Color.Red;

            _halfTransparentRectangle = new ColoredRectangleRuntime();
            _halfTransparentRectangle.Width = 200;
            _halfTransparentRectangle.Height = 100;
            _halfTransparentRectangle.X = 50;
            _halfTransparentRectangle.Y = 100;
            _halfTransparentRectangle.Color = Color.White;
            _halfTransparentRectangle.Alpha = 128;

            // When drawing partially-transparent objects onto a RenderTarget2D, the
            // default BlendState can "punch through" alpha that was already on the
            // target. Use a BlendState that adds alpha instead.
            BlendState blendState = new BlendState();
            blendState.ColorSourceBlend = BlendState.NonPremultiplied.ColorSourceBlend;
            blendState.ColorDestinationBlend = BlendState.NonPremultiplied.ColorDestinationBlend;
            blendState.ColorBlendFunction = BlendState.NonPremultiplied.ColorBlendFunction;
            blendState.AlphaSourceBlend = Blend.SourceAlpha;
            blendState.AlphaDestinationBlend = Blend.DestinationAlpha;
            blendState.AlphaBlendFunction = BlendFunction.Add;
            _halfTransparentRectangle.BlendState = blendState;

            // A clickable box living inside the render target. Its visible background is a child
            // rectangle whose color reflects hover; the container is the hit-test target.
            _interactiveButton = new ContainerRuntime();
            _interactiveButton.X = 75;
            _interactiveButton.Y = 75;
            _interactiveButton.Width = 150;
            _interactiveButton.Height = 150;

            _buttonBackground = new ColoredRectangleRuntime();
            _buttonBackground.Width = 150;
            _buttonBackground.Height = 150;
            _interactiveButton.Children.Add(_buttonBackground);
            _interactiveButton.UpdateLayout();

            // Map a raw window pixel back into render-target space: undo the blit offset, then the
            // blit scale. Consumed only by hit-testing (never rendering), and inherited by the
            // container's descendants via the climbing EffectiveHitTestTransformMatrix getter.
            _interactiveButton.HitTestTransformMatrix =
                System.Numerics.Matrix3x2.CreateTranslation(-BlitOffset.X, -BlitOffset.Y) *
                System.Numerics.Matrix3x2.CreateScale(1f / BlitScale);
        }

        public void Draw(GumBatch gumBatch, SpriteBatch spriteBatch)
        {
            ICursor cursor = FormsUtilities.Cursor;
            bool isOver = cursor != null && _interactiveButton.HasCursorOver(cursor);
            _buttonBackground.Color = isOver ? Color.Yellow : new Color(40, 60, 200);

            _graphicsDevice.SetRenderTarget(_renderTarget);
            _graphicsDevice.Clear(Color.Transparent);
            gumBatch.Begin();
            gumBatch.Draw(_redBackground);
            gumBatch.Draw(_halfTransparentRectangle);
            gumBatch.Draw(_interactiveButton);
            gumBatch.End();
            _graphicsDevice.SetRenderTarget(null);

            Rectangle destination = new Rectangle(
                (int)BlitOffset.X,
                (int)BlitOffset.Y,
                (int)(RenderTargetSize * BlitScale),
                (int)(RenderTargetSize * BlitScale));

            spriteBatch.Begin();
            spriteBatch.Draw(_renderTarget, destination, Color.White);
            spriteBatch.End();
        }

        public void Dispose()
        {
            _renderTarget?.Dispose();
        }
    }
}

#pragma warning restore CS0618
