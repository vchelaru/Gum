using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace KniSkiaGumImmediateMode.Screens;

/// <summary>
/// Demonstrates mixing your own Skia draws with Gum's immediate-mode draws inside a single
/// <see cref="GumBatch.Begin(SKMatrix?, Renderer.GumBatchDrawMode)"/>/<see cref="GumBatch.End"/> pair.
/// In immediate mode each Gum draw lands on the canvas as it is called, so plain
/// <see cref="SKCanvas"/> calls in between keep their call order. Skia's counterpart of the MonoGame
/// sample's draws through <c>GumBatch.SpriteBatch</c>.
/// </summary>
public class MixedBatchScreen : IImmediateModeScreen
{
    private RectangleRuntime _gumRectangle = null!;
    private TextRuntime _label = null!;
    private SKPaint _orangePaint = null!;
    private SKPaint _blackPaint = null!;

    public void Initialize()
    {
        _gumRectangle = new RectangleRuntime();
        _gumRectangle.Width = 160;
        _gumRectangle.Height = 80;
        _gumRectangle.IsFilled = true;
        _gumRectangle.FillColor = SKColors.DarkGreen;
        _gumRectangle.X = 60;
        _gumRectangle.Y = 120;

        _label = new TextRuntime();
        _label.Font = "Arial";
        _label.FontSize = 16;
        _label.Text = "Green rect (Gum) and orange rect (SKCanvas) share one batch";
        _label.X = 60;
        _label.Y = 90;

        _orangePaint = new SKPaint { Color = SKColors.Orange };
        _blackPaint = new SKPaint { Color = SKColors.Black };
    }

    public void Draw(GumBatch gumBatch, SKCanvas canvas)
    {
        gumBatch.Begin();

        // Gum's own draws go through GumBatch as usual.
        gumBatch.Draw(_gumRectangle);
        gumBatch.Draw(_label);

        // Our own Skia draws, on the same canvas, in between. No second Begin/End needed.
        canvas.DrawRect(SKRect.Create(240, 120, 160, 80), _orangePaint);
        canvas.DrawRect(SKRect.Create(60, 220, 340, 4), _blackPaint);

        gumBatch.End();
    }

    public void Dispose()
    {
        _orangePaint?.Dispose();
        _blackPaint?.Dispose();
    }
}
