using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace KniSkiaGumImmediateMode.Screens;

/// <summary>
/// Demonstrates mixing your own <see cref="SpriteBatch"/> draws with Gum's immediate-mode draws. Each
/// <see cref="GumBatch.Begin(SKMatrix?, Renderer.GumBatchDrawMode)"/>/<see cref="GumBatch.End"/> pair is its
/// own pass, shown in the order the passes are called, so Gum draws, then <see cref="SpriteBatch"/> draws,
/// then Gum draws again, all land in call order. The MonoGame sample draws through
/// <c>GumBatch.SpriteBatch</c> inside one batch instead; KNI Skia has no shared batch to draw through.
/// </summary>
public class MixedBatchScreen : IImmediateModeScreen
{
    private Texture2D _pixel = null!;
    private RectangleRuntime _gumRectangle = null!;
    private TextRuntime _label = null!;

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        // A 1x1 white texture lets us draw arbitrary solid-color rects via SpriteBatch
        // without shipping any content assets.
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _gumRectangle = new RectangleRuntime();
        _gumRectangle.Width = 160;
        _gumRectangle.Height = 80;
        _gumRectangle.IsFilled = true;
        _gumRectangle.FillColor = SKColors.DarkGreen;
        _gumRectangle.StrokeWidth = 0;
        _gumRectangle.IsAntialiased = false;
        _gumRectangle.X = 60;
        _gumRectangle.Y = 120;

        _label = new TextRuntime();
        _label.Font = "Arial";
        _label.FontSize = 16;
        _label.Text = "Green rect (Gum) and orange rect (SpriteBatch) draw in call order";
        _label.X = 60;
        _label.Y = 90;
    }

    public void Draw(GumBatch gumBatch, SpriteBatch spriteBatch)
    {
        // Gum's own draws go through GumBatch as usual.
        gumBatch.Begin();
        gumBatch.Draw(_gumRectangle);
        gumBatch.Draw(_label);
        gumBatch.End();

        // Our own SpriteBatch draws, in a pass of their own after Gum's.
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(240, 120, 160, 80), Color.Orange);
        spriteBatch.Draw(_pixel, new Rectangle(60, 220, 340, 4), Color.Black);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _pixel?.Dispose();
    }
}
