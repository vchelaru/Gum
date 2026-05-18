using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace MonoGameGumImmediateMode.Screens
{
    /// <summary>
    /// Demonstrates mixing user <see cref="SpriteBatch"/> draws with Gum's immediate-mode
    /// draws inside a single <see cref="GumBatch.Begin"/>/<see cref="GumBatch.End"/> pair
    /// by drawing through the <see cref="GumBatch.SpriteBatch"/> property. Both sets of
    /// draws land in the same batch, so they share sort order, blend state, and transform.
    /// </summary>
    public class MixedBatchScreen : IImmediateModeScreen
    {
        private Texture2D _pixel;
        private ColoredRectangleRuntime _gumRectangle;
        private TextRuntime _label;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            // A 1x1 white texture lets us draw arbitrary solid-color rects via SpriteBatch
            // without shipping any content assets.
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _gumRectangle = new ColoredRectangleRuntime();
            _gumRectangle.Width = 160;
            _gumRectangle.Height = 80;
            _gumRectangle.Color = Color.DarkGreen;
            _gumRectangle.X = 60;
            _gumRectangle.Y = 120;

            _label = new TextRuntime();
            _label.Font = "Arial";
            _label.FontSize = 16;
            _label.Text = "Green rect (Gum) and orange rect (SpriteBatch) share one batch";
            _label.X = 60;
            _label.Y = 90;
        }

        public void Draw(GumBatch gumBatch, SpriteBatch spriteBatch)
        {
            gumBatch.Begin();

            // Gum's own draws — go through GumBatch as usual.
            gumBatch.Draw(_gumRectangle);
            gumBatch.Draw(_label);

            // Our own SpriteBatch draws, issued through the shared SpriteBatch that
            // GumBatch is currently in the middle of using. No second Begin/End needed.
            gumBatch.SpriteBatch.Draw(
                _pixel,
                new Rectangle(240, 120, 160, 80),
                Color.Orange);

            gumBatch.SpriteBatch.Draw(
                _pixel,
                new Rectangle(60, 220, 340, 4),
                Color.Black);

            gumBatch.End();
        }

        public void Dispose()
        {
            _pixel?.Dispose();
        }
    }
}
