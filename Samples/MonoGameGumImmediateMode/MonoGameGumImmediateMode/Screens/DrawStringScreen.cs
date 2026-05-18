using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;

namespace MonoGameGumImmediateMode.Screens
{
    /// <summary>
    /// Demonstrates the SpriteBatch-style <c>DrawString</c> API. The BitmapFont is
    /// produced in memory by KernSmith — there are no .fnt files on disk.
    /// </summary>
    public class DrawStringScreen : IImmediateModeScreen
    {
        private BitmapFont _font;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            // KernSmith was wired up once in Game1.Initialize, so the
            // InMemoryFontCreator is ready to hand back a BitmapFont for
            // any font/size combination we ask for.
            _font = CustomSetPropertyOnRenderable.InMemoryFontCreator
                .TryCreateFont(new BmfcSave
                {
                    FontName = "Arial",
                    FontSize = 18,
                });
        }

        public void Draw(GumBatch gumBatch, SpriteBatch spriteBatch)
        {
            gumBatch.Begin();

            gumBatch.DrawString(
                _font,
                "This text is drawn with GumBatch.DrawString.",
                new Vector2(20, 80),
                Color.White);

            for (int i = 0; i < 10; i++)
            {
                gumBatch.DrawString(
                    _font,
                    $"Line {i} — DrawString is convenient for HUDs and debug overlays.",
                    new Vector2(20, 120 + 22 * i),
                    Color.LightYellow);
            }

            gumBatch.DrawString(
                _font,
                "Colored text with\nembedded newlines\nrenders across multiple lines.",
                new Vector2(20, 380),
                Color.MediumPurple);

            gumBatch.End();
        }

        public void Dispose()
        {
            // The BitmapFont is owned by the LoaderManager (KernSmith registers
            // it as a disposable), so nothing to clean up here.
        }
    }
}
