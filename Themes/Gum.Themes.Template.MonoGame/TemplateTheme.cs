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
using V3 = Gum.Forms.DefaultVisuals.V3;

namespace Gum.Themes.Template;

/// <summary>
/// Entry point for the theme. Call <see cref="Apply"/> once after initializing Gum
/// to register the bundled fonts, configure the shared
/// <see cref="Styling.ActiveStyle"/> tokens, and install the theme's visuals as the
/// default templates for Forms controls. Colors come from
/// <see cref="TemplateStyling.ActiveStyle"/>'s <see cref="TemplateColors"/>.
/// <para>
/// This is the clone-to-build theme template - see <c>README.md</c> for the recipe
/// (rename, palette, fonts, restyle). The code docs here describe what each piece
/// does so a clone keeps them as-is rather than scrubbing template-only prose.
/// </para>
/// </summary>
public static class TemplateTheme
{
    /// <summary>
    /// Fixed family name the bundled DISPLAY TTFs are registered under. Font
    /// <em>registration</em> is intentionally decoupled from font <em>selection</em>
    /// (<see cref="TemplateText.FontFamily"/>, mutable) — reassigning the selection
    /// before <see cref="Apply"/> can never corrupt this registration.
    /// <para>
    /// A theme can ship more than one family. This template demonstrates the common
    /// "display + body" split: a personality face for buttons / labels / titles
    /// (this <see cref="BundledFontFamily"/>) and a quieter face for typed / list
    /// content (<see cref="BundledBodyFontFamily"/>). Controls that render entered or
    /// tabular text opt into the body face explicitly via
    /// <c>TextInstance.Font = TemplateStyling.ActiveStyle.Text.BodyFontFamily</c>
    /// (see TextBox, ComboBox, ListBoxItem, MenuItem, Tooltip in this theme).
    /// </para></summary>
    internal const string BundledFontFamily = "DM Mono";

    /// <summary>Fixed family name the bundled BODY TTFs are registered under - the
    /// quieter face used for typed / list / menu content. Opt into it per visual via
    /// <c>TextInstance.Font = TemplateStyling.ActiveStyle.Text.BodyFontFamily</c>; the
    /// default selection is <see cref="BundledFontFamily"/>. Delete this (and its
    /// registration + the per-visual opt-ins) if your design uses a single family.</summary>
    internal const string BundledBodyFontFamily = "Nunito";

    /// <summary>Fixed family name the bundled icon font (DejaVu Sans Mono) is
    /// registered under. See <see cref="BundledFontFamily"/> for why this is a fixed
    /// constant rather than the mutable <see cref="TemplateText.IconFontFamily"/>
    /// selection.</summary>
    internal const string BundledIconFontFamily = "DM Mono Icons";

    /// <summary>
    /// Applies the theme: wires KernSmith as the in-memory font creator, registers
    /// the bundled fonts, populates <see cref="Styling.ActiveStyle"/> with the
    /// theme's color and text tokens, and registers the theme's visuals as the
    /// default templates for Forms controls.
    /// </summary>
    public static void Apply()
    {
        ThemePlatform.WireInMemoryFontCreator();

        // Pre-register any glyphs visuals render as Text rather than as sprite-sheet
        // icons. KernSmith bakes only the characters it has been told about, so
        // anything outside ASCII must be declared before the first font generation.
        // Add every non-ASCII glyph your visuals use (e.g. ✓ for CheckBox, ▼ for
        // ComboBox). They live in the bundled icon font, not the body font.
        BmfcSave.AddCharacters("✓▼◀▶");

        // The visuals build their bodies from Apos.Shapes-backed RectangleRuntime /
        // CircleRuntime instances, which require ShapeRenderer to be initialized on
        // XNA-like backends. The shim no-ops on raylib (shapes render natively there)
        // and guards against the host app having already initialized it.
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
        // Display family. DM Mono ships no true 700-weight Bold, so Medium (500)
        // maps to Gum's IsBold slot. Swap these filenames + the family/style
        // mapping for your font. Registered under the fixed Bundled* constant,
        // never the mutable TemplateText.FontFamily selection - see BundledFontFamily.
        RegisterEmbeddedFont(BundledFontFamily, "DMMono-Regular.ttf", style: null);
        RegisterEmbeddedFont(BundledFontFamily, "DMMono-Medium.ttf", style: "Bold");
        RegisterEmbeddedFont(BundledFontFamily, "DMMono-Italic.ttf", style: "Italic");
        RegisterEmbeddedFont(BundledFontFamily, "DMMono-MediumItalic.ttf", style: "BoldItalic");

        // Body family (the second family - delete if your design uses one font).
        // Nunito has no italic cut, so the Italic / BoldItalic style slots point at
        // the upright Regular / Bold files: a stray italic request then resolves to
        // a real font (rendered upright) instead of risking a missing-style lookup.
        RegisterEmbeddedFont(BundledBodyFontFamily, "Nunito-Regular.ttf", style: null);
        RegisterEmbeddedFont(BundledBodyFontFamily, "Nunito-Bold.ttf", style: "Bold");
        RegisterEmbeddedFont(BundledBodyFontFamily, "Nunito-Regular.ttf", style: "Italic");
        RegisterEmbeddedFont(BundledBodyFontFamily, "Nunito-Bold.ttf", style: "BoldItalic");

        // Icon font registered under a distinct family name so visual code addresses
        // it explicitly via TemplateStyling.ActiveStyle.Text.IconFontFamily.
        RegisterEmbeddedFont(BundledIconFontFamily, "DejaVuSansMono.ttf", style: null);
    }

    private static void RegisterEmbeddedFont(string family, string fileName, string? style)
    {
        // Resource prefix is derived from the assembly name so this code works in
        // all three variants (MonoGame / KNI / raylib) without forking. The .Kni and
        // .Raylib csprojs re-embed the TTFs via <Link> so the resource path inside
        // each assembly is "<assembly-name>.Content.Fonts.<file>".
        //
        // CLONE GOTCHA: this resolves only when the assembly name equals the
        // project's root namespace (the fonts are embedded under RootNamespace but
        // looked up here by AssemblyName). Both default from the project name, so
        // leave <AssemblyName> and <RootNamespace> UNSET in the csproj. If a clone
        // sets one and not the other, the build still succeeds but this throws
        // FileNotFoundException at runtime - so run the theme, don't just build it.
        Assembly assembly = typeof(TemplateTheme).Assembly;
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
    // headlessly in a unit test. See Gum.Themes.Template.MonoGame.csproj's InternalsVisibleTo.
    internal static void ConfigureStyling()
    {
        Styling styling = Styling.ActiveStyle;
        TemplateText text = TemplateStyling.ActiveStyle.Text;

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

        // The four Styling.Colors slots that overlap with V3's vocabulary; they also color
        // the controls left at their stock V3 visual (e.g. Label). We push only the 4-token
        // guardrail (TextPrimary, TextMuted, Primary, Accent) and the visuals read the rest
        // of the palette from TemplateStyling.ActiveStyle.Colors directly.
        TemplateColors colors = TemplateStyling.ActiveStyle.Colors;
        styling.Colors.TextPrimary = colors.TextPrimary;
        styling.Colors.TextMuted = colors.TextMuted;
        styling.Colors.Primary = colors.Primary;
        styling.Colors.Accent = colors.Accent;
    }

    private static void RegisterVisuals()
    {
        // ---- "Rich" Variants gallery (OPT-IN, not registered) ---------------
        // The Variants\ folder holds richly-decorated alternates of a subset of
        // these controls (pill button with a drop shadow, rounded/glowing
        // CheckBox & RadioButton & inputs, dashed-border ListBox, shadowed circle
        // slider thumb). They reuse this theme's palette and state logic - only
        // the SHAPES differ - and are NOT registered by default, so the Template's
        // flat look stays the default. To adopt a richer look: copy the
        // variant(s) you want into the theme proper (or just uncomment its
        // registration line below), delete the rest, and uncomment the matching
        // FrameworkElement.DefaultFormsTemplates assignment that follows each
        // default registration. Each commented line below shows the exact swap.

        // ---- Styled by this theme -------------------------------------------
        FrameworkElement.DefaultFormsTemplates[typeof(Button)] =
            new VisualTemplate((_, c) => new ButtonVisual(tryCreateFormsObject: c));
        // To use the Rich variant instead: FrameworkElement.DefaultFormsTemplates[typeof(Button)] = new VisualTemplate((_, c) => new Variants.ButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(TextBox)] =
            new VisualTemplate((_, c) => new TextBoxVisual(tryCreateFormsObject: c));
        // To use the Rich variant instead: FrameworkElement.DefaultFormsTemplates[typeof(TextBox)] = new VisualTemplate((_, c) => new Variants.TextBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(PasswordBox)] =
            new VisualTemplate((_, c) => new PasswordBoxVisual(tryCreateFormsObject: c));
        // To use the Rich variant instead: FrameworkElement.DefaultFormsTemplates[typeof(PasswordBox)] = new VisualTemplate((_, c) => new Variants.PasswordBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Slider)] =
            new VisualTemplate((_, c) => new SliderVisual(tryCreateFormsObject: c));
        // To use the Rich variant instead: FrameworkElement.DefaultFormsTemplates[typeof(Slider)] = new VisualTemplate((_, c) => new Variants.SliderVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] =
            new VisualTemplate((_, c) => new CheckBoxVisual(tryCreateFormsObject: c));
        // To use the Rich variant instead: FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] = new VisualTemplate((_, c) => new Variants.CheckBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(RadioButton)] =
            new VisualTemplate((_, c) => new RadioButtonVisual(tryCreateFormsObject: c));
        // To use the Rich variant instead: FrameworkElement.DefaultFormsTemplates[typeof(RadioButton)] = new VisualTemplate((_, c) => new Variants.RadioButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ToggleButton)] =
            new VisualTemplate((_, c) => new ToggleButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ComboBox)] =
            new VisualTemplate((_, c) => new ComboBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBox)] =
            new VisualTemplate((_, c) => new ListBoxVisual(tryCreateFormsObject: c));
        // To use the Rich variant instead: FrameworkElement.DefaultFormsTemplates[typeof(ListBox)] = new VisualTemplate((_, c) => new Variants.ListBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBoxItem)] =
            new VisualTemplate((_, c) => new ListBoxItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollBar)] =
            new VisualTemplate((_, c) => new ScrollBarVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] =
            new VisualTemplate((_, c) => new ScrollViewerVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Menu)] =
            new VisualTemplate((_, c) => new MenuVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(MenuItem)] =
            new VisualTemplate((_, c) => new MenuItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Window)] =
            new VisualTemplate((_, c) => new WindowVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Splitter)] =
            new VisualTemplate((_, c) => new SplitterVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Tooltip)] =
            new VisualTemplate((_, c) => new TooltipVisual(tryCreateFormsObject: c));

        // ---- Not yet styled: pinned to stock V3 visuals ---------------------
        // FrameworkElement.DefaultFormsTemplates is global static state. Registering
        // every control - even the ones this theme doesn't restyle yet - means
        // Apply fully specifies the template set, so re-applying a theme at runtime
        // (theme switching) doesn't leave another theme's visual in place. As you
        // build out the theme, move a control up to the styled block above and give
        // it a YourThemeVisual subclass.
        FrameworkElement.DefaultFormsTemplates[typeof(Label)] =
            new VisualTemplate((_, c) => new V3.LabelVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ItemsControl)] =
            new VisualTemplate((_, c) => new V3.ItemsControlVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Gum.Forms.Controls.Games.DialogBox)] =
            new VisualTemplate((_, c) => new V3.DialogBoxVisual(tryCreateFormsObject: c));
    }
}
