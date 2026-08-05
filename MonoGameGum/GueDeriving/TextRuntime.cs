using Gum.DataTypes;
#if RAYLIB
using Gum.Renderables;
using RaylibGum.Helpers;
// This file (via Gum/Wireframe/CustomSetPropertyOnRenderable.cs, RAYLIB-namespaced) is the same
// shared dispatcher source XNALIKE compiles, just under a different namespace -- see that file's
// own #if RAYLIB/#else namespace switch.
using CustomSetPropertyOnRenderableType = RaylibGum.Renderables.CustomSetPropertyOnRenderable;
#elif SKIA
using SkiaGum;
using SkiaGum.Helpers;
using SkiaSharp;
using CustomSetPropertyOnRenderableType = SkiaGum.CustomSetPropertyOnRenderable;
#else
using Gum.Graphics;
using Gum.RenderingLibrary;
using CustomSetPropertyOnRenderableType = Gum.Wireframe.CustomSetPropertyOnRenderable;
#endif
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Color = Microsoft.Xna.Framework.Color;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// A visual text element which can display a string.
/// </summary>
public class TextRuntime : InteractiveGue
{
    Text? _containedText;
    Text ContainedText
    {
        get
        {
            if (_containedText == null)
            {
                _containedText = (Text)this.RenderableComponent;
#if XNALIKE
                _containedText.OnPreRender = UpdateAutomaticFontOversampling;
#endif
            }
            return _containedText;
        }
    }

#if !RAYLIB && !SKIA
    /// <summary>
    /// The XNA blend state used when rendering the text. This controls how
    /// color and alpha values blend with the background.
    /// </summary>
    public Microsoft.Xna.Framework.Graphics.BlendState BlendState
    {
        get => ContainedText.BlendState.ToXNA();
        set
        {
            ContainedText.BlendState = value.ToGum();
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(Blend));
        }
    }
#endif

    /// <summary>
    /// The Gum-specific Blend mode for the text. Null means "use the renderer's current
    /// blend mode" (typically alpha blending).
    /// </summary>
    public Gum.RenderingLibrary.Blend? Blend
    {
        get
        {
#if XNALIKE
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedText.BlendState);
#else
            // RAYLIB and SKIA renderables expose Blend directly (no XNA BlendState to translate).
            return ContainedText.Blend;
#endif
        }
        set
        {
#if XNALIKE
            if (value.HasValue)
            {
                BlendState = value.Value.ToBlendState().ToXNA();
            }
            // NotifyPropertyChanged handled by BlendState:
#else
            ContainedText.Blend = value;
            NotifyPropertyChanged();
#endif
        }
    }

    /// <summary>
    /// The red component of the text color. Ranges from 0 to 255.
    /// </summary>
    public int Red
    {
        get => ContainedText.Red;
        set => ContainedText.Red = value;
    }

    /// <summary>
    /// The green component of the text color. Ranges from 0 to 255.
    /// </summary>
    public int Green
    {
        get => ContainedText.Green;
        set => ContainedText.Green = value;
    }

    /// <summary>
    /// The blue component of the text color. Ranges from 0 to 255.
    /// </summary>
    public int Blue
    {
        get => ContainedText.Blue;
        set => ContainedText.Blue = value;
    }

    /// <summary>
    /// The alpha (opacity) component of the text color. Ranges from 0 (fully transparent) to 255 (fully opaque).
    /// </summary>
    public int Alpha
    {
        get => ContainedText.Alpha;
        set => ContainedText.Alpha = value;
    }

    /// <summary>
    /// Gets or sets the color used to render the text. This includes color and alpha (opacity) components.
    /// </summary>
    public Color Color
    {
        get => ContainedText.Color.ToUserColor();
        set
        {
            ContainedText.Color = value.ToContainerColor();
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The horizontal alignment of the text within its bounding box.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment
    {
        get => ContainedText.HorizontalAlignment;
        set
        {
            ContainedText.HorizontalAlignment = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The vertical alignment of the text within its bounding box.
    /// </summary>
    public VerticalAlignment VerticalAlignment
    {
        get => ContainedText.VerticalAlignment;
        set => ContainedText.VerticalAlignment = value;
    }

    /// <summary>
    /// The maximum number of characters to display visually. Characters beyond this count
    /// are hidden but remain in the text string. This is a display-only
    /// property useful for typewriter-style effects where text prints out letter-by-letter.
    /// </summary>
    public int? MaxLettersToShow
    {
        get => ContainedText.MaxLettersToShow;
        set
        {
            ContainedText.MaxLettersToShow = value;
        }
    }


    /// <summary>
    /// The maximum number of lines to display. This can be used to
    /// limit how many lines of text are displayed at one time.
    /// </summary>
    public int? MaxNumberOfLines
    {
        get => ContainedText.MaxNumberOfLines;
        set => ContainedText.MaxNumberOfLines = value;
    }

#if RAYLIB
    /// <summary>
    /// The concrete font object currently active on this text -- whatever <see cref="Font"/>/
    /// <see cref="FontSize"/> resolved to, or an explicitly assigned override. Same concept as
    /// XNALIKE's and Skia's <c>Typeface</c>, typed to this backend's own font representation.
    /// </summary>
    public Font Typeface
    {
        get => ContainedText.Font;
        set => ContainedText.Font = value;
    }

    [Obsolete("Use Typeface instead.")]
    public Font CustomFont
    {
        get => Typeface;
        set => Typeface = value;
    }
#elif SKIA
    /// <inheritdoc cref="Typeface"/>
    public SKTypeface? Typeface
    {
        get => ContainedText.Typeface;
        set => ContainedText.Typeface = value;
    }
#else
    /// <summary>
    /// The concrete font object currently active on this text -- whatever <see cref="Font"/>/
    /// <see cref="FontSize"/> resolved to, or an explicitly assigned override. Same concept as
    /// Raylib's and Skia's <c>Typeface</c>, typed to this backend's own font representation.
    /// </summary>
    public BitmapFont Typeface
    {
        get => ContainedText.BitmapFont;
        set
        {
            if (value != Typeface)
            {
                ContainedText.BitmapFont = value;
                NotifyPropertyChanged();
                UpdateLayout();
            }
        }
    }

    [Obsolete("Use Typeface instead.")]
    public BitmapFont BitmapFont
    {
        get => Typeface;
        set => Typeface = value;
    }
#endif

#if RAYLIB || XNALIKE
    /// <summary>
    /// A multiplier used when rendering the text. The default value is 1.0.
    /// </summary>
    /// <remarks>
    /// Setting this value to a value other than 1 scales the text accordingly. This is
    /// a scalue value applied to the existing font, so a value larger than 1 can result
    /// in the font appearing pixellated.
    ///
    /// Since this value does not affect the underlying Font, it can be changed without
    /// requiring a dedicated font asset.
    /// </remarks>
#else
    /// <summary>
    /// A multiplier used when rendering the text. The default value is 1.0.
    /// </summary>
#endif
    public float FontScale
    {
        get => ContainedText.FontScale;
        set
        {
            if (value != FontScale)
            {
                ContainedText.FontScale = value;
                NotifyPropertyChanged();
                UpdateLayout();
            }
        }
    }

    public float LineHeightMultiplier
    {
        get => ContainedText.LineHeightMultiplier;
        set
        {
            if (value != LineHeightMultiplier)
            {
                ContainedText.LineHeightMultiplier = value;
                NotifyPropertyChanged();
                UpdateLayout();
            }
        }
    }

    bool useCustomFont;
    /// <summary>
    /// Whether to use the CustomFontFile to determine the font value. 
    /// If false, then the font is determiend by looking for an existing
    /// font based on:
    /// * Font
    /// * FontSize
    /// * IsItalic
    /// * IsBold
    /// * UseFontSmoothing
    /// * OutlineThickness
    /// * HasDropshadow and dropshadow fields (when <see cref="HasDropshadow"/> is true)
    /// </summary>
    public bool UseCustomFont
    {
        get { return useCustomFont; }
        set { useCustomFont = value; UpdateToFontValues(); }
    }

    string? customFontFile;
    /// <summary>
    /// Specifies the name of the custom font. This can be specified relative to
    /// FileManager.RelativeDirectory, which is the Content folder for code-only projects,
    /// or the folder containing the .gumx project if loading a Gum project. Accepts either
    /// a pre-baked <c>.fnt</c> atlas (loaded directly) or a <c>.ttf</c>/<c>.otf</c> source
    /// (baked via the same bmfont.exe/KernSmith cascade <see cref="Font"/>-as-path uses, on
    /// XNA-like backends; raylib resolves TTF natively regardless of extension).
    /// </summary>
    public string? CustomFontFile
    {
        get { return customFontFile; }
        set { customFontFile = value; UpdateToFontValues(); }
    }

    string font;
    /// <summary>
    /// The font name, such as "Arial", which is used to load fonts from
    /// </summary>
    public string Font
    {
        get => FontFamily;
        set => FontFamily = value;
    }

    /// <summary>
    /// The font name, such as "Arial", which is used to load fonts from
    /// </summary>
    public string FontFamily
    {
        get { return font; }
        set { font = value; UpdateToFontValues(); }
    }

    float fontSize;
    public float FontSize
    {
        get { return fontSize; }
        set { fontSize = value; UpdateToFontValues(); }
    }

    bool isItalic;
    public bool IsItalic
    {
        get => isItalic;
        set { isItalic = value; UpdateToFontValues(); }
    }

#if SKIA
    float _boldWeight = 1;
    /// <summary>
    /// Gets or sets the weight multiplier for bold text rendering.
    /// A value of 1.0 represents regular weight, while higher values
    /// increase the thickness of strokes (e.g., 1.5 for bold).
    /// </summary>
    /// <remarks>
    /// Skia-only: MonoGame/Raylib bake <see cref="IsBold"/> into the font atlas at generation time
    /// (no runtime scalar exists). KernSmith's underlying generator does have a variable-font weight
    /// axis (<c>VariationAxes["wght"]</c>) that could unlock a continuous weight there too, but Gum's
    /// generator mapping doesn't wire it up -- not currently planned; no user demand for it (issue #3708).
    /// </remarks>
    public float BoldWeight
    {
        get => _boldWeight;
        set
        {
            _boldWeight = value;
            ContainedText.BoldWeight = value;
        }
    }

    public bool IsBold
    {
        get => _boldWeight > 1;
        set { BoldWeight = value ? 1.5f : 1f; }
    }
#else
    bool isBold;
    public bool IsBold
    {
        get => isBold;
        set { isBold = value; UpdateToFontValues(); }
    }
#endif

    // Not sure if we need to make this a public value, but we do need to store it
    // Update - yes we do need this to be public so it can be assigned in codegen:
    bool useFontSmoothing = true;
    public bool UseFontSmoothing
    {
        get { return useFontSmoothing; }
        set { useFontSmoothing = value; UpdateToFontValues(); }
    }

    int outlineThickness;
    public int OutlineThickness
    {
        get { return outlineThickness; }
        set { outlineThickness = value; UpdateToFontValues(); }
    }

#if SKIA
    // Defaults to opaque black (not default(Color), which is fully transparent) to match
    // SkiaGum.Text's own constructor default -- UpdateFonts pushes this to the contained Text
    // unconditionally whenever UpdateToFontValues runs, including as a side effect of setting only
    // OutlineThickness, so an uninitialized transparent default silently clobbered Text's halo
    // color and made RichTextKit's `Style.HaloColor != SKColor.Empty` gate skip the halo draw
    // entirely (issue #4077).
    Color _outlineColor = SkiaSharp.SKColors.Black;
    /// <summary>
    /// The color of the outline drawn when <see cref="OutlineThickness"/> is greater than zero.
    /// Skia-only: MonoGame/Raylib bake the outline into the font atlas at generation time and expose
    /// no runtime outline-color concept, so there is no cross-backend property to mirror. Not
    /// currently planned for XNALIKE/Raylib; no user demand for it (issue #3708).
    /// </summary>
    public Color OutlineColor
    {
        get => _outlineColor;
        set { _outlineColor = value; UpdateToFontValues(); }
    }
#endif

    bool hasDropshadow;
    /// <summary>
    /// When true, KernSmith bakes a drop shadow into the font atlas using the dropshadow fields below.
    /// </summary>
    public bool HasDropshadow
    {
        get => hasDropshadow;
        set
        {
            if (value && !hasDropshadow)
            {
                SeedDefaultDropshadowIfUnset();
            }

            hasDropshadow = value;
            UpdateToFontValues();
            ApplyDropshadowToRenderable();
        }
    }

    byte dropshadowRed;
    byte dropshadowGreen;
    byte dropshadowBlue;
    byte dropshadowAlpha = 180;

    /// <summary>
    /// Color of the baked glyph drop shadow. Alpha is decomposed into KernSmith opacity at generation time.
    /// </summary>
    public Color DropshadowColor
    {
        get => new Color(dropshadowRed, dropshadowGreen, dropshadowBlue, dropshadowAlpha);
        set
        {
#if SKIA
            dropshadowRed = value.Red;
            dropshadowGreen = value.Green;
            dropshadowBlue = value.Blue;
            dropshadowAlpha = value.Alpha;
#else
            dropshadowRed = value.R;
            dropshadowGreen = value.G;
            dropshadowBlue = value.B;
            dropshadowAlpha = value.A;
#endif
            UpdateToFontValues();
            ApplyDropshadowToRenderable();
        }
    }

    public int DropshadowRed
    {
        get => dropshadowRed;
        set { dropshadowRed = (byte)value; UpdateToFontValues(); ApplyDropshadowToRenderable(); }
    }

    public int DropshadowGreen
    {
        get => dropshadowGreen;
        set { dropshadowGreen = (byte)value; UpdateToFontValues(); ApplyDropshadowToRenderable(); }
    }

    public int DropshadowBlue
    {
        get => dropshadowBlue;
        set { dropshadowBlue = (byte)value; UpdateToFontValues(); ApplyDropshadowToRenderable(); }
    }

    public int DropshadowAlpha
    {
        get => dropshadowAlpha;
        set { dropshadowAlpha = (byte)value; UpdateToFontValues(); ApplyDropshadowToRenderable(); }
    }

    float dropshadowOffsetX;
    public float DropshadowOffsetX
    {
        get => dropshadowOffsetX;
        set { dropshadowOffsetX = value; UpdateToFontValues(); ApplyDropshadowToRenderable(); }
    }

    float dropshadowOffsetY;
    public float DropshadowOffsetY
    {
        get => dropshadowOffsetY;
        set { dropshadowOffsetY = value; UpdateToFontValues(); ApplyDropshadowToRenderable(); }
    }

    float dropshadowBlur;
    /// <summary>
    /// Blur radius for the baked drop shadow, passed to KernSmith as a single scalar.
    /// </summary>
    /// <remarks>
    /// On Skia this also seeds <see cref="DropshadowBlurX"/>/<see cref="DropshadowBlurY"/> equally,
    /// so callers that only ever set this scalar keep behaving the same as before those per-axis
    /// properties existed. Set the per-axis properties explicitly to diverge the two.
    /// </remarks>
    public float DropshadowBlur
    {
        get => dropshadowBlur;
        set
        {
            dropshadowBlur = Math.Max(0f, value);
#if SKIA
            _dropshadowBlurX = dropshadowBlur;
            _dropshadowBlurY = dropshadowBlur;
#endif
            UpdateToFontValues();
        }
    }

#if SKIA
    float _dropshadowBlurX;
    /// <summary>
    /// Skia-only independent horizontal blur axis for the drop shadow. Setting <see cref="DropshadowBlur"/>
    /// seeds this and <see cref="DropshadowBlurY"/> equally; set this explicitly to diverge the two,
    /// which the XNALIKE/Raylib baked shadow can't do. Not currently planned for XNALIKE/Raylib; no
    /// user demand for it (issue #3708).
    /// </summary>
    public float DropshadowBlurX
    {
        get => _dropshadowBlurX;
        set { _dropshadowBlurX = value; UpdateToFontValues(); }
    }

    float _dropshadowBlurY;
    /// <inheritdoc cref="DropshadowBlurX"/>
    public float DropshadowBlurY
    {
        get => _dropshadowBlurY;
        set { _dropshadowBlurY = value; UpdateToFontValues(); }
    }
#endif

    /// <summary>
    /// Applies the first-enable defaults for baked shadow so toggling <see cref="HasDropshadow"/>
    /// on at runtime produces a visible dark shadow without further setup.
    /// </summary>
    void SeedDefaultDropshadowIfUnset()
    {
        if (dropshadowOffsetX == 0f && dropshadowOffsetY == 0f && dropshadowBlur == 0f)
        {
            dropshadowOffsetY = 3f;
            dropshadowBlur = 2f;
#if SKIA
            _dropshadowBlurX = dropshadowBlur;
            _dropshadowBlurY = dropshadowBlur;
#endif
        }
    }

    /// <summary>
    /// Issues #4001/#4057: forwards the dropshadow color/offset to the Text renderable so the runtime
    /// draws the shadow as a second (offset, independently tinted) pass under the glyphs. Skia renders
    /// its own blurred shadow instead, so this is XNALIKE/Raylib-only.
    /// </summary>
    void ApplyDropshadowToRenderable()
    {
#if XNALIKE || RAYLIB
        ContainedText.HasDropshadow = HasDropshadow;
        ContainedText.DropshadowColor = DropshadowColor.ToContainerColor();
        ContainedText.DropshadowOffsetX = DropshadowOffsetX;
        ContainedText.DropshadowOffsetY = DropshadowOffsetY;
#endif
    }

#if !FRB
    /// <summary>
    /// Copies font-generation fields (including dropshadow) onto a <see cref="RenderingLibrary.Graphics.Fonts.BmfcSave"/>
    /// for KernSmith or disk-cache font creation.
    /// </summary>
    internal void CopyFontGenerationFieldsTo(BmfcSave bmfcSave, string? resolvedFontFilePath)
    {
        bmfcSave.FontSize = FontSize;
        bmfcSave.OutlineThickness = OutlineThickness;
        bmfcSave.UseSmoothing = UseFontSmoothing;
        bmfcSave.IsItalic = IsItalic;
        bmfcSave.IsBold = IsBold;
        bmfcSave.HasDropshadow = HasDropshadow;
        bmfcSave.DropshadowOffsetX = DropshadowOffsetX;
        bmfcSave.DropshadowOffsetY = DropshadowOffsetY;
        bmfcSave.DropshadowBlur = DropshadowBlur;
        bmfcSave.DropshadowRed = dropshadowRed;
        bmfcSave.DropshadowGreen = dropshadowGreen;
        bmfcSave.DropshadowBlue = dropshadowBlue;
        bmfcSave.DropshadowAlpha = dropshadowAlpha;

        if (resolvedFontFilePath != null)
        {
            bmfcSave.FontFile = resolvedFontFilePath;
            bmfcSave.FontName = System.IO.Path.GetFileNameWithoutExtension(resolvedFontFilePath);
        }
        else
        {
            bmfcSave.FontName = Font;
            bmfcSave.FontFile = null;
        }
    }

    /// <summary>
    /// Gets the FontCache file name for the current font property tuple, including dropshadow when enabled.
    /// </summary>
    internal string GetFontCacheFileName(string? fontFilePath)
    {
        return BmfcSave.GetFontCacheFileNameFor(
            FontSize,
            Font,
            OutlineThickness,
            UseFontSmoothing,
            IsItalic,
            IsBold,
            fontFilePath,
            HasDropshadow,
            DropshadowOffsetX,
            DropshadowOffsetY,
            DropshadowBlur,
            dropshadowRed,
            dropshadowGreen,
            dropshadowBlue,
            dropshadowAlpha);
    }

#if XNALIKE
    /// <summary>
    /// Regenerates this text's font at <paramref name="oversampleRatio"/> times <see cref="FontSize"/>
    /// via <see cref="CustomSetPropertyOnRenderableType.InMemoryFontCreator"/>, then compensates with
    /// the contained Text's internal-only <see cref="Text.OversampleCompensationScale"/> so the
    /// on-screen size is unchanged -- the glyphs are just rasterized at a higher pixel density
    /// (issue #4302). Unlike the compensation this replaced, it composes with the public
    /// <see cref="FontScale"/> instead of overwriting it (issue #4317), so a caller's own FontScale
    /// (including inline [FontScale] BBCode runs) keeps working while oversampling is active.
    /// </summary>
    /// <param name="oversampleRatio">How many times larger than <see cref="FontSize"/> to rasterize
    /// the font, e.g. the current effective camera/layer zoom. Requested as an exact fraction --
    /// KernSmith's rasterizer has fractional precision (see issue #4304), so this is not rounded.</param>
    /// <returns>
    /// True if the font was regenerated. False if <see cref="UseFontOversampling"/> is off, no
    /// in-memory font creator is registered, or <paramref name="oversampleRatio"/> is not positive --
    /// oversampling only makes sense with dynamic font generation, since a disk-based font cache
    /// holds a fixed set of pre-baked sizes.
    /// </returns>
    public bool RegenerateOversampledFont(float oversampleRatio)
    {
        if (!UseFontOversampling || CustomSetPropertyOnRenderableType.InMemoryFontCreator == null || oversampleRatio <= 0)
        {
            return false;
        }

        var rasterFontSize = System.MathF.Max(1f, FontSize * oversampleRatio);

        var fontFilePath = BmfcSave.ResolveTtfSourcePath(UseCustomFont, CustomFontFile, Font);

        var bmfcSave = new BmfcSave();
        CopyFontGenerationFieldsTo(bmfcSave, fontFilePath);
        bmfcSave.FontSize = rasterFontSize;

        var font = CustomSetPropertyOnRenderableType.InMemoryFontCreator.TryCreateFont(bmfcSave);
        if (font == null)
        {
            return false;
        }

        // Issue #4309: pins the OUTGOING (native) font as the measurement source, if nothing is pinned
        // yet, before swapping in this new (oversampled) display font -- see Text.SetOversampledDisplayFont.
        ContainedText.SetOversampledDisplayFont(font);
        ContainedText.OversampleCompensationScale = FontSize / rasterFontSize;

        // BitmapFont/OversampleCompensationScale are renderable-level properties -- assigning them
        // directly (bypassing the normal property-setter cascade a call like FontSize= goes through)
        // never notifies this element's own layout. Without this, a RelativeToChildren box keeps
        // whatever Width it measured against the OLD font and wraps the NEW font's (differently-
        // measuring) glyphs against it.
        UpdateLayout();

        return true;
    }

    float _lastAutoOversampleRatio = 1f;

    /// <summary>
    /// Wired to <see cref="Text.OnPreRender"/> so it runs once per frame for every visible Text on
    /// the layered draw path -- resolves the current effective camera/layer zoom and delegates the
    /// actual regenerate-or-not decision to <see cref="UpdateAutomaticFontOversampling(float)"/>.
    /// Replaces the #4302 manual "press R" trigger with the render-time mechanism issue #4317 asks for.
    /// </summary>
    void UpdateAutomaticFontOversampling()
    {
        var camera = EffectiveManagers?.Renderer?.Camera;
        if (camera != null)
        {
            UpdateAutomaticFontOversampling(GetEffectiveZoom(this.Layer, camera));
        }
    }

    /// <summary>
    /// Given the current effective screen scale (see <see cref="GetEffectiveZoom"/>), re-rasterizes
    /// this Text's font only when the requested raster pixel size would move by at least a full pixel
    /// from what it was last rasterized at -- so continuously zooming doesn't regenerate every frame
    /// for imperceptible deltas. Split out from the parameterless <see cref="UpdateAutomaticFontOversampling()"/>
    /// so the decision logic is testable without a real Camera/Layer/SystemManagers graph.
    /// </summary>
    /// <returns>True if the font was regenerated.</returns>
    internal bool UpdateAutomaticFontOversampling(float effectiveZoom)
    {
        if (!UseFontOversampling || CustomSetPropertyOnRenderableType.InMemoryFontCreator == null)
        {
            return false;
        }

        // Regenerating below native resolution isn't this feature's job -- only oversample, never
        // undersample.
        var oversampleRatio = System.Math.Max(1f, effectiveZoom);

        var rasterDelta = System.MathF.Abs(FontSize * oversampleRatio - FontSize * _lastAutoOversampleRatio);
        if (rasterDelta >= 1f && RegenerateOversampledFont(oversampleRatio))
        {
            _lastAutoOversampleRatio = oversampleRatio;
            return true;
        }

        return false;
    }

    /// <summary>
    /// The effective screen scale a Text on <paramref name="layer"/> renders at -- composes the main
    /// <paramref name="camera"/>'s zoom with the layer's own <see cref="LayerCameraSettings.Zoom"/>
    /// override. Mirrors <c>SpriteRenderer.GetZoomAndMatrix</c>'s zoom resolution; kept as a small,
    /// independent copy here (rather than a shared call into that method) since it sits in
    /// matrix-routing code documented as historically regression-prone -- see the
    /// gum-monogame-rendering skill's "Matrix Routing" section.
    /// </summary>
    internal static float GetEffectiveZoom(Layer? layer, Camera camera)
    {
        var layerCameraSettings = layer?.LayerCameraSettings;
        if (layerCameraSettings == null)
        {
            return camera.Zoom;
        }

        return layerCameraSettings.IsInScreenSpace
            ? layerCameraSettings.Zoom ?? 1f
            : layerCameraSettings.Zoom ?? camera.Zoom;
    }

    /// <summary>
    /// Called by <see cref="CustomSetPropertyOnRenderable.UpdateToFontValues"/> (the single choke
    /// point both the direct-property-setter and string/state-based <c>SetProperty</c> paths
    /// converge on -- see the gum-property-assignment skill) whenever a font property (Font,
    /// FontSize, IsBold, etc.) is re-resolved. Resets the automatic-oversampling hysteresis cache so
    /// the next PreRender re-evaluates against the NEW FontSize instead of comparing it to a ratio
    /// computed for the OLD one, which could otherwise read as "no change" and leave oversampling
    /// stale (issue #4317). <see cref="Text.OversampleCompensationScale"/> itself is reset by the
    /// caller directly, since it lives on the renderable that caller already has a reference to.
    /// </summary>
    internal void ResetAutomaticOversamplingState() => _lastAutoOversampleRatio = 1f;
#endif
#endif

    public TextOverflowHorizontalMode TextOverflowHorizontalMode
    {
        // Currently GraphicalUiElement doesn't expose this property so we have to go through setting it by string:
        get => ContainedText.IsTruncatingWithEllipsisOnLastLine ? TextOverflowHorizontalMode.EllipsisLetter : TextOverflowHorizontalMode.TruncateWord;
        set
        {
            ContainedText.IsTruncatingWithEllipsisOnLastLine = value == TextOverflowHorizontalMode.EllipsisLetter;
            NotifyPropertyChanged();
            UpdateLayout();
        }
    }

    /// <summary>
    /// Whether the last visible line is truncated with a trailing ellipsis ("...") when the text
    /// overflows -- either a single line wider than the box, or (with
    /// <see cref="TextOverflowVerticalMode"/> set to <see cref="TextOverflowVerticalMode.TruncateLine"/>)
    /// the lines that fall past the box height.
    /// </summary>
    public bool IsTruncatingWithEllipsisOnLastLine
    {
        get => ContainedText.IsTruncatingWithEllipsisOnLastLine;
        set
        {
            ContainedText.IsTruncatingWithEllipsisOnLastLine = value;
            NotifyPropertyChanged();
            UpdateLayout();
        }
    }

    // NOTE: TextOverflowVerticalMode is intentionally NOT declared here. GraphicalUiElement (our base)
    // already exposes it with a backing field that its layout/pagination coordination reads (e.g.
    // DialogBox pagination, RefreshTextOverflowVerticalMode). Redeclaring it here hides that property
    // with a forward-to-renderable-only version, leaving the base field stuck at its default so
    // truncation silently stops working (broke DialogBox multi-page splitting). The inherited property
    // already forwards to the contained renderable, so no cast is needed to set it from a TextRuntime.

    /// <summary>
    /// Gets or sets the text rendering position mode to use for the contained text, overriding the default behavior if
    /// specified.
    /// </summary>
    /// <remarks>Set this property to control how text positioning is handled during rendering. If the value
    /// is <see langword="null"/>, the default rendering position mode is used. Changing this property affects only the
    /// contained text and does not impact other elements.</remarks>
    public TextRenderingPositionMode? TextRenderingPositionMode
    {
        get => ContainedText.OverrideTextRenderingPositionMode;
        set => ContainedText.OverrideTextRenderingPositionMode = value;
    }

    /// <summary>
    /// Gets or sets the raw text content displayed by the control. This is the value before line wrapping and bbcode parsing has been applied.
    /// </summary>
    /// <remarks>
    /// Setting this property updates the displayed text and may trigger layout changes if the text
    /// size affects the control's dimensions. If the control's width is set relative to its children and no maximum
    /// width is specified, the text will not be line-wrapped.
    /// If a <see cref="Gum.Localization.LocalizationService"/> is registered, the assigned value is passed through
    /// <see cref="Gum.Localization.LocalizationService.Translate"/> before being applied. To bypass translation
    /// (for example, for user-entered text), use <see cref="SetTextNoTranslate"/> instead.
    /// </remarks>
    public string? Text
    {
        get
        {
            return ContainedText.RawText;
        }
        set
        {
#if SKIA
            // Skia's own SetProperty("Text", ...) dispatch (Runtimes/SkiaGum/CustomSetPropertyOnRenderable.cs)
            // parses BBCode lazily at render time rather than eagerly like the shared SetText helper below,
            // so it isn't safe to redirect here yet -- tracked as a separate follow-up on #3706.
            var widthBefore = ContainedText.WrappedTextWidth;
            var heightBefore = ContainedText.WrappedTextHeight;
            if (this.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToChildren)
            {
                if (this.MaxWidth == null)
                {
                    // make it have no line wrap width before assignign the text:
                    ContainedText.Width = null;
                }
                else
                {
                    ContainedText.Width = this.MaxWidth;
                }
            }

            // Use SetProperty so it goes through the BBCode-checking methods
            this.SetProperty("Text", value);

            NotifyPropertyChanged();
            var shouldUpdate = widthBefore != ContainedText.WrappedTextWidth || heightBefore != ContainedText.WrappedTextHeight;
            if (shouldUpdate)
            {
                UpdateLayout(
                    Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentWidthHeightDependOnChildren |
                    Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentStacks, int.MaxValue / 2);
            }
#else
            CustomSetPropertyOnRenderableType.SetText(ContainedText, this, "Text", value);
            NotifyPropertyChanged();
#endif
        }
    }

    /// <summary>
    /// Sets the text without applying localization/translation. Equivalent to calling
    /// <c>SetProperty("TextNoTranslate", value)</c>.
    /// </summary>
    /// <remarks>
    /// This is a method rather than a property because the "no translate" state is not preserved on
    /// the underlying text renderable — only the final string is stored. A corresponding getter would
    /// have no way to distinguish translated from untranslated text, so a property would be misleading.
    /// Use this for text that should not be localized, such as user-entered input in a TextBox.
    /// </remarks>
    public void SetTextNoTranslate(string? value)
    {
#if SKIA
        // See the Text setter above for why Skia stays on the SetProperty path for now.
        var widthBefore = ContainedText.WrappedTextWidth;
        var heightBefore = ContainedText.WrappedTextHeight;
        if (this.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToChildren)
        {
            if (this.MaxWidth == null)
            {
                ContainedText.Width = null;
            }
            else
            {
                ContainedText.Width = this.MaxWidth;
            }
        }

        this.SetProperty("TextNoTranslate", value);

        NotifyPropertyChanged(nameof(Text));
        var shouldUpdate = widthBefore != ContainedText.WrappedTextWidth || heightBefore != ContainedText.WrappedTextHeight;
        if (shouldUpdate)
        {
            UpdateLayout(
                Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentWidthHeightDependOnChildren |
                Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentStacks, int.MaxValue / 2);
        }
#else
        CustomSetPropertyOnRenderableType.SetText(ContainedText, this, "TextNoTranslate", value);
        NotifyPropertyChanged(nameof(Text));
#endif
    }

    bool _localizeText = true;

    /// <summary>
    /// Whether this instance's <see cref="Text"/> is translated through the active
    /// <see cref="Gum.Localization.LocalizationService"/>. Defaults to true.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="SetTextNoTranslate"/>, which bypasses translation for a single assignment,
    /// this is a persistent, per-instance setting - useful for elements whose text is never
    /// translatable (e.g. numeric readouts). Toggling it re-derives the currently assigned text
    /// under the new setting; it does not affect text assigned via <see cref="SetTextNoTranslate"/>.
    /// </remarks>
    public bool LocalizeText
    {
        get => _localizeText;
        set
        {
            if (_localizeText != value)
            {
                _localizeText = value;
                string? originalText = CustomSetPropertyOnRenderableType.TryGetLocalizationKey(this);
                if (originalText != null)
                {
                    this.Text = originalText;
                }
            }
        }
    }

    /// <summary>
    /// The lines of text after wrapping and bbcode parsing have been applied.
    /// </summary>
    public IReadOnlyList<string> WrappedText => ContainedText.WrappedText;

#if !RAYLIB && !SKIA
    /// <summary>
    /// Controls the draw order of letters within a line, which determines how
    /// adjacent glyphs overlap. See <see cref="Gum.Graphics.OverlapDirection"/>
    /// for details. Defaults to <see cref="OverlapDirection.RightOnTop"/>.
    /// </summary>
    /// <remarks>
    /// Not supported on Raylib or SkiaGum. Both draw a whole line/run in a single call into their
    /// respective text-rendering library (raylib's <c>DrawTextPro</c>, RichTextKit's
    /// <c>TextBlock.Paint</c>) rather than looping per-glyph the way this XNALIKE path does, so
    /// there's no per-glyph draw-order hook to expose. Intentionally XNALIKE-only.
    /// </remarks>
    public OverlapDirection OverlapDirection
    {
        get => ContainedText.OverlapDirection;
        set => ContainedText.OverlapDirection = value;
    }
#endif

    public override GraphicalUiElement Clone()
    {
        var toReturn = (TextRuntime)base.Clone();

        toReturn._containedText = null;

        return toReturn;
    }

    #region Defaults

    // todo - add more here
    public static string DefaultFont = "Arial";
    public static int DefaultFontSize = 18;

    /// <summary>
    /// Whether <see cref="RegenerateOversampledFont"/> is allowed to regenerate fonts at a higher
    /// raster size than <see cref="FontSize"/> for crisper text under camera zoom. Off by default:
    /// pixel-art games deliberately want blocky/nearest-neighbor text, and whether a project wants
    /// oversampling at all is a project-wide decision, so a single global toggle (not a per-instance
    /// one) is sufficient for now. A per-instance override (letting individual TextRuntimes opt out of
    /// or into oversampling independently of this project-wide default) is a plausible future
    /// extension if a real need for mixed pixel-art/crisp text within one project shows up, but is not
    /// built here.
    /// </summary>
    public static bool UseFontOversampling = false;

    /// <summary>
    /// Indicates whether the font should be assigned during object construction.
    /// </summary>
    /// <remarks>Set this field to <see langword="true"/> to assign the font in the constructor, or to <see
    /// langword="false"/> to defer font assignment until later in the object's lifecycle. This can be set to false
    /// if TextRuntime instances are always given a custom font, so this can prevent unnecessary font loading/assignment.</remarks>
    public static bool AssignFontInConstructor = true;

    /// <summary>
    /// A default Typeface to assign to all new TextRuntime instances during construction.
    /// When set, this takes priority over <see cref="DefaultFont"/> and <see cref="DefaultFontSize"/>.
    /// When null, the default font is constructed from <see cref="DefaultFont"/> and <see cref="DefaultFontSize"/>.
    /// </summary>
#if RAYLIB
    public static Font? DefaultTypeface;

    [Obsolete("Use DefaultTypeface instead.")]
    public static Font? DefaultCustomFont
    {
        get => DefaultTypeface;
        set => DefaultTypeface = value;
    }
#elif SKIA
    public static SKTypeface? DefaultTypeface;
#else
    public static BitmapFont? DefaultTypeface;

    [Obsolete("Use DefaultTypeface instead.")]
    public static BitmapFont? DefaultCustomFont
    {
        get => DefaultTypeface;
        set => DefaultTypeface = value;
    }
#endif

    public float DefaultWidth = 0;
    public float DefaultHeight = 0;

    public DimensionUnitType DefaultWidthUnits = DimensionUnitType.RelativeToChildren;
    public DimensionUnitType DefaultHeightUnits = DimensionUnitType.RelativeToChildren;

    #endregion

    public TextRuntime(bool fullInstantiation = true, SystemManagers? systemManagers = null)
    {
        if (fullInstantiation)
        {
            this.SuspendLayout();
            var textRenderable = new Text(systemManagers ?? SystemManagers.Default);
#if !RAYLIB && !SKIA
            textRenderable.RenderBoundary = false;
#endif
            _containedText = textRenderable;

            SetContainedObject(textRenderable);

            Width = DefaultWidth;
            WidthUnits = DefaultWidthUnits;
            Height = DefaultHeight;
            HeightUnits = DefaultHeightUnits;
            if (AssignFontInConstructor)
            {
                if (DefaultTypeface != null)
                {
#if RAYLIB
                    this.Typeface = DefaultTypeface.Value;
#else
                    this.Typeface = DefaultTypeface;
#endif
                }
                else
                {
                    // The surrounding SuspendLayout/ResumeLayout pair coalesces these two
                    // assignments into a single font load at construction time. For default
                    // Arial-18 that load hits the embedded-resource cache and is free, but
                    // if a consumer overrides DefaultFont/DefaultFontSize to anything not
                    // pre-loaded, every `new TextRuntime()` pays a full font generation
                    // before the caller can configure further properties. There is no
                    // batching mechanism for "build many TextRuntimes, generate once."
                    // TODO: consider a sentinel-default-font scheme that defers the first
                    // load until the element is actually added to a managed root.
                    this.FontSize = DefaultFontSize;
                    this.Font = DefaultFont;
                }
            }
            HasEvents = false;

            textRenderable.RawText = "Hello World";
            this.ResumeLayout();
        }
    }

#if !RAYLIB && !SKIA
    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    // Intentionally NOT widened to RAYLIB/SKIA even though sibling runtimes (SpriteRuntime,
    // ColoredRectangleRuntime, ContainerRuntime, etc.) already expose this wrapper on those
    // platforms and the base method it calls works everywhere -- this member is [Obsolete], and
    // gum-cross-platform-unification's rule is to never widen an obsolete member's footprint even
    // when the "outlier" reasoning would otherwise apply. Narrower footprint wins. (#3709)
    [Obsolete("Use the AddToRoot extension method instead (e.g. myText.AddToRoot()).")]
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);
#endif

    /// <summary>
    /// Returns the index of the character at the specified screen position. This returns the index
    /// within the WrappedText, so to index in, you need to loop through each line.
    /// </summary>
    /// <param name="screenX">The screen x position, usually obtained by Cursor.XRespectingGumZoomAndBounds()</param>
    /// <param name="screenY">The screen y position, usually obtained by Cursor.YRespectingGumZoomAndBounds()</param>
    /// <returns>The index in the WrappedText</returns>
    public int GetCharacterIndexAtPosition(float screenX, float screenY) => ContainedText.GetCharacterIndexAtPosition(screenX, screenY);
}
