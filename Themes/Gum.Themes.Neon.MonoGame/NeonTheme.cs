using System.IO;
using System.Reflection;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using GumRuntime;
#if !RAYLIB
using Microsoft.Xna.Framework.Graphics;
#endif
using RenderingLibrary.Graphics.Fonts;

namespace Gum.Themes.Neon;

/// <summary>
/// Entry point for the Neon / Cyberpunk theme. Call <see cref="Apply"/> once
/// after initializing Gum to register the bundled Share Tech Mono / Orbitron
/// fonts, configure the shared <see cref="Styling.ActiveStyle"/> tokens, and
/// install the Neon visuals as the default templates for Forms controls.
/// </summary>
public static class NeonTheme
{
    /// <summary>
    /// Family name the bundled Share Tech Mono TTF is registered under. This
    /// is the body typeface from the CSS spec (<c>--ff: 'Share Tech Mono'</c>).
    /// Use as the <c>Font</c> property on any TextRuntime for the theme font.
    /// </summary>
    public const string FontFamily = "Share Tech Mono";

    /// <summary>
    /// Family name the bundled title typeface (Orbitron) is registered under.
    /// CSS spec uses Orbitron on <c>.nc-win-title</c> and <c>.nc-hdr-title</c>
    /// — only the Window title bar in this theme. Regular slot is registered
    /// from Orbitron-Regular; Bold from Orbitron-Bold; Orbitron-Black is also
    /// embedded for consumers that want the 900-weight directly via a Font
    /// override (e.g. a custom hero label).
    /// </summary>
    public const string TitleFontFamily = "Orbitron";

    /// <summary>
    /// Family name the bundled icon font (DejaVu Sans Mono) is registered
    /// under. Used for glyphs Share Tech Mono doesn't cover — check marks,
    /// dropdown chevrons, scroll-bar arrows (Dingbats and Geometric Shapes).
    /// </summary>
    public const string IconFontFamily = "Neon Icons";

    /// <summary>
    /// Default text size used by the theme. Matches the source mockup's
    /// <c>--fs</c> token (13px).
    /// </summary>
    public const int FontSize = 13;

    /// <summary>
    /// Applies the Neon theme: wires KernSmith as the in-memory font creator,
    /// registers the bundled Share Tech Mono / Orbitron / DejaVu Sans Mono
    /// TTFs, populates <see cref="Styling.ActiveStyle"/> with the Neon color
    /// and text tokens, and registers the theme's visuals as the default
    /// templates for Forms controls.
    /// </summary>
    public static void Apply()
    {
        ThemePlatform.WireInMemoryFontCreator();

        // Pre-register the icon glyphs the theme renders as Text rather than
        // as sprite-sheet icons. KernSmith bakes only the characters it has
        // been told about, so anything outside ASCII has to be declared before
        // the first font generation. The Neon theme uses:
        //   ✓        — CheckBox check (Dingbats)
        //   ✕        — Window close glyph (Dingbats)
        //   ▼ ▲ ◀ ▶  — ComboBox arrow + ScrollBar buttons (Geometric Shapes)
        BmfcSave.AddCharacters("✓✕▼▲◀▶");

        // Neon's visuals build their bodies out of shape-backed
        // RectangleRuntime / CircleRuntime instances. On MonoGame/KNI this
        // requires the Apos.Shapes ShapeRenderer to be initialized; on raylib
        // the shapes render natively and this is a no-op.
        ThemeShapePlatform.InitializeShapeRenderer();

        RegisterBundledFonts();

        ConfigureStyling();

        RegisterVisuals();
    }

#if !RAYLIB
    /// <summary>
    /// Backwards-compatible overload retained for existing MonoGame/KNI callers. The graphics
    /// device is now resolved internally from the active Gum renderer, so the argument is ignored;
    /// prefer the parameterless <see cref="Apply()"/>.
    /// </summary>
    public static void Apply(GraphicsDevice graphicsDevice) => Apply();
#endif

    private static void RegisterBundledFonts()
    {
        // Share Tech Mono ships only a Regular weight — no Bold / Italic /
        // BoldItalic faces exist. Anything mapping to Gum's IsBold slot just
        // falls back to Regular (a faux-bold via shader, if KernSmith does
        // that; otherwise indistinguishable from Regular). The CSS spec uses
        // a single weight for body text so this matches.
        RegisterEmbeddedFont(FontFamily, "ShareTechMono-Regular.ttf", style: null);

        // Orbitron — title typeface. Regular + Bold so Gum's IsBold slot
        // resolves to the 700-weight cut used for window title bars. Black
        // (900) is embedded too, addressable by consumers via a Font override
        // if they want the heaviest weight.
        RegisterEmbeddedFont(TitleFontFamily, "Orbitron-Regular.ttf", style: null);
        RegisterEmbeddedFont(TitleFontFamily, "Orbitron-Bold.ttf", style: "Bold");

        // Icon font registered under a distinct family name so visual code
        // addresses it explicitly via NeonTheme.IconFontFamily.
        RegisterEmbeddedFont(IconFontFamily, "DejaVuSansMono.ttf", style: null);
    }

    private static void RegisterEmbeddedFont(string family, string fileName, string? style)
    {
        // Resource prefix is derived from the assembly name so this code works
        // in both the MonoGame and KNI variants without forking. The KNI
        // csproj re-embeds the TTFs via <Link> so the resource path inside
        // that assembly is "<assembly-name>.Content.Fonts.<file>".
        Assembly assembly = typeof(NeonTheme).Assembly;
        string resourceName = $"{assembly.GetName().Name}.Content.Fonts.{fileName}";

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Embedded font resource '{resourceName}' not found. " +
                $"Verify the .ttf is included as <EmbeddedResource> in the theme csproj.");
        }

        using MemoryStream buffer = new MemoryStream();
        stream.CopyTo(buffer);
        byte[] fontBytes = buffer.ToArray();

        ThemePlatform.RegisterFont(family, fontBytes, style);
    }

    private static void ConfigureStyling()
    {
        Styling styling = Styling.ActiveStyle;

        styling.Text.Normal.Clear();
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "Font", Type = "string", Value = FontFamily });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "FontSize", Type = "int", Value = FontSize });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "IsBold", Type = "bool", Value = false });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "IsItalic", Type = "bool", Value = false });

        // Share Tech Mono has no Bold face; Strong still tracks the same
        // family — Gum's IsBold flag resolves through KernSmith's "Bold" slot
        // which simply isn't registered for Share Tech Mono, so it falls back
        // to Regular. This matches the CSS spec (body text is one weight).
        styling.Text.Strong.Clear();
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "Font", Type = "string", Value = FontFamily });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "FontSize", Type = "int", Value = FontSize });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsBold", Type = "bool", Value = true });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsItalic", Type = "bool", Value = false });

        styling.Colors.TextPrimary = NeonColors.Text;
        styling.Colors.TextMuted = NeonColors.Muted;
        styling.Colors.Primary = NeonColors.Surface1;
        styling.Colors.Accent = NeonColors.Accent;
    }

    private static void RegisterVisuals()
    {
        FrameworkElement.DefaultFormsTemplates[typeof(Button)] =
            new VisualTemplate((_, c) => new ButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] =
            new VisualTemplate((_, c) => new CheckBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(RadioButton)] =
            new VisualTemplate((_, c) => new RadioButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(TextBox)] =
            new VisualTemplate((_, c) => new TextBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(PasswordBox)] =
            new VisualTemplate((_, c) => new PasswordBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Slider)] =
            new VisualTemplate((_, c) => new SliderVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBox)] =
            new VisualTemplate((_, c) => new ListBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBoxItem)] =
            new VisualTemplate((_, c) => new ListBoxItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollBar)] =
            new VisualTemplate((_, c) => new ScrollBarVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] =
            new VisualTemplate((_, c) => new ScrollViewerVisual(tryCreateFormsObject: c));

        // Label gets its color from Styling.ActiveStyle.Colors.TextPrimary
        // (set in ConfigureStyling), so the V3 LabelVisual already renders
        // Neon text color without a subclass.
        FrameworkElement.DefaultFormsTemplates[typeof(Label)] =
            new VisualTemplate((_, c) => new Gum.Forms.DefaultVisuals.V3.LabelVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ComboBox)] =
            new VisualTemplate((_, c) => new ComboBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Window)] =
            new VisualTemplate((_, c) => new WindowVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Splitter)] =
            new VisualTemplate((_, c) => new SplitterVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Menu)] =
            new VisualTemplate((_, c) => new MenuVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(MenuItem)] =
            new VisualTemplate((_, c) => new MenuItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ToggleButton)] =
            new VisualTemplate((_, c) => new ToggleButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Tooltip)] =
            new VisualTemplate((_, c) => new TooltipVisual(tryCreateFormsObject: c));

        // Controls Neon does not restyle are pinned to their stock V3 visuals so
        // NeonTheme.Apply fully specifies the template set. FrameworkElement.DefaultFormsTemplates
        // is global static state; without re-registering these, re-applying a theme at runtime
        // (theme switching) would leave a previously-applied theme's visual in place for these
        // controls. These match what a single-theme Neon run already falls back to.
        FrameworkElement.DefaultFormsTemplates[typeof(ItemsControl)] =
            new VisualTemplate((_, c) => new Gum.Forms.DefaultVisuals.V3.ItemsControlVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Gum.Forms.Controls.Games.DialogBox)] =
            new VisualTemplate((_, c) => new Gum.Forms.DefaultVisuals.V3.DialogBoxVisual(tryCreateFormsObject: c));
    }
}
