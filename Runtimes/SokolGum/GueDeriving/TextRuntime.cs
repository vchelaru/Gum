using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.GueDeriving;

/// <summary>
/// A visual text element which can display a string.
/// </summary>
public class TextRuntime : InteractiveGue
{
    Text? mContainedText;
    Text ContainedText => mContainedText ??= (Text)this.RenderableComponent;

    // Sokol's Text renderable exposes a single Color field rather than separate
    // R/G/B accessors — these shims preserve the shared TextRuntime shape
    // so .gumx assignments of Red/Green/Blue still route cleanly.
    public int Red    { get => ContainedText.Color.R; set { var c = ContainedText.Color; c.R = (byte)value; ContainedText.Color = c; } }
    public int Green  { get => ContainedText.Color.G; set { var c = ContainedText.Color; c.G = (byte)value; ContainedText.Color = c; } }
    public int Blue   { get => ContainedText.Color.B; set { var c = ContainedText.Color; c.B = (byte)value; ContainedText.Color = c; } }
    public int Alpha  { get => ContainedText.Alpha;   set => ContainedText.Alpha = value; }

    public SokolGum.Color Color
    {
        get => ContainedText.Color;
        set { ContainedText.Color = value; NotifyPropertyChanged(); }
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => ContainedText.HorizontalAlignment;
        set { ContainedText.HorizontalAlignment = value; NotifyPropertyChanged(); }
    }

    public VerticalAlignment VerticalAlignment
    {
        get => ContainedText.VerticalAlignment;
        set => ContainedText.VerticalAlignment = value;
    }

    public int? MaxLettersToShow
    {
        get => ContainedText.MaxLettersToShow;
        set => ContainedText.MaxLettersToShow = value;
    }

    public TextOverflowHorizontalMode TextOverflowHorizontalMode
    {
        get => ContainedText.TextOverflowHorizontalMode;
        set { ContainedText.TextOverflowHorizontalMode = value; NotifyPropertyChanged(); }
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

    /// <summary>
    /// Color of the outline drawn behind glyphs when <see cref="OutlineThickness"/>
    /// is > 0. Sokol-only — shared TextRuntime doesn't expose this yet; kept here
    /// because fontstash outlines are drawn at render time from an arbitrary color
    /// rather than baked into a .fnt file. May migrate to shared in a future pass.
    /// </summary>
    public SokolGum.Color OutlineColor
    {
        get => ContainedText.OutlineColor;
        set { ContainedText.OutlineColor = value; NotifyPropertyChanged(); }
    }

    /// <summary>
    /// The fontstash font handle to use directly, bypassing family/size/style
    /// resolution. Analogous to MonoGame's <c>BitmapFont</c> and Raylib's
    /// <c>CustomFont</c> — when assigned, <see cref="UseCustomFont"/> controls
    /// whether this takes priority over the <see cref="Font"/> family lookup.
    /// Assigning this directly (code-only path) also sets ContainedText.Font
    /// immediately without running family resolution.
    /// </summary>
    public SokolGum.Font? CustomFont
    {
        get => ContainedText.Font;
        set
        {
            ContainedText.Font = value;
            NotifyPropertyChanged();
        }
    }

    bool useCustomFont;
    /// <summary>
    /// Whether to use <see cref="CustomFontFile"/> / <see cref="CustomFont"/> to
    /// resolve the font. If false, the font is resolved from
    /// <see cref="Font"/>, <see cref="FontSize"/>, <see cref="IsItalic"/>,
    /// <see cref="IsBold"/>, <see cref="UseFontSmoothing"/>, and
    /// <see cref="OutlineThickness"/>.
    /// </summary>
    public bool UseCustomFont
    {
        get => useCustomFont;
        set { useCustomFont = value; UpdateToFontValues(); }
    }

    string? customFontFile;
    /// <summary>
    /// Path to a custom font file relative to <c>FileManager.RelativeDirectory</c>.
    /// TODO: not yet implemented on Sokol — setting this currently has no effect
    /// beyond triggering a font refresh.
    /// </summary>
    public string? CustomFontFile
    {
        get => customFontFile;
        set { customFontFile = value; UpdateToFontValues(); }
    }

    string font = "";
    /// <summary>
    /// The font family name, such as "Arial". Combined with <see cref="FontSize"/>,
    /// <see cref="IsBold"/>, and <see cref="IsItalic"/> to resolve a fontstash
    /// font at render time via the system font resolver.
    /// </summary>
    public string Font
    {
        get => FontFamily;
        set => FontFamily = value;
    }

    /// <summary>Alias for <see cref="Font"/>, matching the shared TextRuntime shape.</summary>
    public string FontFamily
    {
        get => font;
        set { font = value; UpdateToFontValues(); }
    }

    int fontSize;
    public int FontSize
    {
        get => fontSize;
        set { fontSize = value; UpdateToFontValues(); }
    }

    bool isItalic;
    public bool IsItalic
    {
        get => isItalic;
        set { isItalic = value; UpdateToFontValues(); }
    }

    bool isBold;
    public bool IsBold
    {
        get => isBold;
        set { isBold = value; UpdateToFontValues(); }
    }

    bool useFontSmoothing = true;
    /// <summary>
    /// Matches the shared API surface — fontstash is always antialiased so
    /// the flag is stored but has no effect on Sokol rendering.
    /// </summary>
    public bool UseFontSmoothing
    {
        get => useFontSmoothing;
        set { useFontSmoothing = value; UpdateToFontValues(); }
    }

    int outlineThickness;
    public int OutlineThickness
    {
        get => outlineThickness;
        set { outlineThickness = value; UpdateToFontValues(); }
    }

    public string? Text
    {
        get => ContainedText.RawText;
        set
        {
            // Use SetProperty so it routes through CustomSetPropertyOnRenderable
            // (localization, width-relative-to-children handling).
            this.SetProperty("Text", value);
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// Local font-resolution hook. Does NOT call base <c>UpdateToFontValues</c>
    /// on GraphicalUiElement — those properties/hooks are being refactored
    /// out. Resolves (family, bold, italic) via the atlas's system-font
    /// resolver and assigns to the contained Text renderable. FontSize and
    /// OutlineThickness flow directly to the renderable since fontstash
    /// takes size at draw time.
    /// </summary>
    private void UpdateToFontValues()
    {
        ContainedText.FontSize = fontSize;
        ContainedText.OutlineThickness = outlineThickness;

        if (useCustomFont)
        {
            // CustomFont is already ContainedText.Font — nothing to resolve.
            // TODO: load from CustomFontFile when path is set.
            return;
        }

        if (!string.IsNullOrEmpty(font))
        {
            var atlas = (ISystemManagers.Default as global::RenderingLibrary.SystemManagers)?.Fonts;
            var resolved = atlas?.GetOrLoadFont(font, isBold, isItalic);
            if (resolved is not null)
            {
                ContainedText.Font = resolved;
            }
            // TODO: fall back to a bundled default TTF when resolved is null
            // so missing system fonts don't produce invisible text.
        }
    }

    #region Defaults

    public static string DefaultFont = "Arial";
    public static int DefaultFontSize = 18;
    public static bool AssignFontInConstructor = true;
    public static SokolGum.Font? DefaultCustomFont;

    public float DefaultWidth = 0;
    public float DefaultHeight = 0;
    public Gum.DataTypes.DimensionUnitType DefaultWidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
    public Gum.DataTypes.DimensionUnitType DefaultHeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

    #endregion

    public TextRuntime(bool fullInstantiation = true)
    {
        if (!fullInstantiation)
        {
            return;
        }

        this.SuspendLayout();
        var textRenderable = new Text();
        mContainedText = textRenderable;
        SetContainedObject(textRenderable);

        Width = DefaultWidth;
        WidthUnits = DefaultWidthUnits;
        Height = DefaultHeight;
        HeightUnits = DefaultHeightUnits;

        if (AssignFontInConstructor)
        {
            if (DefaultCustomFont is not null)
            {
                CustomFont = DefaultCustomFont;
            }
            else
            {
                FontSize = DefaultFontSize;
                Font = DefaultFont;
            }
        }
        HasEvents = false;

        textRenderable.RawText = "Hello World";
        this.ResumeLayout();
    }
}
