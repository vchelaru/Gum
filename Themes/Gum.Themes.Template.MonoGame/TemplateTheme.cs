using System.IO;
using System.Reflection;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using GumRuntime;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using V3 = Gum.Forms.DefaultVisuals.V3;

namespace Gum.Themes.Template;

/// <summary>
/// Entry point for the Template theme. Call <see cref="Apply"/> once after
/// initializing Gum to register the bundled fonts, configure the shared
/// <see cref="Styling.ActiveStyle"/> tokens, and install the theme's visuals as
/// the default templates for Forms controls.
///
/// This is a starting point for a new theme. To make your own:
/// <list type="number">
/// <item>Clone the two projects (.MonoGame and .Kni), rename <c>Template</c> -&gt;
/// <c>YourTheme</c> throughout (folder names, file names, PackageId, namespace,
/// and the <c>Template*</c> type names).</item>
/// <item>Fill in <see cref="TemplatePalette"/> from your design's CSS.</item>
/// <item>Swap the TTFs in <c>Content\Fonts</c> and update <see cref="FontFamily"/> /
/// <see cref="IconFontFamily"/> and <see cref="RegisterBundledFonts"/>.</item>
/// <item>Restyle the visuals, and promote controls from the "stock V3" block in
/// <see cref="RegisterVisuals"/> to your own styled visual as you build them out.</item>
/// </list>
/// </summary>
public static class TemplateTheme
{
    /// <summary>Family name the bundled body TTFs are registered under. Use this as
    /// the <c>Font</c> value on any TextRuntime to get the theme font.</summary>
    public const string FontFamily = "DM Mono";

    /// <summary>Family name the bundled icon font (DejaVu Sans Mono) is registered
    /// under. Use this for glyphs the body font doesn't cover - check marks, close
    /// buttons, combo/scrollbar arrows (Dingbats and Geometric Shapes blocks).</summary>
    public const string IconFontFamily = "DM Mono Icons";

    /// <summary>Default text size used by the theme.</summary>
    public const int FontSize = 14;

    /// <summary>
    /// Applies the theme: wires KernSmith as the in-memory font creator, registers
    /// the bundled fonts, populates <see cref="Styling.ActiveStyle"/> with the
    /// theme's color and text tokens, and registers the theme's visuals as the
    /// default templates for Forms controls.
    /// </summary>
    /// <param name="graphicsDevice">The active <see cref="GraphicsDevice"/>; required
    /// by KernSmith to rasterize fonts into textures.</param>
    public static void Apply(GraphicsDevice graphicsDevice)
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(graphicsDevice);

        // Pre-register any glyphs visuals render as Text rather than as sprite-sheet
        // icons. KernSmith bakes only the characters it has been told about, so
        // anything outside ASCII must be declared before the first font generation.
        // Add every non-ASCII glyph your visuals use (e.g. ✓ for CheckBox, ▼ for
        // ComboBox). They live in the bundled icon font, not the body font.
        BmfcSave.AddCharacters("✓▼◀▶");

        // The visuals build their bodies from Apos.Shapes-backed RectangleRuntime /
        // CircleRuntime instances, which require ShapeRenderer to be initialized.
        // Guard for the case where the host app already initialized it.
        if (!ShapeRenderer.Self.IsInitialized)
        {
            ShapeRenderer.Self.Initialize();
        }

        RegisterBundledFonts();

        ConfigureStyling();

        RegisterVisuals();
    }

    private static void RegisterBundledFonts()
    {
        // DM Mono ships no true 700-weight Bold, so Medium (500) maps to Gum's
        // IsBold slot. Swap these filenames + the family/style mapping for your font.
        RegisterEmbeddedFont(FontFamily, "DMMono-Regular.ttf", style: null);
        RegisterEmbeddedFont(FontFamily, "DMMono-Medium.ttf", style: "Bold");
        RegisterEmbeddedFont(FontFamily, "DMMono-Italic.ttf", style: "Italic");
        RegisterEmbeddedFont(FontFamily, "DMMono-MediumItalic.ttf", style: "BoldItalic");

        // Icon font registered under a distinct family name so visual code addresses
        // it explicitly via TemplateTheme.IconFontFamily.
        RegisterEmbeddedFont(IconFontFamily, "DejaVuSansMono.ttf", style: null);
    }

    private static void RegisterEmbeddedFont(string family, string fileName, string? style)
    {
        // Resource prefix is derived from the assembly name so this code works in
        // both the MonoGame and KNI variants without forking. The KNI csproj
        // re-embeds the TTFs via <Link> so the resource path inside that assembly is
        // "<assembly-name>.Content.Fonts.<file>".
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

        if (style == null)
        {
            KernSmithFontCreator.RegisterFont(family, fontBytes);
        }
        else
        {
            KernSmithFontCreator.RegisterFont(family, fontBytes, style: style);
        }
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

        styling.Text.Strong.Clear();
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "Font", Type = "string", Value = FontFamily });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "FontSize", Type = "int", Value = FontSize });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsBold", Type = "bool", Value = true });
        styling.Text.Strong.Variables.Add(
            new VariableSave { Name = "IsItalic", Type = "bool", Value = false });

        // The four Styling.Colors slots that overlap with V3's vocabulary. The rest of
        // the palette is read directly from TemplatePalette by the visuals. These four
        // also color the controls left at their stock V3 visual (e.g. Label).
        styling.Colors.TextPrimary = TemplatePalette.Text;
        styling.Colors.TextMuted = TemplatePalette.Muted;
        styling.Colors.Primary = TemplatePalette.Surface1;
        styling.Colors.Accent = TemplatePalette.Accent;
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

        // ---- Not yet styled: pinned to stock V3 visuals ---------------------
        // FrameworkElement.DefaultFormsTemplates is global static state. Registering
        // every control - even the ones this theme doesn't restyle yet - means
        // Apply fully specifies the template set, so re-applying a theme at runtime
        // (theme switching) doesn't leave another theme's visual in place. As you
        // build out the theme, move a control up to the styled block above and give
        // it a YourThemeVisual subclass.
        FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] =
            new VisualTemplate((_, c) => new V3.CheckBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(RadioButton)] =
            new VisualTemplate((_, c) => new V3.RadioButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ToggleButton)] =
            new VisualTemplate((_, c) => new V3.ToggleButtonVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ComboBox)] =
            new VisualTemplate((_, c) => new V3.ComboBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBox)] =
            new VisualTemplate((_, c) => new V3.ListBoxVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ListBoxItem)] =
            new VisualTemplate((_, c) => new V3.ListBoxItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollBar)] =
            new VisualTemplate((_, c) => new V3.ScrollBarVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] =
            new VisualTemplate((_, c) => new V3.ScrollViewerVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Label)] =
            new VisualTemplate((_, c) => new V3.LabelVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Menu)] =
            new VisualTemplate((_, c) => new V3.MenuVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(MenuItem)] =
            new VisualTemplate((_, c) => new V3.MenuItemVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Window)] =
            new VisualTemplate((_, c) => new V3.WindowVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Splitter)] =
            new VisualTemplate((_, c) => new V3.SplitterVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Tooltip)] =
            new VisualTemplate((_, c) => new V3.TooltipVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(ItemsControl)] =
            new VisualTemplate((_, c) => new V3.ItemsControlVisual(tryCreateFormsObject: c));

        FrameworkElement.DefaultFormsTemplates[typeof(Gum.Forms.Controls.Games.DialogBox)] =
            new VisualTemplate((_, c) => new V3.DialogBoxVisual(tryCreateFormsObject: c));
    }
}
