using System.Runtime.InteropServices;
using System.Text;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using static Sokol.Fontstash;
using static Sokol.SFontstash;

namespace SokolGum.Renderables;

/// <summary>
/// Text renderable. Draws one or more lines via the SystemManagers' shared
/// fontstash context. Supports word-wrap to <see cref="IRenderableIpso.Width"/>,
/// manual <c>\n</c> line breaks, horizontal + vertical alignment inside the
/// renderable's bounding box, line-height scaling, and an N-offset outline
/// via <see cref="OutlineThickness"/>.
///
/// Line layout is recomputed lazily on first Render after any text/size/font
/// change (cheap — fontstash measurement is just table lookups). Fontstash
/// render callbacks emit into sokol_gp, so text batches alongside other
/// renderables in scene-graph order.
/// </summary>
/// <summary>
/// Whether text positions get snapped to whole pixels before rasterization.
/// Subpixel positions can blur glyph outlines for small bitmap-style fonts
/// or on low-DPI displays; snapping eliminates that at the cost of
/// per-frame jitter on animated text. Matches RaylibGum's static-setting
/// pattern so a single process-wide flip changes every Text instance.
/// </summary>
public enum TextRenderingPositionMode
{
    SnapToPixel,
    FreeFloating,
}

/// <summary>Rounding rule used when <see cref="TextRenderingPositionMode.SnapToPixel"/> is active.</summary>
public enum TextPositionRoundingMode
{
    /// <summary>Banker-style round (midpoints away from zero). Matches MonoGame/Raylib default.</summary>
    RoundToInt,
    /// <summary>Always floor — reduces spacing jitter for some fonts.</summary>
    Floor,
    /// <summary>Always ceiling — same reason as Floor, opposite bias.</summary>
    Ceiling,
}

public sealed class Text : RenderableBase
{
    /// <summary>
    /// Process-wide switch for pixel snapping. Defaults to <see cref="TextRenderingPositionMode.SnapToPixel"/>
    /// matching RaylibGum/MonoGame convention. Flip to FreeFloating for
    /// smooth animated text or when rendering at non-integer camera zooms.
    /// </summary>
    public static TextRenderingPositionMode TextRenderingPositionMode { get; set; } = TextRenderingPositionMode.SnapToPixel;

    public static TextPositionRoundingMode TextPositionRoundingMode { get; set; } = TextPositionRoundingMode.RoundToInt;

    /// <summary>
    /// Limits how many characters are rendered — set to a fraction of
    /// <c>RawText.Length</c> and advance over time for a typewriter reveal
    /// effect. Null (the default) draws every character. Counting spans
    /// lines: line 1's characters count first, then line 2, and so on.
    /// </summary>
    public int? MaxLettersToShow { get; set; }

    // Fontstash alignment bitflags from fontstash.h. Fontstash can handle
    // its own horizontal alignment, but we want per-line horizontal anchor
    // computed from Width anyway (for mixed-length lines), so we always
    // draw each line with LEFT | TOP and position manually.
    private const int FONS_ALIGN_LEFT = 1 << 0;
    private const int FONS_ALIGN_TOP  = 1 << 3;

    public Font? Font { get; set; }

    private string? _rawText;
    /// <summary>
    /// The text to render. Setting this invalidates the cached line layout.
    /// Use <c>\n</c> for explicit line breaks; additional wrapping happens
    /// automatically when <see cref="WrapTextInsideBlock"/> is true and
    /// a line exceeds <see cref="IRenderableIpso.Width"/>.
    /// </summary>
    public string? RawText
    {
        get => _rawText;
        set { if (_rawText != value) { _rawText = value; _layoutDirty = true; } }
    }

    private float _fontSize = 16f;
    public float FontSize
    {
        get => _fontSize;
        set { if (_fontSize != value) { _fontSize = value; _layoutDirty = true; } }
    }

    public Color Color = new(230, 230, 230);
    public int Alpha { get => Color.A; set => Color.A = (byte)value; }

    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment   VerticalAlignment   { get; set; } = VerticalAlignment.Top;

    public TextOverflowHorizontalMode TextOverflowHorizontalMode { get; set; } = TextOverflowHorizontalMode.TruncateWord;
    public TextOverflowVerticalMode   TextOverflowVerticalMode   { get; set; } = TextOverflowVerticalMode.SpillOver;

    /// <summary>When true, lines longer than <c>Width</c> are word-wrapped.</summary>
    public bool WrapTextInsideBlock { get; set; } = true;

    /// <summary>Line-to-line spacing multiplier. 1.0 = font-native line height.</summary>
    public float LineHeightMultiplier { get; set; } = 1f;

    /// <summary>
    /// Thickness of a solid outline drawn behind the glyphs in
    /// <see cref="OutlineColor"/>. 0 disables the outline. Rendered by
    /// drawing the text once per cardinal offset in an 8-direction ring
    /// (so cost scales with thickness × 8).
    /// </summary>
    public int OutlineThickness { get; set; }

    public Color OutlineColor = new(0, 0, 0);

    private readonly List<string> _wrappedLines = new();
    private bool _layoutDirty = true;
    private float _lastLaidOutWidth = -1f;
    private float _lastLaidOutFontSize = -1f;

    public override void Render(ISystemManagers? managers)
    {
        if (!Visible || Font is null || string.IsNullOrEmpty(RawText)) return;

        var systemManagers = (managers as SystemManagers) ?? SystemManagers.Default;
        if (systemManagers is null || systemManagers.FontStash == IntPtr.Zero) return;

        var stash = systemManagers.FontStash;

        // Relay-out if text/size/font changed, or if our containing Width
        // was resized and word-wrap could produce different breaks.
        if (_layoutDirty || _lastLaidOutWidth != Width || _lastLaidOutFontSize != FontSize)
        {
            RewrapLines(stash);
            _lastLaidOutWidth = Width;
            _lastLaidOutFontSize = FontSize;
            _layoutDirty = false;
        }

        if (_wrappedLines.Count == 0) return;

        fonsSetFont(stash, Font.Id);
        fonsSetSize(stash, FontSize);
        fonsSetAlign(stash, FONS_ALIGN_LEFT | FONS_ALIGN_TOP);

        // Font-native line height in pixels at the current size.
        float ascender = 0, descender = 0, lineh = 0;
        fonsVertMetrics(stash, ref ascender, ref descender, ref lineh);
        float lineStep = lineh * LineHeightMultiplier;

        float left = this.GetAbsoluteLeft();
        float top  = this.GetAbsoluteTop();

        // Vertical anchor of the block of text as a whole.
        float totalBlockHeight = lineStep * _wrappedLines.Count;
        float yStart = VerticalAlignment switch
        {
            VerticalAlignment.Center       => top + (Height - totalBlockHeight) * 0.5f,
            VerticalAlignment.Bottom       => top + Height - totalBlockHeight,
            VerticalAlignment.TextBaseline => top + Height - lineStep,
            _                              => top,
        };

        // Truncate trailing lines that would spill past Height when requested.
        int linesToDraw = _wrappedLines.Count;
        if (TextOverflowVerticalMode == TextOverflowVerticalMode.TruncateLine
            && Height > 0 && lineStep > 0)
        {
            int fits = Math.Max(1, (int)MathF.Floor(Height / lineStep));
            linesToDraw = Math.Min(linesToDraw, fits);
        }

        int lettersRemaining = MaxLettersToShow ?? int.MaxValue;

        for (int i = 0; i < linesToDraw; i++)
        {
            if (lettersRemaining <= 0) break;

            var line = _wrappedLines[i];
            // Truncate this line to the remaining letter budget so typewriter
            // reveals pause mid-line rather than popping line-by-line.
            if (line.Length > lettersRemaining) line = line[..lettersRemaining];
            lettersRemaining -= line.Length;

            float lineWidth = MeasureWidth(stash, line);
            float x = HorizontalAlignment switch
            {
                HorizontalAlignment.Center => left + (Width - lineWidth) * 0.5f,
                HorizontalAlignment.Right  => left + Width - lineWidth,
                _                          => left,
            };
            float y = yStart + i * lineStep;

            if (TextRenderingPositionMode == TextRenderingPositionMode.SnapToPixel)
            {
                x = SnapToPixel(x);
                y = SnapToPixel(y);
            }

            DrawLineWithOptionalOutline(stash, line, x, y);
        }
    }

    private static float SnapToPixel(float v) => TextPositionRoundingMode switch
    {
        TextPositionRoundingMode.Floor   => MathF.Floor(v),
        TextPositionRoundingMode.Ceiling => MathF.Ceiling(v),
        _                                => (int)MathF.Round(v, MidpointRounding.AwayFromZero),
    };

    /// <summary>
    /// Draws a single line with the configured outline (if any), then the
    /// main color on top. Outline strategy: stroke the 8 cardinal offsets
    /// at each integer distance from 1 to OutlineThickness — same technique
    /// every bitmap-font renderer without native SDF support uses.
    /// </summary>
    private void DrawLineWithOptionalOutline(IntPtr stash, string line, float x, float y)
    {
        if (OutlineThickness > 0)
        {
            fonsSetColor(stash, sfons_rgba(OutlineColor.R, OutlineColor.G, OutlineColor.B, Color.A));
            for (int d = 1; d <= OutlineThickness; d++)
            {
                fonsDrawText(stash, x - d, y,     line, end: null!);
                fonsDrawText(stash, x + d, y,     line, end: null!);
                fonsDrawText(stash, x,     y - d, line, end: null!);
                fonsDrawText(stash, x,     y + d, line, end: null!);
                fonsDrawText(stash, x - d, y - d, line, end: null!);
                fonsDrawText(stash, x + d, y - d, line, end: null!);
                fonsDrawText(stash, x - d, y + d, line, end: null!);
                fonsDrawText(stash, x + d, y + d, line, end: null!);
            }
        }

        fonsSetColor(stash, sfons_rgba(Color.R, Color.G, Color.B, Color.A));
        fonsDrawText(stash, x, y, line, end: null!);
    }

    /// <summary>
    /// Recomputes <see cref="_wrappedLines"/> from <see cref="RawText"/>.
    /// Splits on <c>\n</c> first (manual breaks always honoured), then
    /// word-wraps each paragraph to <see cref="IRenderableIpso.Width"/>
    /// when <see cref="WrapTextInsideBlock"/> is set.
    /// </summary>
    private void RewrapLines(IntPtr stash)
    {
        _wrappedLines.Clear();
        if (string.IsNullOrEmpty(RawText) || Font is null) return;

        fonsSetFont(stash, Font.Id);
        fonsSetSize(stash, FontSize);
        fonsSetAlign(stash, FONS_ALIGN_LEFT | FONS_ALIGN_TOP);

        var paragraphs = RawText.Split('\n');
        bool wrap = WrapTextInsideBlock && Width > 0;

        foreach (var paragraph in paragraphs)
        {
            if (!wrap || MeasureWidth(stash, paragraph) <= Width)
            {
                _wrappedLines.Add(paragraph);
                continue;
            }

            // Word-wrap. Walk word boundaries accumulating the current line
            // until adding the next word would exceed Width; then emit and
            // start over. Whitespace is preserved at word boundaries so
            // trailing spaces don't disappear silently.
            var builder = new StringBuilder();
            int i = 0;
            while (i < paragraph.Length)
            {
                int wordStart = i;
                while (i < paragraph.Length && !char.IsWhiteSpace(paragraph[i])) i++;
                int wordEnd = i;
                while (i < paragraph.Length && char.IsWhiteSpace(paragraph[i]) && paragraph[i] != '\n') i++;
                int spaceEnd = i;

                var word = paragraph[wordStart..wordEnd];
                var trailingWhitespace = paragraph[wordEnd..spaceEnd];
                var candidate = builder.Length == 0 ? word : builder + word;

                if (MeasureWidth(stash, candidate) > Width && builder.Length > 0)
                {
                    _wrappedLines.Add(builder.ToString().TrimEnd());
                    builder.Clear();
                    builder.Append(word).Append(trailingWhitespace);
                }
                else
                {
                    builder.Append(word).Append(trailingWhitespace);
                }
            }
            if (builder.Length > 0) _wrappedLines.Add(builder.ToString().TrimEnd());
        }

        // Optional horizontal ellipsis when a single word (no break point)
        // still exceeds Width. Measure + trim letter-by-letter and append
        // an ellipsis. Conservative first pass — no BiDi / grapheme-cluster
        // handling, which is the same level MonoGame's renderer provides.
        if (wrap && TextOverflowHorizontalMode == TextOverflowHorizontalMode.EllipsisLetter)
        {
            for (int i = 0; i < _wrappedLines.Count; i++)
            {
                var line = _wrappedLines[i];
                if (MeasureWidth(stash, line) <= Width) continue;

                const string ellipsis = "…";
                float ellipsisWidth = MeasureWidth(stash, ellipsis);
                int cut = line.Length;
                while (cut > 0 && MeasureWidth(stash, line[..cut]) + ellipsisWidth > Width)
                    cut--;
                _wrappedLines[i] = line[..cut] + ellipsis;
            }
        }
    }

    private static float MeasureWidth(IntPtr stash, string s)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        // fontstash.h's fonsTextBounds writes 4 floats (minX, minY, maxX, maxY)
        // into the bounds pointer. The P/Invoke signature is `ref float`, so
        // we must back it with a 4-float buffer — passing a single float by
        // ref would let the native side overrun the stack. We only need the
        // advance width (the return value), so the buffer is discard-only.
        Span<float> bounds = stackalloc float[4];
        return fonsTextBounds(stash, 0, 0, s, end: null!,
                              ref MemoryMarshal.GetReference(bounds));
    }
}
