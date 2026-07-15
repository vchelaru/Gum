using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.GueDeriving;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Topten.RichTextKit;
using Vector2 = System.Numerics.Vector2;
using Matrix = System.Numerics.Matrix4x4;
using Gum;
using Gum.DataTypes;
using Gum.Wireframe;

namespace SkiaGum;

#region LetterCustomization

/// <summary>
/// Mirrors <see cref="RenderingLibrary.Graphics.LetterCustomization"/> on the MonoGame-family runtime -
/// the per-letter styling a <c>[Custom]</c> callback can apply. SkiaGum cannot reference that type
/// directly (it lives in the MonoGame-coupled <c>RenderingLibrary.Graphics.Text</c> source file), so this
/// is a local copy, matching the approach <see cref="Gum.Renderables.LetterCustomization"/> takes on
/// Raylib. Only <see cref="XOffset"/>, <see cref="YOffset"/>, <see cref="Color"/> and
/// <see cref="ReplacementCharacter"/> are honored on SkiaGum (issue #3692): they map directly onto a
/// RichTextKit <c>Style</c> override (Color) plus a post-layout glyph nudge (<c>FontRun.MoveGlyphs</c>)
/// for XOffset/YOffset. <see cref="ScaleX"/>/<see cref="ScaleY"/>/<see cref="RotationDegrees"/> would need
/// a per-glyph transform RichTextKit does not expose and are not yet applied, matching the deferred scope
/// documented on <see cref="Text.SupportedTags"/>.
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
/// Mirrors <see cref="RenderingLibrary.Graphics.ParameterizedLetterCustomizationCall"/> on the
/// MonoGame-family runtime. Resolves <see cref="FunctionName"/> against <see cref="Text.Customizations"/>
/// / <see cref="Text.ContextCustomizations"/> lazily (rather than capturing the delegate at parse time)
/// so registering the callback after the markup is assigned still takes effect at the next layout.
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

public class Text : IRenderableIpso, IVisible, IFormsText, ICloneable
{
    /// <summary>
    /// Registry of simple per-letter callbacks for the <c>[Custom=Name]</c> BBCode tag, keyed by name.
    /// Mirrors <see cref="RenderingLibrary.Graphics.Text.Customizations"/> on the MonoGame-family
    /// runtime, and <see cref="Gum.Renderables.Text.Customizations"/> on Raylib -- each backend keeps its
    /// own registry (issue #3692), so a callback must be registered per platform the app targets.
    /// </summary>
    public static Dictionary<string, Func<int, string, LetterCustomization>> Customizations { get; private set; }
        = new();

    /// <summary>
    /// Registry of context-aware per-letter callbacks for the <c>[Custom=Name]</c> BBCode tag - the
    /// callback receives the <see cref="LetterCustomization"/> produced by any enclosing <c>[Custom]</c>
    /// tag so nested tags can chain (e.g. an outer tag sets Color, an inner tag darkens it). Checked
    /// before <see cref="Customizations"/> when both are registered under the same name. Mirrors
    /// <see cref="RenderingLibrary.Graphics.Text.ContextCustomizations"/> on the MonoGame-family runtime.
    /// </summary>
    public static Dictionary<string, Func<int, string, LetterCustomization, LetterCustomization>> ContextCustomizations { get; private set; }
        = new();

    #region Fields/Properties

    [Obsolete("Use GlobalTextScale instead")]
    public static decimal ScreenDensity
    {
        get => GlobalTextScale;
        set => GlobalTextScale = value;
    }

    /// <summary>
    /// Global font scale, used to increase font size according to user settings, such as 
    /// font scale at the OS level.
    /// </summary>
    public static decimal GlobalTextScale
    {
        get => (decimal)GraphicalUiElement.GlobalFontScale;
        set => GraphicalUiElement.GlobalFontScale = (float)value;
    }

    public bool IsRenderTarget => false;

    //public SKTypeface Font { get; set; }
    public string FontName
    {
        get => _fontName; 
        set
        {
            _fontName = value;
            _cachedTextBlock = null;

        }
    }
    public int FontSize
    {
        get => _fontSize; 
        set
        {
            _fontSize = value;
            _cachedTextBlock = null;
        }
    }
    public float FontScale
    {
        get => _fontScale; 
        set
        {
            _fontScale = value;
            _cachedTextBlock = null;
        }
    }

    public float BoldWeight
    {
        get => _boldWeight; 
        set
        {
            _boldWeight = value;
            _cachedTextBlock = null;
        }
    }
    // I don't know if this should be a skia, XamForms, or XNA color...
    public SKColor Color
    {
        get => _color;
        set
        {
            _color = value;
            _cachedTextBlock = null;
        }
    }

    public int Blue
    {
        get => Color.Blue;
        set
        {
            this.Color = new SKColor(this.Color.Red, this.Color.Green, (byte)value, this.Color.Alpha);
        }
    }

    public int Green
    {
        get => Color.Green;
        set
        {
            this.Color = new SKColor(this.Color.Red, (byte)value, this.Color.Blue, this.Color.Alpha);
        }
    }

    public int Red
    {
        get => Color.Red;
        set
        {
            this.Color = new SKColor((byte)value, this.Color.Green, this.Color.Blue, this.Color.Alpha);
        }
    }

    public int Alpha
    {
        get => Color.Alpha;
        set
        {
            this.Color = new SKColor(this.Color.Red, this.Color.Green, this.Color.Blue, (byte)value);

        }
    }

    [Obsolete("Use MaxNumberOfLines instead")]
    public int? MaximumNumberOfLines => MaxNumberOfLines;

    public int? MaxNumberOfLines
    {
        get => _maximumNumberOfLines; set
        {
            _maximumNumberOfLines = value;
            _cachedTextBlock = null;
        }
    }

    /// <summary>
    /// When <c>true</c>, an overflowing last line ends with an ellipsis ("...") once the text is
    /// truncated (by <see cref="MaxNumberOfLines"/> or by <see cref="TextOverflowVerticalMode"/>
    /// clipping to <see cref="Height"/>). Honored through RichTextKit's
    /// <c>TextBlock.EllipsisEnabled</c> in <see cref="GetTextBlock"/> (issue #3677). Set on the
    /// MonoGame/Raylib backends via <c>TextOverflowHorizontalMode.EllipsisLetter</c>.
    /// </summary>
    public bool IsTruncatingWithEllipsisOnLastLine
    {
        get => _isTruncatingWithEllipsisOnLastLine;
        set
        {
            _isTruncatingWithEllipsisOnLastLine = value;
            _cachedTextBlock = null;
        }
    }

    /// <inheritdoc/>
    public bool IsHeightDependentOnLines { get; set; }

    /// <summary>
    /// The maximum number of characters to display visually. Characters beyond this count
    /// are hidden but remain in <see cref="RawText"/>. Intended for typewriter-style reveal
    /// effects (e.g. <c>DialogBox</c>).
    /// </summary>
    /// <remarks>
    /// This is a paint-only effect (issue #3678): <see cref="Render"/> paints a throwaway
    /// <c>TextBlock</c> built from <see cref="GetVisibleWrappedText"/> (the wrapped lines truncated
    /// to this budget), while the cached block that drives <see cref="WrappedText"/>, measurement and
    /// caret math stays built from the full <see cref="RawText"/>. It therefore intentionally does
    /// NOT invalidate <c>_cachedTextBlock</c> -- mirroring how the other render-time effects
    /// (drop shadow, blend) keep out of the layout cache. Matches the per-line reveal the
    /// MonoGame/Raylib/Sokol backends perform.
    /// </remarks>
    public int? MaxLettersToShow { get; set; }

    /// <summary>
    /// The wrapped lines actually revealed given <see cref="MaxLettersToShow"/>: <see cref="WrappedText"/>
    /// with each line truncated to the remaining letter budget so a typewriter reveal pauses mid-line
    /// rather than popping line-by-line (matching the MonoGame/Raylib/Sokol backends). Returns
    /// <see cref="WrappedText"/> unchanged when <see cref="MaxLettersToShow"/> is null, and an empty
    /// list when it is zero. Used by <see cref="Render"/> to build the painted block.
    /// </summary>
    public List<string> GetVisibleWrappedText()
    {
        List<string> fullWrapped = WrappedText;
        if (MaxLettersToShow == null)
        {
            return fullWrapped;
        }

        var revealed = new List<string>();
        int remaining = MaxLettersToShow.Value;
        foreach (var line in fullWrapped)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (line.Length <= remaining)
            {
                revealed.Add(line);
                remaining -= line.Length;
            }
            else
            {
                revealed.Add(line.Substring(0, remaining));
                remaining = 0;
            }
        }

        return revealed;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// SkiaGum's own wrapping is performed by Topten.RichTextKit's <see cref="TextBlock"/>
    /// (see <see cref="GetTextBlock"/>), not by the shared <see cref="IWrappedTextExtensions.UpdateLines"/>
    /// word-wrap algorithm the MonoGame/Raylib backends use, so this value has no effect on
    /// SkiaGum's own layout. RichTextKit performs Unicode (UAX#14) line breaking, which does
    /// break within an overlong word when no earlier break opportunity fits the wrap width, so
    /// <c>true</c> matches its actual behavior (as well as MonoGame/Raylib's default).
    /// </remarks>
    bool IWrappedText.IsMidWordLineBreakEnabled => true;

    /// <summary>
    /// The current wrapped lines after layout, reconstructed from the cached
    /// <see cref="TextBlock"/>'s line ranges so each entry (including any trailing
    /// spaces RichTextKit's layout preserves) lines up with <see cref="RawText"/>
    /// the same way <c>WrappedText[i].Length</c> summed across lines does for the
    /// MonoGame/Raylib backends -- <c>TextBoxBase</c> relies on that for caret-index math.
    /// </summary>
    public List<string> WrappedText
    {
        get
        {
            var textBlock = GetCachedTextBlock();
            if (!ReferenceEquals(textBlock, _wrappedTextSourceBlock))
            {
                _wrappedTextSourceBlock = textBlock;
                _cachedWrappedTextList = BuildWrappedTextList(textBlock);
            }
            return _cachedWrappedTextList;
        }
    }

    private List<string> BuildWrappedTextList(TextBlock textBlock)
    {
        var lines = new List<string>();
        // The TextBlock is built from the tag-stripped, CRLF-normalized layout text when the RawText
        // carries BBCode markup (issue #3679); for plain text _layoutText is mRawText verbatim. Either
        // way RichTextKit's TextLine.Start/Length are code-point indices into that exact string, so
        // index into it rather than mRawText (which still holds the markup).
        var source = _layoutText;
        if (string.IsNullOrEmpty(source))
        {
            return lines;
        }

        foreach (var line in textBlock.Lines)
        {
            if (line.Start + line.Length <= source.Length)
            {
                lines.Add(source.Substring(line.Start, line.Length));
            }
        }

        return lines;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Measured at the base font size (<see cref="FontSize"/> scaled only by
    /// <see cref="GlobalTextScale"/>), with <see cref="FontScale"/> factored out -- matching
    /// callers such as <c>TextBoxBase.GetIndex</c> which multiply the result by
    /// <see cref="FontScale"/> themselves.
    /// </remarks>
    public int LineHeightInPixels
    {
        get
        {
            var textBlock = new TextBlock();
            textBlock.AddText("My", GetRawMeasurementStyle());
            return textBlock.Lines.Count > 0
                ? (int)MathF.Ceiling(textBlock.Lines[0].Height)
                : FontSize;
        }
    }

    public bool IsItalic
    {
        get => _isItalic; set
        {
            _isItalic = value;
            _cachedTextBlock = null;
        }
    }

    /// <summary>
    /// The thickness, in pixels, of the outline drawn around the text. Zero (the default) draws no
    /// outline. Rendered through RichTextKit's halo (<see cref="Style.HaloWidth"/>). Mirrors the
    /// OutlineThickness font property on the MonoGame/Raylib backends, which instead bake the outline
    /// into the generated bitmap font since they have no runtime font-drawing outline mechanism.
    /// </summary>
    public int OutlineThickness
    {
        get => _outlineThickness;
        set
        {
            _outlineThickness = value;
            _cachedTextBlock = null;
        }
    }

    /// <summary>
    /// The color of the outline drawn when <see cref="OutlineThickness"/> is greater than zero.
    /// Defaults to black. This is SkiaGum-specific: the MonoGame/Raylib backends bake the outline
    /// into the font atlas and expose no runtime outline-color property, so there is no shared
    /// cross-backend concept to mirror.
    /// </summary>
    public SKColor OutlineColor
    {
        get => _outlineColor;
        set
        {
            _outlineColor = value;
            _cachedTextBlock = null;
        }
    }

    #region Dropshadow

    // Standalone (non-BBCode) drop shadow for Skia text (issue #3674). Mirrors the drop-shadow
    // vocabulary RenderableShapeBase exposes for Skia shapes so text and shapes share one API.
    // The shadow is a canvas/ImageFilter effect applied in Render (see GetRenderPaint) rather
    // than a RichTextKit Style property, so the setters intentionally do NOT invalidate
    // _cachedTextBlock -- the cached TextBlock carries no shadow state and Render reads these
    // values live each frame.

    private bool _hasDropshadow;

    /// <summary>
    /// When <c>true</c>, a drop shadow is rendered behind the text using
    /// <see cref="SKImageFilter.CreateDropShadow"/>. Mirrors the Skia shape drop-shadow property set.
    /// </summary>
    public bool HasDropshadow
    {
        get => _hasDropshadow;
        set => _hasDropshadow = value;
    }

    private float _dropshadowOffsetX;

    /// <summary>Horizontal offset, in pixels, of the drop shadow from the text.</summary>
    public float DropshadowOffsetX
    {
        get => _dropshadowOffsetX;
        set => _dropshadowOffsetX = value;
    }

    private float _dropshadowOffsetY;

    /// <summary>Vertical offset, in pixels, of the drop shadow from the text.</summary>
    public float DropshadowOffsetY
    {
        get => _dropshadowOffsetY;
        set => _dropshadowOffsetY = value;
    }

    private float _dropshadowBlurX;

    /// <summary>
    /// Horizontal blur amount of the drop shadow. Divided by 3 before being passed to Skia's
    /// sigma-based blur, matching the convention <see cref="RenderableShapeBase"/> uses for shapes.
    /// </summary>
    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set => _dropshadowBlurX = value;
    }

    private float _dropshadowBlurY;

    /// <inheritdoc cref="DropshadowBlurX"/>
    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set => _dropshadowBlurY = value;
    }

    private SKColor _dropshadowColor;

    /// <summary>The color of the drop shadow.</summary>
    public SKColor DropshadowColor
    {
        get => _dropshadowColor;
        set => _dropshadowColor = value;
    }

    public int DropshadowAlpha
    {
        get => DropshadowColor.Alpha;
        set => DropshadowColor = new SKColor(DropshadowColor.Red, DropshadowColor.Green, DropshadowColor.Blue, (byte)value);
    }

    public int DropshadowBlue
    {
        get => DropshadowColor.Blue;
        set => DropshadowColor = new SKColor(DropshadowColor.Red, DropshadowColor.Green, (byte)value, DropshadowColor.Alpha);
    }

    public int DropshadowGreen
    {
        get => DropshadowColor.Green;
        set => DropshadowColor = new SKColor(DropshadowColor.Red, (byte)value, DropshadowColor.Blue, DropshadowColor.Alpha);
    }

    public int DropshadowRed
    {
        get => DropshadowColor.Red;
        set => DropshadowColor = new SKColor((byte)value, DropshadowColor.Green, DropshadowColor.Blue, DropshadowColor.Alpha);
    }

    #endregion

    /// <summary>
    /// The Gum blend mode applied when compositing this text. When null, the SkiaSharp default
    /// (SrcOver / standard alpha blending) is used. Mirrors <see cref="RenderableShapeBase.Blend"/>
    /// and the MonoGame/Raylib Text's Blend surface. Applied as an <see cref="SKPaint.BlendMode"/>
    /// in <see cref="Render"/> (see <see cref="GetRenderPaint"/>); values without a clean SkiaSharp
    /// equivalent fall through to SrcOver (see <see cref="BlendToSkBlendModeExtensions"/>). Like the
    /// drop-shadow setters, this is a live render-time effect and intentionally does not invalidate
    /// the cached <see cref="TextBlock"/>.
    /// </summary>
    public Gum.RenderingLibrary.Blend? Blend { get; set; }

    Vector2 Position;
    IRenderableIpso? mParent;

    /// <summary>
    /// The original BBCode / markup string assigned to <see cref="RawText"/> when it contained inline
    /// styling tags, or null when the assigned text was plain. Mirrors the MonoGame/Raylib Text's
    /// property so shared code can re-parse markup after a font change. Unlike those backends (which
    /// strip the tags out of <see cref="RawText"/>), SkiaGum keeps the markup in <see cref="RawText"/>
    /// and strips it lazily when building the RichTextKit <c>TextBlock</c> (issue #3679).
    /// </summary>
    public string? StoredMarkupText => _hasMarkup ? mRawText : null;

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

    ObservableCollectionNoReset<IRenderableIpso> mChildren;
    public ObservableCollection<IRenderableIpso> Children
    {
        get { return mChildren; }
    }

    public float X
    {
        get { return Position.X; }
        set { Position.X = value; }
    }

    public float Y
    {
        get { return Position.Y; }
        set { Position.Y = value; }
    }

    public float Z
    {
        get;
        set;
    }

    public float? Width
    {
        get;
        set;
    }

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

    public float EffectiveWidth
    {
        get
        {
            return Width ?? 0;
        }
    }

    public float Height
    {
        get;
        set;
    }

    public float DescenderHeight
    {
        get
        {
            float toReturn = 0;
            var textBlock = GetCachedTextBlock();
            if (textBlock.Lines.Count > 0)
            {
                toReturn = textBlock.Lines[textBlock.Lines.Count - 1].MaxDescent;
            }
            return toReturn;
        }
    }

    public bool Wrap => false;

    public HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set
        {
            _horizontalAlignment = value;
            _cachedTextBlock = null;
        }
    }

    // todo - currently this does nothing except satisfy the Gum text object interface
    public VerticalAlignment VerticalAlignment
    {
        get; set;
    }

    public string Name
    {
        get;
        set;
    }

    public float Rotation { get; set; }

    string mRawText;
    public string RawText
    {
        get
        {
            return mRawText;
        }
        set
        {
            if (mRawText != value)
            {
                mRawText = value;
                ParseMarkup();
                _cachedTextBlock = null;

                //UpdateWrappedText();

                //UpdatePreRenderDimensions();
            }
        }
    }

    public ColorOperation ColorOperation { get; set; } = ColorOperation.Modulate;

    public object Tag { get; set; }

    public bool FlipHorizontal
    {
        get;
        set;
    }

    public bool FlipVertical
    {
        get;
        set;
    }

    public float LineHeightMultiplier
    {
        get => _lineHeightMultiplier;
        set
        {
            _lineHeightMultiplier = value;
            _cachedTextBlock = null;
        }
    }
    public float WrappedTextHeight
    {
        get
        {
            var textBlock = GetCachedTextBlock();
            return textBlock.MeasuredHeight;
        }
    }

    public float WrappedTextWidth
    {
        get
        {
            var textBlock = GetCachedTextBlock();
            return textBlock.MeasuredWidth;
        }
    }

    // do nothing, this doesn't render to a local render target
    public void SetNeedsRefreshToTrue() { }

    // This could cache the prerendered for speed, but we currently don't do that...
    public void UpdatePreRenderDimensions() { }

    /// <summary>
    /// Controls what happens when the text is taller than <see cref="Height"/>.
    /// <see cref="TextOverflowVerticalMode.TruncateLine"/> caps the RichTextKit
    /// <c>TextBlock</c> to <see cref="Height"/> (dropping lines that would spill past the
    /// bottom); <see cref="TextOverflowVerticalMode.SpillOver"/> (the default) renders
    /// unbounded. Honored in <see cref="GetTextBlock"/> via <c>TextBlock.MaxHeight</c> (issue #3677).
    /// </summary>
    public TextOverflowVerticalMode TextOverflowVerticalMode
    {
        get => _textOverflowVerticalMode;
        set
        {
            _textOverflowVerticalMode = value;
            _cachedTextBlock = null;
        }
    }

    #endregion

    public Text()
    {
        FontScale = 1;
        Width = 32;
        Height = 32;

        this.Visible = true;
        // White matches the MonoGame (RenderingLibrary.Graphics.Text) and raylib (Renderables.Text)
        // renderable defaults; SkiaGum previously drifted to black, so a code-only `new TextRuntime()`
        // rendered black text on Skia but white everywhere else.
        Color = SKColors.White;
        OutlineColor = SKColors.Black;
        mChildren = new ();
    }

    /// <summary>
    /// Overload provided for API uniformity with the MonoGame/Raylib Text renderable,
    /// which requires a <see cref="SystemManagers"/> at construction time. SkiaGum does
    /// not need the managers at construction — they are passed to <see cref="Render"/>
    /// instead — so the parameter is ignored. This overload exists so that shared
    /// runtime/wrapper code can target all three backends with the same call shape.
    /// </summary>
    public Text(SystemManagers managers) : this() { }

    public void Render(ISystemManagers managers)
    {
        var canvas = (managers as SystemManagers).Canvas;

        if (AbsoluteVisible)
        {
            var textBlock = GetCachedTextBlock();
            
            //// Gum uses counter clockwise rotation, Skia uses clockwise, so invert:
            SKMatrix rotationMatrix = SKMatrix.CreateRotationDegrees(-Rotation);
            var absoluteX = this.GetAbsoluteX();
            var absoluteY = this.GetAbsoluteY();

            if(this.VerticalAlignment == VerticalAlignment.Center)
            {
                // compare the bound height with the actual height, and adjust the offset
                var textBlockHeight = textBlock.MeasuredHeight;
                var boundsHeight = this.Height;

                absoluteY += (boundsHeight - textBlockHeight)/2.0f;
            }

            if(this.VerticalAlignment == VerticalAlignment.Bottom)
            {
                // compare the bound height with the actual height, and adjust the offset
                var textBlockHeight = textBlock.MeasuredHeight;
                var boundsHeight = this.Height;

                absoluteY += boundsHeight - textBlockHeight;
            }

            SKMatrix translateMatrix = SKMatrix.CreateTranslation(absoluteX, absoluteY);
            // Continue to apply the previou matrix in case there is scaling
            // for device density
            SKMatrix result = rotationMatrix;

            SKMatrix.Concat(
                ref result, translateMatrix, result);
            SKMatrix.Concat(
                ref result, canvas.TotalMatrix, result);

            canvas.Save();

            // set the clip rect *after* save so it gets undone and restored
            var clipRect = this.GetEffectiveClipRect();
            if(clipRect != null)
            {
                canvas.ClipRect(clipRect.Value);
            }

            canvas.SetMatrix(result);

            // MaxLettersToShow (issue #3678): the paint block reveals only the first N letters, but
            // vertical alignment above was computed from the full block so the revealed letters stay in
            // their final positions (typewriter reveal). Null MaxLettersToShow returns the full block.
            var paintBlock = GetPaintTextBlock(textBlock);

            // Drop shadow and Blend are canvas effects (not RichTextKit Style properties): paint the
            // text into an offscreen layer whose paint carries the ImageFilter (shadow) and/or
            // BlendMode (blend) so both compose when the layer is composited back on Restore. Same
            // primitive/blur convention as the Skia shapes.
            var renderPaint = GetRenderPaint();
            if (renderPaint != null)
            {
                canvas.SaveLayer(renderPaint);
                DrawTextWithOutline(canvas, paintBlock);
                canvas.Restore();
                renderPaint.Dispose();
            }
            else
            {
                DrawTextWithOutline(canvas, paintBlock);
            }
            canvas.Restore();
        }
    }

    /// <summary>
    /// Paints the outline (when <see cref="OutlineThickness"/> &gt; 0) followed by the text fill.
    /// The outline is a single recolor+dilate pass: the text is painted into a layer whose paint
    /// recolors every glyph to <see cref="OutlineColor"/> (SrcIn) and dilates it by
    /// <see cref="OutlineThickness"/> pixels, producing a uniform outline of that width on every
    /// side. Unlike RichTextKit's centered halo stroke this can neither spike at acute vertices
    /// (no miter join) nor emboss (no half-hidden 1px edge), and it reuses RichTextKit's exact
    /// layout so the outline always registers with the fill.
    /// </summary>
    private void DrawTextWithOutline(SKCanvas canvas, TextBlock paintBlock)
    {
        if (OutlineThickness > 0)
        {
            using var outlinePaint = new SKPaint
            {
                IsAntialias = true,
                ColorFilter = SKColorFilter.CreateBlendMode(OutlineColor, SKBlendMode.SrcIn),
                ImageFilter = SKImageFilter.CreateDilate(OutlineThickness, OutlineThickness),
            };
            canvas.SaveLayer(outlinePaint);
            paintBlock.Paint(canvas, new SKPoint(0, 0));
            canvas.Restore();
        }

        paintBlock.Paint(canvas, new SKPoint(0, 0));
    }
    public BlendState BlendState => BlendState.AlphaBlend;


    public bool ClipsChildren { get; set; }

    TextBlock? _cachedTextBlock;
    float? _lastEffectiveWidth;
    float? _lastEffectiveMaxHeight;
    decimal _lastScreenDensity;
    private bool _isTruncatingWithEllipsisOnLastLine;
    private TextOverflowVerticalMode _textOverflowVerticalMode;
    private string _fontName = "Arial";
    private int _fontSize = 18;
    private float _fontScale;
    private float _boldWeight = 1;
    private SKColor _color;
    private int _outlineThickness;
    private SKColor _outlineColor;
    private bool _isItalic;
    private float _lineHeightMultiplier = 1;
    private HorizontalAlignment _horizontalAlignment;
    private int? _maximumNumberOfLines;
    private TextBlock? _wrappedTextSourceBlock;
    private List<string> _cachedWrappedTextList = new();

    // BBCode inline-styling state (issue #3679). Parsed from mRawText whenever it changes:
    // _hasMarkup is true when the text contains at least one recognized tag, _layoutText is the
    // tag-stripped (and CRLF-normalized) text the RichTextKit TextBlock is built from, and
    // _inlineVariables are the per-character styling directives fed to the shared splitter. For
    // plain text _hasMarkup is false and _layoutText == mRawText, keeping the render path unchanged.
    private bool _hasMarkup;
    private string? _layoutText;
    private List<InlineVariable> _inlineVariables = new();
    private readonly StyledSubstringSplitter _styledSubstringSplitter = new();

    // The subset of BbCodeParser.KnownTags SkiaGum honors. Most map onto a RichTextKit Style override;
    // "custom" (issue #3692) instead produces one single-character InlineVariable per letter, resolved
    // against Customizations/ContextCustomizations in BuildRunStyle and applied as a Style color override
    // plus a post-layout glyph nudge (see GetTextBlock). Font family ("font") and outline are still
    // intentionally excluded (see the PR's deferred scope); unrecognized tags are left as literal
    // characters, matching the parser.
    private static readonly HashSet<string> SupportedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "color",
        "red",
        "green",
        "blue",
        "fontsize",
        "fontscale",
        "isbold",
        "isitalic",
        "custom",
    };

    public TextBlock GetCachedTextBlock(float? forcedWidth = null)
    {
        var effectiveWidth = forcedWidth ?? this.Width;
        // Height only feeds layout when TruncateLine is active, so keying the cache on the
        // effective max height (null under SpillOver) avoids rebuilding every layout frame just
        // because Height changed while overflow is unbounded (issue #3677).
        var effectiveMaxHeight = GetEffectiveMaxHeight();

        if(effectiveWidth != _lastEffectiveWidth
            || _lastScreenDensity != ScreenDensity
            || effectiveMaxHeight != _lastEffectiveMaxHeight)
        {
            _cachedTextBlock = null;
            _lastEffectiveWidth = effectiveWidth;
            _lastScreenDensity = ScreenDensity;
            _lastEffectiveMaxHeight = effectiveMaxHeight;
        }

        if(_cachedTextBlock == null)
        {
            _cachedTextBlock = GetTextBlock(effectiveWidth);
        }
        return _cachedTextBlock;
    }

    /// <summary>
    /// The max height passed to the RichTextKit <c>TextBlock</c>: <see cref="Height"/> when
    /// <see cref="TextOverflowVerticalMode.TruncateLine"/> is active and Height is positive,
    /// otherwise <c>null</c> (unbounded, the SpillOver default).
    /// </summary>
    private float? GetEffectiveMaxHeight() =>
        TextOverflowVerticalMode == TextOverflowVerticalMode.TruncateLine && Height > 0
            ? Height
            : (float?)null;

    public TextBlock GetTextBlock(float? forcedWidth = null) => GetTextBlock(mRawText, forcedWidth, allowMarkup: true);

    /// <summary>
    /// Builds a RichTextKit <c>TextBlock</c> for the given text (normally <see cref="RawText"/>, but
    /// <see cref="GetPaintTextBlock"/> passes the reveal-truncated text for <see cref="MaxLettersToShow"/>).
    /// </summary>
    /// <param name="textToRender">The text to lay out. Ignored when <paramref name="allowMarkup"/> is
    /// true and the RawText carries BBCode -- the parsed <see cref="_layoutText"/> / styled runs are
    /// used instead.</param>
    /// <param name="allowMarkup">When true, BBCode inline styling (issue #3679) is honored by adding
    /// one styled run per <see cref="StyledSubstring"/>. When false (the <see cref="MaxLettersToShow"/>
    /// reveal path), the text is added as a single unstyled run.</param>
    private TextBlock GetTextBlock(string textToRender, float? forcedWidth, bool allowMarkup)
    {
        List<StyledTextRun> runs = allowMarkup && _hasMarkup
            ? GetStyledRuns()
            : new List<StyledTextRun> { new StyledTextRun(textToRender ?? string.Empty, GetStyle()) };
        return BuildTextBlockFromRuns(runs, forcedWidth);
    }

    /// <summary>
    /// Lays out the given styled runs into a RichTextKit <c>TextBlock</c>, applying this Text's
    /// alignment/wrap/overflow properties. Shared by the full-layout path (<see cref="GetTextBlock(string, float?, bool)"/>)
    /// and the <see cref="MaxLettersToShow"/> reveal path (<see cref="GetPaintTextBlock"/>) so both honor
    /// per-run BBCode styling (issue #3679) and the <c>[Custom]</c> glyph-offset callback (issue #3692).
    /// </summary>
    private TextBlock BuildTextBlockFromRuns(List<StyledTextRun> runs, float? forcedWidth)
    {
        var textBlock = new TextBlock();
        // Code-point start index (in the added-text stream, which lines up with _layoutText since
        // ReplacementCharacter substitution keeps a run's length at 1) -> the glyph offset a [Custom]
        // callback produced for that run (issue #3692). Applied once, after Alignment/MaxLines/MaxHeight
        // are set and the block has laid out, via FontRun.MoveGlyphs -- see the loop after the try below.
        Dictionary<int, (float XOffset, float YOffset)>? glyphOffsetsByStart = null;
        try
        {
            textBlock.MaxWidth = forcedWidth ?? this.Width;
            int codePointIndex = 0;
            foreach (var run in runs)
            {
                textBlock.AddText(run.Text, run.Style);
                if (run.XOffset != 0 || run.YOffset != 0)
                {
                    glyphOffsetsByStart ??= new();
                    glyphOffsetsByStart[codePointIndex] = (run.XOffset, run.YOffset);
                }
                codePointIndex += run.Text.Length;
            }
            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    textBlock.Alignment = TextAlignment.Left;
                    break;
                case HorizontalAlignment.Center:
                    textBlock.Alignment = TextAlignment.Center;
                    break;
                case HorizontalAlignment.Right:
                    textBlock.Alignment = TextAlignment.Right;
                    break;
            }

            textBlock.MaxLines = MaximumNumberOfLines;

            // Vertical overflow (issue #3677): TruncateLine caps the block to this Text's Height so
            // RichTextKit drops lines that would spill past the bottom; SpillOver leaves MaxHeight
            // null and renders unbounded (the historical Skia behavior).
            textBlock.MaxHeight = GetEffectiveMaxHeight();

            // Ellipsis (issue #3677): RichTextKit appends "..." to the final line whenever it
            // truncates the block (via MaxLines/MaxHeight) and EllipsisEnabled is true. Its default
            // is true, so set it explicitly to honor IsTruncatingWithEllipsisOnLastLine and suppress
            // the ellipsis when the caller wants a hard cut instead.
            textBlock.EllipsisEnabled = IsTruncatingWithEllipsisOnLastLine;

            if (glyphOffsetsByStart != null)
            {
                // Accessing FontRuns forces layout. Each [Custom] run is a single character
                // (StyledSubstringSplitter never merges "Custom" variables across letters), so it cannot
                // be split by line wrapping -- FontRun.Start reliably identifies the matching run.
                foreach (var fontRun in textBlock.FontRuns)
                {
                    if (glyphOffsetsByStart.TryGetValue(fontRun.Start, out var offset))
                    {
                        fontRun.MoveGlyphs(offset.XOffset, offset.YOffset);
                    }
                }
            }
        }
        catch(Exception e)
        {

#if FULL_DIAGNOSTICS
            throw new InvalidOperationException($"An internal exception has occurred: {e.ToString()} with the following information:" +
                $"forcedWidth {forcedWidth}\n" +
                $"FontName {FontName}\n" +
                $"FontSize {FontSize * (float)GlobalTextScale * FontScale}\n" +
                $"FontWeight {400*BoldWeight}");

#else
            // I guess do nothing?
#endif
        }


        return textBlock;
    }

    /// <summary>
    /// The <c>TextBlock</c> that <see cref="Render"/> actually paints. When <see cref="MaxLettersToShow"/>
    /// is null (or already covers the whole text) this is the full cached block, leaving the render
    /// fast path unchanged. Otherwise a throwaway block is built from the revealed prefix -- styled runs
    /// (<see cref="GetVisibleStyledRuns"/>) when the text carries BBCode markup (issue #3701), or plain
    /// <see cref="GetVisibleWrappedText"/> text otherwise -- so only the first N letters are drawn; because
    /// each revealed line is a prefix of a full wrapped line and the breaks are re-inserted as hard
    /// newlines, the revealed lines land in the same positions as in the full layout. The cached block
    /// (used for measurement / caret math) is untouched. Exposed internally for unit testing.
    /// </summary>
    internal TextBlock GetPaintTextBlock(TextBlock fullBlock)
    {
        if (MaxLettersToShow == null)
        {
            return fullBlock;
        }

        List<string> fullWrapped = WrappedText;
        int fullLength = 0;
        foreach (var line in fullWrapped)
        {
            fullLength += line.Length;
        }

        if (MaxLettersToShow.Value >= fullLength)
        {
            return fullBlock;
        }

        // Reveal keeps per-run BBCode styling (issue #3701): GetVisibleStyledRuns slices the same
        // styled runs GetTextBlock uses for the full layout down to the revealed prefix, rather than
        // falling back to the tag-stripped plain text GetVisibleWrappedText returns.
        List<StyledTextRun> runs = _hasMarkup
            ? GetVisibleStyledRuns()
            : new List<StyledTextRun> { new StyledTextRun(string.Join("\n", GetVisibleWrappedText()), GetStyle()) };
        return BuildTextBlockFromRuns(runs, this.Width);
    }


    internal Style GetStyle() => BuildRunStyle(string.Empty, null).Style;

    /// <summary>
    /// A single run of text plus the RichTextKit <see cref="Style"/> it is drawn with. Produced by
    /// <see cref="GetStyledRuns"/> from BBCode markup (issue #3679) and fed to the TextBlock one
    /// <c>AddText</c> call per run so a line can carry mixed per-run color / size / weight / italic.
    /// <see cref="XOffset"/>/<see cref="YOffset"/> come from a <c>[Custom]</c> callback (issue #3692) and
    /// are applied as a post-layout glyph nudge in <see cref="GetTextBlock(string, float?, bool)"/>, since
    /// RichTextKit's <see cref="Style"/> has no offset concept.
    /// </summary>
    internal readonly struct StyledTextRun
    {
        public readonly string Text;
        public readonly Style Style;
        public readonly float XOffset;
        public readonly float YOffset;

        public StyledTextRun(string text, Style style, float xOffset = 0, float yOffset = 0)
        {
            Text = text;
            Style = style;
            XOffset = xOffset;
            YOffset = yOffset;
        }
    }

    /// <summary>
    /// Splits the text into styled runs according to the inline BBCode variables parsed from the markup
    /// (issue #3679). Plain text (or text with no recognized tags) yields a single run carrying the base
    /// <see cref="GetStyle"/>, so the non-markup path is unchanged. Exposed internally for unit testing.
    /// </summary>
    internal List<StyledTextRun> GetStyledRuns()
    {
        var runs = new List<StyledTextRun>();

        if (!_hasMarkup || string.IsNullOrEmpty(_layoutText))
        {
            runs.Add(new StyledTextRun(_hasMarkup ? _layoutText : mRawText, GetStyle()));
            return runs;
        }

        var substrings = _styledSubstringSplitter.GetStyledSubstrings(0, _layoutText, _inlineVariables);
        if (substrings.Count == 0)
        {
            runs.Add(new StyledTextRun(_layoutText, GetStyle()));
            return runs;
        }

        foreach (var substring in substrings)
        {
            var (style, text, xOffset, yOffset) = BuildRunStyle(substring.Substring, substring.Variables);
            runs.Add(new StyledTextRun(text, style, xOffset, yOffset));
        }

        return runs;
    }

    /// <summary>
    /// <see cref="GetStyledRuns"/>, sliced to the prefix revealed by <see cref="MaxLettersToShow"/>
    /// (issue #3701) with the same hard line breaks <see cref="GetVisibleWrappedText"/> inserts, so the
    /// typewriter reveal keeps per-run BBCode styling instead of falling back to plain text. Both sources
    /// index into <see cref="_layoutText"/> code-point-for-code-point (a run's text is only ever replaced
    /// character-for-character by a <c>[Custom]</c> callback, never lengthened/shortened), so slicing runs
    /// by the visible lines' lengths lines up with the correct styled content. Exposed internally for unit
    /// testing.
    /// </summary>
    internal List<StyledTextRun> GetVisibleStyledRuns()
    {
        List<string> visibleLines = GetVisibleWrappedText();
        List<StyledTextRun> fullRuns = GetStyledRuns();
        var result = new List<StyledTextRun>();

        int runIndex = 0;
        int runOffset = 0;

        for (int lineIndex = 0; lineIndex < visibleLines.Count; lineIndex++)
        {
            int remaining = visibleLines[lineIndex].Length;
            while (remaining > 0 && runIndex < fullRuns.Count)
            {
                StyledTextRun run = fullRuns[runIndex];
                int available = run.Text.Length - runOffset;
                if (available <= 0)
                {
                    runIndex++;
                    runOffset = 0;
                    continue;
                }

                int take = System.Math.Min(available, remaining);
                string text = run.Text.Substring(runOffset, take);
                result.Add(new StyledTextRun(text, run.Style, run.XOffset, run.YOffset));

                runOffset += take;
                remaining -= take;
                if (runOffset >= run.Text.Length)
                {
                    runIndex++;
                    runOffset = 0;
                }
            }

            if (lineIndex < visibleLines.Count - 1)
            {
                Style breakStyle = runIndex < fullRuns.Count ? fullRuns[runIndex].Style : GetStyle();
                result.Add(new StyledTextRun("\n", breakStyle));
            }
        }

        return result;
    }

    /// <summary>
    /// Builds the RichTextKit <see cref="Style"/> for one run: the base font/color/weight/italic from
    /// this Text's properties, overridden by the run's inline BBCode <paramref name="variables"/>
    /// (Color / Red / Green / Blue, FontSize, FontScale, IsBold, IsItalic). A <c>[Custom]</c> variable
    /// (issue #3692) additionally resolves its callback against <see cref="Customizations"/> /
    /// <see cref="ContextCustomizations"/> and can override the color, substitute the run's text (via
    /// <see cref="LetterCustomization.ReplacementCharacter"/>), and produce a glyph offset the caller
    /// applies after layout. A null or empty <paramref name="variables"/> reproduces <see cref="GetStyle"/>
    /// exactly.
    /// </summary>
    private (Style Style, string Text, float XOffset, float YOffset) BuildRunStyle(string runText, List<InlineVariable>? variables)
    {
        var color = this.Color;
        int fontSizePixels = FontSize;
        float fontScale = FontScale;
        bool italic = this.IsItalic;
        int weight = (int)(400 * BoldWeight);
        float xOffset = 0;
        float yOffset = 0;
        string text = runText;

        if (variables != null)
        {
            foreach (var variable in variables)
            {
                switch (variable.VariableName)
                {
                    case "Color":
                        if (variable.Value is System.Drawing.Color drawingColor)
                        {
                            color = new SKColor(drawingColor.R, drawingColor.G, drawingColor.B, drawingColor.A);
                        }
                        break;
                    case "Red":
                        color = new SKColor((byte)variable.Value, color.Green, color.Blue, color.Alpha);
                        break;
                    case "Green":
                        color = new SKColor(color.Red, (byte)variable.Value, color.Blue, color.Alpha);
                        break;
                    case "Blue":
                        color = new SKColor(color.Red, color.Green, (byte)variable.Value, color.Alpha);
                        break;
                    case "FontSize":
                        fontSizePixels = (int)variable.Value;
                        break;
                    case "FontScale":
                        fontScale = (float)variable.Value;
                        break;
                    case "IsBold":
                        weight = (bool)variable.Value ? 700 : (int)(400 * BoldWeight);
                        break;
                    case "IsItalic":
                        italic = (bool)variable.Value;
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
                                        Color = System.Drawing.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue),
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
                                if (response.Color != null)
                                {
                                    var c = response.Color.Value;
                                    color = new SKColor(c.R, c.G, c.B, c.A);
                                }
                                if (response.ReplacementCharacter != null)
                                {
                                    text = response.ReplacementCharacter.Value.ToString();
                                }
                                // ScaleX/ScaleY/RotationDegrees are not applied on SkiaGum yet -- see
                                // LetterCustomization's remarks.
                            }
                        }
                        break;
                }
            }
        }

        var style = new Style()
        {
            FontFamily = FontName,
            FontSize = fontSizePixels * (float)GlobalTextScale * fontScale,
            TextColor = color,
            FontItalic = italic,
            FontWeight = weight,
            LineHeight = LineHeightMultiplier
        };

        // OutlineThickness is intentionally NOT mapped to RichTextKit's Style.HaloColor/HaloWidth here.
        // That halo is a stroke centered on the glyph edge, so at width N the fill covers the inner
        // half and only ~N/2 shows (thin, uneven at small sizes); widening to 2N to compensate hits
        // RichTextKit's hardcoded miter join (no join knob) and spikes at acute vertices. Instead the
        // outline is a uniform recolor+dilate pass in Render (DrawTextWithOutline), which can't spike
        // or emboss and reuses RichTextKit's exact layout.

        return (style, text, xOffset, yOffset);
    }

    /// <summary>
    /// Re-parses <see cref="mRawText"/> into BBCode inline-styling state (issue #3679): sets
    /// <see cref="_hasMarkup"/>, the tag-stripped <see cref="_layoutText"/> the TextBlock is built from,
    /// and <see cref="_inlineVariables"/>. Plain text (no recognized tags) leaves _layoutText equal to
    /// mRawText so the render/measure path is byte-for-byte unchanged. Called whenever RawText changes.
    /// </summary>
    private void ParseMarkup()
    {
        _inlineVariables.Clear();

        if (string.IsNullOrEmpty(mRawText) || mRawText.IndexOf('[') < 0)
        {
            _hasMarkup = false;
            _layoutText = mRawText;
            return;
        }

        // The rendering/wrapping code ignores '\r', so normalize CRLF to LF before computing indexes,
        // mirroring the MonoGame/Raylib SetBbCodeText path. Parsing and stripping from the same
        // normalized string keeps InlineVariable indexes aligned with the layout text.
        var normalized = mRawText.Replace("\r\n", "\n");
        var tags = BbCodeParser.Parse(normalized, SupportedTags);
        if (tags.Count == 0)
        {
            _hasMarkup = false;
            _layoutText = mRawText;
            return;
        }

        _hasMarkup = true;
        _layoutText = BbCodeParser.RemoveTags(normalized, tags);
        BuildInlineVariables(tags);
    }

    /// <summary>
    /// Converts the parsed BBCode <paramref name="tags"/> into <see cref="_inlineVariables"/> keyed by
    /// their character range in the stripped <see cref="_layoutText"/>. Each supported tag maps to a
    /// value the shared <see cref="StyledSubstringSplitter"/> and <see cref="BuildRunStyle"/> understand:
    /// Color -> System.Drawing.Color, Red/Green/Blue -> byte, FontSize -> int, FontScale -> float,
    /// IsBold/IsItalic -> bool. Tags whose argument fails to parse are skipped (rendered with base style).
    /// </summary>
    private void BuildInlineVariables(List<FoundTag> tags)
    {
        foreach (var tag in tags)
        {
            var argument = tag.Open.Argument;
            string variableName;
            object? value;

            switch (tag.Name.ToLowerInvariant())
            {
                case "color":
                    variableName = "Color";
                    value = ParseColor(argument);
                    break;
                case "red":
                    variableName = "Red";
                    value = byte.TryParse(argument, out var red) ? red : (byte?)null;
                    break;
                case "green":
                    variableName = "Green";
                    value = byte.TryParse(argument, out var green) ? green : (byte?)null;
                    break;
                case "blue":
                    variableName = "Blue";
                    value = byte.TryParse(argument, out var blue) ? blue : (byte?)null;
                    break;
                case "fontsize":
                    variableName = "FontSize";
                    value = int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size)
                        ? size
                        : (int?)null;
                    break;
                case "fontscale":
                    variableName = "FontScale";
                    value = float.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale)
                        ? scale
                        : (float?)null;
                    break;
                case "isbold":
                    variableName = "IsBold";
                    value = bool.TryParse(argument, out var bold) ? bold : (bool?)null;
                    break;
                case "isitalic":
                    variableName = "IsItalic";
                    value = bool.TryParse(argument, out var italic) ? italic : (bool?)null;
                    break;
                case "custom":
                    // A [Custom=Name] callback applies per letter (issue #3692), so -- unlike every other
                    // tag above, which adds a single InlineVariable spanning the whole tagged range --
                    // this adds one InlineVariable per character, each with its own CharacterIndex/
                    // TextBlock context. Mirrors the shared MonoGame/Raylib SetBbCodeText per-letter loop
                    // (Gum/Wireframe/CustomSetPropertyOnRenderable.cs).
                    if (!string.IsNullOrEmpty(argument) && !string.IsNullOrEmpty(_layoutText))
                    {
                        var startStripped = tag.Open.StartStrippedIndex;
                        var length = tag.Close.StartStrippedIndex - startStripped;
                        if (length > 0 && startStripped + length <= _layoutText.Length)
                        {
                            var taggedSubstring = _layoutText.Substring(startStripped, length);
                            for (int i = 0; i < taggedSubstring.Length; i++)
                            {
                                _inlineVariables.Add(new InlineVariable
                                {
                                    VariableName = "Custom",
                                    StartIndex = startStripped + i,
                                    CharacterCount = 1,
                                    Value = new ParameterizedLetterCustomizationCall
                                    {
                                        FunctionName = argument,
                                        CharacterIndex = startStripped + i,
                                        TextBlock = taggedSubstring,
                                    },
                                });
                            }
                        }
                    }
                    continue;
                default:
                    continue;
            }

            if (value == null)
            {
                continue;
            }

            _inlineVariables.Add(new InlineVariable
            {
                VariableName = variableName,
                StartIndex = tag.Open.StartStrippedIndex,
                CharacterCount = tag.Close.StartStrippedIndex - tag.Open.StartStrippedIndex,
                Value = value,
            });
        }
    }

    /// <summary>
    /// Parses a <c>[Color=..]</c> argument, honoring both a hex form (<c>0xAARRGGBB</c>) and a named
    /// color (<c>Red</c>, <c>CornflowerBlue</c>, ...), matching the MonoGame/Raylib SetBbCodeText parsing.
    /// Returns null (run keeps the base color) when the argument is empty or unrecognized.
    /// </summary>
    private static System.Drawing.Color? ParseColor(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return null;
        }

        if (argument.StartsWith("0x") &&
            int.TryParse(argument.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int hex))
        {
            return System.Drawing.Color.FromArgb(hex);
        }

        var named = System.Drawing.Color.FromName(argument);
        // FromName returns a non-known color with ARGB 0 for an unrecognized name; treat that as "no color".
        return named.IsKnownColor ? named : (System.Drawing.Color?)null;
    }

    /// <summary>
    /// Builds the <see cref="SKPaint"/> used to composite the text when a render-time effect is
    /// active, carrying the drop-shadow <see cref="SKPaint.ImageFilter"/> (when
    /// <see cref="HasDropshadow"/>) and/or the <see cref="SKPaint.BlendMode"/> (when
    /// <see cref="Blend"/> is set) so the two compose in one <c>canvas.SaveLayer</c>. Returns
    /// <c>null</c> when neither effect is active, leaving <see cref="Render"/>'s fast path
    /// unchanged. The caller owns the returned paint and must dispose it. The blur values are
    /// divided by 3 to match the sigma convention <see cref="RenderableShapeBase"/> uses for Skia shapes.
    /// </summary>
    internal SKPaint? GetRenderPaint()
    {
        if (!HasDropshadow && !Blend.HasValue)
        {
            return null;
        }

        var paint = new SKPaint();

        if (HasDropshadow)
        {
            paint.ImageFilter = SKImageFilter.CreateDropShadow(
                DropshadowOffsetX,
                DropshadowOffsetY,
                DropshadowBlurX / 3.0f,
                DropshadowBlurY / 3.0f,
                DropshadowColor);
        }

        if (Blend.HasValue)
        {
            paint.BlendMode = Blend.Value.ToSKBlendMode();
        }

        return paint;
    }

    /// <summary>
    /// Same font/weight/italics as <see cref="GetStyle"/>, but sized at the base
    /// <see cref="FontSize"/> * <see cref="GlobalTextScale"/> only -- <see cref="FontScale"/>
    /// and <see cref="LineHeightMultiplier"/> are left out because <see cref="MeasureString(string)"/>
    /// and <see cref="LineHeightInPixels"/> return raw glyph-pixel values that callers (e.g.
    /// <c>TextBoxBase</c>) scale by those factors themselves.
    /// </summary>
    private Style GetRawMeasurementStyle()
    {
        return new Style()
        {
            FontFamily = FontName,
            FontSize = FontSize * (float)GlobalTextScale,
            TextColor = this.Color,
            FontItalic = this.IsItalic,
            FontWeight = (int)(400 * BoldWeight),
        };
    }

    /// <inheritdoc/>
    public float MeasureString(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var textBlock = new TextBlock();
        textBlock.AddText(text, GetRawMeasurementStyle());
        return textBlock.MeasuredWidth;
    }

    /// <summary>
    /// Measures <paramref name="text"/> using this Text's active font. The
    /// <paramref name="style"/> parameter is advisory on the SkiaGum runtime and is
    /// ignored -- RichTextKit's own measurement is used regardless of the requested
    /// style. This overload exists so callers can write platform-agnostic code that
    /// also works on runtimes (such as the MonoGame runtime) where the style is honored.
    /// </summary>
    public float MeasureString(string text, HorizontalMeasurementStyle style) => MeasureString(text);

    void IRenderableIpso.SetParentDirect(IRenderableIpso? parent)
    {
        mParent = parent;
    }

    public void PreRender() { }

    public void StartBatch(ISystemManagers systemManagers)
    {
    }

    public void EndBatch(ISystemManagers systemManagers)
    {
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
    IVisible? IVisible.Parent =>((IRenderableIpso)this).Parent as IVisible;

    public string BatchKey => string.Empty;

    #endregion

    public Text Clone()
    {
        var newInstance = (Text)this.MemberwiseClone();
        newInstance.mParent = null;
        newInstance.mChildren = new();
        newInstance._cachedTextBlock = null;
        // MemberwiseClone shares the list reference; give the clone its own copy so re-parsing markup
        // on one instance (via RawText) doesn't mutate the other's inline-styling runs (issue #3679).
        newInstance._inlineVariables = new List<InlineVariable>(_inlineVariables);

        return newInstance;
    }

    object ICloneable.Clone() => Clone();
}
