using RenderingLibrary.Graphics;
using SkiaSharp;

namespace KniSkiaGumImmediateMode.Screens;

/// <summary>
/// Demonstrates the SpriteBatch-style <c>DrawString</c> API. On Skia it takes a font family name and
/// size instead of a BitmapFont; the font comes from the system.
/// </summary>
public class DrawStringScreen : IImmediateModeScreen
{
    private const string FontName = "Arial";
    private const float FontSize = 18;

    public void Initialize()
    {
    }

    public void Draw(GumBatch gumBatch, SKCanvas canvas)
    {
        gumBatch.Begin();

        gumBatch.DrawString(
            FontName,
            FontSize,
            "This text is drawn with GumBatch.DrawString.",
            new SKPoint(20, 80),
            SKColors.White);

        for (int i = 0; i < 10; i++)
        {
            gumBatch.DrawString(
                FontName,
                FontSize,
                $"Line {i} - DrawString is convenient for HUDs and debug overlays.",
                new SKPoint(20, 120 + 22 * i),
                SKColors.LightYellow);
        }

        gumBatch.DrawString(
            FontName,
            FontSize,
            "Colored text with\nembedded newlines\nrenders across multiple lines.",
            new SKPoint(20, 380),
            SKColors.MediumPurple);

        gumBatch.End();
    }

    public void Dispose()
    {
    }
}
