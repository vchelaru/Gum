using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using RenderingLibrary.Math;
using ToolsUtilitiesStandard.Helpers;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>
/// This enum defines the ways the renderer can align
/// text, which can affect clarity - especially with
/// small fonts when drawing with point sampling
/// </summary>
public enum TextRenderingPositionMode
{
    /// <summary>
    /// In this mode, the renderer will ensure text renders at
    /// a whole pixel which can avoid artifacting on small fonts
    /// </summary>
    SnapToPixel,
    
    /// <summary>
    /// In this mode the renderer will render text at its
    /// specified position, even if that's a fractional or
    /// subpixel.
    /// </summary>
    FreeFloating,
}

/// <summary>
/// This enum defines the way the renderer can round text
/// rendering to the nearest pixel. This only applies if
/// TextRenderingPositionMode is set to SnapToPixel
/// </summary>
/// <remarks>
/// Raylib-only (#3708): XNALIKE and Skia both hardcode away-from-zero rounding (matching
/// <c>MathFunctions.RoundToInt</c>) with no Floor/Ceiling knob and no reported jitter. Raylib scales a live
/// TTF via <c>DrawTextPro</c>/<c>MeasureTextEx</c> rather than blitting pre-baked, integer-advance
/// bitmap glyphs, so fractional advances are far more common here, and always-round-to-nearest could
/// visibly jitter inter-character spacing for some fonts. No platform has asked for Floor/Ceiling.
/// </remarks>
public enum TextPositionRoundingMode
{
    /// <summary>
    /// This mode does special integer rounding to the nearest
    /// pixel and will round midpoints away from zero.
    /// </summary>
    RoundToInt,
    
    /// <summary>
    /// This mode always rounds to floor, which may reduce
    /// render jittering affecting the spacing between text.
    /// Use this if your default rendering behavior rounds
    /// to floor for subpixels.
    /// </summary>
    Floor,
    
    /// <summary>
    /// This mode always rounds to ceiling, which may reduce
    /// render jittering affecting the spacing between text.
    /// Use this if your default rendering behavior rounds
    /// to ceiling for subpixels.
    /// </summary>
    Ceiling,
}


#region LetterCustomization

/// <summary>
/// Mirrors <c>RenderingLibrary.Graphics.LetterCustomization</c> on the MonoGame-family runtime -
/// the per-letter styling a <c>[Custom]</c> callback can apply. ScaleX/ScaleY/RotationDegrees change only how
/// the letter is drawn, as on MonoGame: it is scaled about the cell point ScaleXOrigin/ScaleYOrigin name
/// (Center by default) and turned about the scaled cell's top-left; layout and the next letter do not move.
/// </summary>
public struct LetterCustomization
{
    public float? XOffset;
    public float? YOffset;
    public System.Drawing.Color? Color;
    public float? ScaleX;
    public HorizontalAlignment? ScaleXOrigin;
    public float? ScaleY;
    public VerticalAlignment? ScaleYOrigin;
    public float? RotationDegrees;
    public char? ReplacementCharacter;
}

#endregion

#region ParameterizedLetterCustomizationCall

/// <summary>
/// Mirrors <c>RenderingLibrary.Graphics.ParameterizedLetterCustomizationCall</c> on the
/// MonoGame-family runtime. Resolves <see cref="FunctionName"/> against <see cref="Text.Customizations"/>
/// / <see cref="Text.ContextCustomizations"/> lazily (rather than capturing the delegate at parse time)
/// so registering the callback after the markup is assigned still takes effect at draw time.
/// </summary>
public class ParameterizedLetterCustomizationCall
{
    public string FunctionName { get; set; } = string.Empty;

    public Func<int, string, LetterCustomization>? Function
    {
        get
        {
            if (!string.IsNullOrEmpty(FunctionName) && Text.Customizations.TryGetValue(FunctionName, out var func))
            {
                return func;
            }
            return null;
        }
    }

    public Func<int, string, LetterCustomization, LetterCustomization>? ContextFunction
    {
        get
        {
            if (!string.IsNullOrEmpty(FunctionName) && Text.ContextCustomizations.TryGetValue(FunctionName, out var func))
            {
                return func;
            }
            return null;
        }
    }

    public int CharacterIndex { get; set; }

    public string TextBlock { get; set; } = string.Empty;
}

#endregion

public class Text : IVisible, IRenderableIpso,
    IWrappedText, IFormsText, ICloneable
{
    public Text Clone()
    {
        var newInstance = (Text)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.mChildren = new();
        // MemberwiseClone shares these lists; give the clone its own copies so re-wrapping or
        // re-parsing markup on one instance doesn't rewrite the other's lines or runs.
        newInstance.mWrappedText = new List<string>(mWrappedText);
        newInstance.mPreviousWrappedText = new List<string>();
        newInstance.InlineVariables = new List<InlineVariable>(InlineVariables);
        // The splitter caches runs for this instance's lines and variables; shared, two Texts would clear
        // each other's cache every frame.
        newInstance._styledSubstringSplitter = new StyledSubstringSplitter();
        newInstance._styledRunScratch = Array.Empty<StyledRunDraw>();
        // These call back into the source's runtime; the clone's runtime wires its own.
        newInstance.OnPreRender = null;
        newInstance.OnGlyphGrowthCheck = null;
        return newInstance;
    }

    object ICloneable.Clone() => Clone();

    /// <summary>
    /// The line height as defined by the font, ignoring FontScale.
    /// </summary>
    private int _lineHeightInPixels;

    /// <summary>
    /// The descender height (line height minus baseline) as defined by the font, ignoring FontScale.
    /// Recovered from the loaded .fnt's lineHeight/base, or from the glyph offsets of a TTF loaded via
    /// LoadFontEx; defaults to 2 for raylib's built-in default font and sprite-sheet fonts.
    /// </summary>
    private int _descenderHeight = 2;

    /// <summary>
    /// Stores the width of the text object's texture before it has had a chance to render, not including
    /// the FontScale.
    /// </summary>
    /// <remarks>
    /// A text object may need to be positioned according to its dimensions. Normally this would
    /// use a text's render target texture. In some situations (before the render pass has occurred,
    /// or when using character-by-character rendering), the text may not have a render target texture.
    /// Therefore, the pre-rendered values provide size information.
    /// </remarks>
    int? mPreRenderWidth;
    /// <summary>
    /// Stores the height of the text object's texture before it has had a chance to render, not including
    /// the FontScale.
    /// </summary>
    /// <remarks>
    /// See mPreRenderWidth for more information about this member.
    /// </remarks>
    int? mPreRenderHeight;

    bool IWrappedText.IsMidWordLineBreakEnabled => true;

    /// <summary>
    /// Project-wide default font for newly constructed <see cref="Text"/> renderables.
    /// When unset (<c>BaseSize == 0</c>), the constructor falls back to raylib's built-in
    /// pixel font via <see cref="GetFontDefault"/>. Set this once at startup — typically
    /// after <c>LoadFontEx</c> — to make every <c>TextRuntime</c> created thereafter pick
    /// up the chosen font, including instances that don't go through the Forms
    /// <c>Styling</c> pipeline (e.g. raw <c>TextRuntime</c> headers in sample screens).
    /// Issue #2757.
    /// </summary>
    public static Font DefaultFont { get; set; }

    /// <summary>
    /// Registry of simple per-letter callbacks for the <c>[Custom=Name]</c> BBCode tag, keyed by name.
    /// Mirrors <c>RenderingLibrary.Graphics.Text.Customizations</c> on the MonoGame-family
    /// runtime so a callback registered by name behaves the same on every platform.
    /// </summary>
    public static Dictionary<string, Func<int, string, LetterCustomization>> Customizations { get; private set; }
        = new();

    /// <summary>
    /// Registry of context-aware per-letter callbacks for the <c>[Custom=Name]</c> BBCode tag - the
    /// callback receives the <see cref="LetterCustomization"/> produced by any enclosing <c>[Custom]</c>
    /// tag so nested tags can chain (e.g. an outer tag sets Color, an inner tag darkens it). Checked
    /// before <see cref="Customizations"/> when both are registered under the same name. Mirrors
    /// <c>RenderingLibrary.Graphics.Text.ContextCustomizations</c> on the MonoGame-family runtime.
    /// </summary>
    public static Dictionary<string, Func<int, string, LetterCustomization, LetterCustomization>> ContextCustomizations { get; private set; }
        = new();

    public Vector2 Position;
    
    /// <summary>
    /// Whether the renderer should snap text rendering to whole pixels or not. Default
    /// behavior is to snap as this prevents baseline misalignment and artifacts for
    /// small fonts.
    /// </summary>
    public static TextRenderingPositionMode TextRenderingPositionMode = TextRenderingPositionMode.SnapToPixel;

    /// <summary>
    /// Per-instance override for <see cref="TextRenderingPositionMode"/>. When null (the default),
    /// this Text uses the static <see cref="TextRenderingPositionMode"/>; when set, it overrides the
    /// static default for this instance only. Mirrors the MonoGame Text renderable.
    /// </summary>
    public TextRenderingPositionMode? OverrideTextRenderingPositionMode;

    /// <summary>
    /// The position mode actually applied when drawing this Text: the per-instance
    /// <see cref="OverrideTextRenderingPositionMode"/> when set, otherwise the static
    /// <see cref="TextRenderingPositionMode"/>. Matches the MonoGame BitmapFont resolution.
    /// </summary>
    internal TextRenderingPositionMode EffectiveTextRenderingPositionMode =>
        OverrideTextRenderingPositionMode ?? TextRenderingPositionMode;


    /// <summary>
    /// How the renderer should round text rendering to whole pixels. Only applies if
    /// TextRenderingPositionMode is SnapToPixel. Default is to use special integer rounding.
    /// </summary>
    public static TextPositionRoundingMode TextPositionRoundingMode = TextPositionRoundingMode.RoundToInt;

    List<string> mWrappedText = new List<string>();

    // Scratch for the lines of the last wrap, so re-wrapping keeps the strings of lines that did not change.
    List<string> mPreviousWrappedText = new List<string>();
    StyledSubstringSplitter _styledSubstringSplitter = new StyledSubstringSplitter();

    // What DrawStyledLine resolves for each run of the line it is drawing: colour, font, sizes, width and
    // [Custom] offsets. Kept on the Text so a styled line is drawn without allocating (#6426).
    private struct StyledRunDraw
    {
        public Color Color;
        public Font Font;
        public float DrawSize;
        public float Scale;
        /// <summary>The run's line height and baseline (from the line top) in pixels, from the run's own font.</summary>
        public float LineHeight;
        public float BaselineFromTop;
        public float Width;
        public string Text;
        public float XOffset;
        public float YOffset;
        public float Rotation;
        public float ScaleX;
        public float ScaleY;
        public HorizontalAlignment ScaleXOrigin;
        public VerticalAlignment ScaleYOrigin;
    }

    StyledRunDraw[] _styledRunScratch = Array.Empty<StyledRunDraw>();
    float? mWidth = 200;
    float mHeight = 200;

    IRenderableIpso? mParent;

    ObservableCollectionNoReset<IRenderableIpso> mChildren;

    ColorOperation IRenderableIpso.ColorOperation => ColorOperation.Modulate;

    bool IRenderableIpso.ClipsChildren
    {
        get
        {
            return false;
        }
    }


    public IRenderableIpso? Parent
    {
        get { return mParent; }
        set
        {
            if (mParent != value)
            {
                if (mParent != null)
                {
                    mParent.Children.Remove(this);
                }
                mParent = value;
                if (mParent != null)
                {
                    mParent.Children.Add(this);
                }
            }
        }
    }

    public float Z
    {
        get;
        set;
    }

    public string? FontFamily
    {
        get; set;
    }

    Font _font;
    public Font Font
    {
        get
        {
            if (_font.BaseSize == 0)
            {
                _font = GetFontDefault();
            }

            return _font;
        }
        set
        {
            // Raylib_cs.Font is a struct, so font identity is compared via the loaded atlas texture
            // id, mirroring CustomSetPropertyOnRenderable.AssignFontIfChanged.
            bool isNewFont = _font.Texture.Id != value.Texture.Id;

            _font = value;

            // cache this to make checking this faster
            UpdateLineHeightInPixels();

            if (isNewFont)
            {
                // A font reassignment invalidates any wrap/pre-render dimensions measured against
                // the previous font -- most notably raylib's native GetFontDefault() placeholder,
                // which this.Font's getter substitutes until a real font is resolved and assigned.
                // Without this, RawText set before the real font resolves (e.g. under
                // IsAllLayoutSuspended, where Text assignment isn't suspend-aware but font
                // resolution is -- see issue #4853) leaves WrappedTextWidth/mPreRenderWidth
                // permanently measured against the small placeholder font's metrics; nothing else
                // re-wraps just because Font changed underneath. Mirrors the MonoGame/KNI/FNA Text's
                // BitmapFont setter (RenderingLibrary/Graphics/Text.cs), which already calls
                // AssignBitmapFontAndRefresh on every real font reassignment. Both calls are no-ops
                // while RawText is still null (e.g. the very first assignment, from the constructor).
                UpdateWrappedText();
                UpdatePreRenderDimensions();
            }
        }
    }

    float _oversampleCompensationScale = 1;

    /// <summary>
    /// Internal-only multiplicative compensation for automatic font oversampling (issue #4330, Raylib
    /// parity for #4317). Composes with the public <see cref="FontScale"/> instead of overwriting it --
    /// the raster swaps to a higher-resolution <see cref="Font"/> under zoom, and this scale shrinks it
    /// back down to the requested on-screen size. Affects plain text draw size (<see cref="DrawPlainLine"/>),
    /// the pinned layout size, and -- since issue #4365 -- an inline run's default or [FontScale=...]
    /// resolution (<see cref="ResolveRunFont"/>). A run that swaps in a differently-resolved font
    /// ([FontSize=...]/[IsBold=...]/[IsItalic=...]/[OutlineThickness=...]) still keeps drawing at its
    /// own resolved size, unaffected by oversampling (a documented limitation, not an oversight:
    /// composing compensation into that resolution risks destabilizing the line-height/baseline math
    /// those runs already depend on). Defaults to 1 (no compensation).
    /// </summary>
    internal float OversampleCompensationScale
    {
        get => _oversampleCompensationScale;
        set
        {
            if (value != _oversampleCompensationScale)
            {
                _oversampleCompensationScale = value;
                UpdateWrappedText();
                UpdatePreRenderDimensions();
            }
        }
    }

    Font? _measurementFont;

    /// <summary>
    /// Optional font used for layout measurement (<see cref="WrappedTextWidth"/>/<see cref="WrappedTextHeight"/>/
    /// word-wrap) instead of the live <see cref="Font"/> (issue #4330, mirroring #4309 on the MonoGame side).
    /// When set, <see cref="Font"/> is still used to draw glyphs, but never to measure them -- so layout
    /// stays stable even while <see cref="Font"/> is being regenerated at different raster sizes under
    /// camera-zoom oversampling, whose rasterized glyph metrics are not exact multiples of each other
    /// across raster sizes. Null (the default) falls back to measuring the live <see cref="Font"/> directly,
    /// i.e. today's unchanged behavior.
    /// </summary>
    internal Font? MeasurementFont
    {
        get => _measurementFont;
        set
        {
            _measurementFont = value;
            UpdateWrappedText();
            UpdatePreRenderDimensions();
        }
    }

    Font EffectiveMeasurementFont => _measurementFont ?? Font;

    /// <summary>
    /// Runs once per frame during this Text's <see cref="PreRender"/>. Used by <c>TextRuntime</c> to
    /// automatically re-rasterize at the current camera/layer zoom (issue #4330, Raylib parity for
    /// #4317) -- kept here rather than on the runtime because PreRender is the render-time hook the
    /// layered draw path already calls per visible renderable.
    /// </summary>
    internal Action? OnPreRender;

    /// <summary>
    /// Runs synchronously at the top of every <see cref="UpdateWrappedText"/> call, before wrapping.
    /// Used by <c>TextRuntime</c> (issue #4546, Raylib parity for #4542) to detect a character this
    /// Text's live font is missing and grow it in place -- unlike <see cref="OnPreRender"/>'s
    /// once-per-frame cadence, this can't be deferred: a property setter like <c>Text</c> wraps before
    /// it returns (e.g. a TextBox keystroke), so wrap measurement must already see the grown glyph.
    /// </summary>
    internal Action? OnGlyphGrowthCheck;

    /// <summary>
    /// The scale applied when drawing plain (non-BBCode) text: composes the public <see cref="FontScale"/>
    /// with <see cref="OversampleCompensationScale"/>.
    /// </summary>
    private float EffectiveFontScale => FontScale * _oversampleCompensationScale;

    /// <summary>
    /// The scale applied to pre-rendered layout sizes (box width/height, vertical origin offsets). When a
    /// <see cref="MeasurementFont"/> is pinned, pre-rendered sizes were already measured against that
    /// STABLE font in base units, so only the base <see cref="FontScale"/> applies -- the compensation
    /// exists solely to shrink the oversampled <see cref="Font"/>'s larger raster back down for DRAWING,
    /// not for a size that's already correctly baselined. Falls back to <see cref="EffectiveFontScale"/>
    /// when unset (matches today's unchanged behavior, since compensation defaults to 1 with no active
    /// oversampling).
    /// </summary>
    private float MeasurementFontScale => _measurementFont != null ? FontScale : EffectiveFontScale;

    // Issue #4370 (MonoGame parity): word-wrap must convert a Width constraint using the UNcompensated
    // scale, since wrap decisions measure words against EffectiveMeasurementFont (native units). Already
    // a no-op today (this class's own public FontScale never bakes in compensation, unlike MonoGame's
    // Text.cs), but wiring it explicitly -- rather than relying on the interface default falling back to
    // FontScale -- keeps this correct by construction if that ever changes.
    float IWrappedText.WordWrapFontScale => MeasurementFontScale;

    /// <summary>
    /// Swaps in <paramref name="oversampledFont"/> as the DISPLAY font for camera-zoom oversampling
    /// (issue #4330, mirroring #4309), pinning the outgoing (native) <see cref="Font"/> as
    /// <see cref="MeasurementFont"/> first if nothing is pinned yet. Layout keeps measuring against
    /// whatever was actually being shown up to this point, instead of jumping to a different raster
    /// size's own (differently-rasterized) measurement.
    /// </summary>
    internal void SetOversampledDisplayFont(Font oversampledFont)
    {
        if (MeasurementFont == null)
        {
            MeasurementFont = Font;
        }
        else if (_font.BaseSize != 0
            && _font.Texture.Id != MeasurementFont.Value.Texture.Id
            && _font.Texture.Id != oversampledFont.Texture.Id)
        {
            // Issue #4364: MeasurementFont is already pinned, so _font here is a previously generated
            // oversampled font being superseded by another re-rasterize (e.g. mid continuous zoom
            // gesture) -- it isn't cached or referenced anywhere else, so its GPU atlas must be unloaded
            // here or it leaks VRAM on every swap. ManagedFont also unloads its drop-shadow companion
            // (if any) and clears its line-metrics registry entry, mirroring the cleanup a cached font
            // gets elsewhere (see RaylibFontShadowRegistry/RaylibFontMetricsRegistry).
            new ManagedFont(_font).Dispose();
        }

        if (_font.Texture.Id != oversampledFont.Texture.Id || _font.BaseSize != oversampledFont.BaseSize)
        {
            Font = oversampledFont;
            UpdateWrappedText();
            UpdatePreRenderDimensions();
        }
    }

    /// <summary>
    /// Sums the pixel width of the widest of <paramref name="lines"/> when measured with
    /// <paramref name="font"/> at its own <see cref="Font.BaseSize"/> -- used to derive the ACTUAL
    /// measured-width oversample compensation ratio (issue #4330, mirroring #4309's
    /// <c>BitmapFont.GetRequiredWidthAndHeight</c> use) rather than a naive FontSize/rasterFontSize ratio.
    /// </summary>
    internal static float MeasureLinesWidth(Font font, IEnumerable<string> lines)
    {
        float maxWidth = 0;
        foreach (string line in lines)
        {
            maxWidth = System.Math.Max(maxWidth, MeasureTextEx(font, line, font.BaseSize, 0).X);
        }
        return maxWidth;
    }

    private void UpdateLineHeightInPixels()
    {
        GetLineHeightAndDescender(_font, out _lineHeightInPixels, out _descenderHeight);
    }

    /// <summary>
    /// A Gum bitmap font records its .fnt lineHeight/base when loaded (keyed by atlas texture id),
    /// because raylib's Font struct can't hold them. Use those so line height includes the
    /// descender region exactly as the MonoGame BitmapFont does — otherwise a Text (and any
    /// container sized RelativeToChildren around it, e.g. a ListBoxItem) is short by that region.
    /// Fonts without recorded metrics (raylib's built-in default, a TTF from raylib's own LoadFontEx) fall back to
    /// native measurement, which returns ~BaseSize, with the baseline from <see cref="TryGetTtfBaselineY"/>. Extracted from <see cref="UpdateLineHeightInPixels"/>
    /// (which caches this for the LIVE <see cref="Font"/>) so <see cref="MeasurementLineHeightInPixels"/>
    /// can compute the same metrics for <see cref="EffectiveMeasurementFont"/> instead (issue #4330):
    /// height/word-wrap must stay pinned to the measurement font exactly like width already does, or a
    /// RelativeToChildren box's HEIGHT keeps growing with the live oversampled raster even though its
    /// WIDTH is correctly held stable.
    /// </summary>
    private static void GetLineHeightAndDescender(Font font, out int lineHeightInPixels, out int descenderHeight)
    {
        if (IsWindowReady() == false)
        {
            throw new InvalidOperationException("Cannot measure text because IsWindowReady() is false - did you remember to call InitWindow first?");
        }

        if (RaylibFontMetricsRegistry.TryGet(font, out var metrics))
        {
            lineHeightInPixels = metrics.LineHeight;
            descenderHeight = metrics.LineHeight - metrics.BaselineY;
        }
        else
        {
            lineHeightInPixels = (int)(MeasureTextEx(font, "M", font.BaseSize, 0).Y + .5);
            descenderHeight = TryGetTtfBaselineY(font, out int baselineY)
                ? lineHeightInPixels - baselineY
                : 2;
        }
    }

    /// <summary>
    /// Recovers where raylib draws the baseline of a TTF loaded with <c>LoadFontEx</c> (#6567). raylib
    /// offsets each glyph by the scaled font ascent, so a flat-bottomed glyph ('H') ends exactly on the
    /// baseline. Returns false for fonts without an 'H' and for fonts whose glyph cells span the whole
    /// line (raylib's built-in default font, a sprite sheet loaded with <c>LoadFontFromImage</c>), where
    /// the cell's bottom says nothing about the baseline.
    /// </summary>
    private static bool TryGetTtfBaselineY(Font font, out int baselineY)
    {
        baselineY = 0;
        if (!HasGlyphData(font))
        {
            return false;
        }

        GlyphInfo glyph = GetGlyphInfo(font, 'H');
        if (glyph.Value != 'H')
        {
            return false;
        }

        int glyphHeight = (int)GetGlyphAtlasRec(font, 'H').Height;
        if (glyphHeight >= font.BaseSize)
        {
            return false;
        }

        baselineY = glyph.OffsetY + glyphHeight;
        return baselineY > 0 && baselineY <= font.BaseSize;
    }

    /// <summary>
    /// Whether raylib's glyph lookups can read this font. They index the native glyph and rec arrays without
    /// checking them, so an empty or unloaded <see cref="Font"/> would crash the process.
    /// </summary>
    internal static unsafe bool HasGlyphData(Font font)
    {
        return font.Texture.Id != 0 && font.BaseSize > 0 && font.GlyphCount > 0
            && font.Glyphs != null && font.Recs != null;
    }

    /// <summary>
    /// The line height (in pixels) of <see cref="EffectiveMeasurementFont"/> -- used for layout/word-wrap
    /// height instead of the live <see cref="LineHeightInPixels"/> so a pinned box's height stays stable
    /// across oversample raster changes, mirroring how <see cref="MeasurementFontScale"/> already keeps
    /// width stable (issue #4330).
    /// </summary>
    private int MeasurementLineHeightInPixels
    {
        get
        {
            GetLineHeightAndDescender(EffectiveMeasurementFont, out int lineHeightInPixels, out _);
            return lineHeightInPixels;
        }
    }

    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

    public int Alpha
    {
        get
        {
            return Color.A;
        }
        set
        {
            if (value != Color.A)
            {
                Color = new Color(Color.R, Color.G, Color.B, (byte)value);
            }
        }
    }

    public int Red
    {
        get
        {
            return Color.R;
        }
        set
        {
            if (value != Color.R)
            {
                Color = new Color((byte)value, Color.G, Color.B, Color.A);
            }
        }
    }

    public int Green
    {
        get
        {
            return Color.G;
        }
        set
        {
            if (value != Color.G)
            {
                Color = new Color(Color.R, (byte)value, Color.B, Color.A);
            }
        }
    }

    public int Blue
    {
        get
        {
            return Color.B;
        }
        set
        {
            if (value != Color.B)
            {
                Color = new Color(Color.R, Color.G, (byte)value, Color.A);
            }
        }
    }

    public Color Color
    {
        get; set;
    } = Color.White;

    /// <summary>
    /// When true and a "-shadow.fnt" sibling was loaded for the font(s) this Text draws with (see
    /// <see cref="global::RenderingLibrary.RaylibFontShadowRegistry"/>), a drop shadow is drawn as a second
    /// pass (issue #4057, the Raylib counterpart of #4001): the shadow silhouette is drawn offset by
    /// <see cref="DropshadowOffsetX"/>/<see cref="DropshadowOffsetY"/> and tinted
    /// <see cref="DropshadowColor"/>, with the primary glyphs drawn on top. Degrades gracefully to no
    /// shadow when no shadow companion font was loaded for a given run's font.
    /// </summary>
    public bool HasDropshadow { get; set; }
    public Color DropshadowColor { get; set; } = new Color(0, 0, 0, 180);
    public float DropshadowOffsetX { get; set; }
    public float DropshadowOffsetY { get; set; }


    public bool IsTruncatingWithEllipsisOnLastLine { get; set; }

    /// <inheritdoc/>
    public bool IsHeightDependentOnLines { get; set; }

    public string? Name
    {
        get;
        set;
    }

    string? mRawText;
    public string? RawText
    {
        get => mRawText;
        set
        {
            if (mRawText != value)
            {
                mRawText = value;
                UpdateWrappedText();

                UpdatePreRenderDimensions();
            }
        }
    }

    /// <inheritdoc/>
    public float DescenderHeight => _descenderHeight;

    public float FontScale { get; set; } = 1;

    public object? Tag { get; set; }


    /// <summary>
    /// The Gum blend mode applied when this text is drawn. Null means "use the renderer's current
    /// blend mode" (typically alpha blending). Honored in <see cref="Render"/> via the shared
    /// <see cref="global::RenderingLibrary.Graphics.BatchDrawCallCounter"/>, mirroring the raylib Sprite and
    /// NineSlice renderables.
    /// </summary>
    public global::Gum.RenderingLibrary.Blend? Blend { get; set; }

    // Satisfies the IRenderable contract by deriving from Blend, so there is a single source of
    // truth for this Text's blend mode. Render consumes Blend directly; this getter exists only for
    // the interface (e.g. render-target additive-composite detection on RenderableBase does not
    // apply to Text, but any generic IRenderable consumer still gets the correct state).
    global::Gum.BlendState IRenderable.BlendState =>
        Blend.HasValue
            ? global::Gum.RenderingLibrary.BlendExtensions.ToBlendState(Blend.Value)
            : global::Gum.BlendState.NonPremultiplied;

    public float FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value; UpdateLineHeightInPixels();
        }
    }

    public float WrappedTextWidth
    {
        get
        {
            if (mPreRenderWidth != null)
            {
                return mPreRenderWidth.Value * MeasurementFontScale;
            }
            //else if (mTextureToRender?.Width > 0)
            //{
            //    return mTextureToRender.Width * mFontScale;
            //}
            else
            {
                return 0;
            }
        }
    }

    public float WrappedTextHeight
    {
        get
        {
            if (mPreRenderHeight != null)
            {
                return mPreRenderHeight.Value * MeasurementFontScale;
            }
            //else if (mTextureToRender?.Height > 0)
            //{
            //    return mTextureToRender.Height * mFontScale;
            //}
            else
            {
                return 0;
            }
        }
    }

    int? maxLettersToShow;
    /// <summary>
    /// The maximum number of characters to display visually. Characters beyond this count
    /// are hidden but remain in the <see cref="RawText"/> string. This is a display-only
    /// property useful for typewriter-style effects where text prints out letter-by-letter.
    /// </summary>
    public int? MaxLettersToShow
    {
        get => maxLettersToShow;
        set => maxLettersToShow = value;
    }

    int? maxNumberOfLines;
    private float _fontSize = 12;

    /// <summary>
    /// The maximum number of lines to display. This can be used to 
    /// limit how many lines of text are displayed at one time.
    /// </summary>
    public int? MaxNumberOfLines
    {
        get => maxNumberOfLines;
        set
        {
            if (maxNumberOfLines != value)
            {
                maxNumberOfLines = value;
                UpdateWrappedText();

                UpdatePreRenderDimensions();
            }
        }
    }

    public TextOverflowVerticalMode TextOverflowVerticalMode { get; set; }
    


    public HorizontalAlignment HorizontalAlignment
    {
        get; set;
    }

    public VerticalAlignment VerticalAlignment
    {
        get; set;
    }

    public List<string> WrappedText => mWrappedText;

    public float X
    {
        get => Position.X;
        set
        {
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "X");
            Position.X = value;
        }
    }

    public float Y
    {
        get => Position.Y;
        set
        {
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "Y");
            Position.Y = value;
        }
    }

    public bool FlipHorizontal { get; set; }

    float _rotationValue;
    public float Rotation
    {
        get { return _rotationValue; }
        set
        {
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "Rotation");
            _rotationValue = value;
        }
    }

    public float? Width
    {
        get
        {
            return mWidth;
        }
        set
        {
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "Width");
            if (mWidth != value)
            {
                mWidth = value;
                UpdateWrappedText();
                //UpdateLinePrimitive();
                UpdatePreRenderDimensions();
            }

        }
    }

    public float Height
    {
        get
        {
            return mHeight;
        }
        set
        {
            global::RenderingLibrary.Graphics.NonFiniteCheck.ThrowIfNotFinite(value, "Height");
            if (mHeight != value)
            {
                mHeight = value;

                if (TextOverflowVerticalMode != TextOverflowVerticalMode.SpillOver)
                {
                    UpdateWrappedText();
                }

                //UpdateLinePrimitive();

                if (TextOverflowVerticalMode != TextOverflowVerticalMode.SpillOver)
                {
                    UpdatePreRenderDimensions();
                }

            }
        }
    }

    public float EffectiveWidth
    {
        get
        {
            // I think we want to treat these individually so a 
            // width could be set but height could be default
            if (Width != null)
            {
                return Width.Value;
            }
            // If there is a prerendered width/height, then that means that
            // the width/height has updated but it hasn't yet made its way to the
            // texture. This could happen when the text already has a texture, so give
            // priority to the prerendered values as they may be more up-to-date.
            else if (mPreRenderWidth.HasValue)
            {
                return mPreRenderWidth.Value * MeasurementFontScale;
            }
            //else if (mTextureToRender != null)
            //{
            //    if (mTextureToRender.Width == 0)
            //    {
            //        return 10;
            //    }
            //    else
            //    {
            //        return mTextureToRender.Width * mFontScale;
            //    }
            //}
            else
            {
                // This causes problems when the text object has no text:
                //return 32;
                return 0;
            }
        }
    }

    public float EffectiveHeight
    {
        get
        {
            // December 2, 2024
            // Width now treats 0 width as a proper 0 width. Do we want to do the same for height? Not sure at this point...
            if (Height != 0)
            {
                return Height;
            }
            // See EffectiveWidth for an explanation of why the prerendered values need to come first
            else if (mPreRenderHeight.HasValue)
            {
                return mPreRenderHeight.Value * MeasurementFontScale;
            }
            //else if (mTextureToRender != null)
            //{
            //    if (mTextureToRender.Height == 0)
            //    {
            //        return 10;
            //    }
            //    else
            //    {
            //        return mTextureToRender.Height * mFontScale;
            //    }
            //}
            else
            {
                return 32;
            }
        }
    }

    /// <summary>
    /// The original BBCode / markup string assigned to this Text (before tags were stripped),
    /// or null when the assigned text contained no markup. Set by the property pipeline
    /// (<c>CustomSetPropertyOnRenderable</c>) so font/style changes can re-parse the markup.
    /// </summary>
    public string? StoredMarkupText { get; set; }

    /// <summary>
    /// The inline styling variables (Color / FontScale runs, plus resolved-font "BitmapFont" runs for the
    /// Font / FontSize / IsBold / IsItalic / OutlineThickness family tags, and "Custom" per-letter callback
    /// runs) parsed from BBCode markup assigned to this Text. Populated by the property pipeline when the
    /// assigned text contains markup tags; empty for plain text. Consumed by <see cref="Render"/> to draw
    /// per-run styling.
    /// </summary>
    public List<InlineVariable> InlineVariables { get; private set; }

    public float LineHeightMultiplier { get; set; } = 1;


    float IPositionedSizedObject.Width
    {
        get
        {
            return EffectiveWidth;
        }
        set
        {
            Width = value;
        }
    }

    float IPositionedSizedObject.Height
    {
        get
        {
            return EffectiveHeight;
        }
        set
        {
            Height = value;
        }
    }

    // not sure if basesize is correct here...
    public int LineHeightInPixels => _lineHeightInPixels;

    bool IRenderable.Wrap => false;


    bool IRenderableIpso.IsRenderTarget => false;


    public Text() : this(null)
    {
    }

    public Text(ISystemManagers? managers)
    {
        // #2757 — consult the project-wide DefaultFont so non-Forms TextRuntime instances
        // (e.g. raw section headers in sample screens) pick up the user-chosen font instead
        // of raylib's tiny pixel default. BaseSize == 0 is the uninitialized-Font sentinel.
        Font = DefaultFont.BaseSize > 0 ? DefaultFont : GetFontDefault();
        mChildren = new();
        InlineVariables = new List<InlineVariable>();
        Visible = true;
    }

    /// <summary>
    /// Re-runs the shared wrap loop (<see cref="IWrappedTextExtensions.RebuildLines"/>) to rebuild
    /// <see cref="WrappedText"/>. Public so the BBCode dispatch (CustomSetPropertyOnRenderable) can
    /// re-wrap once <see cref="InlineVariables"/> populate — the first wrap runs when RawText is set,
    /// before the runs exist, so it is blind to any enlarged [FontSize]/[FontScale] run (#3532).
    /// </summary>
    public void UpdateWrappedText()
    {
        OnGlyphGrowthCheck?.Invoke();

        this.RebuildLines(mWrappedText, mPreviousWrappedText);
    }

    /// <summary>
    /// Returns the size of the string, ignoring font scale, but considering the bitmap font.
    /// </summary>
    /// <param name="whatToMeasure"></param>
    /// <returns></returns>
    public float MeasureString(string whatToMeasure)
    {
        var measurementFont = EffectiveMeasurementFont;
        return MeasureTextEx(measurementFont, whatToMeasure, measurementFont.BaseSize, 0).X;
    }

    /// <summary>
    /// Returns the size of the string using Raylib's native text measurement. The
    /// <paramref name="style"/> parameter is advisory on the Raylib runtime and is
    /// ignored - Raylib's native engine is used regardless of the requested style.
    /// This overload exists so callers can write platform-agnostic code that also
    /// works on runtimes (such as the MonoGame runtime) where the style is honored.
    /// </summary>
    public float MeasureString(string whatToMeasure, HorizontalMeasurementStyle style)
    {
        var measurementFont = EffectiveMeasurementFont;
        return MeasureTextEx(measurementFont, whatToMeasure, measurementFont.BaseSize, 0).X;
    }

    public virtual void PreRender()
    {
        OnPreRender?.Invoke();
    }

    /// <summary>
    /// Splits a single wrapped line into styled runs according to which <see cref="InlineVariables"/>
    /// are active over it. Backed by the shared <see cref="StyledSubstringSplitter"/>.
    /// </summary>
    public List<StyledSubstring> GetStyledSubstrings(int startOfLineIndex, string lineOfText) =>
        _styledSubstringSplitter.GetStyledSubstrings(startOfLineIndex, lineOfText, InlineVariables);

    // For the lines of WrappedText, which are drawn and measured every frame: the runs are cached until the
    // line or the inline variables change. Read-only to callers.
    internal List<StyledSubstring> GetCachedStyledSubstrings(int lineIndex, int startOfLineIndex, string lineOfText) =>
        _styledSubstringSplitter.GetStyledSubstrings(lineIndex, startOfLineIndex, lineOfText, InlineVariables);

    /// <summary>
    /// Rounds a position/origin to whole pixels using the configured <see cref="TextPositionRoundingMode"/>
    /// when the <see cref="EffectiveTextRenderingPositionMode"/> is SnapToPixel; otherwise returns the value unchanged.
    /// Rounding origin along with position avoids the baseline misalignment / "sizzle" seen on small pixel
    /// fonts when vertical alignment produces fractional values. Text turned by <see cref="_renderRotation"/>
    /// snaps only at a whole quarter turn, see the overload taking the rotation.
    /// </summary>
    private Vector2 SnapToPixelIfNeeded(Vector2 value) => SnapToPixelIfNeeded(value, _renderRotation);

    /// <summary>
    /// Snaps like <see cref="SnapToPixelIfNeeded(Vector2)"/> for something drawn turned counterclockwise by
    /// <paramref name="rotationDegrees"/>: only a whole quarter turn keeps the pixel grid, so any other turn is
    /// left unsnapped, as MonoGame's SpriteRenderer does for glyphs.
    /// </summary>
    private Vector2 SnapToPixelIfNeeded(Vector2 value, float rotationDegrees)
    {
        if (EffectiveTextRenderingPositionMode != TextRenderingPositionMode.SnapToPixel || !IsQuarterTurn(rotationDegrees))
        {
            return value;
        }

        switch (TextPositionRoundingMode)
        {
            case TextPositionRoundingMode.Floor:
                return new Vector2((int)Math.Floor(value.X), (int)Math.Floor(value.Y));
            case TextPositionRoundingMode.Ceiling:
                return new Vector2((int)Math.Ceiling(value.X), (int)Math.Ceiling(value.Y));
            default:
                return new Vector2(MathFunctions.RoundToInt(value.X), MathFunctions.RoundToInt(value.Y));
        }
    }

    /// <summary>
    /// True when <paramref name="rotationDegrees"/> is a whole number of quarter turns, within the tenth of a
    /// degree MonoGame's SpriteRenderer tolerates.
    /// </summary>
    private static bool IsQuarterTurn(float rotationDegrees)
    {
        float quarterTurns = System.Math.Abs(rotationDegrees / 90f);
        return System.Math.Abs(quarterTurns - MathF.Round(quarterTurns)) < 0.1f / 90f;
    }

    /// <summary>
    /// The absolute rotation the current <see cref="Render"/> call turns the whole block by, read by the
    /// snapping so it snaps only at a whole quarter turn.
    /// </summary>
    private float _renderRotation;

    public void Render(ISystemManagers managers)
    {
        if (!Visible) return;

        var fontValue = Font;

        var absoluteLeft = this.GetAbsoluteLeft();
        var position = new Vector2(
            absoluteLeft,
            this.GetAbsoluteTop());
        var origin = new Vector2(
            0,
            0);


        if (VerticalAlignment == VerticalAlignment.Center)
        {
            position.Y += this.Height / 2;
            origin.Y = MeasurementFontScale * mPreRenderHeight / 2 ?? 0;
        }
        if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            position.Y += this.Height;
            origin.Y = MeasurementFontScale * mPreRenderHeight ?? 0;
        }

        // Text.Rotation turns the whole block counterclockwise about the Text's top-left, as on MonoGame and
        // Skia, so styled runs and [Custom] letters (which push their own matrix) turn with it. Everything
        // below lays out unturned; the pivot is snapped so a quarter turn keeps snapped glyphs on the grid.
        _renderRotation = this.GetAbsoluteRotation();
        bool isRotated = _renderRotation != 0;
        if (isRotated)
        {
            Vector2 pivot = SnapToPixelIfNeeded(new Vector2(absoluteLeft, this.GetAbsoluteTop()));
            Raylib_cs.Rlgl.PushMatrix();
            Raylib_cs.Rlgl.Translatef(pivot.X, pivot.Y, 0);
            // Gum rotation is counterclockwise; rlgl's is clockwise on a y-down screen.
            Raylib_cs.Rlgl.Rotatef(-_renderRotation, 0, 0, 1);
            Raylib_cs.Rlgl.Translatef(-pivot.X, -pivot.Y, 0);
        }

        int lettersShown = 0;
        // Absolute index of the first character of the current line within the stripped RawText,
        // used to look up which InlineVariables are active. UpdateLines keeps explicit '\n' chars
        // in the line string, so advancing by the (untruncated) line length keeps this in sync.
        int startOfLineIndex = 0;

        // Honor an explicit Blend by wrapping every glyph draw in a begin/end pair, mirroring the
        // raylib Sprite and NineSlice renderables. Null Blend leaves the renderer's ambient mode.
        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.BeginBlendMode(Blend.Value);
        }

        // Per-line vertical offsets. Plain text (no inline runs) advances every line by the same height;
        // when inline [FontSize]/[FontScale] runs can enlarge a line, each line advances by its own height
        // so a line after an enlarged run is not drawn on top of it (#3532). The styled offset is summed as
        // the lines are drawn (the same GetLineAdvance GetLineTopOffsets sums), so nothing is allocated.
        bool isStyled = InlineVariables.Count > 0;
        float styledLineTop = 0;

        for(int i = 0; i < WrappedText.Count; i++)
        {
            var line = WrappedText[i];

            // How many letters of this line are drawn. The line itself stays whole so the styled run cache,
            // which is keyed on the whole line, is not rebuilt for every new count (#6440).
            int lettersToDraw = line.Length;
            if (maxLettersToShow.HasValue)
            {
                var lettersRemaining = maxLettersToShow.Value - lettersShown;
                if (lettersRemaining <= 0)
                {
                    break;
                }
                if (lettersRemaining < line.Length)
                {
                    lettersToDraw = lettersRemaining;
                }
                lettersShown += lettersToDraw;
            }

            var linePosition = position;
            linePosition.Y += isStyled
                ? styledLineTop
                : GetLineAdvance(i, startOfLineIndex, line) * i;

            var substrings = InlineVariables.Count > 0
                ? GetCachedStyledSubstrings(i, startOfLineIndex, line)
                : null;

            if (substrings == null || substrings.Count == 0)
            {
                DrawPlainLine(fontValue, GetFirstLetters(line, lettersToDraw), linePosition, absoluteLeft, origin.Y);
            }
            else
            {
                DrawStyledLine(fontValue, substrings, lettersToDraw, linePosition, absoluteLeft, origin.Y);
            }

            if (isStyled)
            {
                styledLineTop += GetLineAdvance(i, startOfLineIndex, WrappedText[i]);
            }

            startOfLineIndex += WrappedText[i].Length;
        }

        if (isRotated)
        {
            Raylib_cs.Rlgl.PopMatrix();
        }

        if (Blend.HasValue)
        {
            global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter.EndBlendMode();
        }
    }

    /// <summary>
    /// The Y offset (in pixels from this Text's top) at which each wrapped line is drawn, growing a
    /// line's advance by the tallest inline [FontSize]/[FontScale] run on it so a line following an
    /// enlarged run is placed below that run's full height rather than one base line-height down — which
    /// overlapped it (issue #3532). Matches the per-line height the size pass reports
    /// (<see cref="GetInlineVariableAwareWidthAndHeight"/>). Consumed by <see cref="Render"/>; public so
    /// the vertical stacking can be unit-tested without rendering.
    /// </summary>
    public IReadOnlyList<float> GetLineTopOffsets()
    {
        var offsets = new float[WrappedText.Count];
        float runningY = 0;
        int startOfLineIndex = 0;
        for (int i = 0; i < WrappedText.Count; i++)
        {
            offsets[i] = runningY;
            var line = WrappedText[i];
            runningY += GetLineAdvance(i, startOfLineIndex, line);
            startOfLineIndex += line.Length;
        }
        return offsets;
    }

    /// <summary>
    /// How far below its own top the next wrapped line sits: the one <see cref="StyledLineHeight"/> formula
    /// with the line's tallest size run, shared by <see cref="Render"/> and <see cref="GetLineTopOffsets"/>
    /// (and, in base units, by the size pass).
    /// </summary>
    private float GetLineAdvance(int lineIndex, int startOfLineIndex, string line)
    {
        return StyledLineHeight(EffectiveFontScale, GetTallestSizeRunScale(lineIndex, startOfLineIndex, line));
    }

    /// <summary>
    /// The height of a line: the base line height times the larger of the base scale times
    /// <see cref="LineHeightMultiplier"/> and the tallest size run's scale. A run's scale is absolute and is
    /// not multiplied, so <c>[FontScale=3]</c> stays 3 lines tall at any multiplier, and a line with no size
    /// run (<paramref name="tallestSizeRunScale"/> 0) is the multiplied base. Matches MonoGame's
    /// <c>Text.ComputeStyledLineHeight</c>, <c>max(base * multiplier, run * 1)</c> (#6450). The size pass
    /// calls it in base units: a base scale of 1 and the run as a factor of the base scale.
    /// </summary>
    private float StyledLineHeight(float baseScale, float tallestSizeRunScale)
    {
        return TextLineHeight.Compute(LineHeightInPixels, baseScale, LineHeightMultiplier, tallestSizeRunScale);
    }

    /// <summary>
    /// True when a run changes the size it is drawn at: a [FontScale] or a font-family ("BitmapFont" marker,
    /// see <see cref="ResolveRunFont"/>) tag. Colour and other runs leave their line at the multiplied base
    /// height, as in MonoGame.
    /// </summary>
    private static bool IsSizeRun(List<InlineVariable> variables)
    {
        for (int variableIndex = 0; variableIndex < variables.Count; variableIndex++)
        {
            InlineVariable variable = variables[variableIndex];
            string name = variable.VariableName;
            if (name == nameof(FontScale) || (name == "BitmapFont" && !IsBaseFontMarker(variable.Value)))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// True for the run a closing [/FontSize] adds when no font creator is wired: a size of 0 means "back to
    /// the base font" (#6468), so it ends the open size run without being a size run itself.
    /// </summary>
    private static bool IsBaseFontMarker(object? value) => value is float size && size <= 0;

    /// <summary>
    /// The layout scale of the tallest size run ([FontScale] or a [FontSize] font swap) on the wrapped line
    /// beginning at <paramref name="startOfLineIndex"/> in the stripped text, or 0 when the line has none.
    /// Resolved the way <see cref="DrawStyledLine"/> lays each run out, so drawn and vertically-advanced
    /// sizes cannot drift.
    /// </summary>
    private float GetTallestSizeRunScale(int lineIndex, int startOfLineIndex, string line)
    {
        if (InlineVariables.Count == 0)
        {
            return 0;
        }

        var substrings = GetCachedStyledSubstrings(lineIndex, startOfLineIndex, line);
        float maxScale = 0;
        for (int substringIndex = 0; substringIndex < substrings.Count; substringIndex++)
        {
            var variables = substrings[substringIndex].Variables;
            if (IsSizeRun(variables))
            {
                maxScale = System.Math.Max(maxScale, ResolveRunFont(variables, _font, _oversampleCompensationScale).LayoutScale);
            }
        }
        return maxScale;
    }

    // No OverlapDirection over here (or in DrawStyledLine below) -- both hand the whole line/run to
    // one DrawTextPro call, there's no per-glyph draw-order knob to hook into. Getting it would mean
    // hand-rolling the glyph loop ourselves instead of letting raylib draw the string, and nothing in
    // Gum actually uses overlap direction. XNALIKE-only is fine, not adding it here.

    // No RenderBoundary either -- it's a debug outline used by the Gum tool's XNA-based editor views
    // (rulers, distance arrows), not a feature any game project needs. XNALIKE-only is fine, not
    // adding it here (#3708).

    string? _firstLettersSource;
    int _firstLettersLength;
    string _firstLetters = string.Empty;

    /// <summary>
    /// The first <paramref name="length"/> letters of <paramref name="source"/>: the source itself when all of
    /// it is wanted, otherwise a substring kept until the source or the length changes. A typewriter reveal
    /// holds a count for several frames and cuts only one line (or run) per frame, so one cached entry
    /// allocates nothing while the count is held and one string when it changes (#6440).
    /// </summary>
    private string GetFirstLetters(string source, int length)
    {
        if (length >= source.Length)
        {
            return source;
        }

        if (!ReferenceEquals(source, _firstLettersSource) || length != _firstLettersLength)
        {
            _firstLetters = source.Substring(0, length);
            _firstLettersSource = source;
            _firstLettersLength = length;
        }
        return _firstLetters;
    }

    /// <summary>
    /// Draws a single line with no inline styling, honoring horizontal alignment and pixel snapping.
    /// </summary>
    private void DrawPlainLine(Font fontValue, string line, Vector2 linePosition, float absoluteLeft, float originY)
    {
        var origin = new Vector2(0, originY);
        linePosition.X = absoluteLeft;

        if (HorizontalAlignment == HorizontalAlignment.Center)
        {
            linePosition.X += (this.Width ?? 32) / 2;
            origin.X = MeasureTextEx(fontValue, line, fontValue.BaseSize * EffectiveFontScale, 0).X / 2;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            linePosition.X += this.Width ?? 32;
            origin.X = MeasureTextEx(fontValue, line, fontValue.BaseSize * EffectiveFontScale, 0).X;
        }

        linePosition = SnapToPixelIfNeeded(linePosition);
        origin = SnapToPixelIfNeeded(origin);

        if (HasDropshadow && RaylibFontShadowRegistry.TryGet(fontValue.Texture.Id, out Font shadowFont))
        {
            var shadowPosition = SnapToPixelIfNeeded(
                linePosition + new Vector2(DropshadowOffsetX, DropshadowOffsetY) * EffectiveFontScale);
            DrawTextPro(shadowFont, line, shadowPosition, origin, 0, fontValue.BaseSize * EffectiveFontScale, 0, DropshadowColor);
        }

        DrawOutline(fontValue, line, linePosition, origin, fontValue.BaseSize * EffectiveFontScale, Color);

        // Texture filtering is applied once when the font's atlas texture is loaded/built (see
        // ContentLoader.DefaultTextureFilter, #3496), not per-draw — a per-line GPU state reset here
        // was both redundant and ignored the project's texture filter setting.
        DrawTextPro(fontValue, line, linePosition, origin, 0, fontValue.BaseSize * EffectiveFontScale, 0, Color);
    }

    /// <summary>
    /// The font, draw size, and layout scale a styled run resolves to. <see cref="Font"/> is the actual
    /// Raylib font used to draw and measure the run (the base font, or a per-run [FontSize] swap font);
    /// <see cref="DrawSize"/> is the pixel size passed to DrawTextPro/MeasureTextEx; <see cref="LayoutScale"/>
    /// is the run's size relative to the base font's metrics, used for baseline/height layout.
    /// </summary>
    private readonly struct ResolvedRunFont
    {
        public readonly Font Font;
        public readonly float DrawSize;
        public readonly float LayoutScale;

        public ResolvedRunFont(Font font, float drawSize, float layoutScale)
        {
            Font = font;
            DrawSize = drawSize;
            LayoutScale = layoutScale;
        }
    }

    /// <summary>
    /// Resolves the font/size a styled run draws and is measured with, against <paramref name="baseFont"/>
    /// composed with <paramref name="oversampleCompensationScale"/>. No tag, or a [FontScale=v] run,
    /// keeps <paramref name="baseFont"/> at v (or the Text's own <see cref="FontScale"/>) times its base
    /// size. Callers pick which font/compensation pair to resolve against depending on intent (issue
    /// #4365): draw call sites (<see cref="DrawStyledLine"/>, <see cref="GetTallestSizeRunScale"/>) pass the
    /// LIVE <see cref="Font"/> and the current <see cref="OversampleCompensationScale"/>, so the run's
    /// DRAWN size tracks the oversample ratio the same way plain text does; the measurement call site
    /// (<see cref="MeasureStyledLineInBaseUnits"/>, used for both wrap and pre-render size) passes
    /// <see cref="EffectiveMeasurementFont"/> with no compensation, so a run measures against the SAME
    /// stable font regardless of what the live Font is currently oversampled to -- otherwise a word
    /// straddling a wrap boundary could land on a different line depending on oversampling (manual-test
    /// finding), the same class of instability <see cref="MeasurementFont"/> already prevents for plain
    /// text. A [FontSize=N] run either swaps in a font re-rasterized at N (crisp; its inline value is a
    /// <see cref="Font"/>) drawn at its native size, or - when no font creator was wired so no swap font
    /// could be built - falls back to scaling the base atlas to N px (its inline value is a float);
    /// neither depends on <paramref name="baseFont"/>/<paramref name="oversampleCompensationScale"/> (see
    /// <see cref="OversampleCompensationScale"/>'s doc comment). Shared by <see cref="DrawStyledLine"/>
    /// (draw) and <see cref="GetInlineVariableAwareWidthAndHeight"/> (measure) so the drawn and measured
    /// size of a run cannot diverge - that drift is what reproduced the RelativeToChildren spill (#3524).
    /// </summary>
    private ResolvedRunFont ResolveRunFont(List<InlineVariable> variables, Font baseFont, float oversampleCompensationScale)
    {
        Font drawFont = baseFont;
        bool hasSwapFont = false;
        float scale = FontScale * oversampleCompensationScale;
        // The run's scale without the oversample compensation, which a size font does not need (#6486).
        float sizeFontScale = FontScale;
        float? absolutePixelSize = null;

        foreach (var variable in variables)
        {
            switch (variable.VariableName)
            {
                case nameof(FontScale):
                    // An inline [FontScale=v] tag is an ABSOLUTE per-run scale (not relative to the
                    // Text-level FontScale), so it must independently compose the same compensation.
                    sizeFontScale = (float)variable.Value;
                    scale = sizeFontScale * oversampleCompensationScale;
                    break;
                // "BitmapFont" is the shared historical marker name the font-resolution stack model emits for
                // every font-family tag (Font / FontSize / IsBold / IsItalic / OutlineThickness). On Raylib
                // the value is a Raylib_cs.Font (a crisp per-run swap font) or a float (an absolute pixel size
                // to scale the base atlas to when no font creator produced a crisp font) - not a BitmapFont.
                case "BitmapFont":
                    if (variable.Value is Font swapFont && swapFont.BaseSize > 0)
                    {
                        drawFont = swapFont;
                        hasSwapFont = true;
                    }
                    else if (variable.Value is float pixelSize)
                    {
                        // A size of 0 is the closing-tag marker: back to the base font (see IsBaseFontMarker).
                        absolutePixelSize = pixelSize > 0 ? pixelSize : null;
                    }
                    break;
            }
        }

        float baseSize = baseFont.BaseSize;
        float drawSize;
        if (hasSwapFont)
        {
            // A crisp font rasterized at the requested size: draw it at its native size times the run's
            // scale, exactly as MonoGame renders a swapped BitmapFont.
            drawSize = drawFont.BaseSize * sizeFontScale;
        }
        else if (absolutePixelSize.HasValue)
        {
            // No font creator wired: approximate the swap by scaling the base atlas to the absolute size.
            drawSize = absolutePixelSize.Value * sizeFontScale;
        }
        else
        {
            drawSize = baseSize * scale;
        }

        float layoutScale = baseSize > 0 ? drawSize / baseSize : scale;
        return new ResolvedRunFont(drawFont, drawSize, layoutScale);
    }

    /// <summary>
    /// Draws a single line as a sequence of styled runs (BBCode markup), applying per-run Color and per-run
    /// font (a resolved swap font from the Font / FontSize / IsBold / IsItalic / OutlineThickness family, or
    /// a FontScale multiplier / FontSize absolute-px atlas scale). Runs are laid out left-to-right and
    /// baseline-aligned so a larger run sits on the same baseline as its neighbors. <c>[Custom]</c> per-letter
    /// callbacks (#3640) run per-run below, applying XOffset/YOffset/Color/ReplacementCharacter directly; a
    /// scaled or rotated letter is drawn by <see cref="DrawTransformedRun"/>.
    /// </summary>
    private void DrawStyledLine(Font fontValue, List<StyledSubstring> substrings, int lettersToDraw, Vector2 linePosition, float absoluteLeft, float originY)
    {
        // First pass: resolve each run's effective color, font/size, custom-callback offsets and measured
        // width, plus the line totals needed for horizontal alignment (total width) and baseline alignment
        // (tallest run's baseline). The font, draw size and layout scale come from the shared resolver, so
        // draw and measure use the same per-run font and cannot diverge (issue #3524).
        // The per-run results live in a scratch array kept on this Text, so drawing an unchanged line
        // allocates nothing (#6426).
        if (_styledRunScratch.Length < substrings.Count)
        {
            _styledRunScratch = new StyledRunDraw[substrings.Count];
        }
        StyledRunDraw[] runs = _styledRunScratch;
        float totalWidth = 0;
        // Issue #4365: EffectiveFontScale, not FontScale -- must match ResolveRunFont's now-compensated
        // LayoutScale (see GetTallestSizeRunScale) so the baseline math below isn't skewed under an active
        // oversample ratio.
        float maxBaseline = (LineHeightInPixels - _descenderHeight) * EffectiveFontScale;

        // Only the runs holding the first lettersToDraw letters of the line are drawn (the MaxLettersToShow
        // reveal), the last of them cut short.
        int runCount = 0;
        int lettersLeft = lettersToDraw;

        for (int s = 0; s < substrings.Count && lettersLeft > 0; s++)
        {
            var run = substrings[s];
            var runColor = Color;
            var resolvedFont = ResolveRunFont(run.Variables, _font, _oversampleCompensationScale);
            var runText = GetFirstLetters(run.Substring, lettersLeft);
            lettersLeft -= run.Substring.Length;
            runCount++;
            float xOffset = 0;
            float yOffset = 0;
            float rotationDegrees = 0;
            float scaleX = 1;
            float scaleY = 1;
            HorizontalAlignment scaleXOrigin = HorizontalAlignment.Center;
            VerticalAlignment scaleYOrigin = VerticalAlignment.Center;

            foreach (var variable in run.Variables)
            {
                switch (variable.VariableName)
                {
                    case nameof(Color):
                        if (variable.Value is System.Drawing.Color drawingColor)
                        {
                            runColor = new Color(drawingColor.R, drawingColor.G, drawingColor.B, drawingColor.A);
                        }
                        break;
                    case nameof(Red):
                        runColor = new Color((byte)variable.Value, runColor.G, runColor.B, runColor.A);
                        break;
                    case nameof(Green):
                        runColor = new Color(runColor.R, (byte)variable.Value, runColor.B, runColor.A);
                        break;
                    case nameof(Blue):
                        runColor = new Color(runColor.R, runColor.G, (byte)variable.Value, runColor.A);
                        break;
                    case "Custom":
                        if (variable.Value is ParameterizedLetterCustomizationCall call)
                        {
                            var contextFunction = call.ContextFunction;
                            var function = call.Function;
                            if (contextFunction != null || function != null)
                            {
                                LetterCustomization response;
                                if (contextFunction != null)
                                {
                                    var context = new LetterCustomization
                                    {
                                        XOffset = xOffset,
                                        YOffset = yOffset,
                                        Color = System.Drawing.Color.FromArgb(runColor.A, runColor.R, runColor.G, runColor.B),
                                        ScaleX = scaleX,
                                        ScaleY = scaleY,
                                        RotationDegrees = rotationDegrees,
                                    };
                                    response = contextFunction(call.CharacterIndex, call.TextBlock, context);
                                }
                                else
                                {
                                    response = function!(call.CharacterIndex, call.TextBlock);
                                }

                                if (response.XOffset != null)
                                {
                                    xOffset = response.XOffset.Value;
                                }
                                if (response.YOffset != null)
                                {
                                    yOffset = response.YOffset.Value;
                                }
                                if (response.RotationDegrees != null)
                                {
                                    rotationDegrees = response.RotationDegrees.Value;
                                }
                                if (response.Color != null)
                                {
                                    var c = response.Color.Value;
                                    runColor = new Color(c.R, c.G, c.B, c.A);
                                }
                                if (response.ReplacementCharacter != null)
                                {
                                    runText = response.ReplacementCharacter.Value.ToString();
                                }
                                if (response.ScaleX != null)
                                {
                                    scaleX = response.ScaleX.Value;
                                }
                                if (response.ScaleY != null)
                                {
                                    scaleY = response.ScaleY.Value;
                                }
                                // The origins come from this response only, as on MonoGame.
                                scaleXOrigin = response.ScaleXOrigin ?? HorizontalAlignment.Center;
                                scaleYOrigin = response.ScaleYOrigin ?? VerticalAlignment.Center;
                            }
                        }
                        break;
                }
            }

            ref StyledRunDraw drawRun = ref runs[s];
            drawRun.Color = runColor;
            drawRun.Font = resolvedFont.Font;
            // A [Custom] scale or rotation changes only how the letter is drawn, so it sizes nothing here.
            drawRun.DrawSize = resolvedFont.DrawSize;
            drawRun.Scale = resolvedFont.LayoutScale;
            SetRunLineMetrics(ref drawRun);
            drawRun.Text = runText;
            drawRun.XOffset = xOffset;
            drawRun.YOffset = yOffset;
            drawRun.Rotation = rotationDegrees;
            drawRun.ScaleX = scaleX;
            drawRun.ScaleY = scaleY;
            drawRun.ScaleXOrigin = scaleXOrigin;
            drawRun.ScaleYOrigin = scaleYOrigin;
            drawRun.Width = MeasureTextEx(resolvedFont.Font, runText, drawRun.DrawSize, 0).X;
            totalWidth += drawRun.Width;
            maxBaseline = System.Math.Max(maxBaseline, drawRun.BaselineFromTop);
        }

        float lineStartX = absoluteLeft;
        if (HorizontalAlignment == HorizontalAlignment.Center)
        {
            lineStartX += ((this.Width ?? 32) - totalWidth) / 2;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            lineStartX += (this.Width ?? 32) - totalWidth;
        }

        // Texture filtering is applied once when the font's atlas texture is loaded/built (see
        // ContentLoader.DefaultTextureFilter, #3496), not per-draw.
        float advance = 0;
        for (int s = 0; s < runCount; s++)
        {
            ref StyledRunDraw drawRun = ref runs[s];
            // Push shorter runs down so every run shares the tallest run's baseline.
            float baselineDelta = maxBaseline - drawRun.BaselineFromTop;

            var runPosition = new Vector2(lineStartX + advance + drawRun.XOffset, linePosition.Y + baselineDelta + drawRun.YOffset);
            var runOrigin = new Vector2(0, originY);

            if (drawRun.ScaleX != 1 || drawRun.ScaleY != 1 || drawRun.Rotation != 0)
            {
                DrawTransformedRun(ref drawRun, new Vector2(runPosition.X, runPosition.Y - originY));
                advance += drawRun.Width;
                continue;
            }

            runPosition = SnapToPixelIfNeeded(runPosition);
            runOrigin = SnapToPixelIfNeeded(runOrigin);

            // Issue #4057: each run looks up its OWN font's shadow companion (a run may draw with a
            // swapped [FontSize]/[IsBold]/etc. font), so a run whose font has no shadow companion
            // simply draws no shadow - the same graceful degradation as the base-font path.
            if (HasDropshadow && RaylibFontShadowRegistry.TryGet(drawRun.Font.Texture.Id, out Font runShadowFont))
            {
                var shadowRunPosition = SnapToPixelIfNeeded(
                    runPosition + new Vector2(DropshadowOffsetX, DropshadowOffsetY) * drawRun.Scale);
                DrawTextPro(runShadowFont, drawRun.Text, shadowRunPosition, runOrigin, 0, drawRun.DrawSize, 0, DropshadowColor);
            }

            DrawOutline(drawRun.Font, drawRun.Text, runPosition, runOrigin, drawRun.DrawSize, drawRun.Color);
            DrawTextPro(drawRun.Font, drawRun.Text, runPosition, runOrigin, 0, drawRun.DrawSize, 0, drawRun.Color);

            advance += drawRun.Width;
        }
    }

    /// <summary>
    /// A run drawn with the Text's own font scales that font's metrics; a run with a swapped font uses the
    /// swapped font's own line height and baseline at its draw size, as MonoGame aligns each run by its
    /// font's <c>BaselineY</c>.
    /// </summary>
    private void SetRunLineMetrics(ref StyledRunDraw drawRun)
    {
        if (drawRun.Font.Texture.Id == _font.Texture.Id || drawRun.Font.BaseSize <= 0)
        {
            drawRun.LineHeight = LineHeightInPixels * drawRun.Scale;
            drawRun.BaselineFromTop = (LineHeightInPixels - _descenderHeight) * drawRun.Scale;
        }
        else
        {
            GetLineHeightAndDescender(drawRun.Font, out int runLineHeight, out int runDescender);
            float runFontScale = drawRun.DrawSize / drawRun.Font.BaseSize;
            drawRun.LineHeight = runLineHeight * runFontScale;
            drawRun.BaselineFromTop = (runLineHeight - runDescender) * runFontScale;
        }
    }

    /// <summary>
    /// Draws a <c>[Custom]</c> letter that is scaled or rotated, as MonoGame and Skia do: its cell (laid out at
    /// <paramref name="cellTopLeft"/>, unscaled) is scaled by ScaleX/ScaleY about the point ScaleXOrigin/ScaleYOrigin
    /// name, then turned counterclockwise about the scaled cell's top-left. DrawTextPro takes one font size, so
    /// the non-uniform scale goes on the rlgl matrix instead. Its top-left snaps to the pixel grid when the
    /// letter's turn plus the Text's is a whole quarter turn, as MonoGame snaps such a glyph's position (#6554).
    /// </summary>
    private void DrawTransformedRun(ref StyledRunDraw drawRun, Vector2 cellTopLeft)
    {
        Vector2 scaledTopLeft = LetterCellScale.GetScaledTopLeft(cellTopLeft.X, cellTopLeft.Y, drawRun.Width, drawRun.LineHeight,
            drawRun.BaselineFromTop, drawRun.ScaleX, drawRun.ScaleY, drawRun.ScaleXOrigin, drawRun.ScaleYOrigin);
        float totalRotation = _renderRotation + drawRun.Rotation;
        scaledTopLeft = SnapToPixelIfNeeded(scaledTopLeft, totalRotation);

        if (HasDropshadow && RaylibFontShadowRegistry.TryGet(drawRun.Font.Texture.Id, out Font runShadowFont))
        {
            DrawTransformed(runShadowFont, ref drawRun, SnapToPixelIfNeeded(
                scaledTopLeft + new Vector2(DropshadowOffsetX, DropshadowOffsetY) * drawRun.Scale, totalRotation), DropshadowColor);
        }
        // An upright ring as thick as DrawOutline's, around the transformed letter.
        if (RaylibFontOutlineRegistry.TryGetOffsets(drawRun.Font, drawRun.DrawSize, out Vector2[] outlineOffsets))
        {
            Color outlineColor = new Color((byte)0, (byte)0, (byte)0, drawRun.Color.A);
            foreach (Vector2 offset in outlineOffsets)
            {
                DrawTransformed(drawRun.Font, ref drawRun, scaledTopLeft + offset, outlineColor);
            }
        }
        DrawTransformed(drawRun.Font, ref drawRun, scaledTopLeft, drawRun.Color);
    }

    /// <summary>
    /// Draws the outline of a font loaded without a font creator (#6639): the glyphs in black, at the offsets of a
    /// round outline of the font's OutlineThickness stretched by the same factor as the glyphs (#6669), so the fill
    /// drawn after it sits on top. A font with an outline baked into its atlas (from a creator) has none registered.
    /// </summary>
    private static void DrawOutline(Font font, string text, Vector2 position, Vector2 origin, float drawSize, Color color)
    {
        if (!RaylibFontOutlineRegistry.TryGetOffsets(font, drawSize, out Vector2[] offsets))
        {
            return;
        }

        Color outlineColor = new Color((byte)0, (byte)0, (byte)0, color.A);
        foreach (Vector2 offset in offsets)
        {
            DrawTextPro(font, text, position + offset, origin, 0, drawSize, 0, outlineColor);
        }
    }

    private static void DrawTransformed(Font font, ref StyledRunDraw drawRun, Vector2 scaledTopLeft, Color color)
    {
        Raylib_cs.Rlgl.PushMatrix();
        Raylib_cs.Rlgl.Translatef(scaledTopLeft.X, scaledTopLeft.Y, 0);
        // Gum rotation is counterclockwise; rlgl's is clockwise on a y-down screen.
        Raylib_cs.Rlgl.Rotatef(-drawRun.Rotation, 0, 0, 1);
        Raylib_cs.Rlgl.Scalef(drawRun.ScaleX, drawRun.ScaleY, 1);
        DrawTextPro(font, drawRun.Text, Vector2.Zero, Vector2.Zero, 0, drawRun.DrawSize, 0, color);
        Raylib_cs.Rlgl.PopMatrix();
    }

    public void SetNeedsRefreshToTrue()
    {
    }

    public void UpdatePreRenderDimensions()
    {
        int requiredWidth = 0;
        int requiredHeight = 0;
        bool usedStyledSizePass = false;

        if (this.mRawText != null)
        {
            // #3481: when the text carries inline [FontScale=N] runs, each line's reported size must
            // grow by the largest scale covering it, or a tall run overflows its slot and overlaps
            // the next stacked sibling. FontScale <= 0 falls through to the plain path (everything
            // downstream multiplies by it, so the result is 0 either way, and it avoids a
            // divide-by-zero in the per-run base-unit conversion). Kept in parity with the MonoGame
            // Text.UpdatePreRenderDimensions (RenderingLibrary/Graphics/Text.cs).
            if (InlineVariables.Count > 0 && FontScale > 0)
            {
                GetInlineVariableAwareWidthAndHeight(out requiredWidth, out requiredHeight);
                usedStyledSizePass = true;
            }
            else
            {
                GetRequiredWidthAndHeight(WrappedText, out requiredWidth, out requiredHeight, null);
            }
        }

        mPreRenderWidth = (int)(requiredWidth + .5f);
        // The styled size pass already applies the multiplier per line (a size run is not multiplied, #6450).
        mPreRenderHeight = usedStyledSizePass
            ? requiredHeight
            : (int)(requiredHeight * LineHeightMultiplier + .5f);
    }

    /// <summary>
    /// Mirrors <see cref="GetRequiredWidthAndHeight"/> but grows each line by the largest inline
    /// <see cref="FontScale"/> run covering it (issue #3481).
    /// </summary>
    /// <remarks>
    /// <see cref="mPreRenderWidth"/>/<see cref="mPreRenderHeight"/> are stored in BASE units (the
    /// caller multiplies by <see cref="FontScale"/> downstream), while an inline [FontScale=N] is an
    /// ABSOLUTE per-run scale (matching how <see cref="DrawStyledLine"/> draws it). So each run's
    /// contribution is divided by <see cref="FontScale"/> to land back in base units. Each line's height
    /// is <see cref="StyledLineHeight"/>: the larger of the multiplied base and the tallest size run
    /// ([FontScale] or [FontSize]), so the multiplier applies to the base but not to a run, and a line
    /// with no size run (plain, or only Color runs) is the multiplied base. This matches the drawn
    /// advance (<see cref="GetLineAdvance"/>); the caller does not multiply the result again.
    /// Line-wrapping is intentionally still base-scale-only (deferred per #3471); this only affects
    /// size reporting.
    /// </remarks>
    private void GetInlineVariableAwareWidthAndHeight(out int requiredWidth, out int requiredHeight)
    {
        float maxWidth = 0;
        float totalHeight = 0;

        int startOfLineIndex = 0;
        for (int lineIndex = 0; lineIndex < WrappedText.Count; lineIndex++)
        {
            var line = WrappedText[lineIndex];
            var substrings = GetCachedStyledSubstrings(lineIndex, startOfLineIndex, line);

            float lineHeightFactor;
            float lineWidthInBaseUnits;

            if (substrings.Count == 0)
            {
                lineHeightFactor = 0;
                lineWidthInBaseUnits = MeasureString(line);
            }
            else
            {
                lineWidthInBaseUnits = MeasureStyledLineInBaseUnits(substrings, out lineHeightFactor);
            }

            maxWidth = System.Math.Max(maxWidth, lineWidthInBaseUnits);
            totalHeight += StyledLineHeight(1, lineHeightFactor);

            startOfLineIndex += line.Length;
        }

        const int MaxWidthAndHeight = 4096;
        // Width must round UP (not to-nearest): this value becomes the Text's own self-imposed wrap
        // width (RelativeToChildren sets Text.Width to it), which is then compared against the same
        // line's raw, unrounded measurement in IWrappedTextExtensions.UpdateLines. Rounding to-nearest
        // can round DOWN when the fractional part is < 0.5 (e.g. maxWidth=152.4 -> 152), making that
        // self-imposed width narrower than the content that produced it, so the very run that determined
        // the width no longer "fits" and gets force-wrapped (#3645). Only affects fractional widths, which
        // only occur via inline-run scaling (e.g. [FontScale]) - the non-inline-variable path measures
        // whole bitmap-font glyph advances, which are already integers.
        requiredWidth = System.Math.Min((int)System.Math.Ceiling(maxWidth), MaxWidthAndHeight);
        requiredHeight = System.Math.Min((int)(totalHeight + .5f), MaxWidthAndHeight);
    }

    /// <summary>
    /// Sums the base-unit width of a single line already split into styled runs, measuring each run with
    /// the same per-run font/size <see cref="DrawStyledLine"/> draws it with (a [FontSize] crisp-swap or
    /// scaled-atlas run, or a [FontScale] multiplier run), and reports the tallest run's height factor
    /// relative to the base scale (0 when the line has no size run).
    /// </summary>
    /// <remarks>
    /// Shared by the size pass (<see cref="GetInlineVariableAwareWidthAndHeight"/>, #3481/#3524) and the
    /// font-aware wrap seam (<see cref="MeasureString(string, int)"/>, #3532) so the measured, drawn, and
    /// wrapped size of a run cannot drift. Widths are returned in BASE units (<see cref="FontScale"/>
    /// factored out), matching <see cref="MeasureString(string)"/>; the multiplied base is the floor
    /// (<see cref="StyledLineHeight"/>), so an inline run smaller than it does not shrink the line. Guards <see cref="FontScale"/> == 0
    /// because the wrap seam calls this regardless of scale (the size pass is already gated on scale &gt; 0).
    /// </remarks>
    private float MeasureStyledLineInBaseUnits(List<StyledSubstring> substrings, out float lineHeightFactor)
    {
        float baseScale = FontScale;
        float maxRunScale = 0;
        float lineWidthAtScale = 0;
        for (int substringIndex = 0; substringIndex < substrings.Count; substringIndex++)
        {
            var substring = substrings[substringIndex];
            // Same resolver the draw loop uses, but against EffectiveMeasurementFont with no
            // compensation (issue #4365) -- measuring against the LIVE oversampled Font here would
            // measure a DIFFERENT, non-linearly hinted raster than pre-oversampling, drifting a wrap
            // decision or pre-render size by a few pixels depending on live zoom state. Draw/measure
            // still can't drift on SIZE (#3524): compensation composes DrawSize back down to the exact
            // same on-screen size EffectiveMeasurementFont already represents in native units.
            var resolvedFont = ResolveRunFont(substring.Variables, EffectiveMeasurementFont, oversampleCompensationScale: 1f);
            lineWidthAtScale += MeasureTextEx(resolvedFont.Font, substring.Substring, resolvedFont.DrawSize, 0).X;
            if (IsSizeRun(substring.Variables))
            {
                maxRunScale = System.Math.Max(maxRunScale, resolvedFont.LayoutScale);
            }
        }
        lineHeightFactor = baseScale > 0 ? maxRunScale / baseScale : 0;
        return baseScale > 0 ? lineWidthAtScale / baseScale : 0;
    }

    /// <summary>
    /// Font-aware measurement used by line wrapping (issue #3532): measures <paramref name="whatToMeasure"/>
    /// (which begins at <paramref name="absoluteStartIndexInStrippedText"/> in the stripped text) honoring
    /// the inline [FontSize]/[FontScale] runs active over that range, so a line containing an enlarged run
    /// wraps at the run's real size instead of the base size. Falls back to the base
    /// <see cref="MeasureString(string)"/> when no inline runs cover the range, keeping plain-text wrapping
    /// byte-identical.
    /// </summary>
    public float MeasureString(string whatToMeasure, int absoluteStartIndexInStrippedText)
    {
        if (InlineVariables.Count == 0)
        {
            return MeasureString(whatToMeasure);
        }

        var substrings = GetStyledSubstrings(absoluteStartIndexInStrippedText, whatToMeasure);
        if (substrings.Count == 0)
        {
            return MeasureString(whatToMeasure);
        }

        return MeasureStyledLineInBaseUnits(substrings, out _);
    }

    public void GetRequiredWidthAndHeight(IEnumerable<string> lines, out int requiredWidth, out int requiredHeight, List<float>? widths)
    {

        float maxWidth = 0;
        float maxHeight = 0;

        foreach (string line in lines)
        {
            maxHeight += MeasurementLineHeightInPixels;
            float lineWidth = 0;

            lineWidth = (int)Math.Ceiling(this.MeasureString(line));
            if (widths != null)
            {
                widths.Add(lineWidth);
            }
            maxWidth = System.Math.Max(lineWidth, maxWidth);
        }

        const int MaxWidthAndHeight = 4096; // change this later?
        requiredWidth = System.Math.Min((int)(maxWidth +.5f), MaxWidthAndHeight);
        requiredHeight = System.Math.Min((int)(maxHeight + .5f), MaxWidthAndHeight);
        //if (requiredWidth != 0 && mOutlineThickness != 0)
        //{
        //    requiredWidth += mOutlineThickness * 2;
        //}
    }

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }

    #region IVisible Implementation

    /// <inheritdoc/>
    public bool Visible
    {
        get;
        set;
    }

    /// <inheritdoc/>
    public bool AbsoluteVisible => ((IVisible)this).GetAbsoluteVisible();
    /// <inheritdoc/>
    IVisible? IVisible.Parent => ((IRenderableIpso)this).Parent as IVisible;

    #endregion
}

public static class StringExtensions
{
    public static string SubstringEnd(this string value, int lettersToRemove)
    {
        if (value.Length <= lettersToRemove)
        {
            return string.Empty;
        }
        else
        {
            return value.Substring(0, value.Length - lettersToRemove);
        }
    }
}
