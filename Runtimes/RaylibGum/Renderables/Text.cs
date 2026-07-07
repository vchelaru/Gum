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


public class Text : IVisible, IRenderableIpso,
    IWrappedText, IFormsText, ICloneable
{
    public Text Clone()
    {
        var newInstance = (Text)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.mChildren = new();
        // MemberwiseClone shares the list reference; give the clone its own copy so
        // re-parsing markup on one instance doesn't mutate the other's runs.
        newInstance.InlineVariables = new List<InlineVariable>(InlineVariables);
        return newInstance;
    }

    object ICloneable.Clone() => Clone();

    /// <summary>
    /// The line height as defined by the font, ignoring FontScale.
    /// </summary>
    private int _lineHeightInPixels;

    /// <summary>
    /// The descender height (line height minus baseline) as defined by the font, ignoring FontScale.
    /// Recovered from the loaded .fnt's lineHeight/base; defaults to 2 for fonts without recorded
    /// metrics (raylib's built-in default font, TTF loaded via LoadFontEx).
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
    readonly StyledSubstringSplitter _styledSubstringSplitter = new StyledSubstringSplitter();
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

    public string FontFamily
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
            _font = value;

            // cache this to make checking this faster
            UpdateLineHeightInPixels();
        }
    }

    private void UpdateLineHeightInPixels()
    {
        if (IsWindowReady() == false)
        {
            throw new InvalidOperationException("Cannot measure text because IsWindowReady() is false - did you remember to call InitWindow first?");
        }

        // A Gum bitmap font records its .fnt lineHeight/base when loaded (keyed by atlas texture id),
        // because raylib's Font struct can't hold them. Use those so line height includes the
        // descender region exactly as the MonoGame BitmapFont does — otherwise a Text (and any
        // container sized RelativeToChildren around it, e.g. a ListBoxItem) is short by that region.
        // Fonts without recorded metrics (raylib's built-in default, TTF via LoadFontEx) fall back to
        // native measurement, which returns ~BaseSize.
        if (RaylibFontMetricsRegistry.TryGet(_font.Texture.Id, out var metrics))
        {
            _lineHeightInPixels = metrics.LineHeight;
            _descenderHeight = metrics.LineHeight - metrics.BaselineY;
        }
        else
        {
            _lineHeightInPixels = (int)(MeasureTextEx(_font, "M", _font.BaseSize, 0).Y + .5);
            _descenderHeight = 2;
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


    public bool IsTruncatingWithEllipsisOnLastLine { get; set; }

    /// <inheritdoc/>
    public bool IsHeightDependentOnLines { get; set; }

    public string Name
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

    public float DescenderHeight => _descenderHeight;

    public float FontScale { get; set; } = 1;

    public object Tag { get; set; }


    /// <summary>
    /// The Gum blend mode applied when this text is drawn. Null means "use the renderer's current
    /// blend mode" (typically alpha blending). Honored in <see cref="Render"/> via the shared
    /// <see cref="RenderingLibrary.Graphics.BatchDrawCallCounter"/>, mirroring the raylib Sprite and
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

    public int FontSize
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
                return mPreRenderWidth.Value * FontScale;
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
                return mPreRenderHeight.Value * FontScale;
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
    private int _fontSize = 12;

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
        set => Position.X = value;
    }

    public float Y
    {
        get => Position.Y;
        set => Position.Y = value;
    }

    public bool FlipHorizontal { get; set; }

    public float Rotation { get; set; }

    public float? Width
    {
        get
        {
            return mWidth;
        }
        set
        {
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
                return mPreRenderWidth.Value * FontScale;
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
                return mPreRenderHeight.Value * FontScale;
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
    /// The inline styling variables (Color / FontScale / FontSize runs) parsed from BBCode markup assigned
    /// to this Text. Populated by the property pipeline when the assigned text contains markup tags; empty
    /// for plain text. Consumed by <see cref="Render"/> to draw per-run styling. Custom per-letter callbacks
    /// and [Font=Name] family swaps are not yet applied on the Raylib runtime (#3471).
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
    /// Re-runs the shared wrap loop (<see cref="IWrappedTextExtensions.UpdateLines"/>) to rebuild
    /// <see cref="WrappedText"/>. Public so the BBCode dispatch (CustomSetPropertyOnRenderable) can
    /// re-wrap once <see cref="InlineVariables"/> populate — the first wrap runs when RawText is set,
    /// before the runs exist, so it is blind to any enlarged [FontSize]/[FontScale] run (#3532).
    /// </summary>
    public void UpdateWrappedText()
    {
        mWrappedText.Clear();
        this.UpdateLines(mWrappedText);
    }

    /// <summary>
    /// Returns the size of the string, ignoring font scale, but considering the bitmap font.
    /// </summary>
    /// <param name="whatToMeasure"></param>
    /// <returns></returns>
    public float MeasureString(string whatToMeasure)
    {
        return MeasureTextEx(Font, whatToMeasure, _font.BaseSize, 0).X;
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
        return MeasureTextEx(Font, whatToMeasure, _font.BaseSize, 0).X;
    }

    public virtual void PreRender() { }

    /// <summary>
    /// Splits a single wrapped line into styled runs according to which <see cref="InlineVariables"/>
    /// are active over it. Backed by the shared <see cref="StyledSubstringSplitter"/>.
    /// </summary>
    public List<StyledSubstring> GetStyledSubstrings(int startOfLineIndex, string lineOfText) =>
        _styledSubstringSplitter.GetStyledSubstrings(startOfLineIndex, lineOfText, InlineVariables);

    /// <summary>
    /// Rounds a position/origin to whole pixels using the configured <see cref="TextPositionRoundingMode"/>
    /// when the <see cref="EffectiveTextRenderingPositionMode"/> is SnapToPixel; otherwise returns the value unchanged.
    /// Rounding origin along with position avoids the baseline misalignment / "sizzle" seen on small pixel
    /// fonts when vertical alignment produces fractional values.
    /// </summary>
    private Vector2 SnapToPixelIfNeeded(Vector2 value)
    {
        if (EffectiveTextRenderingPositionMode != TextRenderingPositionMode.SnapToPixel)
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
            origin.Y = FontScale * mPreRenderHeight / 2 ?? 0;
        }
        if (VerticalAlignment == VerticalAlignment.Bottom)
        {
            position.Y += this.Height;
            origin.Y = FontScale * mPreRenderHeight ?? 0;
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

        // Per-line vertical offsets. Plain text (no inline runs) keeps the allocation-free uniform
        // advance; when inline [FontSize]/[FontScale] runs can enlarge a line, each line advances by its
        // own height so a line after an enlarged run is not drawn on top of it (#3532).
        IReadOnlyList<float>? lineTopOffsets = InlineVariables.Count > 0 ? GetLineTopOffsets() : null;

        for(int i = 0; i < WrappedText.Count; i++)
        {
            var line = WrappedText[i];

            if (maxLettersToShow.HasValue)
            {
                var lettersRemaining = maxLettersToShow.Value - lettersShown;
                if (lettersRemaining <= 0)
                {
                    break;
                }
                if (lettersRemaining < line.Length)
                {
                    line = line.Substring(0, lettersRemaining);
                }
                lettersShown += line.Length;
            }

            var linePosition = position;
            linePosition.Y += lineTopOffsets != null
                ? lineTopOffsets[i]
                : i * LineHeightInPixels * LineHeightMultiplier;

            var substrings = InlineVariables.Count > 0
                ? GetStyledSubstrings(startOfLineIndex, line)
                : null;

            if (substrings == null || substrings.Count == 0)
            {
                DrawPlainLine(fontValue, line, linePosition, absoluteLeft, origin.Y);
            }
            else
            {
                DrawStyledLine(fontValue, substrings, linePosition, absoluteLeft, origin.Y);
            }

            startOfLineIndex += WrappedText[i].Length;
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
            float lineScale = GetLineLayoutScale(startOfLineIndex, line);
            float lineHeightFactor = FontScale > 0 ? lineScale / FontScale : 1;
            runningY += LineHeightInPixels * lineHeightFactor * LineHeightMultiplier;
            startOfLineIndex += line.Length;
        }
        return offsets;
    }

    /// <summary>
    /// The layout scale of the wrapped line beginning at <paramref name="startOfLineIndex"/> in the
    /// stripped text: <see cref="FontScale"/> for a plain line, or the tallest inline
    /// [FontSize]/[FontScale] run's scale for a styled line (the same per-run resolution
    /// <see cref="DrawStyledLine"/> lays out with, so drawn and vertically-advanced sizes cannot drift).
    /// </summary>
    private float GetLineLayoutScale(int startOfLineIndex, string line)
    {
        if (InlineVariables.Count == 0)
        {
            return FontScale;
        }

        var substrings = GetStyledSubstrings(startOfLineIndex, line);
        float maxScale = FontScale;
        for (int substringIndex = 0; substringIndex < substrings.Count; substringIndex++)
        {
            maxScale = System.Math.Max(maxScale, ResolveRunFont(substrings[substringIndex].Variables).LayoutScale);
        }
        return maxScale;
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
            origin.X = MeasureTextEx(fontValue, line, fontValue.BaseSize * FontScale, 0).X / 2;
        }
        else if (HorizontalAlignment == HorizontalAlignment.Right)
        {
            linePosition.X += this.Width ?? 32;
            origin.X = MeasureTextEx(fontValue, line, fontValue.BaseSize * FontScale, 0).X;
        }

        linePosition = SnapToPixelIfNeeded(linePosition);
        origin = SnapToPixelIfNeeded(origin);

        // Texture filtering is applied once when the font's atlas texture is loaded/built (see
        // ContentLoader.DefaultTextureFilter, #3496), not per-draw — a per-line GPU state reset here
        // was both redundant and ignored the project's texture filter setting.
        DrawTextPro(fontValue, line, linePosition, origin, 0, fontValue.BaseSize * FontScale, 0, Color);
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
    /// Resolves the font/size a styled run draws and is measured with. A [FontScale=v] run keeps the base
    /// font at v times the base size. A [FontSize=N] run either swaps in a font re-rasterized at N (crisp;
    /// its inline value is a <see cref="Font"/>) drawn at its native size, or - when no font creator was
    /// wired so no swap font could be built - falls back to scaling the base atlas to N px (its inline value
    /// is a float). Shared by <see cref="DrawStyledLine"/> (draw) and
    /// <see cref="GetInlineVariableAwareWidthAndHeight"/> (measure) so the drawn and measured size of a run
    /// cannot diverge - that drift is what reproduced the RelativeToChildren spill (#3524).
    /// </summary>
    private ResolvedRunFont ResolveRunFont(List<InlineVariable> variables)
    {
        Font drawFont = _font;
        bool hasSwapFont = false;
        float scale = FontScale;
        float? absolutePixelSize = null;

        foreach (var variable in variables)
        {
            switch (variable.VariableName)
            {
                case nameof(FontScale):
                    scale = (float)variable.Value;
                    break;
                case nameof(FontSize):
                    if (variable.Value is Font swapFont && swapFont.BaseSize > 0)
                    {
                        drawFont = swapFont;
                        hasSwapFont = true;
                    }
                    else if (variable.Value is float pixelSize)
                    {
                        absolutePixelSize = pixelSize;
                    }
                    break;
            }
        }

        float baseSize = _font.BaseSize;
        float drawSize;
        if (hasSwapFont)
        {
            // A crisp font rasterized at the requested size: draw it at its native size times the base
            // render scale, exactly as MonoGame renders a swapped BitmapFont.
            drawSize = drawFont.BaseSize * FontScale;
        }
        else if (absolutePixelSize.HasValue)
        {
            // No font creator wired: approximate the swap by scaling the base atlas to the absolute size.
            drawSize = absolutePixelSize.Value * FontScale;
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
    /// size (FontScale multiplier or FontSize absolute-px swap). Runs are laid out left-to-right and
    /// baseline-aligned so a larger run sits on the same baseline as its neighbors. Custom per-letter
    /// callbacks and [Font=Name] family swaps are not applied here on the Raylib runtime yet (#3471).
    /// </summary>
    private void DrawStyledLine(Font fontValue, List<StyledSubstring> substrings, Vector2 linePosition, float absoluteLeft, float originY)
    {
        // First pass: resolve each run's effective color, font/size and measured width, plus the line
        // totals needed for horizontal alignment (total width) and baseline alignment (tallest run's
        // baseline). The font, draw size and layout scale come from the shared resolver, so draw and
        // measure use the same per-run font and cannot diverge (issue #3524).
        var runColors = new Color[substrings.Count];
        var runFonts = new Font[substrings.Count];
        var runDrawSizes = new float[substrings.Count];
        var runScales = new float[substrings.Count];
        var runWidths = new float[substrings.Count];
        float totalWidth = 0;
        float maxScale = FontScale;

        for (int s = 0; s < substrings.Count; s++)
        {
            var run = substrings[s];
            var runColor = Color;
            var resolvedFont = ResolveRunFont(run.Variables);

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
                }
            }

            runColors[s] = runColor;
            runFonts[s] = resolvedFont.Font;
            runDrawSizes[s] = resolvedFont.DrawSize;
            runScales[s] = resolvedFont.LayoutScale;
            runWidths[s] = MeasureTextEx(resolvedFont.Font, run.Substring, resolvedFont.DrawSize, 0).X;
            totalWidth += runWidths[s];
            maxScale = System.Math.Max(maxScale, resolvedFont.LayoutScale);
        }

        float maxBaseline = (LineHeightInPixels - _descenderHeight) * maxScale;

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
        for (int s = 0; s < substrings.Count; s++)
        {
            var runScale = runScales[s];
            // Push shorter runs down so every run shares the tallest run's baseline.
            float baselineDelta = maxBaseline - (LineHeightInPixels - _descenderHeight) * runScale;

            var runPosition = new Vector2(lineStartX + advance, linePosition.Y + baselineDelta);
            var runOrigin = new Vector2(0, originY);

            runPosition = SnapToPixelIfNeeded(runPosition);
            runOrigin = SnapToPixelIfNeeded(runOrigin);

            DrawTextPro(runFonts[s], substrings[s].Substring, runPosition, runOrigin, 0, runDrawSizes[s], 0, runColors[s]);

            advance += runWidths[s];
        }
    }

    public void SetNeedsRefreshToTrue()
    {
    }

    public void UpdatePreRenderDimensions()
    {
        int requiredWidth = 0;
        int requiredHeight = 0;

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
            }
            else
            {
                GetRequiredWidthAndHeight(WrappedText, out requiredWidth, out requiredHeight, null);
            }
        }

        mPreRenderWidth = (int)(requiredWidth + .5f);
        mPreRenderHeight = (int)(requiredHeight * LineHeightMultiplier + .5f);
    }

    /// <summary>
    /// Mirrors <see cref="GetRequiredWidthAndHeight"/> but grows each line by the largest inline
    /// <see cref="FontScale"/> run covering it (issue #3481).
    /// </summary>
    /// <remarks>
    /// <see cref="mPreRenderWidth"/>/<see cref="mPreRenderHeight"/> are stored in BASE units (the
    /// caller multiplies by <see cref="FontScale"/> downstream), while an inline [FontScale=N] is an
    /// ABSOLUTE per-run scale (matching how <see cref="DrawStyledLine"/> draws it). So each run's
    /// contribution is divided by <see cref="FontScale"/> to land back in base units. A line with no
    /// runs, or only non-scale runs (e.g. Color), keeps a factor of 1. FontScale (the base Text
    /// scale) is the floor, so an inline run smaller than the base does not shrink the line.
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
            var substrings = GetStyledSubstrings(startOfLineIndex, line);

            float lineHeightFactor;
            float lineWidthInBaseUnits;

            if (substrings.Count == 0)
            {
                lineHeightFactor = 1;
                lineWidthInBaseUnits = MeasureString(line);
            }
            else
            {
                lineWidthInBaseUnits = MeasureStyledLineInBaseUnits(substrings, out lineHeightFactor);
            }

            maxWidth = System.Math.Max(maxWidth, lineWidthInBaseUnits);
            totalHeight += LineHeightInPixels * lineHeightFactor;

            startOfLineIndex += line.Length;
        }

        const int MaxWidthAndHeight = 4096;
        requiredWidth = System.Math.Min((int)(maxWidth + .5f), MaxWidthAndHeight);
        requiredHeight = System.Math.Min((int)(totalHeight + .5f), MaxWidthAndHeight);
    }

    /// <summary>
    /// Sums the base-unit width of a single line already split into styled runs, measuring each run with
    /// the same per-run font/size <see cref="DrawStyledLine"/> draws it with (a [FontSize] crisp-swap or
    /// scaled-atlas run, or a [FontScale] multiplier run), and reports the tallest run's height factor
    /// relative to the base line height.
    /// </summary>
    /// <remarks>
    /// Shared by the size pass (<see cref="GetInlineVariableAwareWidthAndHeight"/>, #3481/#3524) and the
    /// font-aware wrap seam (<see cref="MeasureString(string, int)"/>, #3532) so the measured, drawn, and
    /// wrapped size of a run cannot drift. Widths are returned in BASE units (<see cref="FontScale"/>
    /// factored out), matching <see cref="MeasureString(string)"/>; <see cref="FontScale"/> is the floor,
    /// so an inline run smaller than the base does not shrink the line. Guards <see cref="FontScale"/> == 0
    /// because the wrap seam calls this regardless of scale (the size pass is already gated on scale &gt; 0).
    /// </remarks>
    private float MeasureStyledLineInBaseUnits(List<StyledSubstring> substrings, out float lineHeightFactor)
    {
        float baseScale = FontScale;
        float maxRunScale = baseScale;
        float lineWidthAtScale = 0;
        for (int substringIndex = 0; substringIndex < substrings.Count; substringIndex++)
        {
            var substring = substrings[substringIndex];
            // Same resolver the draw loop uses, measured with the same per-run font at the same
            // size, so a run is measured at exactly the size it is drawn - draw/measure drift is
            // what reproduced the RelativeToChildren spill (#3524).
            var resolvedFont = ResolveRunFont(substring.Variables);
            lineWidthAtScale += MeasureTextEx(resolvedFont.Font, substring.Substring, resolvedFont.DrawSize, 0).X;
            maxRunScale = System.Math.Max(maxRunScale, resolvedFont.LayoutScale);
        }
        lineHeightFactor = baseScale > 0 ? maxRunScale / baseScale : 1;
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
            maxHeight += LineHeightInPixels;
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
