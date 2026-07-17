using System.IO;
using System.Reflection;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using GumRuntime;
#if !RAYLIB && !SKIA
using Microsoft.Xna.Framework.Graphics;
#endif
using RenderingLibrary.Graphics.Fonts;
using V3 = Gum.Forms.DefaultVisuals.V3;

namespace Gum.Themes.Hazard;

/// <summary>
/// Entry point for the Hazard theme - an industrial space-salvage HUD look
/// (hazard-yellow on warm near-black, muted-gold borders, square-cornered chrome,
/// Saira Condensed type). Call <see cref="Apply"/> once after initializing Gum to
/// register the bundled fonts, configure the shared
/// <see cref="Styling.ActiveStyle"/> tokens, and install the theme's visuals as the
/// default templates for Forms controls. Colors come from
/// <see cref="HazardStyling.ActiveStyle"/>'s <see cref="HazardColors"/>.
/// </summary>
public static class HazardTheme
{
    /// <summary>
    /// Fixed family name the bundled Saira Condensed TTFs are registered under.
    /// Font <em>registration</em> is intentionally decoupled from font
    /// <em>selection</em> (<see cref="HazardText.FontFamily"/>, mutable) —
    /// reassigning the selection before <see cref="Apply"/> can never corrupt
    /// this registration.
    /// </summary>
    internal const string BundledFontFamily = "Saira Condensed";

    /// <summary>
    /// Fixed family name the bundled icon font (DejaVu Sans Mono) is registered
    /// under. See <see cref="BundledFontFamily"/> for why this is a fixed
    /// constant rather than the mutable <see cref="HazardText.IconFontFamily"/>
    /// selection.
    /// </summary>
    internal const string BundledIconFontFamily = "Saira Condensed Icons";

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
        // Add every non-ASCII glyph the visuals render: ✓ (CheckBox check) and ▼
        // (ComboBox dropdown arrow). They live in the bundled icon font, not the
        // body font (Saira Condensed lacks the Dingbats / Geometric Shapes blocks).
        BmfcSave.AddCharacters("✓▼");

        // The visuals build their bodies from shape-backed RectangleRuntime /
        // CircleRuntime instances; on XNA-like backends those require ShapeRenderer
        // to be initialized (no-op on raylib, which renders shapes natively).
        ThemeShapePlatform.InitializeShapeRenderer();

        RegisterBundledFonts();

        ConfigureStyling();

        RegisterVisuals();
    }

#if !RAYLIB && !SKIA
    /// <summary>
    /// Backwards-compatible overload retained for existing MonoGame/KNI callers. The graphics
    /// device is now resolved internally from the active Gum renderer, so the argument is ignored;
    /// prefer the parameterless <see cref="Apply()"/>.
    /// </summary>
    public static void Apply(GraphicsDevice graphicsDevice) => Apply();
#endif

    private static void RegisterBundledFonts()
    {
        // Saira Condensed Regular -> Normal; SemiBold (600) maps to Gum's IsBold
        // slot, matching the design's uppercase label/heading weight. Saira
        // Condensed ships no italic, and the Forms styling never requests one.
        RegisterEmbeddedFont(BundledFontFamily, "SairaCondensed-Regular.ttf", style: null);
        RegisterEmbeddedFont(BundledFontFamily, "SairaCondensed-SemiBold.ttf", style: "Bold");

        // Icon font registered under a distinct family name so visual code addresses
        // it explicitly via HazardStyling.ActiveStyle.Text.IconFontFamily.
        RegisterEmbeddedFont(BundledIconFontFamily, "DejaVuSansMono.ttf", style: null);
    }

    private static void RegisterEmbeddedFont(string family, string fileName, string? style)
    {
        // Resource prefix is derived from the assembly name so this code works in
        // both the MonoGame and KNI variants without forking. The KNI csproj
        // re-embeds the TTFs via <Link> so the resource path inside that assembly is
        // "<assembly-name>.Content.Fonts.<file>".
        //
        // CLONE GOTCHA: this resolves only when the assembly name equals the
        // project's root namespace (the fonts are embedded under RootNamespace but
        // looked up here by AssemblyName). Both default from the project name, so
        // leave <AssemblyName> and <RootNamespace> UNSET in the csproj. If a clone
        // sets one and not the other, the build still succeeds but this throws
        // FileNotFoundException at runtime - so run the theme, don't just build it.
        Assembly assembly = typeof(HazardTheme).Assembly;
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
    // headlessly in a unit test. See Gum.Themes.Hazard.MonoGame.csproj's InternalsVisibleTo.
    internal static void ConfigureStyling()
    {
        Styling styling = Styling.ActiveStyle;
        HazardText text = HazardStyling.ActiveStyle.Text;

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
        // also color the controls left at their stock V3 visual (e.g. Label). We push
        // only the 4-token guardrail (TextPrimary, TextMuted, Primary, Accent) and the
        // visuals read the rest of the palette from HazardStyling.ActiveStyle.Colors directly.
        HazardColors colors = HazardStyling.ActiveStyle.Colors;
        styling.Colors.TextPrimary = colors.TextPrimary;
        styling.Colors.TextMuted = colors.TextMuted;
        styling.Colors.Primary = colors.Primary;
        styling.Colors.Accent = colors.Accent;
    }

    private static void RegisterVisuals()
    {
        // ---- Styled by this theme -------------------------------------------
        FrameworkElement.DefaultFormsTemplates[typeof(Button)] =
            new VisualTemplate((_, c) => new ButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(TextBox)] =
            new VisualTemplate((_, c) => new TextBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(PasswordBox)] =
            new VisualTemplate((_, c) => new PasswordBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Slider)] =
            new VisualTemplate((_, c) => new SliderVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] =
            new VisualTemplate((_, c) => new CheckBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(RadioButton)] =
            new VisualTemplate((_, c) => new RadioButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ToggleButton)] =
            new VisualTemplate((_, c) => new ToggleButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ComboBox)] =
            new VisualTemplate((_, c) => new ComboBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBox)] =
            new VisualTemplate((_, c) => new ListBoxVisual(tryCreateFormsObject: c));

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
