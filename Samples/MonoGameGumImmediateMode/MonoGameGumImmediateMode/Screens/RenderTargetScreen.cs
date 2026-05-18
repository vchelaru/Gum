using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace MonoGameGumImmediateMode.Screens
{
    /// <summary>
    /// Demonstrates using <see cref="GumBatch"/> to draw onto a <see cref="RenderTarget2D"/>,
    /// then presenting that texture via <see cref="SpriteBatch"/>. Also shows the
    /// custom BlendState pattern required when drawing partially-transparent objects
    /// onto a render target.
    /// </summary>
    public class RenderTargetScreen : IImmediateModeScreen
    {
        private GraphicsDevice _graphicsDevice;
        private RenderTarget2D _renderTarget;
        private ColoredRectangleRuntime _redBackground;
        private ColoredRectangleRuntime _halfTransparentRectangle;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _renderTarget = new RenderTarget2D(graphicsDevice, 300, 300);

            _redBackground = new ColoredRectangleRuntime();
            _redBackground.Width = 300;
            _redBackground.Height = 300;
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
        }

        public void Draw(GumBatch gumBatch, SpriteBatch spriteBatch)
        {
            _graphicsDevice.SetRenderTarget(_renderTarget);
            _graphicsDevice.Clear(Color.Transparent);
            gumBatch.Begin();
            gumBatch.Draw(_redBackground);
            gumBatch.Draw(_halfTransparentRectangle);
            gumBatch.End();
            _graphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin();
            spriteBatch.Draw(_renderTarget, new Vector2(60, 100), Color.White);
            spriteBatch.End();
        }

        public void Dispose()
        {
            _renderTarget?.Dispose();
        }
    }
}
