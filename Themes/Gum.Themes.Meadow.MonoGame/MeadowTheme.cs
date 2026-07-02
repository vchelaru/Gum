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

namespace Gum.Themes.Meadow;

/// <summary>
/// Entry point for the Meadow theme — a cozy cottagecore look in cream, sage,
/// peach, teal, sky-blue, and coral. Call <see cref="Apply"/> once after
/// initializing Gum to register the bundled fonts, configure the shared
/// <see cref="Styling.ActiveStyle"/> tokens, and install the Meadow visuals as
/// the default templates for Forms controls.
/// <para>
/// Meadow ships two user-facing typefaces: <b>Baloo 2</b> (rounded display face)
/// for buttons, check / radio labels, and window titles — the default
/// <see cref="MeadowText.FontFamily"/> so it flows to controls that read
/// <c>Styling.ActiveStyle.Text</c> — and <b>Quicksand</b>
/// (<see cref="MeadowText.BodyFontFamily"/>) for text-entry / list / menu content,
/// which the relevant visuals opt into explicitly via their <c>TextInstance.Font</c>.
/// </para>
/// </summary>
public static class MeadowTheme
{
    /// <summary>
    /// Fixed family name the bundled Baloo 2 TTFs are registered under. Font
    /// <em>registration</em> is intentionally decoupled from font
    /// <em>selection</em> (<see cref="MeadowText.FontFamily"/>, mutable) —
    /// reassigning the selection before <see cref="Apply"/> can never corrupt
    /// this registration.
    /// </summary>
    internal const string BundledFontFamily = "Baloo 2";

    /// <summary>
    /// Fixed family name the bundled Quicksand TTFs are registered under — the
    /// body / data face used by text inputs, list rows, combo fields, and menu
    /// items. See <see cref="BundledFontFamily"/> for why this is a fixed
    /// constant rather than the mutable <see cref="MeadowText.BodyFontFamily"/>
    /// selection.
    /// </summary>
    internal const string BundledBodyFontFamily = "Quicksand";

    /// <summary>
    /// Fixed family name the bundled icon font (DejaVu Sans Mono) is registered
    /// under. Used for glyphs the body fonts don't cover — check marks and
    /// dropdown chevrons (Dingbats and Geometric Shapes blocks). See
    /// <see cref="BundledFontFamily"/> for why this is a fixed constant rather
    /// than the mutable <see cref="MeadowText.IconFontFamily"/> selection.
    /// </summary>
    internal const string BundledIconFontFamily = "Meadow Icons";

    /// <summary>
    /// Applies the Meadow theme: wires KernSmith as the in-memory font
    /// creator, registers the bundled Baloo 2 + Quicksand TTFs (plus the DejaVu
    /// icon font), populates <see cref="Styling.ActiveStyle"/> with the Meadow
    /// color and text tokens, and registers the theme's visuals as the default
    /// templates for Forms controls.
    /// </summary>
    public static void Apply()
    {
        ThemePlatform.WireInMemoryFontCreator();

        // Pre-register the icon glyphs the theme renders as Text rather than as
        // sprite-sheet icons. KernSmith bakes only the characters it has been
        // told about, so anything outside ASCII has to be declared before the
        // first font generation. ✓ (Dingbats) and ▼ (Geometric Shapes) are the
        // only icon glyphs Meadow needs; both live in the bundled DejaVu
        // Sans Mono icon font (the body fonts don't cover them).
        BmfcSave.AddCharacters("✓▼");

        // Meadow's visuals build their bodies out of Apos.Shapes-backed RectangleRuntime /
        // CircleRuntime instances, which require ShapeRenderer to be initialized on XNA-like
        // backends. The shim no-ops on raylib (shapes render natively there) so consumers don't
        // need to know the theme uses shapes internally.
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
        // Baloo 2 (display) and Quicksand (body) are both weight-only families
        // with no italic cut — Meadow's design uses neither italic. We still
        // register the Italic / BoldItalic style slots (pointing at the upright
        // SemiBold / Bold TTFs) so any control that requests an italic style
        // resolves to a real font rather than risking a missing-style lookup;
        // the glyphs simply render upright. SemiBold (600) fills Gum's Normal
        // slot — Baloo's lighter cuts read too thin for this chunky, cozy look.
        RegisterEmbeddedFont(BundledFontFamily, "Baloo2-SemiBold.ttf", style: null);
        RegisterEmbeddedFont(BundledFontFamily, "Baloo2-Bold.ttf", style: "Bold");
        RegisterEmbeddedFont(BundledFontFamily, "Baloo2-SemiBold.ttf", style: "Italic");
        RegisterEmbeddedFont(BundledFontFamily, "Baloo2-Bold.ttf", style: "BoldItalic");

        RegisterEmbeddedFont(BundledBodyFontFamily, "Quicksand-Medium.ttf", style: null);
        RegisterEmbeddedFont(BundledBodyFontFamily, "Quicksand-Bold.ttf", style: "Bold");
        RegisterEmbeddedFont(BundledBodyFontFamily, "Quicksand-Medium.ttf", style: "Italic");
        RegisterEmbeddedFont(BundledBodyFontFamily, "Quicksand-Bold.ttf", style: "BoldItalic");

        // Icon font registered under a distinct family name so visual code
        // addresses it explicitly via MeadowStyling.ActiveStyle.Text.IconFontFamily.
        RegisterEmbeddedFont(BundledIconFontFamily, "DejaVuSansMono.ttf", style: null);
    }

    private static void RegisterEmbeddedFont(string family, string fileName, string? style)
    {
        // Resource prefix is derived from the assembly name so this code works
        // in both the MonoGame and KNI variants without forking. The KNI csproj
        // re-embeds the TTFs via <Link> so the resource path inside that
        // assembly is "<assembly-name>.Content.Fonts.<file>".
        Assembly assembly = typeof(MeadowTheme).Assembly;
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

    // Internal (not private) so Tests/Gum.Themes.Tests can exercise the guardrail-token sync
    // (TextPrimary/TextMuted/Primary/Accent → V3.Styling.ActiveStyle.Colors) without going
    // through Apply(), which requires a real GraphicsDevice for font wiring and can't run
    // headlessly in a unit test. See Gum.Themes.Meadow.MonoGame.csproj's InternalsVisibleTo.
    internal static void ConfigureStyling()
    {
        Styling styling = Styling.ActiveStyle;
        MeadowText text = MeadowStyling.ActiveStyle.Text;

        styling.Text.Normal.Clear();
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "Font", Type = "string", Value = text.FontFamily });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "FontSize", Type = "int", Value = text.FontSize });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "IsBold", Type = "bool", Value = false });
        styling.Text.Normal.Variables.Add(
            new VariableSave { Name = "IsItalic", Type = "bool", Value = false });

        styling.Text.Strong.Clear();
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "Font", Type = "string", Value = text.FontFamily });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "FontSize", Type = "int", Value = text.FontSize });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsBold", Type = "bool", Value = true });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsItalic", Type = "bool", Value = false });

        // The four Styling.Colors slots that overlap with V3's vocabulary; they
        // also color the controls left at their stock V3 visual (e.g. Label, which
        // therefore renders teal-dark in the default Baloo 2 display face). We push
        // only the 4-token guardrail (TextPrimary, TextMuted, Primary, Accent) and the
        // visuals read the rest of the palette from MeadowStyling.ActiveStyle.Colors directly.
        MeadowColors colors = MeadowStyling.ActiveStyle.Colors;
        styling.Colors.TextPrimary = colors.TextPrimary;
        styling.Colors.TextMuted = colors.TextMuted;
        styling.Colors.Primary = colors.Primary;
        styling.Colors.Accent = colors.Accent;
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

        // Label gets its color from Styling.ActiveStyle.Colors.TextPrimary (set
        // in ConfigureStyling), so the V3 LabelVisual already renders Meadow
        // text color without a subclass.
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

        // Controls Meadow does not restyle are pinned to their stock V3 visuals so
        // MeadowTheme.Apply fully specifies the template set. FrameworkElement.DefaultFormsTemplates
        // is global static state; without re-registering these, re-applying a theme at runtime
        // (theme switching) would leave a previously-applied theme's visual in place for these
        // controls. These match what a single-theme Meadow run already falls back to.
        FrameworkElement.DefaultFormsTemplates[typeof(ItemsControl)] =
            new VisualTemplate((_, c) => new Gum.Forms.DefaultVisuals.V3.ItemsControlVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Gum.Forms.Controls.Games.DialogBox)] =
            new VisualTemplate((_, c) => new Gum.Forms.DefaultVisuals.V3.DialogBoxVisual(tryCreateFormsObject: c));
    }
}
