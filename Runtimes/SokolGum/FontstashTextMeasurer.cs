using System.Runtime.InteropServices;
using static Sokol.Fontstash;

namespace SokolGum;

/// <summary>
/// <see cref="ITextMeasurer"/> backed by a <see cref="FontAtlas"/>'s
/// fontstash context. Mirrors what <see cref="Gum.Renderables.Text.Render"/>
/// already does for measurement, extracted so layout-time size queries
/// (which run before rendering) share the same source of truth.
/// </summary>
public sealed class FontstashTextMeasurer : ITextMeasurer
{
    private readonly FontAtlas _atlas;

    public FontstashTextMeasurer(FontAtlas atlas)
    {
        if (atlas is null)
        {
            throw new ArgumentNullException(nameof(atlas));
        }
        _atlas = atlas;
    }

    /// <inheritdoc/>
    public void GetVerticalMetrics(Font? font, float fontSize,
        out float ascent, out float descent, out float lineHeight)
    {
        ascent = descent = lineHeight = 0f;
        if (font is null)
        {
            return;
        }

        IntPtr stash = _atlas.Stash;
        if (stash == IntPtr.Zero)
        {
            return;
        }

        float scale = _atlas.OversampleFactor;
        fonsSetFont(stash, font.Id);
        fonsSetSize(stash, fontSize * scale);
        fonsVertMetrics(stash, ref ascent, ref descent, ref lineHeight);
        // Fontstash returns metrics at the rasterization size (FontSize*scale);
        // divide by scale to recover logical pixels.
        ascent /= scale;
        descent /= scale;
        lineHeight /= scale;
    }

    /// <inheritdoc/>
    public float MeasureLineWidth(Font? font, float fontSize, ReadOnlySpan<char> text)
    {
        if (font is null || text.IsEmpty)
        {
            return 0f;
        }
        IntPtr stash = _atlas.Stash;
        if (stash == IntPtr.Zero)
        {
            return 0f;
        }

        float scale = _atlas.OversampleFactor;
        fonsSetFont(stash, font.Id);
        fonsSetSize(stash, fontSize * scale);
        // fontstash.h's fonsTextBounds writes 4 floats (minX, minY, maxX, maxY)
        // into the bounds pointer; we must back the ref with a 4-float buffer.
        Span<float> bounds = stackalloc float[4];
        // fontstash P/Invoke expects a string; slice to preserve existing
        // behavior. If perf ever shows up on a profile, add a char* overload.
        string s = new string(text);
        float raw = fonsTextBounds(stash, 0, 0, s, end: null!,
                                   ref MemoryMarshal.GetReference(bounds));
        return raw / scale;
    }
}
