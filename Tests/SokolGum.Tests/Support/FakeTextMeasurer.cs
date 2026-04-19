using SokolGum;

namespace SokolGum.Tests.Support;

/// <summary>
/// Deterministic measurer for unit tests — doesn't touch fontstash/GPU.
/// Returns predictable metrics so layout assertions can target exact values.
/// </summary>
public class FakeTextMeasurer : ITextMeasurer
{
    public float LineHeight { get; set; } = 24f;
    public float Ascent { get; set; } = 20f;
    public float Descent { get; set; } = -4f;
    public float PixelsPerChar { get; set; } = 10f;

    public void GetVerticalMetrics(Font? font, float fontSize,
        out float ascent, out float descent, out float lineHeight)
    {
        ascent = Ascent;
        descent = Descent;
        lineHeight = LineHeight;
    }

    public float MeasureLineWidth(Font? font, float fontSize, ReadOnlySpan<char> text)
        => text.Length * PixelsPerChar;
}
