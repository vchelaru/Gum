using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace KniSkiaGumImmediateMode.Screens;

/// <summary>
/// Demonstrates rendering a <see cref="TextRuntime"/> through GumBatch. Unlike <c>DrawString</c>,
/// TextRuntime supports wrapping, alignment, rotation, and other Gum layout rules.
/// </summary>
public class TextRuntimeScreen : IImmediateModeScreen
{
    private TextRuntime _wrappedText = null!;
    private TextRuntime _rotatedText = null!;

    public void Initialize()
    {
        _wrappedText = new TextRuntime();
        _wrappedText.Font = "Arial";
        _wrappedText.FontSize = 16;
        _wrappedText.Text =
            "This TextRuntime wraps within its 280-pixel width. " +
            "Skia draws it with the system's Arial font.";
        _wrappedText.X = 20;
        _wrappedText.Y = 80;
        _wrappedText.Width = 280;
        _wrappedText.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        _wrappedText.Height = 0;
        _wrappedText.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        _rotatedText = new TextRuntime();
        _rotatedText.Font = "Arial";
        _rotatedText.FontSize = 20;
        _rotatedText.IsBold = true;
        _rotatedText.Text = "Rotated, bold, 20pt";
        _rotatedText.X = 500;
        _rotatedText.Y = 220;
        _rotatedText.Rotation = 30;
    }

    public void Draw(GumBatch gumBatch, SKCanvas canvas)
    {
        gumBatch.Begin();
        gumBatch.Draw(_wrappedText);
        gumBatch.Draw(_rotatedText);
        gumBatch.End();
    }

    public void Dispose()
    {
    }
}
